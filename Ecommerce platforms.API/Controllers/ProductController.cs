using AutoMapper;
using Ecommerce_platforms.API.ModelsDTO;
using Ecommerce_platforms.Core.IRepository;
using Ecommerce_platforms.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce_platforms.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ProductController> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        // ✅ Get all products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDTO>>> GetAllProducts()
        {
            try
            {
                var products = await _unitOfWork.Product.GetAllProductsWithPictures();
                if (!products.Any())
                    return NotFound("No products found.");

                
               var productDTOs = new List<ProductDTO>();
                foreach (var product in products)
                {
                    var brandName = await _unitOfWork.Brand.GetBrandNameByBrandID(product.BrandId);
                    productDTOs.Add(new ProductDTO
                    {
                        Id = product.Id,
                        Name = product.Name,
                        Description = product.Description,
                        Price = product.Price,
                        Count = product.Count,
                        PictureUrl = !string.IsNullOrEmpty(product.PictureUrl) ? $"https://localhost:7070{product.PictureUrl}" : null,
                        BrandName = brandName ?? "Unknown Brand",
                        IsAvailable = product.Count > 0
                    });
                }


                return Ok(productDTOs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products.");
                return StatusCode(500, "An error occurred while retrieving products.");
            }
        }

        // ✅ Get a single product by ID
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDTO>> GetProduct(int id)
        {
            try
            {
                var product = await _unitOfWork.Product.GetAllProductWithPictures(id);
                if (product == null)
                    return NotFound($"Product with ID {id} not found.");

                var productDTO = new ProductDTO
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Count = product.Count,
                    PictureUrl = !string.IsNullOrEmpty(product.PictureUrl) ? $"https://localhost:7070{product.PictureUrl}" : null,
                    BrandName = await _unitOfWork.Brand.GetBrandNameByBrandID(product.BrandId),
                    
                    IsAvailable = product.Count > 0
                };

                return Ok(productDTO);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving product with ID {id}.");
                return StatusCode(500, "An error occurred while retrieving the product.");
            }
        }

        // ✅ Add a new product with an image
        [Consumes("multipart/form-data")]
        [HttpPost("CreateNew")]
        public async Task<IActionResult> CreateNew([FromForm] CreateNewProductDTO newProduct)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("Invalid ModelState:");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine(error.ErrorMessage);
                    }
                    return BadRequest(ModelState);
                }

                string filePath = null;
                if (newProduct.PictureUrl != null)
                {
                    filePath = await SaveImageAsync(newProduct.PictureUrl);
                }

                var product = new Product
                {
                    Name = newProduct.Name,
                    Description = newProduct.Description,
                    Price = newProduct.Price,
                    Count = newProduct.Count,
                    BrandId = newProduct.BrandId,
                    PictureUrl = filePath
                };

                var createdProduct = await _unitOfWork.Product.AddAsync(product);
                if (createdProduct == null)
                    return BadRequest("Product could not be created.");

                return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.Id }, _mapper.Map<ProductDTO>(createdProduct));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product.");
                return StatusCode(500, "An error occurred while creating the product.");
            }
        }

        // ✅ Update an existing product
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ProductDTO>> UpdateProduct(int id, [FromForm] UpdateProductDTO updatedProduct)
        {
            try
            {
                var existingProduct = await _unitOfWork.Product.GetByIdAsync(id);
                if (existingProduct == null)
                    return NotFound($"Product with ID {id} not found.");

                // ✅ Only update the picture if a new file is uploaded
                if (updatedProduct.PictureUrl != null)
                {
                    if (!string.IsNullOrEmpty(existingProduct.PictureUrl))
                    {
                        DeleteImage(existingProduct.PictureUrl);
                    }

                    string filePath = await SaveImageAsync(updatedProduct.PictureUrl);
                    existingProduct.PictureUrl = filePath;
                }

                // ✅ Update other fields
                existingProduct.Name = updatedProduct.Name;
                existingProduct.Description = updatedProduct.Description;
                existingProduct.Price = updatedProduct.Price;
                existingProduct.Count = updatedProduct.Count;
                existingProduct.BrandId = updatedProduct.BrandId;

                var result = await _unitOfWork.Product.UpdateAsync(existingProduct);
                if (result == null)
                    return BadRequest("Error while updating product.");

                return Ok(_mapper.Map<ProductDTO>(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating product with ID {id}.");
                return StatusCode(500, "An error occurred while updating the product.");
            }
        }

        // ✅ Delete a product
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _unitOfWork.Product.GetByIdAsync(id);
                if (product == null)
                    return NotFound($"Product with ID {id} not found.");

                if (!string.IsNullOrEmpty(product.PictureUrl))
                {
                    DeleteImage(product.PictureUrl);
                }

                await _unitOfWork.Product.DeleteAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting product with ID {id}.");
                return StatusCode(500, "An error occurred while deleting the product.");
            }
        }

        // ✅ Helper method to save images
        private async Task<string> SaveImageAsync(IFormFile picture)
        {
            var uploadsFolder = Path.Combine("wwwroot/images/Products");
            Directory.CreateDirectory(uploadsFolder);

            string fileName = $"{Guid.NewGuid()}{Path.GetExtension(picture.FileName)}";
            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await picture.CopyToAsync(stream);
            }

            return $"/images/Products/{fileName}";
        }

        private void DeleteImage(string filePath)
        {
            string fullPath = Path.Combine("wwwroot", filePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}
