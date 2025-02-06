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
    public class DeliveryMethodRepository : GenericRepository<DeliveryMethod>, IDeliveryMethod
    {
        public DeliveryMethodRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<DeliveryMethod> GetDeliveryMethodAsync(int deliveryMethodId)
        {
            return await _context.DeliveryMethods
                .FirstOrDefaultAsync(d => d.Id == deliveryMethodId);
        }


    }
}
