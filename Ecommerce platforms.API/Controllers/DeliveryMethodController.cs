using Ecommerce_platforms.Core.IRepository;
using Ecommerce_platforms.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce_platforms.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeliveryMethodController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        public DeliveryMethodController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllDeliveryMethodAsync()
        {
            var deliveryMethods = await _unitOfWork.DeliveryMethod.GetAllAsync();
            if (deliveryMethods == null || !deliveryMethods.Any())
            {
                return NotFound("No delivery methods found.");
            }
            var result = new
            {
                DeliveryMethods = deliveryMethods
            };
            return Ok(result);
        }
    }
}
