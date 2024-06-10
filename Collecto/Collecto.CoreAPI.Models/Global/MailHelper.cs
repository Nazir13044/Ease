using System.Net;
using System.Net.Mail;
using System.Text;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Collecto.CoreAPI.Models.Global
{
    public static class MailHelper
    {
        private static IPAddress _senderIp;
        static MailHelper()
        {
            _senderIp = IPAddress.None;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="msg"></param>
        /// <param name="mobileNumber"></param>
        /// <param name="subject"></param>
        /// <param name="emailAddress"></param>
        public static void SendSMSAndMail(AppSettings settings, string msg, string mobileNumber, string subject, string emailAddress)
        {
            try
            {
                if (string.IsNullOrEmpty(msg) == false && string.IsNullOrWhiteSpace(msg) == false)
                {
                    //Send SMS
                    SendSMSOrWhatsAppMessage(settings: settings, whatsAppMsg: false, msg: msg, mobileNumber: mobileNumber);

                    //Send Email
                    string bccEmail = settings.EmailBcc;
                    if (string.IsNullOrEmpty(bccEmail))
                        bccEmail = emailAddress;

                    List<string> to = new() { emailAddress };
                    List<string> bcc = new() { bccEmail };
                    SendMailMessage(settings: settings, to: to, cc: null, bcc: bcc, attachments: null, embeddedImages: null, isHtmlBody: false, priority: MailPriority.Normal, subject: subject, messageBody: msg);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="whatsAppMsg"></param>
        /// <param name="msg"></param>
        /// <param name="mobileNumber"></param>
        public static void SendSMSOrWhatsAppMessage(AppSettings settings, bool whatsAppMsg, string msg, string mobileNumber)
        {
            try
            {
                mobileNumber = mobileNumber.Replace("+", "");
                if (string.IsNullOrEmpty(settings.WaAccountSid) == false && string.IsNullOrWhiteSpace(settings.WaAuthToken) == false && string.IsNullOrWhiteSpace(settings.WaMsgSvcSid) == false)
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    TwilioClient.Init(username: settings.WaAccountSid, password: settings.WaAuthToken);
                    if (whatsAppMsg && string.IsNullOrEmpty(settings.WaSenderId) == false)
                    {
                        mobileNumber = $"+88{(TransactionManagement.Helper.Global.StringFunctions.Right(inputString: mobileNumber, length: 11))}";
                        MessageResource message = MessageResource.Create(body: msg, from: new PhoneNumber($"whatsapp:{settings.WaSenderId}"), to: new PhoneNumber($"whatsapp:{mobileNumber}"));
                    }
                    else if (string.IsNullOrEmpty(settings.WaMsgSvcSid) == false)
                    {
                        mobileNumber = $"+88{(TransactionManagement.Helper.Global.StringFunctions.Right(inputString: mobileNumber, length: 11))}";
                        CreateMessageOptions messageOptions = new(to: new PhoneNumber(number: mobileNumber))
                        {
                            Body = msg,
                            MessagingServiceSid = settings.WaMsgSvcSid
                        };
                        MessageResource message = MessageResource.Create(messageOptions);
                    }
                }
                else if (string.IsNullOrEmpty(settings.SmsApiUrl) == false && string.IsNullOrEmpty(settings.SmsAccessInfo) == false)
                {
                    string url = settings.SmsApiUrl;
                    string accessInfo = TransactionManagement.Helper.Global.CipherFunctions.Decrypt(settings.SmsSecretKey, settings.SmsAccessInfo);
                    if (string.IsNullOrEmpty(accessInfo))
                        return;

                    //Send SMS
                    string smsString = string.Format("sms[0][0]={0}&sms[0][1]={1}&sms[0][2]={2}", mobileNumber, msg, Guid.NewGuid().ToString());

                    ServicePointManager.Expect100Continue = false;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    /*
                    HttpWebRequest webRequest = WebRequest.Create(url) as HttpWebRequest;
                    webRequest.Method = "POST";
                    webRequest.ContentType = "application/x-www-form-urlencoded";

                    string content = accessInfo + smsString;
                    byte[] data = Encoding.UTF8.GetBytes(content);
                    webRequest.ContentLength = data.Length;
                    using (Stream stream = webRequest.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                        stream.Close();
                    }

                    using (HttpWebResponse webResponse = webRequest.GetResponse() as HttpWebResponse)
                    {
                        string result = new StreamReader(webResponse.GetResponseStream()).ReadToEnd();
                    }
                    webRequest = null;
                    */

                    using HttpClient client = new();
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Accept.Clear();

                    //Content
                    string content = accessInfo + smsString;
                    HttpRequestMessage request = new(HttpMethod.Post, url)
                    {
                        Content = new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded")
                    };

                    var httpResponse = client.PostAsync(url, request.Content).Result;
                    if (httpResponse != null && httpResponse.Content != null)
                    {
                        content = httpResponse.Content.ReadAsStringAsync().Result;
                    }
                    else
                    {
                        content = "ERROR: -999";
                    }
                }
            }
            catch (Exception e)
            {
                try
                {
                    string text = e.Message;
                    Exception ie = e.InnerException;
                    while (ie != null)
                    {
                        text += ", " + ie.Message;
                        ie = ie.InnerException;
                    }

                    string path = settings.EmailErrorLogPath;
                    if (string.IsNullOrEmpty(path))
                        path = @"C:\Mail.Web\MailError";

                    string logFileSpec = Path.Combine(path, string.Format("{0}log.txt", DateTime.Today.ToString("yyMMdd")));
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);

                    if (File.Exists(logFileSpec))
                    {
                        using StreamWriter sw = File.AppendText(logFileSpec);
                        string log = Environment.NewLine + new string('*', 40) + Environment.NewLine;
                        log += string.Format("  TimeStamp: {0} {1}{2}", DateTime.Today.ToShortDateString(), DateTime.Now.ToLongTimeString(), Environment.NewLine);
                        log += new string('*', 40);
                        sw.WriteLine(log);
                        sw.WriteLine(text);
                        sw.Flush();
                        sw.Close();
                    }
                    else
                    {
                        using StreamWriter sw = File.CreateText(logFileSpec);
                        string log = new string('*', 40) + Environment.NewLine;
                        log += string.Format("  TimeStamp: {0} {1}{2}", DateTime.Today.ToShortDateString(), DateTime.Now.ToLongTimeString(), Environment.NewLine);
                        log += new string('*', 40);
                        sw.WriteLine(log);
                        sw.WriteLine(text);
                        sw.Flush();
                        sw.Close();
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="to"></param>
        /// <param name="cc"></param>
        /// <param name="bcc"></param>
        /// <param name="attachments"></param>
        /// <param name="embeddedImages"></param>
        /// <param name="isHtmlBody"></param>
        /// <param name="priority"></param>
        /// <param name="subject"></param>
        /// <param name="messageBody"></param>
        /// <returns></returns>
        public static bool SendMailMessage(AppSettings settings, List<string> to, List<string> cc, List<string> bcc, List<string> attachments, List<string> embeddedImages, bool isHtmlBody, MailPriority priority, string subject, string messageBody)
        {
            try
            {
                #region Read config info

                if (string.IsNullOrEmpty(settings.EmailHost))
                    throw new Exception("No Setting has been found for Host address [EmailHost].");

                if (settings.EmailPort <= 0)
                    throw new Exception("No Setting has been found for Port number [EmailPort].");

                if (string.IsNullOrEmpty(settings.EmailSenderId))
                    throw new Exception("No Setting has been found for Sender email address [EmailSenderId].");

                if (string.IsNullOrEmpty(settings.EmailSenderPwd))
                    throw new Exception("No Setting has been found for Sender email address password [EmailSenderPwd].");

                #endregion

                using MailMessage message = new() { Subject = subject, Body = messageBody, IsBodyHtml = isHtmlBody, Priority = priority };
                if (string.IsNullOrEmpty(settings.EmailSenderId) == false && string.IsNullOrEmpty(settings.EmailSenderName) == false)
                {
                    message.From = new MailAddress(settings.EmailSenderId, settings.EmailSenderName);
                    message.Sender = new MailAddress(settings.EmailSenderId, settings.EmailSenderName);
                }

                if (to != null && to.Count > 0)
                {
                    foreach (string email in to)
                        message.To.Add(email);
                }

                if (cc != null && cc.Count > 0)
                {
                    foreach (string email in cc)
                        message.CC.Add(email);
                }

                if (bcc != null && bcc.Count > 0)
                {
                    foreach (string email in bcc)
                        message.Bcc.Add(email);
                }

                if (attachments != null && attachments.Count > 0)
                {
                    foreach (string attachment in attachments)
                    {
                        Attachment item = new(attachment);
                        message.Attachments.Add(item);
                    }
                }

                AlternateView altView;
                if (isHtmlBody)
                {
                    message.IsBodyHtml = true;
                    altView = AlternateView.CreateAlternateViewFromString(content: messageBody, contentEncoding: null, mediaType: "text/html");
                    if (embeddedImages != null && embeddedImages.Count > 0)
                    {
                        foreach (string imbeddedImage in embeddedImages)
                        {
                            string contentId = imbeddedImage;
                            int indexOf = imbeddedImage.IndexOf('.');
                            if (indexOf > 0)
                                contentId = contentId[..indexOf];

                            LinkedResource lnkRsrc = new(imbeddedImage)
                            {
                                ContentId = contentId
                            };
                            altView.LinkedResources.Add(lnkRsrc);
                        }
                    }
                }
                else
                {
                    message.IsBodyHtml = false;
                    altView = AlternateView.CreateAlternateViewFromString(content: messageBody, contentEncoding: null, mediaType: "text/plain");
                }
                message.AlternateViews.Add(item: altView);

                if (settings.EmailTlsVersion > 0)
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)settings.EmailTlsVersion;

                using SmtpClient client = new() { Host = settings.EmailHost, Port = settings.EmailPort, EnableSsl = settings.EmailEnableSsl };
                NetworkCredential nc = new(settings.EmailSenderId, settings.EmailSenderPwd);
                client.UseDefaultCredentials = settings.EmailUseDefaultCredentials;
                client.Credentials = nc;
                client.Send(message);

                return true;
            }
            catch (Exception e)
            {
                try
                {
                    string text = e.Message;
                    Exception ie = e.InnerException;
                    while (ie != null)
                    {
                        text += ", " + ie.Message;
                        ie = ie.InnerException;
                    }

                    string path = settings.EmailErrorLogPath;
                    if (string.IsNullOrEmpty(path))
                        path = @"C:\Mail.Web\MailError";

                    string logFileSpec = Path.Combine(path, string.Format("{0}log.txt", DateTime.Today.ToString("yyMMdd")));
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);

                    if (File.Exists(logFileSpec))
                    {
                        using StreamWriter sw = File.AppendText(logFileSpec);
                        string log = Environment.NewLine + new string('*', 40) + Environment.NewLine;
                        log += string.Format("  TimeStamp: {0} {1}{2}", DateTime.Today.ToShortDateString(), DateTime.Now.ToLongTimeString(), Environment.NewLine);
                        log += new string('*', 40);
                        sw.WriteLine(log);
                        sw.WriteLine(text);
                        sw.Flush();
                        sw.Close();
                    }
                    else
                    {
                        using StreamWriter sw = File.CreateText(logFileSpec);
                        string log = new string('*', 40) + Environment.NewLine;
                        log += string.Format("  TimeStamp: {0} {1}{2}", DateTime.Today.ToShortDateString(), DateTime.Now.ToLongTimeString(), Environment.NewLine);
                        log += new string('*', 40);
                        sw.WriteLine(log);
                        sw.WriteLine(text);
                        sw.Flush();
                        sw.Close();
                    }
                }
                catch { }
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="to"></param>
        /// <param name="cc"></param>
        /// <param name="bcc"></param>
        /// <param name="attachments"></param>
        /// <param name="embeddedImages"></param>
        /// <param name="isHtmlBody"></param>
        /// <param name="priority"></param>
        /// <param name="subject"></param>
        /// <param name="messageBody"></param>
        public static async void SendMailMessageAsync(AppSettings settings, List<string> to, List<string> cc, List<string> bcc, List<string> attachments, List<string> embeddedImages, bool isHtmlBody, MailPriority priority, string subject, string messageBody)
        {
            try
            {
                #region Read config info

                if (string.IsNullOrEmpty(settings.EmailHost))
                    throw new Exception("No Setting has been found for Host address [EmailHost].");

                if (settings.EmailPort <= 0)
                    throw new Exception("No Setting has been found for Port number [EmailPort].");

                if (string.IsNullOrEmpty(settings.EmailSenderId))
                    throw new Exception("No Setting has been found for Sender email address [EmailSenderId].");

                if (string.IsNullOrEmpty(settings.EmailSenderPwd))
                    throw new Exception("No Setting has been found for Sender email address password [EmailSenderPwd].");

                #endregion

                MailMessage message = new() { Subject = subject, Body = messageBody, IsBodyHtml = isHtmlBody, Priority = priority };
                if (string.IsNullOrEmpty(settings.EmailSenderId) == false && string.IsNullOrEmpty(settings.EmailSenderName) == false)
                {
                    message.From = new MailAddress(settings.EmailSenderId, settings.EmailSenderName);
                    message.Sender = new MailAddress(settings.EmailSenderId, settings.EmailSenderName);
                }

                if (to != null && to.Count > 0)
                {
                    foreach (string email in to)
                        message.To.Add(email);
                }

                if (cc != null && cc.Count > 0)
                {
                    foreach (string email in cc)
                        message.CC.Add(email);
                }

                if (bcc != null && bcc.Count > 0)
                {
                    foreach (string email in bcc)
                        message.Bcc.Add(email);
                }

                if (attachments != null && attachments.Count > 0)
                {
                    foreach (string attachment in attachments)
                    {
                        Attachment item = new(attachment);
                        message.Attachments.Add(item);
                    }
                }

                AlternateView altView;
                if (isHtmlBody)
                {
                    message.IsBodyHtml = true;
                    altView = AlternateView.CreateAlternateViewFromString(content: messageBody, contentEncoding: null, mediaType: "text/html");
                    if (embeddedImages != null && embeddedImages.Count > 0)
                    {
                        foreach (string embeddedImage in embeddedImages)
                        {
                            string contentId = embeddedImage;
                            int indexOf = embeddedImage.IndexOf('.');
                            if (indexOf > 0)
                                contentId = contentId[..indexOf];

                            LinkedResource lnkRsrc = new(embeddedImage)
                            {
                                ContentId = contentId
                            };
                            altView.LinkedResources.Add(lnkRsrc);
                        }
                    }
                }
                else
                {
                    message.IsBodyHtml = false;
                    altView = AlternateView.CreateAlternateViewFromString(content: messageBody, contentEncoding: null, mediaType: "text/plain");
                }
                message.AlternateViews.Add(item: altView);

                if (settings.EmailTlsVersion > 0)
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)settings.EmailTlsVersion;

                SmtpClient client = new() { Host = settings.EmailHost, Port = settings.EmailPort, EnableSsl = settings.EmailEnableSsl };
                NetworkCredential nc = new(settings.EmailSenderId, settings.EmailSenderPwd);
                client.UseDefaultCredentials = settings.EmailUseDefaultCredentials;
                client.Credentials = nc;
                client.SendCompleted += (s, e) =>
                {
                    client.Dispose();
                    message.Dispose();
                };

                await client.SendMailAsync(message: message);
            }
            catch (Exception e)
            {
                try
                {
                    string text = e.Message;
                    Exception ie = e.InnerException;
                    while (ie != null)
                    {
                        text += ", " + ie.Message;
                        ie = ie.InnerException;
                    }

                    string path = settings.EmailErrorLogPath;
                    if (string.IsNullOrEmpty(path))
                        path = @"C:\Mail.Web\MailError";

                    string logFileSpec = Path.Combine(path, string.Format("{0}log.txt", DateTime.Today.ToString("yyMMdd")));
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);

                    if (File.Exists(logFileSpec))
                    {
                        using StreamWriter sw = File.AppendText(logFileSpec);
                        string log = Environment.NewLine + new string('*', 40) + Environment.NewLine;
                        log += string.Format("  TimeStamp: {0} {1}{2}", DateTime.Today.ToShortDateString(), DateTime.Now.ToLongTimeString(), Environment.NewLine);
                        log += new string('*', 40);
                        sw.WriteLine(log);
                        sw.WriteLine(text);
                        sw.Flush();
                        sw.Close();
                    }
                    else
                    {
                        using StreamWriter sw = File.CreateText(logFileSpec);
                        string log = new string('*', 40) + Environment.NewLine;
                        log += string.Format("  TimeStamp: {0} {1}{2}", DateTime.Today.ToShortDateString(), DateTime.Now.ToLongTimeString(), Environment.NewLine);
                        log += new string('*', 40);
                        sw.WriteLine(log);
                        sw.WriteLine(text);
                        sw.Flush();
                        sw.Close();
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="to"></param>
        /// <param name="cc"></param>
        /// <param name="bcc"></param>
        /// <param name="attachments"></param>
        /// <param name="embeddedImages"></param>
        /// <param name="isBodyHtml"></param>
        /// <param name="priority"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public static bool SendIpBindingMailMessage(AppSettings settings, List<string> to, List<string> cc, List<string> bcc, List<string> attachments, List<string> embeddedImages, bool isBodyHtml, MailPriority priority, string subject, string body)
        {
            try
            {
                #region Read config info

                if (string.IsNullOrEmpty(settings.EmailHost))
                    throw new Exception("No Setting has been found for Host address [EmailHost].");

                if (settings.EmailPort <= 0)
                    throw new Exception("No Setting has been found for Port number [EmailPort].");

                if (string.IsNullOrEmpty(settings.EmailSenderIp))
                    throw new Exception("No Setting has been found for Email Sender ip [EmailSenderIp].");

                if (string.IsNullOrEmpty(settings.EmailSenderId))
                    throw new Exception("No Setting has been found for Sender email address [EmailSenderId].");

                #endregion

                _senderIp = IPAddress.Parse(settings.EmailSenderIp);
                using MailMessage message = new() { Subject = subject, Body = body, IsBodyHtml = isBodyHtml, Priority = priority };
                if (string.IsNullOrEmpty(settings.EmailSenderId) == false && string.IsNullOrEmpty(settings.EmailSenderName) == false)
                {
                    message.From = new MailAddress(settings.EmailSenderId, settings.EmailSenderName);
                    message.Sender = new MailAddress(settings.EmailSenderId, settings.EmailSenderName);
                }
                else
                {
                    message.From = new MailAddress(settings.EmailSenderId);
                    message.Sender = new MailAddress(settings.EmailSenderId);
                }

                //Add recipient to
                foreach (string email in to)
                    message.To.Add(email);

                //Add recipient cc
                if (cc != null && cc.Count > 0)
                {
                    foreach (string email in cc)
                        message.CC.Add(email);
                }

                //Add recipient bcc
                if (bcc != null && bcc.Count > 0)
                {
                    foreach (string email in bcc)
                        message.Bcc.Add(email);
                }

                //Add Attachment(s)
                if (attachments != null && attachments.Count > 0)
                {
                    foreach (string attachment in attachments)
                    {
                        Attachment item = new(attachment);
                        message.Attachments.Add(item);
                    }
                }

                AlternateView altView;
                if (message.IsBodyHtml)
                {
                    altView = AlternateView.CreateAlternateViewFromString(content: body, contentEncoding: null, mediaType: "text/html");
                    if (embeddedImages != null && embeddedImages.Count > 0)
                    {
                        foreach (string embeddedImage in embeddedImages)
                        {
                            string contentId = embeddedImage;
                            int indexOf = embeddedImage.IndexOf('.');
                            if (indexOf > 0)
                                contentId = contentId[..indexOf];
                            LinkedResource lnkRsrc = new(embeddedImage)
                            {
                                ContentId = contentId
                            };
                            altView.LinkedResources.Add(lnkRsrc);
                        }
                    }
                }
                else
                {
                    altView = AlternateView.CreateAlternateViewFromString(content: body, contentEncoding: null, mediaType: "text/plain");
                }
                message.AlternateViews.Add(item: altView);

                //Finally send mail 
                SmtpClient client = new() { Host = settings.EmailHost, Port = settings.EmailPort, EnableSsl = settings.EmailEnableSsl };
                client.Send(message);
                client.ServicePoint.BindIPEndPointDelegate = new BindIPEndPoint(BindIPEndPointCallback);
                client.ServicePoint.ConnectionLeaseTimeout = 0;
                client.Send(message);

                return true;
            }
            catch (Exception e)
            {
                try
                {
                    string text = e.Message;
                    Exception ie = e.InnerException;
                    while (ie != null)
                    {
                        text += ", " + ie.Message;
                        ie = ie.InnerException;
                    }

                    string path = settings.EmailErrorLogPath;
                    if (string.IsNullOrEmpty(path))
                        path = @"C:\Mail.Web\MailError";

                    string logFileSpec = Path.Combine(path, string.Format("{0}log.txt", DateTime.Today.ToString("yyMMdd")));
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);

                    if (File.Exists(logFileSpec))
                    {
                        using StreamWriter sw = File.AppendText(logFileSpec);
                        string log = Environment.NewLine + new string('*', 40) + Environment.NewLine;
                        log += string.Format("  TimeStamp: {0} {1}{2}", DateTime.Today.ToShortDateString(), DateTime.Now.ToLongTimeString(), Environment.NewLine);
                        log += new string('*', 40);
                        sw.WriteLine(log);
                        sw.WriteLine(text);
                        sw.Flush();
                        sw.Close();
                    }
                    else
                    {
                        using StreamWriter sw = File.CreateText(logFileSpec);
                        string log = new string('*', 40) + Environment.NewLine;
                        log += string.Format("  TimeStamp: {0} {1}{2}", DateTime.Today.ToShortDateString(), DateTime.Now.ToLongTimeString(), Environment.NewLine);
                        log += new string('*', 40);
                        sw.WriteLine(log);
                        sw.WriteLine(text);
                        sw.Flush();
                        sw.Close();
                    }
                }
                catch { }
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="servicePoint"></param>
        /// <param name="remoteEndPoint"></param>
        /// <param name="retryCount"></param>
        /// <returns></returns>
        private static IPEndPoint BindIPEndPointCallback(ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount)
        {
            return new IPEndPoint(_senderIp, 0);
        }
    }
}
