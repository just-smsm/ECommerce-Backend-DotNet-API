using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
namespace Ecommerce_platforms.Core.Models
{
    public class Cart : ModelBase
    {
        public string Email { get; set; } // User's email address for cart
        
        // Navigation property

        public decimal TotalPrice { get; set; }
        public ICollection<CartItem> Items { get; set; }

        public Cart()
        {
            Items = new List<CartItem>();
        }
    }
}
