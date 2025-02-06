using Ecommerce_platforms.Core.IRepository;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Ecommerce_platforms.Repository.Repository
{
    public class ImageService : IImageService
    {
        private readonly string _imageFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Profiles");

        public ImageService()
        {
            // Ensure the directory exists
            if (!Directory.Exists(_imageFolderPath))
                Directory.CreateDirectory(_imageFolderPath);
        }

        public async Task<string> SaveImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file");

            // Generate a unique file name to avoid conflicts
            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(_imageFolderPath, uniqueFileName);

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return the relative path of the uploaded image
            return $"Profiles/{uniqueFileName}";
        }

        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                Console.WriteLine("DeleteImageAsync: imageUrl is empty or null.");
                return false;
            }

            try
            {
                var fileName = Path.GetFileName(imageUrl); // Extract only the file name
                var filePath = Path.Combine(_imageFolderPath, fileName);

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"DeleteImageAsync: File not found - {filePath}");
                    return false; // Return false instead of failing
                }

                File.Delete(filePath);
                Console.WriteLine($"DeleteImageAsync: File deleted successfully - {filePath}");
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"DeleteImageAsync: Unauthorized Access - {ex.Message}");
                return false;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"DeleteImageAsync: IO Exception - {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DeleteImageAsync: Unexpected error - {ex.Message}");
                return false;
            }
        }

    }
}
