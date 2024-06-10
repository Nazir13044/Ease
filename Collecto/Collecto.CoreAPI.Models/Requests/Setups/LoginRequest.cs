using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Collecto.CoreAPI.Models.Requests.Setups
{
    public class FindAccountRequest
    {
        [Required, NotNull, StringLength(maximumLength: 200, MinimumLength = 4, ErrorMessage = "Login Id or Email address or Mobile number must be between 4 and 100 characters.")]
        public string AccountId { get; set; }
    }

    public class LoginRequest
    {
        [Required, NotNull, StringLength(maximumLength: 150, MinimumLength = 4, ErrorMessage = "Login Id must be between 4 and 30 characters.")]
        public string LoginId { get; set; }

        [Required, NotNull, StringLength(maximumLength: 150, MinimumLength = 1, ErrorMessage = "Password must be between 1 and 30 characters.")]
        public string Password { get; set; }

        public string AppId { get; set; }

        [Required, NotNull, StringLength(maximumLength: 8, MinimumLength = 5, ErrorMessage = "Version must be 5 and 8 digits (Example: 0.0.0 or 99.99.99)")]
        public string AppVersion { get; set; }
    }
}
