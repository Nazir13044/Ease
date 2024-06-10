namespace Collecto.CoreAPI.Models.Objects.Systems
{
    public class User
    {
        public const int SuperUser_Id = -9;
        public const string SuperUser_LoginId = "superuser";

        public int Id { get; set; }
        public string LoginId { get; set; }
        public int LogId { get; set; }
        public string UserName { get; set; }
        public UserTypeEnum UserType { get; set; }
        public StatusEnum Status { get; set; }
        public AccessStatusEnum AccessStatus { get; set; }
        public bool NeverExpires { get; set; }
        public int? SystemId { get; set; }
        public int? SalespointId { get; set; }
        public int? SubsystemId { get; set; }
        public string LastPasswords { get; set; }
        public DateTime? LastPassChgDate { get; set; }
        public DateTime? ExpireDate { get; set; }
        public DateTime? NextLoginTime { get; set; }
        public DateTime SystemDate { get; set; }
        public bool DbOnStartup { get; set; }
        public LoginStatusEnum LoginStatus { get; set; }
        public AuthenticationMethodEnum AuthMethod { get; set; }
        public bool AuthRequiredAtlogin { get; set; }
        public List<string> ModuleIds { get; set; }
        public string EmailAddress { get; set; }
        public string AuthKey { get; set; }
        public string AuthValue { get; set; }
        public string MobileNo { get; set; }
        public string UnsuccessfulMsg { get; set; }
        public string ThemeName { get; set; }
        public string SchemeName { get; set; }
        public string MenuLayout { get; set; }
        public bool IsLocked { get; set; }
        public DateTime MinReportDate { get; set; }
        public string AppId { get; set; }
        public bool BatchEnabled { get; set; }
        public bool DisallowMultiLogin { get; set; }
        public int IdleTime { get; set; }
        public int PingTime { get; set; }
        public int TimeoutTime { get; set; }
    }

    public class LoginHistory
    {
        public int SlNo { get; set; }
        public string LoginIp { get; set; }
        public DateTime LoginTime { get; set; }
        public string LogoutIp { get; set; }
        public DateTime? LogoutTime { get; set; }
    }

    public class DashboardSeriesItem
    {
        public string Type => "line";
        public bool Visible { get; set; }
        public string Name { get; set; }
        public List<decimal> Data { get; set; }
    }

    public class NotificationUser
    {
        public string LoginId { get; set; }
        public string UserName { get; set; }
        public string AppId { get; set; }
    }

    public class UserForceLogout
    {
        public int UserId { get; set; }
        public string LoginId { get; set; }
        public string UserName { get; set; }
    }

    public class UserSearch : UserForceLogout
    {
        public string MobileNo { get; set; }
        public string EmailAddress { get; set; }
        public StatusEnum Status { get; set; }
        public UserTypeEnum UserType { get; set; }
        public string UserTypeDetail => UserType.ToString();
        public bool IsLocked { get; set; }
        public string StatusDetail => Status.ToString();
        public string AuthId { get; set; }
    }
}
