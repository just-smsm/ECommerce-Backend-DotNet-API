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
    public class OrderRepository : GenericRepository<Order>, IOrder
    {
        public OrderRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Order> DeliverOrder(int orderId, int? deliveryMethodId)
        {
            var existingOrder = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (existingOrder != null)
            {
                existingOrder.OrderStatus = OrderStatus.Delivered;

                // Update DeliveryMethodId only if a valid one is provided
                if (deliveryMethodId.HasValue)
                {
                    existingOrder.DeliveryMethodId = deliveryMethodId.Value;
                }

                await _context.SaveChangesAsync();
                return existingOrder;
            }

            return null; // Return null if the order is not found
        }

        public Task<List<Order>> GetAllOrdersWithPendingDeliveryAsync()
        {
            return _context.Orders
                           .Where(o => o.OrderStatus != OrderStatus.Delivered)
                           .ToListAsync();
        }

        public Task<List<Order>> GetAllOrdersWithDeliveryAsync()
        {
            return _context.Orders
                           .Where(o => o.OrderStatus == OrderStatus.Delivered)
                           .ToListAsync();
        }

        public async Task<Order> GetOrderWithOrderItems(int orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems) // ✅ Include related items
                .Include(o => o.ShippingAddress) // ✅ Ensure shipping address is loaded
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

    }
}
