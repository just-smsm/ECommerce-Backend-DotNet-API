using AutoMapper;
using Ecommerce_platforms.API.ModelsDTO;
using Ecommerce_platforms.Repository.Auth;
using Ecommerce_platforms.Repository.Data.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Ecommerce_platforms.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IAuth _auth;

        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IAuth auth, IMapper mapper)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _auth = auth;
            _mapper = mapper;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTo loginDTO)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(loginDTO.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDTO.Password))
                return Unauthorized("Invalid email or password.");

            await _signInManager.SignInAsync(user, isPersistent: false);
            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new LoginResponseDTO
            {
                Message = "User Login Successfully",
                Email = user.Email,
                DisplayName = user.FName + " " + user.LName,
                RoleName = roles.FirstOrDefault() ?? "No Role",
                Token = await _auth.CreateToken(user, _userManager)
            });
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDTO registerUserDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Invalid input",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            if (await _userManager.FindByEmailAsync(registerUserDTO.Email) != null)
            {
                return BadRequest(new { message = "Email is already registered." });
            }

            // ✅ Ensure ConfirmPassword is checked by ModelState (Handled by [Compare] attribute)
            var user = new AppUser
            {
                Email = registerUserDTO.Email,
                UserName = registerUserDTO.Email.Split("@")[0],
                FName = registerUserDTO.FirstName,
                LName = registerUserDTO.LastName
            };

            var createResult = await _userManager.CreateAsync(user, registerUserDTO.Password);
            if (!createResult.Succeeded)
            {
                return BadRequest(new
                {
                    message = "User creation failed",
                    errors = createResult.Errors.Select(e => e.Description)
                });
            }

            await _userManager.AddToRoleAsync(user, "User");

            var token = await _auth.CreateToken(user, _userManager);
            return Ok(new
            {
                message = "User registered successfully",
                displayName = $"{user.FName} {user.LName}",
                email = user.Email,
                token
            });
        }

        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRoleToUser([FromHeader(Name = "Authorization")] string authorizationHeader, [FromBody] AssignRoleDTO assignRoleDTO)
        {
            // Step 1: Extract token from the Authorization header
            if (string.IsNullOrWhiteSpace(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                return BadRequest("Authorization header is missing or invalid.");

            var token = authorizationHeader.Substring("Bearer ".Length).Trim();

            // Step 2: Decode the JWT token and get the email claim and role claims
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Step 3: Try to extract the email using different possible claim types
            var emailFromToken = jwtToken.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Email)?.Value
                                ?? jwtToken.Claims.FirstOrDefault(claim => claim.Type == "email")?.Value // Custom claim for email
                                ?? jwtToken.Claims.FirstOrDefault(claim => claim.Type == "sub")?.Value;  // Sometimes email is under 'sub'

            if (emailFromToken == null)
                return BadRequest("Email not found in token.");

            // Step 4: Extract the roles from the token (use "role" claim instead of ClaimTypes.Role)
            var rolesFromToken = jwtToken.Claims.Where(claim => claim.Type == "role").Select(c => c.Value).ToList();

            // Step 5: Check if the user has the "Admin" role
            if (!rolesFromToken.Contains("Admin"))
                return Unauthorized("Only Admins can assign roles.");

            // Step 6: Find the user by email from the DTO
            var user = await _userManager.FindByEmailAsync(assignRoleDTO.Email);
            if (user == null)
                return NotFound("User not found");

            // Step 7: Assign the role to the user
            var result = await _userManager.AddToRoleAsync(user, assignRoleDTO.RoleName);
            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));

            return Ok("Role assigned successfully");
        }

        
    }
}
