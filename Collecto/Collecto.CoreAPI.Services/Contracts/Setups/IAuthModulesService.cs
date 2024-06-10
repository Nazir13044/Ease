using Collecto.CoreAPI.Models.Requests.Setups;
using Collecto.CoreAPI.Models.Responses.Setups;

namespace Collecto.CoreAPI.Services.Contracts.Setups
{
    public interface IAuthModulesService
    {
        Task<AuthSummariesResponse> GetAuthSummariesAsync(int systemId, int subsystemId, int userId, short status, int entryModule);
        Task<AuthDetailsResponse> GetAuthDetailsAsync(string moduleId, int systemId, int subsystemId, short status);
        Task<bool> UpdateAuthStatusAsync(string moduleId, string ipAddress, string remarks, short status, int userId, string ids);
        Task<AuthModulesBasicResponse> GetAuthModulesBasicAsync();
        Task<AuthDetailsResponse> GetInactiveDetailsAsync(InactiveDetailRequest request, int systemId, int userId);
    }
}
