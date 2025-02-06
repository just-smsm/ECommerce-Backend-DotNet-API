
using Ecommerce_platforms.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce_platforms.Core.IRepository
{
    public interface IBrand:IGenericRepository<Brand>
    {
        public Task<string> GetBrandNameByBrandID(int brandID);
       
    }
}
