using Asp.Versioning;
using AutoMapper;
using Collecto.CoreAPI.Helper;
using Collecto.CoreAPI.Models;
using Collecto.CoreAPI.Models.Global;
using Collecto.CoreAPI.Models.Objects;
using Collecto.CoreAPI.Models.Objects.Systems;
using Collecto.CoreAPI.Models.Requests.Setups;
using Collecto.CoreAPI.Models.Requests.Systems;
using Collecto.CoreAPI.Models.Responses;
using Collecto.CoreAPI.Models.Responses.Setups;
using Collecto.CoreAPI.Models.Responses.Systems;
using Collecto.CoreAPI.Services.Contracts.Systems;
using Collecto.CoreAPI.SignalRHub;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.DirectoryServices;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.Versioning;
using System.Security.Claims;
using System.Text;

namespace Collecto.CoreAPI.Controllers.V1
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="service"></param>
    /// <param name="mapper"></param>
    /// <param name="appSettings"></param>
    /// <param name="cache"></param>
    /// <param name="logger"></param>
    /// <param name="hub"></param>
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [ValidateAntiForgeryToken]
    [Route("api/users")]
    [Route("api/v{version:apiVersion}/users")]
    public class UserController(IUserService service, IMapper mapper, IOptions<AppSettings> appSettings, ICollectoCache cache, ILogger<UserController> logger, IHubContext<NotificationHub, INotificationHub> hub) : ControllerBase
    {
        private readonly IMapper _mapper = mapper;
        private readonly ILogger _logger = logger;
        private readonly ICollectoCache _cache = cache;
        private readonly IUserService _service = service;
        private readonly AppSettings _appSettings = appSettings.Value;
        private readonly IHubContext<NotificationHub, INotificationHub> _hub = hub;
        private readonly DateTimeOffset _options = Helper.Helper.CreateCollectoCacheOptions();

        /// <summary>
        /// Authenticates a user using credentials from an SQL Server.
        /// </summary>
        /// <remarks>
        /// Processes the login request and returns a response indicating the success or failure of the authentication.
        /// </remarks>
        /// <param name="request">The login request containing user credentials.</param>
        /// <returns>A response indicating if the login was successful. If successful, ValidUser is true.</returns>
        /// <response code="200">Login successful, returns ValidUser: true and a non-empty UserName.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponse))]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            LoginResponse response = new();

            if (request == null)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Parameter value is null.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            if (string.IsNullOrEmpty(request.LoginId))
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Login ID is required.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            if (string.IsNullOrEmpty(request.Password))
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Password is required.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            if (string.IsNullOrEmpty(request.AppVersion))
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Version is required.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            try
            {
                #region Decrypt LoginID

                request.LoginId = Helper.Helper.DecryptData(secret: _appSettings.CipherSecretKey, data: request.LoginId);
                request.Password = Helper.Helper.DecryptData(secret: _appSettings.CipherSecretKey, data: request.Password);
                request.AppId = Helper.Helper.DecryptData(secret: _appSettings.CipherSecretKey, data: request.AppId);

                #endregion

                bool checkPwd = true;
                #region If AD (Active Directory) authentication is enabled do validate

                if (_appSettings.ADConfig.Enabled)
                {
                    checkPwd = false;
#pragma warning disable CA1416 // Validate platform compatibility
                    int adLoginStatus = GetADLoginStatus(loginId: request.LoginId, password: request.Password);
#pragma warning restore CA1416 // Validate platform compatibility
                    if (adLoginStatus != 1)
                    {
                        response.LoginStatus = LoginStatusEnum.Unsuccessful;
                        response.ReturnStatus = StatusCodes.Status403Forbidden;
                        if (adLoginStatus == 2)
                            response.ReturnMessage.Add("Active Directory User is DISABLED.");
                        else if (adLoginStatus == 3)
                            response.ReturnMessage.Add("Active Directory User's Password has EXPIRED.");
                        else
                            response.ReturnMessage.Add("Active Directory User/Password is INVALID.");

                        return StatusCode(StatusCodes.Status403Forbidden, response);
                    }
                }
                #endregion

                string ipAddress = HttpContext.Connection.GetIpAddress();
                User user = await _service.LoginAsync(loginId: request.LoginId, password: request.Password, appId: request.AppId, appVersion: request.AppVersion, ipAddress: ipAddress, checkPwd: checkPwd);
                if (user != null && user.LoginStatus == LoginStatusEnum.VersionMismatch)
                {
                    response.LoginStatus = LoginStatusEnum.Unsuccessful;
                    response.ReturnStatus = StatusCodes.Status403Forbidden;
                    response.ReturnMessage.Add($"Your current version: {request.AppVersion}, Please update your version: {user.UnsuccessfulMsg}<br/>You can update this version simply Reload/Refresh Browser.");
                    return StatusCode(StatusCodes.Status417ExpectationFailed, response);
                }

                if (user == null || user.Id == 0 || user.LoginStatus == LoginStatusEnum.Unsuccessful)
                {
                    string usm = string.Empty;
                    if (user != null && string.IsNullOrEmpty(user.UnsuccessfulMsg) == false)
                        usm = $" ({user.UnsuccessfulMsg})";

                    response.LoginStatus = LoginStatusEnum.Unsuccessful;
                    response.ReturnStatus = StatusCodes.Status403Forbidden;
                    response.ReturnMessage.Add(checkPwd ? $"Login ID/Password is invalid{usm}." : "You are not Authorised to login into the System");
                    return StatusCode(StatusCodes.Status417ExpectationFailed, response);
                }

                if (user.IsLocked)
                {
                    response.LoginStatus = user.LoginStatus;
                    response.ReturnStatus = StatusCodes.Status403Forbidden;
                    if (user.NextLoginTime.HasValue == false)
                        response.ReturnMessage.Add("You are locked, please contact Head office.");
                    else
                        response.ReturnMessage.Add($"You can Login after {user.NextLoginTime:dd-MMM-yyyy H:mm:ss}");
                    return StatusCode(StatusCodes.Status417ExpectationFailed, response);
                }

                if (user.Status != StatusEnum.Authorized)
                {
                    response.LoginStatus = user.LoginStatus;
                    response.ReturnStatus = StatusCodes.Status403Forbidden;
                    response.ReturnMessage.Add("You are not Authorized to Login into the System, Please contact with System Administrator.");
                    return StatusCode(StatusCodes.Status417ExpectationFailed, response);
                }

                response.ValidUser = (user.LoginStatus == LoginStatusEnum.Success);
                if (response.ValidUser == false)
                {
                    response.LoginStatus = user.LoginStatus;
                    response.ReturnStatus = StatusCodes.Status403Forbidden;
                    response.ReturnMessage.Add("Unknown error.");
                    return StatusCode(StatusCodes.Status417ExpectationFailed, response);
                }

                response = _mapper.Map<LoginResponse>(user);
                response.ValidUser = true;
                response.ReturnStatus = StatusCodes.Status200OK;
                response.PwdChangeRequired = (user.AccessStatus == AccessStatusEnum.FirstTime) || (user.ExpireDate.HasValue && user.ExpireDate.Value.Date < DateTime.Today.Date);

                string userPwd = TransactionManagement.Helper.Global.CipherFunctions.Encrypt(key: _appSettings.PwdSecretKey, data: request.Password);
                byte[] key = Encoding.ASCII.GetBytes(_appSettings.JwtCryptoKey);
                JwtSecurityTokenHandler tokenHandler = new();
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        Helper.Helper.CreateClaim("UserId", $"{user.Id}"),
                        Helper.Helper.CreateClaim("UserType", $"{(short)user.UserType}"),
                        Helper.Helper.CreateClaim("LoginId", user.LoginId),
                        Helper.Helper.CreateClaim("SystemID", $"{user.SystemId}"),
                        Helper.Helper.CreateClaim("SubsystemID", $"{user.SubsystemId}"),
                        Helper.Helper.CreateClaim("UserPwd", userPwd),
                        Helper.Helper.CreateClaim("AuthKey", $"{user.AuthKey}"),
                        Helper.Helper.CreateClaim("BatchEnable", $"{user.BatchEnabled}")
                    }),
                    Expires = DateTime.UtcNow.AddHours(12),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
                string userToken = tokenHandler.WriteToken(token);
                response.Expires = tokenDescriptor.Expires;
                response.AuthenticationToken = userToken;
                response.LoginTime = $"{DateTime.Now:dd-MM-yy H:mm:ss}";

                await HttpContext.Session.SetModulesToSession(value: user.ModuleIds);
                if (user.LoginStatus == LoginStatusEnum.Success)
                {
                    if (user.AuthMethod == AuthenticationMethodEnum.Email && string.IsNullOrEmpty(user.EmailAddress) == false && string.IsNullOrWhiteSpace(user.EmailAddress) == false && string.IsNullOrWhiteSpace(user.AuthValue) == false)
                    {
                        List<string> to = new(user.EmailAddress.Split(separator: ';', options: StringSplitOptions.RemoveEmptyEntries));
                        MailHelper.SendMailMessageAsync(settings: _appSettings, to: to,
                            cc: null, bcc: null, attachments: null, embeddedImages: null, isHtmlBody: false, priority: System.Net.Mail.MailPriority.Normal,
                            subject: "Your OTP", messageBody: string.Format("Your OTP: {0} and is valid for 5 minutes only", user.AuthValue));
                    }
                    else if (user.AuthMethod == AuthenticationMethodEnum.MobileSMS && string.IsNullOrEmpty(user.MobileNo) == false && string.IsNullOrWhiteSpace(user.MobileNo) == false && string.IsNullOrWhiteSpace(user.AuthValue) == false)
                    {
                        MailHelper.SendSMSOrWhatsAppMessage(settings: _appSettings, whatsAppMsg: false, msg: string.Format("Your OTP: {0} and is valid for 5 minutes only", user.AuthValue), mobileNumber: user.MobileNo);
                    }
                }

                if (string.IsNullOrEmpty(_appSettings.HangfireDb) == false)
                {
                    RecurringJob.AddOrUpdate("GetUserProfile", () => _service.GetUserProfileAsync(user.Id), Cron.Minutely);
                }

                if (user.DisallowMultiLogin)
                    await _hub.Clients.All.NotifyUser(userId: user.Id, msgType: 1, ipAddress: ipAddress);
                else
                    await _hub.Clients.All.NotifyUser(userId: user.Id, msgType: 2, ipAddress: ipAddress);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                response.ReturnStatus = StatusCodes.Status500InternalServerError;
                response.ReturnMessage.Add(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("loadMenu")]
        public async Task<IActionResult> LoadMenu([FromQuery] ByUserIdRequest request)
        {
            BooleanResponse response = new() { ReturnStatus = StatusCodes.Status200OK };
            if (request == null)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Parameter value is null.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            try
            {
                MenuItem item = await _service.GetUserPermissionsAsync(userId: request.UserId);
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                response.ReturnStatus = StatusCodes.Status500InternalServerError;
                response.ReturnMessage.Add(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("addUser")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BooleanResponse))]
        public async Task<IActionResult> AddUser([FromBody] NewUserRequest request)
        {
            BooleanResponse response = new() { ReturnStatus = StatusCodes.Status200OK };

            if (request == null)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Parameter value is null.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            bool permitted = await HttpContext.Session.IsPermitted("SDS.1.4.2_1");
            if (permitted == false)
            {
                response.ReturnStatus = StatusCodes.Status403Forbidden;
                response.ReturnMessage.Add("You are not authorize to Add User.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            try
            {
                string ipAddress = HttpContext.Connection.GetIpAddress();
                int createdBy = HttpContext.User.GetClaimValue<int>("UserId");
                response.Value = await _service.AddUserAsync(user: request, ipAddress: ipAddress, createdBy: createdBy); ;
                response.ReturnMessage.Add("User added successfully...");

                //Cache
                _cache.Clear("User");

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                response.ReturnStatus = StatusCodes.Status500InternalServerError;
                response.ReturnMessage.Add(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("editUser")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BooleanResponse))]
        public async Task<IActionResult> EditUser([FromBody] UserRequest request)
        {
            BooleanResponse response = new() { ReturnStatus = StatusCodes.Status200OK };

            if (request == null)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Parameter value is null.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            bool permitted = await HttpContext.Session.IsPermitted("SDS.1.4.2_2");
            if (permitted == false)
            {
                response.ReturnStatus = StatusCodes.Status403Forbidden;
                response.ReturnMessage.Add("You are not authorize to Update User.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            if (request.AuthMethod == AuthenticationMethodEnum.ThirdPartyAuthenticator && (string.IsNullOrEmpty(request.AuthKey) || string.IsNullOrWhiteSpace(request.AuthKey) || request.AuthKey.Length <= 0))
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("For third party Authenticator, Authentication key is required.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            try
            {
                string ipAddress = HttpContext.Connection.GetIpAddress();
                int modifiedBy = HttpContext.User.GetClaimValue<int>("UserId");
                response.Value = await _service.EditUserAsync(user: request, ipAddress: ipAddress, modifiedBy: modifiedBy);
                response.ReturnMessage.Add("User edited successfully...");

                //Cache
                _cache.Clear("User");

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                response.ReturnStatus = StatusCodes.Status500InternalServerError;
                response.ReturnMessage.Add(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("deleteUser")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BooleanResponse))]
        public async Task<IActionResult> DeleteUser([FromBody] ByUserIdRequest request)
        {
            BooleanResponse response = new() { ReturnStatus = StatusCodes.Status200OK };

            if (request == null)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Parameter value is null.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            bool permitted = await HttpContext.Session.IsPermitted("SDS.1.4.2_3");
            if (permitted == false)
            {
                response.ReturnStatus = StatusCodes.Status403Forbidden;
                response.ReturnMessage.Add("You are not authorize to Delete User.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            try
            {
                int deletedBy = HttpContext.User.GetClaimValue<int>("UserId");
                response.Value = await _service.DeleteUserAsync(userId: request.UserId, deletedBy: deletedBy);
                response.ReturnMessage.Add("User deleted successfully...");

                //Cache
                _cache.Clear("User");

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                response.ReturnStatus = StatusCodes.Status500InternalServerError;
                response.ReturnMessage.Add(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("unlockUser")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BooleanResponse))]
        public async Task<IActionResult> UnlockUser([FromBody] UserUnlockRequest request)
        {
            BooleanResponse response = new() { ReturnStatus = StatusCodes.Status200OK };

            if (request == null)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Parameter value is null.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            bool permitted = await HttpContext.Session.IsPermitted("SDS.1.4.2_2");
            if (permitted == false)
            {
                response.ReturnStatus = StatusCodes.Status403Forbidden;
                response.ReturnMessage.Add("You are not authorize to Unlock User.");
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            try
            {
                int unlockedBy = HttpContext.User.GetClaimValue<int>("UserId");
                response.Value = await _service.UnlockUserAsync(userId: request.UserId, loginId: request.LoginId, unlockedBy: unlockedBy);
                response.ReturnMessage.Add("User Unlocked successfully...");

                //Cache
                _cache.Clear("User");

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                response.ReturnStatus = StatusCodes.Status500InternalServerError;
                response.ReturnMessage.Add(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [IgnoreAntiforgeryToken]
        [HttpPost("validateOtp")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BooleanResponse))]
        public async Task<IActionResult> ValidateOtpAsync([FromBody] OtpValidationRequest request)
        {
            BooleanResponse response = new() { Value = false, ReturnStatus = StatusCodes.Status200OK };
            if (request == null)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Parameter value is null.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            if (string.IsNullOrEmpty(request.OtpCode) || request.OtpCode.Length != 6)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Otp must be 6 digit.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            try
            {
                if (request.AuthMethod == AuthenticationMethodEnum.ThirdPartyAuthenticator)
                {
                    string secretKey = HttpContext.User.GetClaimValue<string>("AuthKey");
                    if (string.IsNullOrEmpty(secretKey))
                    {
                        response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                        response.ReturnMessage.Add("Authentication key is required.");
                        return StatusCode(StatusCodes.Status417ExpectationFailed, response);
                    }

                    TOtpService otp = new();
                    DateTime now = DateTime.UtcNow;
                    response.Value = otp.ValidateTwoFactorPIN(secretKey, request.OtpCode, now);
                }
                else
                {
                    response.Value = await _service.ValidateAuthValueAsync(request.OtpCode, request.UserId);
                }

                if (response.Value == false)
                {
                    response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                    response.ReturnMessage.Add("This is not a valid Otp.");
                    return StatusCode(StatusCodes.Status417ExpectationFailed, response);
                }
                else
                {
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                response.ReturnStatus = StatusCodes.Status406NotAcceptable;
                response.ReturnMessage.Add(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("resetPassword")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BooleanResponse))]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            BooleanResponse response = new() { ReturnStatus = StatusCodes.Status200OK };
            if (request == null)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Parameter value is null.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            if (request.UserId == 0)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("User is not valid.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            if (string.IsNullOrEmpty(request.Password) || string.IsNullOrWhiteSpace(request.Password))
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Invalid parameter value Password.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            if (string.IsNullOrEmpty(request.ConfirmPassword) || string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Invalid parameter value Confirm Password.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            if (request.Password.Equals(request.ConfirmPassword) == false)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("New password and confirm password are not same.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            bool permitted = await HttpContext.Session.IsPermitted("SDS.1.4.2_2");
            if (permitted == false)
            {
                response.ReturnStatus = StatusCodes.Status403Forbidden;
                response.ReturnMessage.Add("You are not authorize to Reset Password.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            try
            {
                string ipAddress = HttpContext.Connection.GetIpAddress();
                int changedBy = HttpContext.User.GetClaimValue<int>("UserId");
                response.Value = await _service.ResetPasswordAsync(userId: request.UserId, newPassword: request.ConfirmPassword, ipAddress: ipAddress, changedBy: changedBy);
                response.ReturnMessage.Add("Password Reset successfully, User must change password at next Login.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                response.ReturnStatus = StatusCodes.Status500InternalServerError;
                response.ReturnMessage.Add(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [IgnoreAntiforgeryToken]
        [HttpPost("changePassword")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BooleanResponse))]
        public async Task<IActionResult> ChangePassword([FromBody] PasswordChangeRequest request)
        {
            BooleanResponse response = new() { ReturnStatus = StatusCodes.Status200OK };
            if (request == null)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Parameter value is null.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            if (request.UserId == 0)
            {
                response.ReturnStatus = StatusCodes.Status403Forbidden;
                response.ReturnMessage.Add("Your not a valid user.");
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            if (string.IsNullOrEmpty(request.OldPassword) || string.IsNullOrWhiteSpace(request.OldPassword))
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Invalid parameter value Old Password.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            if (string.IsNullOrEmpty(request.Password) || string.IsNullOrWhiteSpace(request.Password))
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Invalid parameter value Password.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            if (string.IsNullOrEmpty(request.ConfirmPassword) || string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Invalid parameter value Confirm Password.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            if (request.Password.Equals(request.ConfirmPassword) == false)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("New password and confirm password are not same.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            try
            {
                string ipAddress = HttpContext.Connection.GetIpAddress();
                int changedBy = HttpContext.User.GetClaimValue<int>("UserId");
                response.Value = await _service.ChangePasswordAsync(userId: request.UserId, oldPassword: request.OldPassword, newPassword: request.ConfirmPassword, ipAddress: ipAddress, changedBy: changedBy);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                response.ReturnStatus = StatusCodes.Status500InternalServerError;
                response.ReturnMessage.Add(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("updateMyPassword")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BooleanResponse))]
        public async Task<IActionResult> UpdateMyPassword([FromBody] PasswordChangeRequest request)
        {
            BooleanResponse response = new() { ReturnStatus = StatusCodes.Status200OK };
            if (request == null)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Parameter value is null.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            if (string.IsNullOrEmpty(request.OldPassword) || string.IsNullOrWhiteSpace(request.OldPassword))
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Invalid parameter value Old Password.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            if (string.IsNullOrEmpty(request.Password) || string.IsNullOrWhiteSpace(request.Password))
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Invalid parameter value Password.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            if (string.IsNullOrEmpty(request.ConfirmPassword) || string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Invalid parameter value Confirm Password.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            if (request.Password.Equals(request.ConfirmPassword) == false)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("New password and confirm password are not same.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            try
            {
                string ipAddress = HttpContext.Connection.GetIpAddress();
                int userId = HttpContext.User.GetClaimValue<int>("UserId");
                response.Value = await _service.ChangePasswordAsync(userId: userId, oldPassword: request.OldPassword, newPassword: request.ConfirmPassword, ipAddress: ipAddress, changedBy: userId);
                response.ReturnMessage.Add("Password changed successfully.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                response.ReturnStatus = StatusCodes.Status500InternalServerError;
                response.ReturnMessage.Add(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// Retrieves a list of users based on specified search criteria.
        /// </summary>
        /// <returns>
        /// A list of up to 50 users that match the search criteria.
        /// </returns>
        /// <response code="200">Returns the top 50 users matching the search criteria.</response>
        /// <response code="204">No users found matching the search criteria.</response>
        [HttpPost("getUsers")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserSearchResponse))]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(UserSearchResponse))]
        public async Task<IActionResult> GetUsers([FromBody] UserSearchRequest request)
        {
            UserSearchResponse response = new() { ReturnStatus = StatusCodes.Status200OK };
            if (request == null)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Parameter value is null.");
                return BadRequest(response);
            }

            try
            {
                int userId = HttpContext.User.GetClaimValue<int>("UserId");
                short usrTp = Convert.ToInt16(HttpContext.User.GetClaimValue<int>("UserType"));
                if (usrTp <= 0)
                    usrTp = 1;

                UserTypeEnum userType = (UserTypeEnum)usrTp;
                string key = $"GetUsers~{request.Criteria}~{request.Status}~{request.SortField}~{request.SortOrder}~{request.Skip}~{request.PageSize}~{userType}~{userId}";
                if (_cache.TryGetValue(key: key, value: out response) == false)
                {
                    response = await _service.GetUsersAsync(request: request, userType: userType);

                    //Cache
                    _ = _cache.Set(key: key, value: response, options: _options);
                }

                response.ReturnStatus = StatusCodes.Status200OK;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                response.ReturnStatus = StatusCodes.Status500InternalServerError;
                response.ReturnMessage.Add(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet("getUsersForForceLogout")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserForceLogoutResponse))]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(UserForceLogoutResponse))]
        public async Task<IActionResult> GetForceLogoutUsers()
        {
            UserForceLogoutResponse response = new() { ReturnStatus = StatusCodes.Status200OK };
            try
            {
                int userId = HttpContext.User.GetClaimValue<int>("UserId");
                response = await _service.GetForceLogoutUsersAsync(exceptUserId: userId);

                response.ReturnStatus = StatusCodes.Status200OK;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                response.ReturnStatus = StatusCodes.Status500InternalServerError;
                response.ReturnMessage.Add(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpPost("forceLogoutNow")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BooleanResponse))]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(BooleanResponse))]
        public async Task<IActionResult> ForceLogoutNow([FromBody] ForceUserLogoutRequest request)
        {
            BooleanResponse response = new() { ReturnStatus = StatusCodes.Status200OK };
            if (request == null || request.UserIds == null || request.UserIds.Count <= 0)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Parameter value is null/no User was selected.");
                return BadRequest(response);
            }

            try
            {
                string ipAddress = HttpContext.Connection.GetIpAddress();
                response.Value = await _service.ForceLogoutNowAsync(userIds: request.UserIds, ipAddress: ipAddress);
                foreach (int userId in request.UserIds)
                {
                    await _hub.Clients.All.NotifyUser(userId: userId, msgType: 1, ipAddress: ipAddress);
                }
                response.ReturnMessage.Add("Process completed successfully...");
                response.ReturnStatus = StatusCodes.Status200OK;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                response.ReturnStatus = StatusCodes.Status500InternalServerError;
                response.ReturnMessage.Add(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("getUser")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserGetResponse))]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(UserGetResponse))]
        public async Task<IActionResult> GetUser([FromQuery] ByUserIdRequest request)
        {
            UserGetResponse response = new() { ReturnStatus = StatusCodes.Status200OK };
            if (request == null)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Parameter value is null.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            try
            {
                string key = $"GetUser~{request.UserId}";
                if (_cache.TryGetValue(key: key, value: out response) == false)
                {
                    response = await _service.GetUserAsync(userId: request.UserId);

                    //Cache
                    _ = _cache.Set(key: key, value: response, options: _options);
                }
                response.ReturnStatus = StatusCodes.Status200OK;
                return Ok(response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                response.ReturnStatus = StatusCodes.Status500InternalServerError;
                response.ReturnMessage.Add(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet("getCurrentUser")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserGetResponse))]
        public async Task<IActionResult> GetCurrentUser()
        {
            UserGetResponse response = new() { ReturnStatus = StatusCodes.Status200OK };
            try
            {
                int userId = HttpContext.User.GetClaimValue<int>("UserId");

                string key = $"GetUser~{userId}";
                if (_cache.TryGetValue(key: key, value: out response) == false)
                {
                    response = await _service.GetUserAsync(userId: userId);
                    response.ReturnStatus = StatusCodes.Status200OK;

                    //Cache
                    _ = _cache.Set(key: key, value: response, options: _options);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                response.ReturnStatus = StatusCodes.Status500InternalServerError;
                response.ReturnMessage.Add(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet("getMyProfile")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserProfileResponse))]
        public async Task<IActionResult> GetMyProfile()
        {
            UserProfileResponse response = new() { ReturnStatus = StatusCodes.Status200OK };
            try
            {
                int userId = HttpContext.User.GetClaimValue<int>("UserId");
                response = await _service.GetUserProfileAsync(userId: userId);
                response.ReturnStatus = StatusCodes.Status200OK;

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                response.ReturnStatus = StatusCodes.Status500InternalServerError;
                response.ReturnMessage.Add(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [HttpPost("logOut")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task LogOut([FromBody] LogoutRequest request)
        {
            try
            {
                string ipAddress = HttpContext.Connection.GetIpAddress();
                int userId = HttpContext.User.GetClaimValue<int>("UserId");

                HttpContext.Session.Clear();
                await HttpContext.Session.CommitAsync();

                _ = await _service.LogoutAsync(ipAddress: ipAddress, userId: userId, logId: request.LogId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>0
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost("sessionExpired")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task SessionExpired([FromBody] LogoutRequest request)
        {
            try
            {
                string ipAddress = HttpContext.Connection.GetIpAddress();
                int userId = HttpContext.User.GetClaimValue<int>("UserId");

                HttpContext.Session.Clear();
                _ = await _service.LogoutAsync(ipAddress: ipAddress, userId: userId, logId: request.LogId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("sendQrCodeViaEmail")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BooleanResponse))]
        public IActionResult SendQrCodeViaEmail([FromForm] QRCodeUploadRequest request)
        {
            BooleanResponse response = new() { ReturnStatus = StatusCodes.Status200OK };
            try
            {
                if (request == null)
                {
                    response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                    response.ReturnMessage.Add("Parameter value is null.");
                    return StatusCode(StatusCodes.Status417ExpectationFailed, response);
                }

                if (string.IsNullOrEmpty(request.EmailAddress))
                {
                    response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                    response.ReturnMessage.Add("There is no email address to send mail.");
                    return StatusCode(StatusCodes.Status417ExpectationFailed, response);
                }

                if (string.IsNullOrEmpty(request.FileName))
                {
                    response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                    response.ReturnMessage.Add("There is no image to send to send mail.");
                    return StatusCode(StatusCodes.Status417ExpectationFailed, response);
                }

                if (request.FileData.Length <= 0)
                {
                    response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                    response.ReturnMessage.Add("There is no image to send to send mail.");
                    return StatusCode(StatusCodes.Status417ExpectationFailed, response);
                }

                var fileSpec = Path.Combine(_appSettings.UploadFolder, request.FileName);
                if (System.IO.File.Exists(fileSpec))
                    System.IO.File.Delete(fileSpec);

                using (var stream = new FileStream(fileSpec, FileMode.Create))
                {
                    request.FileData.CopyTo(stream);
                }

                bool sent = MailHelper.SendMailMessage(settings: _appSettings, to: new List<string> { request.EmailAddress },
                       cc: null, bcc: null, attachments: new List<string> { fileSpec }, embeddedImages: null, isHtmlBody: false,
                       priority: System.Net.Mail.MailPriority.High, subject: "QR Code", messageBody: "Scan image");

                if (sent)
                {
                    if (System.IO.File.Exists(fileSpec))
                        System.IO.File.Delete(fileSpec);

                    response.Value = sent;
                    response.ReturnMessage.Add($"Successfully mail sent to {request.EmailAddress}");
                    return Ok(response);
                }
                else
                {
                    response.Value = sent;
                    response.ReturnMessage.Add($"Cannot send mail to {request.EmailAddress}");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity, response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                response.ReturnStatus = StatusCodes.Status500InternalServerError;
                response.ReturnMessage.Add(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("uploadProfileImage")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BooleanResponse))]
        public IActionResult UploadProfileImage([FromForm] FileUploadRequest request)
        {
            BooleanResponse response = new() { ReturnStatus = StatusCodes.Status200OK };
            try
            {
                if (request == null)
                {
                    response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                    response.ReturnMessage.Add("Parameter value is null.");
                    return StatusCode(StatusCodes.Status417ExpectationFailed, response);
                }

                if (string.IsNullOrEmpty(request.FileName))
                {
                    response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                    response.ReturnMessage.Add("There is no Image to set Profile image.");
                    return StatusCode(StatusCodes.Status417ExpectationFailed, response);
                }

                if (request.FileData.Length <= 0)
                {
                    response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                    response.ReturnMessage.Add("There is no image to set Profile image.");
                    return StatusCode(StatusCodes.Status417ExpectationFailed, response);
                }

                long maxSz = 20 * 1024;
                if (request.FileData.Length > maxSz)
                {
                    response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                    response.ReturnMessage.Add("Maximum size allowed is 20 Kb");
                    return StatusCode(StatusCodes.Status417ExpectationFailed, response);
                }

                var fileSpec = Path.Combine(_appSettings.ProfileImageFolder, request.FileName);
                if (System.IO.File.Exists(fileSpec))
                    System.IO.File.Delete(fileSpec);

                using (var stream = new FileStream(fileSpec, FileMode.Create))
                {
                    request.FileData.CopyTo(stream);
                }
                response.Value = true;
                response.ReturnMessage.Add("Refresh page to view your profile image.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                response.ReturnStatus = StatusCodes.Status500InternalServerError;
                response.ReturnMessage.Add(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("sendPassword")]
        [AllowAnonymous, IgnoreAntiforgeryToken]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BooleanResponse))]
        public async Task<IActionResult> SendPassword([FromBody] SendPasswordRequest request)
        {
            BooleanResponse response = new() { ReturnStatus = StatusCodes.Status200OK };
            if (request == null)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Parameter value is null.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            if (string.IsNullOrEmpty(request.UserId))
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("User Id is required.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            if (string.IsNullOrEmpty(request.MobileNo))
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Mobile number is required.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            if (string.IsNullOrEmpty(request.EmailAddress))
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Email address is required.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            try
            {
                const string key = "Set_2024_Collecto_#key";
                string decipherValue = Helper.Helper.DecryptData(secret: key, data: request.UserId);
                if (string.IsNullOrEmpty(decipherValue))
                {
                    response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                    response.ReturnMessage.Add("User Id is required.");
                    return StatusCode(StatusCodes.Status417ExpectationFailed, response);
                }
                if (int.TryParse(decipherValue, out int userId) == false)
                {
                    response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                    response.ReturnMessage.Add("User Id is required.");
                    return StatusCode(StatusCodes.Status417ExpectationFailed, response);
                }
                if (userId == 0)
                {
                    response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                    response.ReturnMessage.Add("User Id is required.");
                    return StatusCode(StatusCodes.Status417ExpectationFailed, response);
                }

                request.MobileNo = Helper.Helper.DecryptData(secret: key, data: request.MobileNo);
                request.EmailAddress = Helper.Helper.DecryptData(secret: key, data: request.EmailAddress);
                if (string.IsNullOrEmpty(request.MobileNo) && string.IsNullOrEmpty(request.EmailAddress))
                {
                    response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                    response.ReturnMessage.Add("Mobile number and Email address both cannot be empty.");
                    return StatusCode(StatusCodes.Status417ExpectationFailed, response);
                }

                //Do reset password
                string newPassword = $"{new Random().Next(100000, 999999)}";
                string ipAddress = HttpContext.Connection.GetIpAddress();
                response.Value = await _service.SendPasswordAsync(userId: userId, newPassword: newPassword, ipAddress: ipAddress);
                if (response.Value)
                {
                    if (string.IsNullOrEmpty(request.EmailAddress) == false && string.IsNullOrWhiteSpace(request.EmailAddress) == false)
                    {
                        List<string> to = new(request.EmailAddress.Split(separator: ';', options: StringSplitOptions.RemoveEmptyEntries));
                        MailHelper.SendMailMessageAsync(settings: _appSettings, to: to, cc: null, bcc: null, attachments: null, embeddedImages: null, isHtmlBody: false, priority: System.Net.Mail.MailPriority.Normal,
                            subject: "One Time Password", messageBody: $"Your one time password: {newPassword} and must change password at next Login.");
                    }
                    if (string.IsNullOrEmpty(request.MobileNo) == false && string.IsNullOrWhiteSpace(request.MobileNo) == false)
                    {
                        MailHelper.SendSMSOrWhatsAppMessage(settings: _appSettings, whatsAppMsg: false, msg: $"Your one time password: {newPassword} and must change password at next Login.", mobileNumber: request.MobileNo);
                    }

                    response.ReturnMessage.Add("Password sent to your Email address and/or Mobile number, User must change password at next Login.");
                }
                else
                {
                    response.ReturnMessage.Add("Cannot do action on your request.");
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                response.ReturnStatus = StatusCodes.Status500InternalServerError;
                response.ReturnMessage.Add(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// Validate platform compatibility
        /// </summary>
        /// <param name="loginId"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [SupportedOSPlatform("windows")]
        private int GetADLoginStatus(string loginId, string password)
        {
            try
            {
                const string displayNameAttribute = "DisplayName";
                const string samAccountNameAttribute = "SAMAccountName";
                const string userAccountControlAttribute = "useraccountcontrol";

                string username = (string.IsNullOrEmpty(_appSettings.ADConfig.Domain) || string.IsNullOrWhiteSpace(_appSettings.ADConfig.Domain)) ? loginId : $"{loginId}@{_appSettings.ADConfig.Domain}";
                using DirectoryEntry entry = new(path: _appSettings.ADConfig.Path, username: username, password: password);
                using DirectorySearcher searcher = new(searchRoot: entry);
                searcher.Filter = $"({samAccountNameAttribute}={loginId})";
                searcher.PropertiesToLoad.Add(value: displayNameAttribute);
                searcher.PropertiesToLoad.Add(value: samAccountNameAttribute);
                searcher.PropertiesToLoad.Add(value: userAccountControlAttribute);
                var result = searcher.FindOne();
                if (result == null)
                    return 0;

                ResultPropertyValueCollection displayName = result.Properties[name: displayNameAttribute];
                ResultPropertyValueCollection samAccountName = result.Properties[name: samAccountNameAttribute];
                ResultPropertyValueCollection userAccountControl = result.Properties[name: userAccountControlAttribute];
                int uacFlag = (userAccountControl != null && userAccountControl.Count > 0) ? Convert.ToInt32(userAccountControl[0]) : 0;
                if ((uacFlag & 0x000002) == 0x000002)       //Disabled
                    return 2;
                else if ((uacFlag & 0x800000) == 0x800000)  //Password expired
                    return 3;

                if (displayName != null && displayName.Count > 0 && samAccountName != null && samAccountName.Count > 0)
                    return 1;
                else
                    return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                return 0;
            }
        }
    }
}
