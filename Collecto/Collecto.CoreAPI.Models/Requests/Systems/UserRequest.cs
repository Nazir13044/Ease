using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Collecto.CoreAPI.Models.Requests.Systems
{
    public class ByUserIdRequest
    {
        public int UserId { get; set; }
    }

    public class SendPasswordRequest
    {
        [Required, NotNull, StringLength(maximumLength: 300, MinimumLength = 1, ErrorMessage = "User Id is required.")]
        public string UserId { get; set; }
        public string MobileNo { get; set; }
        public string EmailAddress { get; set; }
    }

    public class UserUnlockRequest : ByUserIdRequest
    {
        [Required, NotNull, StringLength(maximumLength: 30, MinimumLength = 1, ErrorMessage = "Login Id must be between 1 and 30 characters.")]
        public string LoginId { get; set; }
    }

    public class ResetPasswordRequest : ByUserIdRequest
    {
        [Required, NotNull, StringLength(maximumLength: 30, MinimumLength = 1, ErrorMessage = "Password must be between 1 and 30 characters.")]
        public string Password { get; set; }

        [Required, NotNull, StringLength(maximumLength: 30, MinimumLength = 1, ErrorMessage = "Confirm Password must be between 1 and 30 characters.")]
        public string ConfirmPassword { get; set; }
    }

    public class PasswordChangeRequest : ResetPasswordRequest
    {
        [Required, NotNull, StringLength(maximumLength: 30, MinimumLength = 1, ErrorMessage = "Old Password must be between 1 and 30 characters.")]
        public string OldPassword { get; set; }
    }

    public class LogoutRequest
    {
        public int LogId { get; set; }
    }

    public class UserThemeRequest
    {
        [Required, NotNull, StringLength(15, MinimumLength = 1, ErrorMessage = "Menu Layout must be between 1 to 15 characters.")]
        public string MenuLayout { get; set; }

        [Required, NotNull, StringLength(15, MinimumLength = 1, ErrorMessage = "Theme Name must be between 1 to 15 characters.")]
        public string ThemeName { get; set; }

        [Required, NotNull, StringLength(10, MinimumLength = 1, ErrorMessage = "Scheme Name must be between 1 to 10 characters.")]
        public string SchemeName { get; set; }
    }

    public class ByFranchiseIdRequest
    {
        public int FranchiseId { get; set; }
    }

    public class UserRequestBase
    {
        [Required, NotNull, StringLength(30, MinimumLength = 3, ErrorMessage = "Login Id must be between 3 to 30 characters.")]
        public string LoginId { get; set; }

        [Required, NotNull, StringLength(75, MinimumLength = 3, ErrorMessage = "User Name must be between 3 to 100 characters.")]
        public string UserName { get; set; }

        [StringLength(15, MinimumLength = 11, ErrorMessage = "Mobile number must be 11 characters.")]
        [RegularExpression(@"^[01]{2}[123456789]{1}[0-9]{8}$", ErrorMessage = "Mobile number is invalid.")]
        public string MobileNo { get; set; }

        [Required, NotNull, StringLength(50, MinimumLength = 5, ErrorMessage = "Email address must be between 5 to 50 characters.")]
        [RegularExpression(@"^(([a-zA-Z0-9_\-\.]+)\@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)(\s*;\s*|\s*$))*$", ErrorMessage = "Email address is invalid.")]
        public string EmailAddress { get; set; }
        public bool AuthReqAtlogin { get; set; }
        public bool NeverExpire { get; set; }
        public bool DbOnStartup { get; set; }
        public bool DisallowMultiLogin { get; set; }
        public StatusEnum Status { get; set; }
        public AuthenticationMethodEnum AuthMethod { get; set; }
        public AccessStatusEnum AccessStatus { get; set; }
        public List<int> GroupIds { get; set; }
    }

    public class UserRequest : UserRequestBase
    {
        [Required, Range(minimum: 1, maximum: int.MaxValue, ConvertValueInInvariantCulture = true, ErrorMessage = "Select valid user")]
        public int UserId { get; set; }

        public string AuthKey { get; set; }
    }

    public class UserAttributesRequest
    {
        [Required, Range(minimum: 1, maximum: int.MaxValue, ConvertValueInInvariantCulture = true, ErrorMessage = "Select valid user")]
        public int UserId { get; set; }
        public short AtribSetupId { get; set; }
        public int SubsystemId { get; set; }
        public short LevelId { get; set; }
        public int HierarchyId { get; set; }
        public int SalespointId { get; set; }
        public List<int> SalespointIds { get; set; }
        public short EmployeeType { get; set; }
        public int EmployeeId { get; set; }
    }

    public class NewUserRequest : UserRequestBase
    {
        [Required, Range(minimum: 2, maximum: 5, ConvertValueInInvariantCulture = true, ErrorMessage = "User type is required (Value range: 2 to 5)")]
        public UserTypeEnum UserType { get; set; }

        [Required, NotNull]
        [StringLength(30, MinimumLength = 1, ErrorMessage = "User Name must be between 1 to 30 characters.")]
        public string Password { get; set; }
    }

    public class UserSearchRequest : ValueStatusAndPageAndSortSearchRequest
    {
        public bool CheckOwner { get; set; }
    }

    public class ForceUserLogoutRequest
    {
        public List<int> UserIds { get; set; }
    }
}
