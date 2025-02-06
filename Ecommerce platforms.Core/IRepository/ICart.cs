using Ecommerce_platforms.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce_platforms.Core.IRepository
{
    public interface ICart:IGenericRepository<Cart>
    {
        public Task<Cart> CreateCart(string email, int productId);
        public Task<Cart> UpdateProductQuantity(string email, int productId,int quantity);
        public Task<Cart> DeleteCartProduct(string email, int productId);
        public Task<Cart> GetCartBYEmail(string email);
        public Task<Cart> ClearCart(string email);
        public Task<IEnumerable<CartItem>> GetCartItemsByEmail(string email);
        public Task UpdateCount(ICollection<CartItem> items);
    }
}
