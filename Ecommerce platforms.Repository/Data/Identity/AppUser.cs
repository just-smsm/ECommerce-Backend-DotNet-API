using Microsoft.AspNetCore.Identity;

namespace Ecommerce_platforms.Repository.Data.Identity
{
    public class AppUser : IdentityUser
    {
        public string FName { get; set; }
        public string LName { get; set; }
        
        public string? ProfileImageUrl { get; set; }
    }
}
