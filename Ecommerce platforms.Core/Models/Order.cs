using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Ecommerce_platforms.Core.Models
{
    public class Order : ModelBase
    {
        public string? PaymentIntentId { get; set; }
        public String UserEmail { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public Address? ShippingAddress { get; set; }
        public int? DeliveryMethodId { get; set; }

        public string? ClientSecret { get; set; }
        public ICollection<OrderItems> OrderItems { get; set; }
       
        public DeliveryMethod DeliveryMethod { get; set; }

    }
}
