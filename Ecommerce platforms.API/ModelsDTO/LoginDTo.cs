using System.ComponentModel.DataAnnotations;

namespace Ecommerce_platforms.API.ModelsDTO
{
    public class LoginDTo
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_])[A-Za-z\d\W_]{8,}$",
            ErrorMessage = "Password must be at least 8 characters long and include at least one uppercase letter, one lowercase letter, one number, and one special character.")]

        public string Password { get; set; }
    }
}
