using Ecommerce_platforms.API.ModelsDTO;
using Ecommerce_platforms.Core.IRepository;
using Ecommerce_platforms.Repository.Data.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Ecommerce_platforms.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IImageService _imageService;
        private readonly SignInManager<AppUser> _signInManager;

        public ProfileController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IImageService imageService)
        {
            _userManager = userManager;
            _imageService = imageService;
            _signInManager = signInManager;
        }

        // ✅ Get User Profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile([FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            var user = await GetAuthenticatedUserAsync(authorizationHeader);
            if (user == null)
                return Unauthorized(new { Message = "User not found or not authenticated." });

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
            string profileImageUrl = string.IsNullOrEmpty(user.ProfileImageUrl) ? null : $"https://localhost:7070/{user.ProfileImageUrl}";

            return Ok(new
            {
                user.FName,
                user.LName,
                user.Email,
                user.PhoneNumber,
                ProfileImageUrl = profileImageUrl ?? "null",
                RoleName = role
            });
        }

        // ✅ Change Password
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] ChangePasswordDTO changePasswordDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = "Invalid password format." });

            var user = await GetAuthenticatedUserAsync(authorizationHeader);
            if (user == null)
                return Unauthorized(new { Message = "User not found or not authenticated." });

            var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
            return result.Succeeded ? Ok(new { Message = "Password changed successfully." }) : BadRequest(result.Errors);
        }

        // ✅ Reset Password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO resetPasswordDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            var result = await _userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.NewPassword);
            return result.Succeeded ? Ok(new { Message = "Password reset successfully." }) : BadRequest(result.Errors);
        }

        // ✅ Upload Profile Image
        [HttpPost("upload-profile-image")]
        public async Task<IActionResult> UploadProfileImage(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "No file provided." });

            if (!IsValidImage(file))
                return BadRequest(new { Message = "Invalid file type or size. Only JPG, JPEG, PNG, and GIF are allowed (Max: 5MB)." });

            var user = await GetAuthenticatedUserAsync(authorizationHeader);
            if (user == null)
                return Unauthorized(new { Message = "User not found or not authenticated." });

            try
            {
                if (!string.IsNullOrEmpty(user.ProfileImageUrl))
                    await _imageService.DeleteImageAsync(user.ProfileImageUrl);

                user.ProfileImageUrl = await _imageService.SaveImageAsync(file);
                var result = await _userManager.UpdateAsync(user);

                return result.Succeeded ? Ok(new { Message = "Image uploaded successfully." }) : BadRequest(result.Errors);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Failed to upload image. Please try again." });
            }
        }

        [HttpDelete("delete-profile-image")]
        public async Task<IActionResult> DeleteProfileImage([FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            var user = await GetAuthenticatedUserAsync(authorizationHeader);
            if (user == null)
                return Unauthorized(new { Message = "User not found or not authenticated." });

            if (string.IsNullOrEmpty(user.ProfileImageUrl))
                return BadRequest(new { Message = "No profile image found to delete." });

            try
            {
                Console.WriteLine($"Attempting to delete image: {user.ProfileImageUrl}");

                var isDeleted = await _imageService.DeleteImageAsync(user.ProfileImageUrl);
                if (!isDeleted)
                {
                    Console.WriteLine("File deletion failed! File might not exist or has permission issues.");
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Failed to delete image from storage." });
                }

                user.ProfileImageUrl = null;
                var result = await _userManager.UpdateAsync(user);

                return result.Succeeded ? Ok(new { Message = "Profile image deleted successfully." }) : BadRequest(result.Errors);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteProfileImage: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred while deleting the image." });
            }
        }

        // ✅ Update Profile
        [HttpPost("update-profile")]
        public async Task<IActionResult> UpdateProfile(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] UpdateProfileDTO updateProfileDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await GetAuthenticatedUserAsync(authorizationHeader);
            if (user == null)
                return Unauthorized(new { Message = "User not found or not authenticated." });

            var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, updateProfileDto.Password, false);
            if (!passwordCheck.Succeeded)
                return Unauthorized(new { Message = "Invalid password." });

            user.FName = updateProfileDto.FName;
            user.LName = updateProfileDto.LName;
            user.PhoneNumber = updateProfileDto.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded ? Ok(new { Message = "Profile updated successfully." }) : BadRequest(result.Errors);
        }

        // ✅ Helper Methods
        private async Task<AppUser> GetAuthenticatedUserAsync(string authorizationHeader)
        {
            var userId = GetUserIdFromToken(authorizationHeader);
            return string.IsNullOrEmpty(userId) ? null : await _userManager.FindByIdAsync(userId);
        }

        private string GetUserIdFromToken(string authorizationHeader)
        {
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                return null;

            var token = authorizationHeader.Substring("Bearer ".Length);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

            return jwtToken?.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
        }

        private bool IsValidImage(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = System.IO.Path.GetExtension(file.FileName).ToLower();

            return allowedExtensions.Contains(fileExtension) && file.Length <= 5 * 1024 * 1024;
        }
    }
}
