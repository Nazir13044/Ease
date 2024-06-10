namespace Collecto.CoreAPI.Models.Requests.Setups
{
    public class AuthSummaryRequest
    {
        public short Status { get; set; }
    }

    public class AuthDetailRequest : AuthSummaryRequest
    {
        public string ModuleId { get; set; }
    }

    public class AuthUpdateRequest : AuthDetailRequest
    {
        public string Remarks { get; set; }
        public List<int> Ids { get; set; }
    }

    public class InactiveDetailRequest : ValueAndStatusSearchRequest
    {
        public int SubsystemId { get; set; }
        public string ModuleId { get; set; }
        public int SalespointId { get; set; }
    }
}
