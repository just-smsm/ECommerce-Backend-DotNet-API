
using Ecommerce_platforms.Core.IRepository;
using Ecommerce_platforms.Core.Models;
using Ecommerce_platforms.Repository.Data;
using ElSory.Repository.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce_platforms.Repository.Repository
{
    public class ProductRepository : GenericRepository<Product>,IProduct
    {
        public ProductRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Product>> GetAllProductsWithPictures()
        {
            return await _context.Products.Include(p=>p.Brand).ToListAsync();
        }

        public async Task<Product> GetAllProductWithPictures(int id)
        {
            return await _context.Products.Include(p=>p.Brand).FirstOrDefaultAsync(i=>i.Id==id);
        }

        public async Task<ICollection<Product>> productsOnspecificBrands(int brandID)
        {
            return await _context.Products.Where(p=>p.BrandId==brandID).ToListAsync();
        }
    }
}
