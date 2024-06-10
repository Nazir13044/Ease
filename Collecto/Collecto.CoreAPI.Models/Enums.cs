namespace Collecto.CoreAPI.Models
{
    [Flags]
    public enum StatusEnum : short
    {
        Initiated = 1,
        Inactive = 2,
        Rejected = 4,
        Authenticated = 8,
        Authorized = 16
    }
    public enum UserTypeEnum : short
    {
        All = 0,
        SuperUser = 1,
        SuperAdmin = 2,
        Administrator = 3,
        FieldLevelUser = 4,
        DistributorAdmin = 5,
        DistributorUser = 6,
        CustomerUser = 7
    }
    public enum AuthenticationMethodEnum : short
    {
        None = 0,
        Email = 1,
        MobileSMS = 2,
        ThirdPartyAuthenticator = 3
    }
    public enum AccessStatusEnum : short
    {
        FirstTime = 1,
        LoggedIn = 2,
        LoggedOut = 3
    }
    public enum LoginStatusEnum : short
    {
        Success = 0,
        Error = 1,
        Unsuccessful = 2,
        VersionMismatch = 3
    }
}
