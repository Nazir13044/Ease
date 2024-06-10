using Collecto.CoreAPI.Models;
using Collecto.CoreAPI.Models.Objects.Systems;
using Collecto.CoreAPI.Models.Requests.Systems;
using Collecto.CoreAPI.Models.Responses.Setups;
using Collecto.CoreAPI.Models.Responses.Systems;

namespace Collecto.CoreAPI.Services.Contracts.Systems
{
    public interface IUserService
    {
        Task<User> LoginAsync(string loginId, string password, string appId, string appVersion, string ipAddress, bool checkPwd);
        Task<bool> AddUserAsync(NewUserRequest user, string ipAddress, int createdBy);
        Task<bool> EditUserAsync(UserRequest user, string ipAddress, int modifiedBy);
        Task<bool> DeleteUserAsync(int userId, int deletedBy);
        Task<bool> UnlockUserAsync(int userId, string loginId, int unlockedBy);
        Task<FindAccountResponse> FindAccountAsync(string accountId);
        Task<UserProfileResponse> GetUserProfileAsync(int userId);
        Task<MenuItem> GetUserPermissionsAsync(int userId);
        Task<bool> ValidateAuthValueAsync(string authValue, int userId);
        Task<bool> ResetPasswordAsync(int userId, string newPassword, string ipAddress, int changedBy);
        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword, string ipAddress, int changedBy);
        Task<UserSearchResponse> GetUsersAsync(UserSearchRequest request, UserTypeEnum userType);
        Task<UserForceLogoutResponse> GetForceLogoutUsersAsync(int exceptUserId);
        Task<bool> ForceLogoutNowAsync(List<int> userIds, string ipAddress);
        Task<UserGetResponse> GetUserAsync(int userId);
        Task<bool> LogoutAsync(string ipAddress, int userId, int logId);
        Task<bool> SendPasswordAsync(int userId, string newPassword, string ipAddress);
    }
}
