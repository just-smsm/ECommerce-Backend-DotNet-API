using System.ComponentModel.DataAnnotations;
namespace Ecommerce_platforms.API.ModelsDTO
{
    public class PayRequestDTO
    {
        [Required]
        [MinLength(4)]
        [MaxLength(50)]
        public string Name { get; set; }
        [Required]
        [MinLength(4)]
        [MaxLength(50)]
        public string Phone { get; set; }
     
        [Required]
        [MinLength(4)]
        [MaxLength(50)]
        public string City { get; set; }
        
        [Required]
        [MinLength(4)]
        [MaxLength(100)]
        public string Details { get; set; }
    }
}
