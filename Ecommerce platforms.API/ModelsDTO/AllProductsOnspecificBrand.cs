using System.ComponentModel.DataAnnotations.Schema;

namespace Ecommerce_platforms.API.ModelsDTO
{
    public class AllProductsOnspecificBrand
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string PictureUrl { get; set; }
        public string Description { get; set; }
        public int Price { get; set; }
        public int Count { get; set; }
        public bool IsAvailable { get; set; }
    }
}
