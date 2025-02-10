using AutoMapper;
using Ecommerce_platforms.API.ModelsDTO;
using Ecommerce_platforms.Core.IRepository;
using Ecommerce_platforms.Core.Models;
using Ecommerce_platforms.Repository.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Stripe.Checkout;
using System.Collections.Generic;
using System;
using Address = Ecommerce_platforms.Core.Models.Address;

namespace Ecommerce_platforms.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CartController> _logger;
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;

        public CartController(IUnitOfWork unitOfWork, UserManager<AppUser> userManager, ILogger<CartController> logger, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart([FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            if (string.IsNullOrEmpty(authorizationHeader))
                return Unauthorized("Authorization header is missing.");

            var email = GetEmailFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(email))
                return Unauthorized("Invalid token or user not found.");

            var cart = await _unitOfWork.Cart.GetCartBYEmail(email);
            if (cart == null)
                return NotFound("Cart not found.");

            var cartItems = cart.Items?.Select(item => new CartItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Price = item.Price,
                PictureUrl = $"https://localhost:7070{item.PictureUrl}",
                Quantity = item.Quantity
            }).ToList() ?? new List<CartItem>();

            return Ok(new
            {
                items = cartItems,
                totalPrice = cartItems.Sum(i => i.Price * i.Quantity)
            });
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart([FromHeader(Name = "Authorization")] string authorizationHeader, [FromBody] int productId)
        {
            var email = GetEmailFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(email))
                return Unauthorized("Invalid token or user not found.");

            var cart = await _unitOfWork.Cart.CreateCart(email, productId);
            if (cart == null)
                return BadRequest("Error while adding product to cart.");

            return Ok(cart);
        }

        [HttpDelete("{productId}")]
        public async Task<IActionResult> RemoveFromCart([FromHeader(Name = "Authorization")] string authorizationHeader, [FromRoute] int productId)
        {
            var email = GetEmailFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(email))
                return Unauthorized("Invalid token or user not found.");

            var cart = await _unitOfWork.Cart.DeleteCartProduct(email, productId);
            return cart == null ? BadRequest("Error while deleting product.") : Ok(cart);
        }
        [HttpPut]
        public async Task<IActionResult> UpdateProductQuantity([FromHeader(Name = "Authorization")] string authorizationHeader, [FromBody] UpdateQuantityDTo updateQuantityDto)
        {
            var email = GetEmailFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(email))
                return Unauthorized("Invalid token or user email not found.");

            var cart = await _unitOfWork.Cart.UpdateProductQuantity(email, updateQuantityDto.ProductId, updateQuantityDto.Quantity);
            if (cart == null)
                return BadRequest("Error while updating product quantity.");

            return Ok(new CartDto
            {
                Email = cart.Email,
                TotalPrice = cart.TotalPrice,
                Items = cart.Items.Select(ci => new CartItemDto
                {
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    Price = ci.Price
                }).ToList()
            });
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCart([FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            var email = GetEmailFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(email))
                return Unauthorized("Invalid token or user email not found.");

            var cart = await _unitOfWork.Cart.GetCartBYEmail(email);
            if (cart == null)
                return NotFound("Cart not found.");

            cart.Items.Clear();
            cart.TotalPrice = 0;

            if (!await SaveChangesAsync(email, "clearing cart"))
                return StatusCode(500, "An error occurred while saving changes.");

            return Ok(new { totalPrice = cart.TotalPrice });
        }

        [HttpPost("pay")]
        public async Task<IActionResult> Pay([FromHeader(Name = "Authorization")] string authorizationHeader, [FromBody] PayRequestDTO payRequest)
        {
            var email = GetEmailFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(email))
                return Unauthorized("Invalid token or user email not found.");

            var cart = await _unitOfWork.Cart.GetCartBYEmail(email);
            if (cart == null || !cart.Items.Any())
                return NotFound("Cart is empty or not found.");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return NotFound("User not found.");

            // Create the order before initiating the Stripe session
            var order = CreateOrder(cart, user, payRequest);
            await _unitOfWork.Order.AddAsync(order);
            await _unitOfWork.Complete(); // Save order before Stripe session

            // Stripe checkout session options
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = cart.Items.Select(item => new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.ProductName,
                        },
                        UnitAmount = (long)(item.Price * 100),
                    },
                    Quantity = item.Quantity,
                }).ToList(),
                Mode = "payment",
                SuccessUrl = $"http://localhost:4200/success?items={Uri.EscapeDataString(string.Join(",", cart.Items.Select(i => $"{i.ProductId}-{i.ProductName}-{i.Quantity}-{i.Price}")))}",
                CancelUrl = $"{Request.Scheme}://{Request.Host}/checkout/cancel",
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            // Fetch session details again to get the PaymentIntentId
            var updatedSession = await service.GetAsync(session.Id);
            order.PaymentIntentId = updatedSession.PaymentIntentId;
            order.ClientSecret = session.Id;

            _logger.LogInformation($"Stripe session created: Session ID: {session.Id}, PaymentIntentId: {order.PaymentIntentId}");

            await _unitOfWork.Complete();
            await _unitOfWork.Cart.UpdateCount(cart.Items);
            // Clear cart after successful order creation
            cart.Items.Clear();
            cart.TotalPrice = 0;

            await _unitOfWork.Complete();

            return Ok(new { sessionUrl = session.Url });
        }

        private Order CreateOrder(Cart cart, AppUser user, PayRequestDTO payRequest)
        {
            return new Order
            {
                UserEmail = user.Email,
                OrderItems = cart.Items.Select(cartItem => new OrderItems
                {
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.ProductName,
                    Price = cartItem.Price,
                    Quantity = cartItem.Quantity,
                    PictureUrl = cartItem.PictureUrl,
                    SubTotal = cartItem.Price * cartItem.Quantity
                }).ToList(),
                OrderStatus = OrderStatus.Payed,
                ShippingAddress = new Address
                {
                    Name = payRequest.Name,
                    Phone = payRequest.Phone,
                    City = payRequest.City,
                    Details = payRequest.Details
                }
            };
        }

        private string GetEmailFromToken(string authorizationHeader)
        {
            var token = GetTokenFromHeader(authorizationHeader);
            if (string.IsNullOrEmpty(token))
                return null;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                return jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email")?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing token");
                return null;
            }
        }

        private string GetTokenFromHeader(string authorizationHeader)
        {
            return string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ")
                ? null
                : authorizationHeader.Substring("Bearer ".Length).Trim();
        }

        private async Task<bool> SaveChangesAsync(string email, string action)
        {
            try
            {
                await _unitOfWork.Complete();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes for user: {email} during {action}", email, action);
                return false;
            }
        }
    }
}
