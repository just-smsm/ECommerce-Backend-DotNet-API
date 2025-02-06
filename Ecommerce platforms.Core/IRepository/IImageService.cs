using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
namespace Ecommerce_platforms.Core.IRepository
{
    public interface IImageService
    {
        Task<string> SaveImageAsync(IFormFile file); // Save image and return URL
        Task<bool> DeleteImageAsync(string imageUrl);
        
    }
}
