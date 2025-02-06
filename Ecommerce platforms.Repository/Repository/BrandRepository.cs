
using Ecommerce_platforms.Core.IRepository;
using Ecommerce_platforms.Core.Models;
using Ecommerce_platforms.Repository.Data;
using ElSory.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce_platforms.Repository.Repository
{
    public class BrandRepository : GenericRepository<Brand>, IBrand
    {
        public BrandRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<string> GetBrandNameByBrandID(int brandID)
        {
            var brand = await _context.Brands.FirstOrDefaultAsync(b=>b.Id== brandID);
            return brand.Name;
        }

       

        // Implement additional methods if any
    }
}
