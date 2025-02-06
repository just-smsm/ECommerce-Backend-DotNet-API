using Ecommerce_platforms.Repository.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce_platforms.Repository.Auth
{
    public class Auth : IAuth
    {
        private readonly IConfiguration _configuration;

        public Auth(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<string> CreateToken(AppUser user, UserManager<AppUser> userManager)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (userManager == null) throw new ArgumentNullException(nameof(userManager));

            // ✅ Prepare a list of claims including "UserId"
            var authClaims = new List<Claim>
            {
                new Claim("UserId", user.Id),  // ✅ Use "UserId" instead of ClaimTypes.NameIdentifier
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            // ✅ Fetch user roles and add them to the claims list
            var userRoles = await userManager.GetRolesAsync(user);
            foreach (var role in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            // ✅ Generate the security key
            var authKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]));

            // ✅ Create the token descriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Audience = _configuration["JWT:ValidAudience"],
                Issuer = _configuration["JWT:ValidIssuer"],
                Expires = DateTime.UtcNow.AddDays(double.Parse(_configuration["JWT:DurationInDays"])),
                Subject = new ClaimsIdentity(authClaims),
                SigningCredentials = new SigningCredentials(authKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            // ✅ Return the serialized token (including "UserId")
            return tokenHandler.WriteToken(token);
        }
    }
}
