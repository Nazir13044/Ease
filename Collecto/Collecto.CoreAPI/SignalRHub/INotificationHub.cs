namespace Collecto.CoreAPI.SignalRHub
{
    /// <summary>
    /// 
    /// </summary>
    public interface INotificationHub
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId">User id to notify.</param>
        /// <param name="msgType">1: Force to logout all, 2: Same user is logged in from another device</param>
        /// <param name="ipAddress">Ip address from which the user is logged in.</param>
        /// <returns></returns>
        Task NotifyUser(int userId, int msgType, string ipAddress);
    }
}
