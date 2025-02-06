using Ecommerce_platforms.Core.IRepository;
using Ecommerce_platforms.Core.Models;
using Ecommerce_platforms.Repository.Data;
using ElSory.Repository.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Ecommerce_platforms.Repository.Repository
{
    public class CartRepository : GenericRepository<Cart>, ICart
    {
        public CartRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Cart> ClearCart(string email)
        {
            try
            {
                var cart = await GetCartBYEmail(email);
                if (cart == null)
                {
                    return null; // or throw an exception if you prefer
                }

                cart.Items.Clear();
                await _context.SaveChangesAsync();
                return cart;
            }
            catch (Exception ex)
            {
                // Handle or log the exception
                throw new Exception("Error clearing cart", ex);
            }
        }

        public async Task<Cart> CreateCart(string email, int productId)
        {
            try
            {
                var cart = await _context.Carts
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.Email == email);

                if (cart == null)
                {
                    cart = new Cart { Email = email };
                    await _context.Carts.AddAsync(cart);
                }

                var product = await _context.Products.FirstOrDefaultAsync(i => i.Id == productId);
                if (product == null)
                {
                    // Handle case where product doesn't exist
                    return null; // or throw an exception if you prefer
                }

                if (cart.Items == null)
                {
                    cart.Items = new List<CartItem>();
                }
               
                var existingCartItem = cart.Items.FirstOrDefault(item => item.ProductId == productId);

                if (existingCartItem != null)
                {
                    existingCartItem.Quantity++;
                }
                else
                {
                    var Cartitem = new CartItem(productId, product.Name,1, product.Price, product.PictureUrl);
                    
                    cart.Items.Add(Cartitem);
                }
                cart.TotalPrice=cart.Items.Sum(i=>i.Price*i.Quantity);
                await _context.SaveChangesAsync();
                return cart;
            }
            catch (Exception ex)
            {
                // Handle or log the exception
                throw new Exception("Error creating cart", ex);
            }
        }

        public async Task<Cart> DeleteCartProduct(string email, int productId)
        {
            try
            {
                var cart = await GetCartBYEmail(email);
                if (cart == null)
                {
                    return null; // or throw an exception if you prefer
                }

                var itemToRemove = cart.Items.FirstOrDefault(item => item.ProductId == productId);
                cart.TotalPrice = cart.Items.Sum(i => i.Price * i.Quantity);
                if (itemToRemove != null)
                {
                    
                    cart.Items.Remove(itemToRemove);
                    cart.TotalPrice = cart.Items.Sum(i => i.Price * i.Quantity);
                    await _context.SaveChangesAsync();
                }

                return cart;
            }
            catch (Exception ex)
            {
                // Handle or log the exception
                throw new Exception("Error deleting cart product", ex);
            }
        }

        public async Task<Cart> GetCartBYEmail(string email)
        {
            try
            {
                return await _context.Carts
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.Email == email);
            }
            catch (Exception ex)
            {
                // Handle or log the exception
                throw new Exception("Error retrieving cart by email", ex);
            }
        }
        public async Task UpdateCount(ICollection<CartItem> items)
        {
            foreach (var item in items)
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
                if (product != null)
                {
                    product.Count -= item.Quantity;
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<CartItem>> GetCartItemsByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return new List<CartItem>(); // Always return an empty list if email is invalid

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Email == email);

            if (cart?.Items == null || !cart.Items.Any())
                return new List<CartItem>(); // Return an empty list if cart or items are null

            return cart.Items.Select(item => new CartItem
            {
                PictureUrl = $"https://localhost:7070{item.PictureUrl}",
                Price = item.Price,
                Quantity = item.Quantity,
                ProductId = item.ProductId,
                ProductName = item.ProductName
            }).ToList();
        }
        public async Task<Cart> UpdateProductQuantity(string email, int productId, int quantity)
        {
            var cart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Email == email);
            if (cart != null)
            {
                var productOnCartItem = cart.Items.FirstOrDefault(ci => ci.ProductId == productId);
                if (productOnCartItem != null)
                {
                    productOnCartItem.Quantity += quantity;
                    cart.TotalPrice = cart.Items.Sum(t => t.Quantity * t.Price);
                    await _context.SaveChangesAsync();
                }
            }
            return cart;
        }

    }
}
