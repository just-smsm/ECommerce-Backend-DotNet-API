using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Ecommerce_platforms.API.ModelsDTO
{
    public class BrandDTO
    {
        public int Id { get; set; }

        [Required]
        [MinLength(4)]
        [MaxLength(50)]
        public string Name { get; set; }

        

        [Required(ErrorMessage = "Picture is required")]
        public IFormFile Picture { get; set; } // Used only for file upload
    }
}
