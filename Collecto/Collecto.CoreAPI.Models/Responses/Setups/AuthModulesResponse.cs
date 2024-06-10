using Collecto.CoreAPI.Models.Objects.Setups;

namespace Collecto.CoreAPI.Models.Responses.Setups
{
    public class AuthSummariesResponse : TotalRowsResponseBase
    {
        public List<AuthModules> Value { get; set; }
    }

    public class AuthDetailsResponse : ResponseBase
    {
        public List<AuthModuleColumn> Columns { get; set; }
        public List<Dictionary<string, object>> Data { get; set; }
    }

    public class AuthModulesBasicResponse : ResponseBase
    {
        public List<AuthModuleBasic> Value { get; set; }
    }
}
