using Ecommerce_platforms.API.ModelsDTO;
using Ecommerce_platforms.Core.IRepository;
using Ecommerce_platforms.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce_platforms.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("OrderById")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await _unitOfWork.Order.GetOrderWithOrderItems(id);

            if (order == null)
            {
                return NotFound(new { message = $"Order with ID {id} not found." });
            }

            // ✅ Convert OrderStatus to a string
            string orderStatusString = order.OrderStatus.ToString();

            // ✅ Fetch delivery method & handle null case
            var deliveryMethod = order.DeliveryMethodId.HasValue
                ? await _unitOfWork.DeliveryMethod.GetDeliveryMethodAsync(order.DeliveryMethodId.Value)
                : null;

            // ✅ Create a DTO list for order items
            var orderItems = order.OrderItems?.Select(orderItem => new
            {
                ProductName = orderItem.ProductName,
                PictureUrl = $"https://localhost:7070{orderItem.PictureUrl}", // ✅ Fixed string interpolation
                Price = orderItem.Price,
                Quantity = orderItem.Quantity
            }).ToList();

            var result = new
            {
                OrderId = order.Id,
                UserEmail = order.UserEmail,
                OrderStatus = orderStatusString, // ✅ Now returns readable string (e.g., "Payed", "Cancelled")
                ShippingAddress = new
                {
                    Name = order.ShippingAddress?.Name,
                    Phone = order.ShippingAddress?.Phone,
                    City = order.ShippingAddress?.City,
                    Details = order.ShippingAddress?.Details
                },
                DeliveryMethodName = deliveryMethod?.ShortName ?? "N/A", // ✅ Prevents null exception
                DeliveryMethodTime = deliveryMethod?.DeliveryTime ?? "N/A", // ✅ Prevents null exception
                OrderItems = orderItems // ✅ Properly formatted order items
            };

            return Ok(result);
        }


        [HttpGet("AllOrders")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetAllOrders()
        {
            var orders = await _unitOfWork.Order.GetAllAsync();

            if (orders == null || !orders.Any())
            {
                return NotFound("No orders found.");
            }

            var orderDtos = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                UserEmail = o.UserEmail,
                OrderStatus = o.OrderStatus.ToString(),  // Convert Enum to String
                ShippingAddress = o.ShippingAddress,
                DeliveryMethodId = o.DeliveryMethodId,
                DeliveryMethod = o.DeliveryMethod,
                ClientSecret = o.ClientSecret
            }).ToList();

            return Ok(orderDtos);
        }

        [HttpGet("AllDeliverOrders")]
        public async Task<ActionResult<IEnumerable<Order>>> GetAllDeliverOrders()
        {
            return await _unitOfWork.Order.GetAllOrdersWithDeliveryAsync();
        }
        [HttpGet("GetNotDeliverOrder")]
        public async Task<ActionResult<IEnumerable<Order>>> GetAllOrdersWithPendingDeliveryAsync()
        {
            return await _unitOfWork.Order.GetAllOrdersWithPendingDeliveryAsync();
        }
        [HttpPost("deliverOrder")]
        public async Task<IActionResult> DeliverOrder(DeliverOrderDTO orderDTO)
        {
            var updatedOrder = await _unitOfWork.Order.DeliverOrder(orderDTO.OrderId, orderDTO.DeliveryMethodId);
            if (updatedOrder == null)
            {

                return NotFound($"Order with ID {orderDTO.OrderId} not found.");
            }

            return Ok(updatedOrder); // Return the updated order
        }

    }
}
