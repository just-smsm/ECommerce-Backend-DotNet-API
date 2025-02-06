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
    public class BrandController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BrandController> _logger;

        public BrandController(IUnitOfWork unitOfWork, IMapper mapper, ILogger<BrandController> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/brand
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetAllBrands>>> GetAllBrands()
        {
            try
            {
                var brands = await _unitOfWork.Brand.GetAllAsync();
                if (brands == null || !brands.Any())
                    return NotFound("No brands found.");

                var brandDTOs = brands.Select(brand => new GetAllBrands
                {
                    Id = brand.Id,
                    Name = brand.Name,
                    Picture = $"https://localhost:7070{brand.PictureUrl}"
                }).ToList();

                return Ok(brandDTOs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving brands.");
                return StatusCode(500, "An error occurred while retrieving brands.");
            }
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<GetAllBrands>> GetBrandById(int id)
        {
            try
            {
                var brand = await _unitOfWork.Brand.GetByIdAsync(id);
                if (brand == null)
                    return NotFound($"Brand with ID {id} not found.");

                var brandDTO = new GetAllBrands
                {
                    Id = brand.Id,
                    Name = brand.Name,
                    Picture = $"https://localhost:7070{brand.PictureUrl}"
                };

                return Ok(brandDTO);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving brand with ID {id}.");
                return StatusCode(500, "An error occurred while retrieving the brand.");
            }
        }
        [HttpGet("AllProductsOnspecificBrandBYBrandId")]
        public async Task<ActionResult<ICollection<AllProductsOnspecificBrand>>> GetProductsOnspecificBrands(int brandID)
        {
            var products = await _unitOfWork.Product.productsOnspecificBrands(brandID);

            if (products == null || !products.Any())
            {
                return NotFound("No products found for the specified brand.");
            }

            var allProducts = products.Select(product => new AllProductsOnspecificBrand
            {
                ProductId = product.Id,
                Name = product.Name,
                Price = product.Price,
                PictureUrl = $"https://localhost:7070{product.PictureUrl}",
                Description = product.Description,
                Count = product.Count,
                IsAvailable = product.Count>0 
                
                
            }).ToList();

            return Ok(allProducts);
        
        }

        // POST: api/brand
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddBrandAsync([FromForm] BrandDTO brandDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                string filePath = await SaveImageAsync(brandDTO.Picture);

                var brand = new Brand
                {
                    Name = brandDTO.Name,
                    PictureUrl = filePath
                };

                var result = await _unitOfWork.Brand.AddAsync(brand);
                if (result == null) return BadRequest("Error while creating brand.");

                return CreatedAtAction(nameof(GetBrandById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating brand.");
                return StatusCode(500, "An error occurred while creating the brand.");
            }
        }

        // PUT: api/brand/{id}
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateBrand(int id, [FromForm] BrandDTO brandDTO)
        {
            try
            {
                var existingBrand = await _unitOfWork.Brand.GetByIdAsync(id);
                if (existingBrand == null)
                    return NotFound($"Brand with ID {id} not found.");

                if (brandDTO.Picture != null)
                {
                    string filePath = await SaveImageAsync(brandDTO.Picture);
                    existingBrand.PictureUrl = filePath;
                }

                existingBrand.Name = brandDTO.Name;

                var result = await _unitOfWork.Brand.UpdateAsync(existingBrand);
                if (result == null) return BadRequest("Error while updating brand.");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating brand with ID {id}.");
                return StatusCode(500, "An error occurred while updating the brand.");
            }
        }

        // DELETE: api/brand/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            try
            {
                var existingBrand = await _unitOfWork.Brand.GetByIdAsync(id);
                if (existingBrand == null)
                    return NotFound($"Brand with ID {id} not found.");

                await _unitOfWork.Brand.DeleteAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting brand with ID {id}.");
                return StatusCode(500, "An error occurred while deleting the brand.");
            }
        }

        // POST: api/brand/{brandId}/add-products
       
        // Helper method to save image
        private async Task<string> SaveImageAsync(IFormFile picture)
        {
            if (picture == null) return null;

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/Brands");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(picture.FileName);
            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await picture.CopyToAsync(stream);
            }

            return "/images/Brands/" + fileName; // Return relative path
        }
    }
}
