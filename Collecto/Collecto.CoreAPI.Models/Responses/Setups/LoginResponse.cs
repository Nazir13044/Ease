namespace Collecto.CoreAPI.Models.Responses.Setups
{
    public class FindAccountResponse : BooleanResponse
    {
        public int UserId { get; set; }
        public string PhoneNo { get; set; }
        public string EmailAddress { get; set; }
        public string PhoneNoMasked { get; set; }
        public string EmailAddressMasked { get; set; }
    }

    public class LoginResponse : ResponseBase1
    {
        public int Id { get; set; }
        public LoginStatusEnum LoginStatus { get; set; }
        public string LoginId { get; set; }
        public string UserName { get; set; }
        public bool ValidUser { get; set; }
        public int? SubsystemId { get; set; }
        public int? SalespointId { get; set; }
        public bool AuthRequiredAtlogin { get; set; }
        public AuthenticationMethodEnum AuthMethod { get; set; }
        public string AuthenticationToken { get; set; }
        public bool PwdChangeRequired { get; set; }
        public DateTime? Expires { get; set; }
        public string LoginTime { get; set; }
        public int LogId { get; set; }
        public string MenuLayout { get; set; }
        public string ThemeName { get; set; }
        public string SchemeName { get; set; }
        public UserTypeEnum UserType { get; set; }
        public bool BatchEnabled { get; set; }
        public bool DbOnStartup { get; set; }
        public int IdleTime { get; set; }
        public int PingTime { get; set; }
        public int TimeoutTime { get; set; }
        public List<string> ModuleIds { get; set; }
    }
}
