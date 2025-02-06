using Ecommerce_platforms.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ecommerce_platforms.Core.IRepository
{
    public interface IOrder : IGenericRepository<Order>
    {
        Task<List<Order>> GetAllOrdersWithPendingDeliveryAsync();
        Task<Order> DeliverOrder(int orderId, int? deliveryMethodId); 
        Task<List<Order>> GetAllOrdersWithDeliveryAsync();
        Task<Order> GetOrderWithOrderItems(int orderId);
    }
}
