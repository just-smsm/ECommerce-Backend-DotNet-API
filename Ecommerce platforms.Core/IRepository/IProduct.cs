using Ecommerce_platforms.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce_platforms.Core.IRepository
{
    public interface IProduct:IGenericRepository<Product>
    {

        public  Task<IEnumerable<Product>> GetAllProductsWithPictures();
        public Task<Product> GetAllProductWithPictures(int id);
        public Task<ICollection<Product>> productsOnspecificBrands(int brandID);
    }
}
