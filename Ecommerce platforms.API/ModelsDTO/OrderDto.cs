using Ecommerce_platforms.Core.Models;

namespace Ecommerce_platforms.API.ModelsDTO
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string UserEmail { get; set; }
        public string OrderStatus { get; set; }  // This will hold the string value
        public Address? ShippingAddress { get; set; }
        public int? DeliveryMethodId { get; set; }
        public DeliveryMethod? DeliveryMethod { get; set; }
        public string? ClientSecret { get; set; }
    }

}
