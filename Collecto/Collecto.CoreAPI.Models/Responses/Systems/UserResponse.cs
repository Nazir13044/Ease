using Collecto.CoreAPI.Models.Objects.Systems;

namespace Collecto.CoreAPI.Models.Responses.Systems
{
    public class UserProfileBase : ResponseBase
    {
        public int UserId { get; set; }
        public string LoginId { get; set; }
        public string UserName { get; set; }
        public string MobileNo { get; set; }
        public string EmailAddress { get; set; }
    }

    public class UserGetResponse : UserProfileBase
    {
        public bool IsLocked { get; set; }
        public bool NeverExpire { get; set; }
        public UserTypeEnum UserType { get; set; }
        public StatusEnum Status { get; set; }
        public bool AuthRequiredAtlogin { get; set; }
        public AuthenticationMethodEnum AuthMethod { get; set; }
        public AccessStatusEnum AccessStatus { get; set; }
        public bool DbOnStartup { get; set; }
        public bool DisallowMultiLogin { get; set; }
        public List<int> GroupIds { get; set; }
        public string UserTypeDetail => UserType.ToString();
    }

    public class UserProfileResponse : UserProfileBase
    {
        public DateTime? PwdLastChangedTime { get; set; }
        public List<LoginHistory> LoginHistories { get; set; }
    }

    public class UserAttributesResponse : UserProfileBase
    {
        public short AtribSetupId { get; set; }
        public int? SubsystemId { get; set; }
        public short LevelId { get; set; }
        public int? HierarchyId { get; set; }
        public int? SalespointId { get; set; }
        public short EmployeeType { get; set; }
        public int? EmployeeId { get; set; }
        public List<int> SalespointIds { get; set; }
        public bool HasSetup => SubsystemId != null && SubsystemId.HasValue && SubsystemId.Value > 0;
    }

    public class DashboardDataResponse : ResponseBase
    {
        public int NoOfRemCreated { get; set; }
        public int NoOfRemUpdated { get; set; }
        public int NoOfRemDeleted { get; set; }
        public int NoOfBnfCreated { get; set; }
        public int NoOfBnfUpdated { get; set; }
        public int NoOfBnfDeleted { get; set; }
        public int NoOfTTIssued { get; set; }
        public decimal TtIssuedAmount { get; set; }
        public int NoOfTTPaid { get; set; }
        public decimal TtPaidAmount { get; set; }
        public int NoOfTTPayCnfmd { get; set; }
        public decimal TtPayCnfmdAmount { get; set; }
        public int NoOfTTCnlApld { get; set; }
        public decimal TtCnlApldAmount { get; set; }
        public int NoOfTTCnlCnfmd { get; set; }
        public decimal TtCnlCnfmdAmount { get; set; }
        public int NoOfTTComplain { get; set; }
        public decimal TtComplainAmount { get; set; }
        public int NoOfTTStatusSet { get; set; }
        public decimal TtStatusSetAmount { get; set; }
        public decimal TtIssueLimit { get; set; }
        public decimal CreditOnRequest { get; set; }
        public decimal DayStartBalance { get; set; }
        public decimal DepositedAmount { get; set; }
        public decimal TtCancelledAmount { get; set; }
        public decimal SubTotal { get; set; }
        public decimal CanIssueTT { get; set; }
        public decimal DayEndBalance { get; set; }

        public List<string> SeriesCategories { get; set; }
        public List<DashboardSeriesItem> SeriesData { get; set; }
    }

    public class UsersNotifiationResponse : ResponseBase
    {
        public UsersNotifiationResponse()
        {
            Value = [];
        }
        public List<NotificationUser> Value { get; set; }
    }

    public class UserSearchResponse : TotalRowsResponseBase
    {
        public UserSearchResponse()
        {
            Value = [];
        }
        public List<UserSearch> Value { get; set; }
    }

    public class UserForceLogoutResponse : ResponseBase
    {
        public UserForceLogoutResponse()
        {
            Value = [];
        }
        public List<UserForceLogout> Value { get; set; }
    }
}
