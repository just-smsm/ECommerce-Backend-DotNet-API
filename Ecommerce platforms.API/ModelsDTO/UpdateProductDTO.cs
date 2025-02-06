using System.ComponentModel.DataAnnotations;

namespace Ecommerce_platforms.API.ModelsDTO
{
    public class UpdateProductDTO
    {
        
        [Required]
        [MinLength(4)]
        [MaxLength(50)]
        public string Name { get; set; }

        public IFormFile? PictureUrl { get; set; } // ✅ Now Optional for Updates

        [Required]
        [MinLength(4)]
        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        public int Price { get; set; }

        [Required]
        public int Count { get; set; }

        public int BrandId { get; set; }
    }
}
