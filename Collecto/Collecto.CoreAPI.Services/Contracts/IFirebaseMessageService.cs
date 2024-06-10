using Collecto.CoreAPI.Models.Objects;
using Collecto.CoreAPI.Models.Responses;
using Collecto.CoreAPI.Models.Responses.Systems;

namespace Collecto.CoreAPI.Services.Contracts
{
    public interface IFirebaseMessageService
    {
        Task<BooleanResponse> SendNotification(FirebaseMessage message);
        Task<UsersNotifiationResponse> GetUsersForNotificationAsync();
    }
}
