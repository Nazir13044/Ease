using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Collecto.CoreAPI.Models.Requests.Setups
{
    public class OtpValidationRequest
    {
        public int UserId { get; set; }
        public AuthenticationMethodEnum AuthMethod { get; set; }
        [Required, NotNull, StringLength(6, MinimumLength = 6, ErrorMessage = "Otp must be 6 digit number.")]
        public string OtpCode { get; set; }
    }
}
