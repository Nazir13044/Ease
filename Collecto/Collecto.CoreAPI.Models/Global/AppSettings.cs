using Collecto.CoreAPI.TransactionManagement.DataAccess;
using System.ComponentModel.DataAnnotations;

namespace Collecto.CoreAPI.Models.Global
{
    /// <summary>
    /// AppSettings
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Default Database Connection to be used 
        /// </summary>
        [Required]
        public string DefaultDB { get; set; }

        /// <summary>
        /// Used to decrypt value found over http call
        /// </summary>
        public string CipherSecretKey { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether a cookie is inaccessible by client-side script.
        /// </summary>
        public bool CookieHttpOnly { get; set; } = false;

        /// <summary>
        /// Gets or sets a value that indicates whether to transmit the cookie using Secure Sockets Layer (SSL)--that is, over HTTPS only.
        /// </summary>
        public bool CookieSecure { get; set; } = false;

        /// <summary>
        /// Gets or sets the max-age for the cookie in minutes.
        /// </summary>
        public int CookieLifeTime { get; set; } = 360;

        /// <summary>
        /// Gets or sets the value for the SameSite attribute of the cookie. 
        /// -1: No SameSite field will be set, the client should follow its default cookie policy.
        /// 0: Indicates the client should disable same-site restrictions.
        /// 1: Indicates the client should send the cookie with "same-site" requests, and with "cross-site" top-level navigations.
        /// 2: Indicates the client should only send the cookie with "same-site" requests.
        /// </summary>
        public int CookieSameSite { get; set; } = 0;

        /// <summary>
        /// Entity framework Database Connection string
        /// </summary>
        public string EfCoreDb { get; set; }

        /// <summary>
        /// For Jwt Token
        /// </summary>
        public string JwtCryptoKey { get; set; }
        public string JwtIssuer { get; set; }
        public string JwtAudience { get; set; }
        public bool JwtValidateIssuer { get; set; }
        public bool JwtValidateAudience { get; set; }

        /// <summary>
        /// Folder management
        /// </summary>
        public string UploadFolder { get; set; }
        public string ProfileImageFolder { get; set; }
        public string DownloadFolder { get; set; }
        public string ReportFolder { get; set; }

        /// <summary>
        /// For user password
        /// </summary>
        public string PwdSecretKey { get; set; }

        public string BaseKey { get; set; }

        /// <summary>
        /// For SMS Management
        /// </summary>
        public string SmsSecretKey { get; set; }
        public string SmsApiUrl { get; set; }
        public string SmsAccessInfo { get; set; }

        /// <summary>
        /// Email management
        /// </summary>
        public int EmailPort { get; set; }
        public bool EmailEnableSsl { get; set; }
        public string EmailHost { get; set; }
        public string EmailSenderName { get; set; }
        public string EmailSenderIp { get; set; }
        public string EmailSenderId { get; set; }

        private string _emailSenderPwd;
        public string EmailSenderPwd
        {
            get
            {
                if (string.IsNullOrEmpty(_emailSenderPwd) || string.IsNullOrEmpty(PwdSecretKey))
                    return string.Empty;

                return TransactionManagement.Helper.Global.CipherFunctions.DecryptByAES(privateKey: PwdSecretKey, publicKey: PwdSecretKey, data: _emailSenderPwd, input: 2);
            }
            set { _emailSenderPwd = value; }
        }
        public string EmailBcc { get; set; }
        public string EmailErrorLogPath { get; set; }
        public bool EmailUseDefaultCredentials { get; set; }
        public int EmailTlsVersion { get; set; }

        /// <summary>
        /// CORS settings
        /// </summary>
        public string CorsSetting { get; set; }

        /// <summary>
        /// Firebase Messaging
        /// </summary>
        public string FbApiKey { get; set; }
        public string FbVapidKey { get; set; }
        public string FbProjectId { get; set; }
        public string FbSenderId { get; set; }
        public string FbStorageBucket { get; set; }
        public string FbAuthDomain { get; set; }
        public string FbDatabaseURL { get; set; }
        public string FirebaseUrl { get; set; } = string.Empty;
        public string FbServerKey { get; set; } = string.Empty;

        /// <summary>
        /// Hangfire Database for Scheduler 
        /// </summary>
        public string HangfireDb { get; set; } = string.Empty;

        /// <summary>
        /// Logger enabled if the is there is value
        /// </summary>
        public string LoggerPath { get; set; } = string.Empty;
        public int LoggerMinLevel { get; set; }

        /// <summary>
        /// Distrubuted cache (Redis)
        /// </summary>
        public string RedisCacheUrl { get; set; } = string.Empty;

        /// <summary>
        /// Distrubuted Session (Redis) 
        /// </summary>
        public string RedisSessionUrl { get; set; } = string.Empty;

        /// <summary>
        /// What's app messaging
        /// </summary>
        public string WaSenderId { get; set; } = string.Empty;
        public string WaAuthToken { get; set; } = string.Empty;
        public string WaAccountSid { get; set; } = string.Empty;
        public string WaMsgSvcSid { get; set; } = string.Empty;

        /// <summary>
        /// Application url for reset password, authentication or any other url
        /// </summary>
        public string AppUrl { get; set; } = string.Empty;

        /// <summary>
        /// Ftp Crypto key
        /// </summary>
        public string FtpCryptoKey { get; set; }

        /// <summary>
        /// SOAP Api crypto information
        /// </summary>
        public string ApiCryptoKey { get; set; }
        public int ApiSecurityMode { get; set; }

        public ADSettings ADConfig { get; set; }
        [Required]
        public ApiSettings API { get; set; }
        [Required]
        public Swagger Swagger { get; set; }
        [Required]
        public List<DbConnectionNode> DbConfig { get; set; }
        public DbConnectionNode DefaultConnection => GetConnectionNode(DefaultDB);

        public DbConnectionNode GetConnectionNode(string key)
        {
            if (string.IsNullOrEmpty(key))
                key = DefaultDB;

            if (DbConfig == null || DbConfig.Count <= 0 || string.IsNullOrEmpty(key))
                return null;

            return DbConfig.FirstOrDefault(x => x.ConnectionNode.Key == key);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class DbConnectionNode
    {
        public ConnectionNode ConnectionNode { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ADSettings
    {
        public bool Enabled { get; set; }
        public string Domain { get; set; }
        public string Path { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ApiSettings
    {
        [Required]
        public string Title { get; set; }
        public string Description { get; set; }
        public ApiContact Contact { get; set; }
        public string TermsOfServiceUrl { get; set; }
        public ApiLicense License { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ApiContact
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Url { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ApiLicense
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Swagger
    {
        [Required]
        public bool Enabled { get; set; }
    }
}
