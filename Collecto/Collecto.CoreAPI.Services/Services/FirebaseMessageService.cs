using Collecto.CoreAPI.Models.Global;
using Collecto.CoreAPI.Models.Objects;
using Collecto.CoreAPI.Models.Objects.Systems;
using Collecto.CoreAPI.Models.Responses;
using Collecto.CoreAPI.Models.Responses.Systems;
using Collecto.CoreAPI.Services.Contracts;
using Collecto.CoreAPI.TransactionManagement.DataAccess;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Data;
using System.Net;

namespace Collecto.CoreAPI.Services.Services
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="settings"></param>
    public class FirebaseMessageService(IOptions<AppSettings> settings) : IFirebaseMessageService
    {
        private readonly AppSettings _settings = settings?.Value;

        public async Task<UsersNotifiationResponse> GetUsersForNotificationAsync()
        {
            UsersNotifiationResponse response = new() { ReturnStatus = 200 };
            try
            {
                using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode);
                try
                {
                    using (IDataReader dr = await tc.ExecuteReaderAsync("SELECT LoginId, UserName, AppId FROM Users WHERE UserId!=-9 AND Status=16 AND LEN(ISNULL(AppId,''))>0"))
                    {
                        while (dr.Read())
                        {
                            NotificationUser item = new()
                            {
                                LoginId = dr.GetString(0),
                                UserName = dr.GetString(1),
                                AppId = dr.GetString(2),
                            };
                            response.Value.Add(item);
                        }
                        dr.Close();
                    }

                    tc.End();

                    response.ReturnStatus = 200;
                }
                catch (Exception ie)
                {
                    tc?.HandleError();

                    throw DBCustomError.GenerateCustomError(ie);
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }

            return response;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<BooleanResponse> SendNotification(FirebaseMessage message)
        {
            BooleanResponse response = new();
            try
            {
                if (string.IsNullOrEmpty(_settings.FbServerKey))
                    throw new Exception("Server Key not found in appSetting...");

                if (string.IsNullOrEmpty(_settings.FirebaseUrl))
                    throw new Exception("Firebase url not found in appSetting...");


                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.TryAddWithoutValidation("accept", "*.*");
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"key={_settings.FbServerKey}");

                    foreach (string to in message.To)
                    {
                        FbMessage fbMsg = new(receiver: to, message: message.Notification);
                        string json = JsonConvert.SerializeObject(fbMsg);
                        StringContent data = new(json, System.Text.Encoding.UTF8, "application/json");
                        HttpResponseMessage httpResponse = await client.PostAsync(_settings.FirebaseUrl, data);
                        if (httpResponse.StatusCode == HttpStatusCode.Accepted || httpResponse.StatusCode == HttpStatusCode.OK || httpResponse.StatusCode == HttpStatusCode.Created)
                        {
                            string result = httpResponse.Content.ReadAsStringAsync().Result;
                            NotificationResponse notifyResp = JsonConvert.DeserializeObject<NotificationResponse>(result);
                            if (notifyResp.success == 1)
                            {
                                response.ReturnStatus = 200;
                                response.ReturnMessage.Add("Message send successfully.");
                            }
                            else
                            {
                                response.ReturnStatus = 400;
                                response.ReturnMessage.Add("Can not send Message.");
                            }
                        }
                        else
                        {
                            response.ReturnStatus = 400;
                            response.ReturnMessage.Add($"An error has occurred when try to get server response: {httpResponse.StatusCode}");
                        }
                    }
                }
                return response;
            }
            catch (Exception e)
            {
                throw new Exception((e.InnerException != null ? e.InnerException.Message : e.Message), e);
            }
        }

        #region Helper class

        private class FbMessage
        {
            public FbMessage(string receiver, FirebaseMessageData message)
            {
                to = receiver;
                notification = new Notification { body = message.Body, title = message.Title };
            }
            public string to { get; set; }
            public Notification notification { get; set; }
        }

        private class Notification
        {
            public string title { get; set; }
            public string body { get; set; }
        }

        private class NotificationResponse
        {
            public string multicast_id { get; set; }
            public int success { get; set; }
            public int failure { get; set; }
            public int canonical_ids { get; set; }
            public List<Message> results { get; set; }
        }

        private class Message
        {
            public string message_id { get; set; }
        }
    }

    #endregion
}
