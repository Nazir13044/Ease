using Microsoft.AspNetCore.SignalR;

namespace Collecto.CoreAPI.SignalRHub
{
    /// <summary>
    /// 
    /// </summary>
    public class NotificationHub : Hub<INotificationHub>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="msgType"></param>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public async Task NotifyUser(int userId, int msgType, string ipAddress)
        {
            try
            {
                await Clients.All.NotifyUser(userId: userId, msgType: msgType, ipAddress: ipAddress);
            }
            catch
            {
            }
        }
    }
}
