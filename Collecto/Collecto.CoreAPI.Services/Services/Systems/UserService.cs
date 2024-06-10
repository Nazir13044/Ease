using Collecto.CoreAPI.Models;
using Collecto.CoreAPI.Models.Global;
using Collecto.CoreAPI.Models.Objects;
using Collecto.CoreAPI.Models.Objects.Systems;
using Collecto.CoreAPI.Models.Requests.Systems;
using Collecto.CoreAPI.Models.Responses.Setups;
using Collecto.CoreAPI.Models.Responses.Systems;
using Collecto.CoreAPI.Services.Contracts.Systems;
using Collecto.CoreAPI.TransactionManagement.DataAccess;
using Collecto.CoreAPI.TransactionManagement.DataAccess.SQL;
using Collecto.CoreAPI.TransactionManagement.Helper;
using Microsoft.Extensions.Options;
using System.Data;
using System.Data.SqlClient;

namespace Collecto.CoreAPI.Services.Services.Systems
{
    public class UserService(IOptions<AppSettings> settings, IOptions<MenuSettings> menuSettings) : IUserService
    {
        private readonly AppSettings _settings = settings.Value;
        private readonly MenuSettings _menuSettings = menuSettings.Value;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loginId"></param>
        /// <param name="password"></param>
        /// <param name="appId"></param>
        /// <param name="appVersion"></param>
        /// <param name="ipAddress"></param>
        /// <param name="checkPwd"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<User> LoginAsync(string loginId, string password, string appId, string appVersion, string ipAddress, bool checkPwd)
        {
            User user = new() { LoginId = loginId, LoginStatus = LoginStatusEnum.Unsuccessful };
            try
            {
                password = EncryptPassword(password: password);

                using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode, true);
                try
                {
                    bool batchEnabled = false;
                    DateTime sysDate = DateTime.Today.Date;
                    string appVer = string.Empty, alParams = string.Empty;
                    int maxTryCount = 5, lockTime = 1, marMonths = 24, idleTime = 0, timeoutTime = 0, pingTime = 0;

                    #region Read Params from ThisSystem

                    using (IDataReader dr = tc.ExecuteReader("SELECT CAST(GETDATE() as date), AppVersion FROM ThisSystem"))
                    {
                        if (dr.Read())
                        {
                            sysDate = dr.GetDateTime(0);
                            appVer = dr.GetString(1);
                        }
                        dr.Close();
                    }

                    if (string.IsNullOrEmpty(alParams) == false)
                    {
                        string[] times = alParams.Split(separator: ',', options: StringSplitOptions.RemoveEmptyEntries);
                        if (times.Length == 3)
                        {
                            if (int.TryParse(times[0], out idleTime) == false)
                                idleTime = 0;

                            if (int.TryParse(times[1], out timeoutTime) == false)
                                timeoutTime = 0;

                            if (int.TryParse(times[2], out pingTime) == false)
                                pingTime = 0;
                        }
                    }

                    if (loginId.ToLower().Equals(User.SuperUser_LoginId) == false && appVersion.Equals(appVer) == false)
                    {
                        user.UnsuccessfulMsg = appVer;
                        user.LoginStatus = LoginStatusEnum.VersionMismatch;
                    }

                    #endregion

                    if (user.LoginStatus != LoginStatusEnum.VersionMismatch)
                    {
                        #region Read User data using authentication data

                        string commandText;
                        if (checkPwd == false)
                        {
                            commandText = SQLParser.MakeSQL("SELECT UserId, UserName, UserType, Status, MobileNo, EmailAddress, AuthReqAtlogin, AuthMethod,"
                              + " AuthKey, AppId, AccessStatus, NeverExpire, ISNULL(LastPasswords,''), LastPassChgDate, ExpireDate, ISNULL(ThemeName,'yellow'),"
                              + " ISNULL(SchemeName,'dark'), ISNULL(MenuLayout,'static'), ISNULL(IsLocked,0), NextLoginTime, SystemID, SalesPointID, SubsystemID,"
                              + " DBOnStartup, DAMultiLogin FROM Users WHERE LoginID=%s", loginId);
                        }
                        else
                        {
                            commandText = SQLParser.MakeSQL("SELECT UserId, UserName, UserType, Status, EmailAddress, AccessStatus, ISNULL(LastPasswords,''), LastPassChgDate,"
                                + " ExpireDate, SystemID, SalesPointID, SubsystemID, DBOnStartup FROM Users WHERE LoginID=%s AND Password=%s", loginId, password);
                        }

                        using (IDataReader dr = tc.ExecuteReader(commandText: commandText))
                        {
                            if (dr.Read())
                            {
                                user = new User
                                {
                                    Id = dr.GetInt32(0),
                                    UserName = dr.GetString(1),
                                    UserType = (UserTypeEnum)dr.GetInt16(2),
                                    Status = (StatusEnum)dr.GetInt16(3),
                                    EmailAddress = dr.GetString(4),
                                    AccessStatus = (AccessStatusEnum)dr.GetInt16(5),
                                    LastPasswords = dr.IsDBNull(6) ? string.Empty : dr.GetString(6),
                                    LastPassChgDate = dr.IsDBNull(7) ? null : dr.GetDateTime(7),
                                    ExpireDate = dr.IsDBNull(8) ? null : dr.GetDateTime(8),
                                    SystemId = dr.IsDBNull(9) ? null : dr.GetInt32(9),
                                    SalespointId = dr.IsDBNull(10) ? null : dr.GetInt32(10),
                                    SubsystemId = dr.IsDBNull(11) ? null : dr.GetInt32(11),
                                    DbOnStartup = dr.GetInt16(12) != 0,

                                    LoginId = loginId,
                                    IdleTime = idleTime,
                                    PingTime = pingTime,
                                    SystemDate = sysDate,
                                    TimeoutTime = timeoutTime,
                                    BatchEnabled = batchEnabled,
                                    LoginStatus = LoginStatusEnum.Success
                                };
                            }
                            dr.Close();

                            user.MinReportDate = (marMonths <= 0) ? new DateTime(2024, 1, 1) : sysDate.AddMonths(-1 * marMonths);
                        }

                        #endregion

                        #region If the user was locked, try set set unlock if Time expired

                        if (loginId.ToLower().Equals(User.SuperUser_LoginId) == false && user.IsLocked)
                        {
                            int isSuccessful = 0;
                            SqlParameter[] p =
                            [
                                SqlHelperExtension.CreateInParam(pName: "@LoginId", pType: SqlDbType.VarChar, pValue: loginId, size: 30),
                                SqlHelperExtension.CreateInParam(pName: "@LockTime", pType: SqlDbType.Int, pValue: lockTime),
                                SqlHelperExtension.CreateOutParam(pName: "@IsSuccessful", pType: SqlDbType.Int, pValue: isSuccessful),
                            ];
                            _ = tc.ExecuteNonQuerySp(spName: "dbo.DoUnlockUser", parameterValues: p);
                            if (p[2] != null && p[2].Value != null && p[2].Value != DBNull.Value)
                                isSuccessful = Convert.ToInt32(p[2].Value);
                            p = null;

                            if (isSuccessful == 1)
                                user.IsLocked = false;
                        }

                        #endregion

                        #region Keep log for unauthrise access and Set user lock if exceeds max try

                        if (loginId.ToLower().Equals(User.SuperUser_LoginId) == false && maxTryCount > 0 && user.LoginStatus == LoginStatusEnum.Unsuccessful)
                        {
                            int remaingTry = 0;
                            DateTime? nextLoginTime = null;
                            string tryLoginInfo = $"{loginId}~{password}~13";
                            SqlParameter[] p =
                            [
                                SqlHelperExtension.CreateInParam(pName: "@LoginId", pType: SqlDbType.VarChar, pValue: loginId, size: 30),
                                SqlHelperExtension.CreateInParam(pName: "@TryLoginInfo", pType: SqlDbType.VarChar, pValue: tryLoginInfo, size: 100),
                                SqlHelperExtension.CreateInParam(pName: "@IpAddress", pType: SqlDbType.VarChar, pValue: ipAddress, size: 20),
                                SqlHelperExtension.CreateInParam(pName: "@MaxTryCount", pType: SqlDbType.SmallInt, pValue: maxTryCount),
                                SqlHelperExtension.CreateInParam(pName: "@LockTime", pType: SqlDbType.Int, pValue: lockTime),
                                SqlHelperExtension.CreateOutParam(pName: "@RemaingTry", pType: SqlDbType.SmallInt, pValue: remaingTry),
                                SqlHelperExtension.CreateOutParam(pName: "@NextLoginTime", pType: SqlDbType.DateTime, pValue: nextLoginTime),
                            ];
                            _ = tc.ExecuteNonQuerySp(spName: "dbo.LogUnathoriseAccess", parameterValues: p);
                            if (p[5] != null && p[5].Value != null && p[5].Value != DBNull.Value)
                                remaingTry = Convert.ToInt32(p[5].Value);

                            if (p[6] != null && p[6].Value != null && p[6].Value != DBNull.Value)
                                nextLoginTime = Convert.ToDateTime(p[6].Value);

                            if (remaingTry <= 0)
                            {
                                user.IsLocked = true;
                                if (lockTime <= 0)
                                {
                                    user.NextLoginTime = nextLoginTime;
                                    user.UnsuccessfulMsg = $"Please contact with Head office to unlock";
                                }
                                else
                                {
                                    user.NextLoginTime = nextLoginTime;
                                    user.UnsuccessfulMsg = $"You can Login after {user.NextLoginTime:dd-MMM-yyyy H:mm:ss}";
                                }
                            }
                            else
                            {
                                user.UnsuccessfulMsg = $"{remaingTry} more attempt{(remaingTry > 1 ? "s" : "")} remaining";
                            }
                            p = null;
                        }

                        #endregion

                        #region Generate and Save Otp if Otp is enabled and send thru SMS/Email

                        if (user.LoginStatus == LoginStatusEnum.Success && user.Status == StatusEnum.Authorized && (user.AuthMethod == AuthenticationMethodEnum.Email || user.AuthMethod == AuthenticationMethodEnum.MobileSMS))
                        {
                            TOtpService otp = new();
                            string secretKey = $"{user.Id}~{user.LoginId}";
                            secretKey = Base32Encoding.EncodeAsBase32String(input: secretKey, addPadding: false);
                            user.AuthValue = TOtpService.GetCurrentPIN(secretKey: secretKey);
                            SetAuthValue(tc: tc, authValue: user.AuthValue, validMinutes: 5, userId: user.Id);
                        }

                        #endregion

                        #region If login successful and user is active read module id for this user 

                        if (user.LoginStatus == LoginStatusEnum.Success && user.Status == StatusEnum.Authorized && user.IsLocked == false)
                        {
                            int logId = 0;
                            SqlParameter[] p =
                            [
                                SqlHelperExtension.CreateInParam(pName: "@UserId", pType: SqlDbType.Int, pValue: user.Id),
                                SqlHelperExtension.CreateInParam(pName: "@IpAddress", pType: SqlDbType.VarChar, pValue: ipAddress, size: 20),
                                SqlHelperExtension.CreateInParam(pName: "@AppId", pType: SqlDbType.VarChar, pValue: appId, size: 250),
                                SqlHelperExtension.CreateInParam(pName: "@LoginId", pType: SqlDbType.VarChar, pValue: user.LoginId, size: 30),
                                SqlHelperExtension.CreateInParam(pName: "@LockTime", pType: SqlDbType.Int, pValue: lockTime)
                            ];
                            using (IDataReader dr = tc.ExecuteReaderSp(spName: "dbo.GetPermissionKeys", parameterValues: p))
                            {
                                user.ModuleIds = [];
                                while (dr.Read())
                                {
                                    string moduleId = dr.GetString(0);
                                    user.ModuleIds.Add(moduleId);

                                    logId = dr.GetInt32(1);

                                    bool retValue = dr.GetInt16(2) != 0;
                                    if (retValue)
                                        user.ModuleIds.Add($"{moduleId}_1");

                                    retValue = dr.GetInt16(3) != 0;
                                    if (retValue)
                                        user.ModuleIds.Add($"{moduleId}_2");

                                    retValue = dr.GetInt16(4) != 0;
                                    if (retValue)
                                        user.ModuleIds.Add($"{moduleId}_3");
                                }
                                dr.Close();
                            }
                            user.LogId = logId;
                        }

                        #endregion
                    }

                    tc.End();
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

            return user;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="ipAddress"></param>
        /// <param name="createdBy"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> AddUserAsync(NewUserRequest user, string ipAddress, int createdBy)
        {
            bool returnValue;
            try
            {
                #region Password Policy if Does not change password on next login

                if (user.AccessStatus != AccessStatusEnum.FirstTime)
                {
                    ReadPwdParams(enfStgPwd: out bool enfStgPwd, minLen: out short minLen, maxLen: out short maxLen, reservedWords: out string reservedWords);
                    ValidatePwdPolicies(password: user.Password, minLen: minLen, maxLen: maxLen, enfStgPwd: enfStgPwd, reservedWords: reservedWords);
                }

                #endregion

                int userId = 0;
                user.Password = EncryptPassword(password: user.Password);

                if (string.IsNullOrEmpty(user.EmailAddress) == false)
                    user.EmailAddress = user.EmailAddress.Replace(" ", "");

                using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode, true);
                try
                {
                    SqlParameter[] p =
                    [
                        SqlHelperExtension.CreateInParam(pName: "@LoginId", pType: SqlDbType.VarChar, pValue: user.LoginId, size: 30),
                        SqlHelperExtension.CreateInParam(pName: "@UserName", pType: SqlDbType.VarChar, pValue: user.UserName, size: 75),
                        SqlHelperExtension.CreateInParam(pName: "@Password", pType: SqlDbType.VarChar, pValue: user.Password, size: 65),
                        SqlHelperExtension.CreateInParam(pName: "@MobileNo", pType: SqlDbType.VarChar, pValue: user.MobileNo, size: 50),
                        SqlHelperExtension.CreateInParam(pName: "@EmailAddress", pType: SqlDbType.VarChar, pValue: user.EmailAddress ?? string.Empty, size: 50),
                        SqlHelperExtension.CreateInParam(pName: "@AuthReqAtlogin", pType: SqlDbType.SmallInt, pValue: user.AuthReqAtlogin ? 1 : 0),
                        SqlHelperExtension.CreateInParam(pName: "@AuthMethod", pType: SqlDbType.SmallInt, pValue: user.AuthMethod),
                        SqlHelperExtension.CreateInParam(pName: "@DBOnStartup", pType: SqlDbType.SmallInt, pValue: user.DbOnStartup ? 1 : 0),
                        SqlHelperExtension.CreateInParam(pName: "@UserType", pType: SqlDbType.SmallInt, pValue: user.UserType),
                        SqlHelperExtension.CreateInParam(pName: "@Status", pType: SqlDbType.SmallInt, pValue: user.Status),
                        SqlHelperExtension.CreateInParam(pName: "@AccessStatus", pType: SqlDbType.SmallInt, pValue: user.AccessStatus),
                        SqlHelperExtension.CreateInParam(pName: "@NeverExpire", pType: SqlDbType.SmallInt, pValue: user.NeverExpire ? 1 : 0),
                        SqlHelperExtension.CreateInParam(pName: "@DAMultiLogin", pType: SqlDbType.SmallInt, pValue: user.DisallowMultiLogin ? 1 : 0),
                        SqlHelperExtension.CreateInParam(pName: "@IpAddress", pType: SqlDbType.VarChar, pValue: ipAddress, size: 20),
                        SqlHelperExtension.CreateInParam(pName: "@CreatedBy", pType: SqlDbType.Int, pValue: createdBy),
                        SqlHelperExtension.CreateOutParam(pName: "@UserId", pType: SqlDbType.Int, pValue: userId),
                    ];
                    _ = tc.ExecuteNonQuerySp(spName: "dbo.InsertUser", parameterValues: p);
                    if (p[15] != null && p[15].Value != null && p[15].Value != DBNull.Value)
                        userId = Convert.ToInt32(p[15].Value);

                    if (user.GroupIds != null && user.GroupIds.Count > 0 && userId > 0)
                    {
                        string cmdText = string.Empty;
                        foreach (int groupId in user.GroupIds)
                            cmdText += $"INSERT INTO UserGroups(UserId,GroupId) VALUES({userId},{groupId});";

                        _ = tc.ExecuteNonQuery(commandText: cmdText);
                    }
                    p = null;

                    tc.End();

                    returnValue = true;
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

            return returnValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="ipAddress"></param>
        /// <param name="modifiedBy"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> EditUserAsync(UserRequest user, string ipAddress, int modifiedBy)
        {
            bool returnValue = false;
            try
            {
                if (string.IsNullOrEmpty(user.EmailAddress) == false)
                    user.EmailAddress = user.EmailAddress.Replace(" ", "");

                using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode, true);
                try
                {
                    SqlParameter[] p =
                    [
                        SqlHelperExtension.CreateInParam(pName: "@LoginId", pType: SqlDbType.VarChar, pValue: user.LoginId, size: 30),
                        SqlHelperExtension.CreateInParam(pName: "@UserName", pType: SqlDbType.VarChar, pValue: user.UserName, size: 75),
                        SqlHelperExtension.CreateInParam(pName: "@MobileNo", pType: SqlDbType.VarChar, pValue: user.MobileNo, size: 50),
                        SqlHelperExtension.CreateInParam(pName: "@EmailAddress", pType: SqlDbType.VarChar, pValue: user.EmailAddress??string.Empty, size: 50),
                        SqlHelperExtension.CreateInParam(pName: "@AuthReqAtlogin", pType: SqlDbType.SmallInt, pValue: user.AuthReqAtlogin ? 1 : 0),
                        SqlHelperExtension.CreateInParam(pName: "@AuthMethod", pType: SqlDbType.SmallInt, pValue: user.AuthMethod),
                        SqlHelperExtension.CreateInParam(pName: "@AuthKey", pType: SqlDbType.VarChar, pValue: user.AuthKey, size: 100),
                        SqlHelperExtension.CreateInParam(pName: "@DBOnStartup", pType: SqlDbType.SmallInt, pValue: user.DbOnStartup ? 1 : 0),
                        SqlHelperExtension.CreateInParam(pName: "@Status", pType: SqlDbType.SmallInt, pValue: user.Status),
                        SqlHelperExtension.CreateInParam(pName: "@AccessStatus", pType: SqlDbType.SmallInt, pValue: user.AccessStatus),
                        SqlHelperExtension.CreateInParam(pName: "@NeverExpire", pType: SqlDbType.SmallInt, pValue: user.NeverExpire ? 1 : 0),
                        SqlHelperExtension.CreateInParam(pName: "@DAMultiLogin", pType: SqlDbType.SmallInt, pValue: user.DisallowMultiLogin ? 1 : 0),
                        SqlHelperExtension.CreateInParam(pName: "@IpAddress", pType: SqlDbType.VarChar, pValue: ipAddress, size: 20),
                        SqlHelperExtension.CreateInParam(pName: "@ModifiedBy", pType: SqlDbType.Int, pValue: modifiedBy),
                        SqlHelperExtension.CreateInParam(pName: "@UserId", pType: SqlDbType.Int, pValue: user.UserId)
                    ];
                    _ = tc.ExecuteNonQuerySp(spName: "dbo.UpdateUser", parameterValues: p);

                    if (user.GroupIds != null && user.GroupIds.Count > 0 && user.UserId > 0)
                    {
                        string cmdText = string.Empty;
                        foreach (int groupId in user.GroupIds)
                            cmdText += $"INSERT INTO UserGroups(UserId,GroupId) VALUES({user.UserId},{groupId});";

                        _ = tc.ExecuteNonQuery(commandText: cmdText);
                    }
                    p = null;

                    tc.End();

                    returnValue = true;
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

            return returnValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="deletedBy"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> DeleteUserAsync(int userId, int deletedBy)
        {
            bool returnValue;
            try
            {
                using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode, true);
                try
                {
                    SqlParameter[] p =
                    [
                        SqlHelperExtension.CreateInParam(pName: "@DeletedBy", pType: SqlDbType.Int, pValue: deletedBy),
                        SqlHelperExtension.CreateInParam(pName: "@UserId", pType: SqlDbType.Int, pValue: userId)
                    ];
                    _ = tc.ExecuteNonQuerySp(spName: "dbo.DeleteUser", parameterValues: p);
                    p = null;

                    tc.End();

                    returnValue = true;
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

            return returnValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="loginId"></param>
        /// <param name="unlockedBy"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> UnlockUserAsync(int userId, string loginId, int unlockedBy)
        {
            bool returnValue;
            try
            {
                using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode, true);
                try
                {
                    SqlParameter[] p =
                    [
                        SqlHelperExtension.CreateInParam(pName: "@UserId", pType: SqlDbType.Int, pValue: userId),
                        SqlHelperExtension.CreateInParam(pName: "@LoginId", pType: SqlDbType.VarChar, pValue: loginId, size: 30),
                        SqlHelperExtension.CreateInParam(pName: "@UnlockedBy", pType: SqlDbType.Int, pValue: unlockedBy)
                    ];
                    _ = tc.ExecuteNonQuerySp(spName: "dbo.UnlockUser", parameterValues: p);
                    p = null;

                    tc.End();

                    returnValue = true;
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

            return returnValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<FindAccountResponse> FindAccountAsync(string accountId)
        {
            FindAccountResponse response = new();
            try
            {
                using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode);
                try
                {
                    using (IDataReader dr = await tc.ExecuteReaderAsync("SELECT UserID, EmailAddress, MobileNo FROM Users WHERE LoginID=%s OR EmailAddress=%s OR MobileNo=%s", accountId, accountId, accountId))
                    {
                        if (dr.Read())
                        {
                            response.UserId = dr.GetInt32(0);
                            response.EmailAddress = dr.GetString(1);
                            response.PhoneNo = dr.GetString(2);
                            response.Value = true;
                        }
                        dr.Close();
                    }
                    tc.End();

                    if (response.Value == false)
                    {
                        response.ReturnMessage.Add($"[{accountId}] is not a valid Login Id/Email address/Mobile number.");
                    }
                    else
                    {
                        if (response.PhoneNo.Length > 8)
                            response.PhoneNoMasked = string.Concat(response.PhoneNo.AsSpan(0, response.PhoneNo.Length - 8), "*****", response.PhoneNo.AsSpan(response.PhoneNo.Length - 3, 3));

                        int idx = response.EmailAddress.IndexOf('@');
                        if (idx != -1)
                        {
                            string firstPart = response.EmailAddress[..idx];
                            if (firstPart.Length >= 5)
                                firstPart = string.Concat(firstPart.AsSpan(0, firstPart.Length - 5), "*****");

                            response.EmailAddressMasked = string.Concat(firstPart, response.EmailAddress.AsSpan(idx, response.EmailAddress.Length - idx));
                        }
                    }

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
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<UserProfileResponse> GetUserProfileAsync(int userId)
        {
            UserProfileResponse response = new() { ReturnStatus = 200 };
            try
            {
                using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode);
                try
                {
                    using (IDataReader dr = tc.ExecuteReader("SELECT LoginId, UserName, LastPassChgDate FROM Users WHERE UserId=%n", userId))
                    {
                        if (dr.Read())
                        {
                            response = new UserProfileResponse
                            {
                                UserId = userId,
                                LoginId = dr.GetString(0),
                                UserName = dr.GetString(1),
                                PwdLastChangedTime = dr.IsDBNull(2) ? null : dr.GetDateTime(2),
                                ReturnStatus = 200
                            };
                        }
                        dr.Close();
                    }

                    response.LoginHistories = new List<LoginHistory>();
                    using (IDataReader dr = tc.ExecuteReader("SELECT TOP 10 LoginIPAddress, LoginTime, ISNULL(LogoutIPAddress,''), LogoutTime"
                        + " FROM AccessLog WHERE UserID=%n ORDER BY LoginTime DESC", userId))
                    {
                        int slNo = 0;
                        while (dr.Read())
                        {
                            slNo++;
                            LoginHistory item = new()
                            {
                                SlNo = slNo,
                                LoginIp = dr.GetString(0),
                                LoginTime = dr.GetDateTime(1),
                                LogoutIp = dr.GetString(2),
                                LogoutTime = dr.IsDBNull(3) ? null : dr.GetDateTime(3),
                            };
                            response.LoginHistories.Add(item: item);
                        }
                        dr.Close();
                    }

                    tc.End();
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
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<MenuItem> GetUserPermissionsAsync(int userId)
        {
            MenuItem menu = new() { Items = [] };
            MenuItem pi = new() { Label = "File", Icon = "fa fa-cogs", RouterLink = string.Empty, Items = [] };
            pi.Items.Add(new() { Label = "My Profile", Icon = "fa fa-user", RouterLink = "/myprofile" });
            pi.Items.Add(new() { Label = "Change My Password", Icon = "fa fa-unlock", RouterLink = "/changemypwd" });
            pi.Items.Add(new() { Label = "Change My Theme", Icon = "fa fa-user", RouterLink = "/changemytheme" });
            pi.Items.Add(new() { Label = "WhatsApp Message", Icon = "fa fa-whatsapp", RouterLink = "/sendwhatsappmsg" });
            pi.Items.Add(new() { Label = "Send Notification", Icon = "fa fa-commenting-o", RouterLink = "/sendfirebasemsg" });
            pi.Items.Add(new() { Label = "Uplaod Image", Icon = "fa fa-upload", RouterLink = "/uploadImage" });
            if (userId == User.SuperUser_Id)
                pi.Items.Add(new() { Label = "Label Setting", Icon = "fa fa-cog", RouterLink = "/labelsetting" });

            menu.Items.Add(pi);

            List<GroupPermission> pmns = [];
            try
            {
                using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode);
                try
                {
                    SqlParameter[] p =
                    [
                        SqlHelperExtension.CreateInParam(pName: "@UserId", pType: SqlDbType.Int, pValue: userId)
                    ];
                    using IDataReader dr = tc.ExecuteReaderSp(spName: "dbo.GetUserPermissions", parameterValues: p);
                    while (dr.Read())
                    {
                        GroupPermission item = new()
                        {
                            ModuleId = dr.GetString(0),
                            AllowSelect = dr.GetInt16(1) != 0,
                            AllowAdd = dr.GetInt16(2) != 0,
                            AllowEdit = dr.GetInt16(3) != 0,
                            AllowDelete = dr.GetInt16(4) != 0
                        };
                        pmns.Add(item);
                    }
                    dr.Close();

                    tc.End();
                }
                catch (Exception ie)
                {
                    tc?.HandleError();

                    throw DBCustomError.GenerateCustomError(ie);
                }

                #region Making Hierarchy

                List<GroupPermission> items = [];
                List<GroupPermission> fullMenu = GlobalFunctions.BuildMenu();
                IEnumerable<GroupPermission> data = fullMenu.Where(x => (string.IsNullOrEmpty(x.ParentId) || string.IsNullOrWhiteSpace(x.ParentId)) && x.Visible && pmns.Where(y => y.AllowSelect).Select(z => z.ModuleId).Contains(x.ModuleId));
                foreach (GroupPermission datum in data)
                {
                    MakeHierarchy(parent: datum, data: fullMenu, pmns: pmns);
                }
                items.AddRange(data);

                foreach (GroupPermission src in items)
                {
                    MenuItem dst = src.Copy();
                    dst.Label = _menuSettings.GetItem(src.ModuleId, src.ModuleName).Value;
                    menu.Items.Add(dst);

                    CopyHierarchy(src: src, dst: dst);
                }
                items.Clear();
                items = null;
                data = null;

                #endregion
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }

            return menu;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="authValue"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> ValidateAuthValueAsync(string authValue, int userId)
        {
            bool returnValue = false;
            try
            {
                using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode, true);
                try
                {
                    SqlParameter[] p =
                    [
                        SqlHelperExtension.CreateInParam(pName: "@AuthValue", pType: SqlDbType.VarChar, pValue: authValue, size: 10),
                        SqlHelperExtension.CreateInParam(pName: "@UserId", pType: SqlDbType.Int, pValue: userId),
                        SqlHelperExtension.CreateOutParam(pName: "@Valid", pType: SqlDbType.SmallInt, pValue: 0),
                    ];
                    _ = tc.ExecuteNonQuerySp(spName: "dbo.ValidateAuthValue", parameterValues: p);
                    if (p[2].Value != null && p[2].Value != DBNull.Value)
                        returnValue = (Convert.ToInt16(p[2].Value) > 0);
                    p = null;

                    tc.End();
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

            return returnValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="newPassword"></param>
        /// <param name="ipAddress"></param>
        /// <param name="changedBy"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> ResetPasswordAsync(int userId, string newPassword, string ipAddress, int changedBy)
        {
            bool returnValue = false;

            try
            {
                ReadPwdParams(enfStgPwd: out bool enfStgPwd, minLen: out short minLen, maxLen: out short maxLen, reservedWords: out string reservedWords);
                ValidatePwdPolicies(password: newPassword, minLen: minLen, maxLen: maxLen, enfStgPwd: enfStgPwd, reservedWords: reservedWords);

                try
                {
                    newPassword = EncryptPassword(password: newPassword);
                    using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode, true);
                    try
                    {
                        SqlParameter[] p =
                        [
                            SqlHelperExtension.CreateInParam(pName: "@NewPwd", pType: SqlDbType.VarChar, pValue: newPassword, size: 65),
                            SqlHelperExtension.CreateInParam(pName: "LastPwds", pType: SqlDbType.VarChar, pValue: string.Empty, size: 660),
                            SqlHelperExtension.CreateInParam(pName: "@LastPwdChgDate", pType: SqlDbType.DateTime, pValue: DateTime.Now),
                            SqlHelperExtension.CreateInParam(pName: "@PwdExpireDate", pType: SqlDbType.DateTime, pValue: DateTime.Now),
                            SqlHelperExtension.CreateInParam(pName: "@IpAddress", pType: SqlDbType.VarChar, pValue: ipAddress, size: 20),
                            SqlHelperExtension.CreateInParam(pName: "@UserId", pType: SqlDbType.Int, pValue: userId),
                            SqlHelperExtension.CreateInParam(pName: "@ChangedBy", pType: SqlDbType.Int, pValue: changedBy),
                            SqlHelperExtension.CreateInParam(pName: "@ResetPwd", pType: SqlDbType.SmallInt, pValue: 1)
                        ];
                        _ = tc.ExecuteNonQuerySp(spName: "dbo.UpdateUserPassword", parameterValues: p);
                        p = null;

                        tc.End();

                        returnValue = true;
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
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }

            return returnValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        /// <param name="ipAddress"></param>
        /// <param name="changedBy"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword, string ipAddress, int changedBy)
        {
            bool returnValue = false;

            try
            {
                bool neverExpire = false, enfStgPwd = false;
                Queue<string> pwdHistories = new();
                List<string> actPwdHistories = [];
                short minLen = 0, maxLen = 0, daLastPwds = 0, expiryDays = 0;
                string password = string.Empty, lastPasswords = string.Empty, reservedWords = string.Empty;

                #region Reading Data from DB

                try
                {
                    using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode);
                    try
                    {
                        using IDataReader dr = await tc.ExecuteReaderAsync("SELECT A.Password, A.LastPasswords, A.NeverExpires, B.IsStrongPassword,"
                            + " B.MinPasswordLength, B.MaxPasswordLength, A.PasswordExpiryDays"
                            + " FROM Users A LEFT OUTER JOIN ThisSystem B ON A.UserID = A.UserID WHERE A.UserID=%n", userId);

                        if (dr.Read())
                        {
                            password = dr.GetString(0);
                            lastPasswords = dr.IsDBNull(1) ? string.Empty : dr.GetString(1);
                            neverExpire = !dr.IsDBNull(2) && dr.GetInt16(2) > 0;
                            enfStgPwd = !dr.IsDBNull(3) && dr.GetInt16(3) > 0;
                            minLen = dr.IsDBNull(4) ? Convert.ToInt16(0) : (short)dr.GetInt32(4);
                            maxLen = dr.IsDBNull(5) ? Convert.ToInt16(30) : (short)dr.GetInt32(5);
                            expiryDays = dr.IsDBNull(6) ? Convert.ToInt16(0) : (short)dr.GetInt32(6);
                        }
                        dr.Close();

                        tc.End();
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
                #endregion

                #region Validation

                oldPassword = EncryptPassword(password: oldPassword);
                if (oldPassword.Equals(password) == false)
                    throw new Exception("Old and Current Password are not same.");

                #region Password Histories

                if (string.IsNullOrEmpty(lastPasswords) == false && string.IsNullOrWhiteSpace(lastPasswords) == false)
                {
                    string[] pwds = lastPasswords.Split(',');
                    foreach (string pwd in pwds)
                    {
                        pwdHistories.Enqueue(pwd);
                    }
                }

                #endregion

                #region Actual Password Histories

                string[] pwdsHists = [.. pwdHistories];
                if (daLastPwds > 0 && pwdsHists.Length > 0)
                {
                    for (int idx = pwdsHists.Length - 1; idx >= 0; idx--)
                    {
                        if (actPwdHistories.Contains(pwdsHists[idx]) == false && actPwdHistories.Count <= daLastPwds)
                            actPwdHistories.Add(pwdsHists[idx]);
                    }
                }

                #endregion

                CheckPasswordHistory(password: newPassword, minLen: minLen, maxLen: maxLen, enfStgPwd: enfStgPwd, reservedWords: reservedWords, daLastPwds: daLastPwds, pwdHistories: pwdHistories, actPwdHistories: actPwdHistories);

                #endregion

                #region Updateing Data

                if (pwdHistories.ToArray().Length > 0)
                    lastPasswords = string.Join(",", [.. pwdHistories]);

                DateTime? expireDate = null;
                DateTime lastPassChgDate = DateTime.Now;
                if (neverExpire == false && expiryDays > 0)
                    expireDate = DateTime.Now.AddDays(expiryDays);

                try
                {
                    newPassword = EncryptPassword(password: newPassword);
                    using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode, true);
                    try
                    {
                        SqlParameter[] p =
                        [
                            SqlHelperExtension.CreateInParam(pName: "@NewPwd", pType: SqlDbType.VarChar, pValue: newPassword, size: 65),
                            SqlHelperExtension.CreateInParam(pName: "LastPwds", pType: SqlDbType.VarChar, pValue: lastPasswords, size: 660),
                            SqlHelperExtension.CreateInParam(pName: "@LastPwdChgDate", pType: SqlDbType.DateTime, pValue: lastPassChgDate),
                            SqlHelperExtension.CreateInParam(pName: "@PwdExpireDate", pType: SqlDbType.DateTime, pValue: expireDate),
                            SqlHelperExtension.CreateInParam(pName: "@IpAddress", pType: SqlDbType.VarChar, pValue: ipAddress, size: 20),
                            SqlHelperExtension.CreateInParam(pName: "@UserId", pType: SqlDbType.Int, pValue: userId),
                            SqlHelperExtension.CreateInParam(pName: "@ChangedBy", pType: SqlDbType.Int, pValue: changedBy),
                            SqlHelperExtension.CreateInParam(pName: "@ResetPwd", pType: SqlDbType.SmallInt, pValue: 0)
                        ];
                        _ = tc.ExecuteNonQuerySp(spName: "dbo.UpdateUserPassword", parameterValues: p);
                        p = null;

                        tc.End();

                        returnValue = true;
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

                #endregion
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }

            return returnValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="userType"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<UserSearchResponse> GetUsersAsync(UserSearchRequest request, UserTypeEnum userType)
        {
            UserSearchResponse response = new();
            try
            {
                string andClause = string.Empty;
                if (string.IsNullOrEmpty(request.Criteria) == false)
                {
                    string criteria = request.Criteria.Replace("'", "''");
                    andClause = $" AND (LoginID='{criteria}' OR MobileNo='{criteria}' OR EmailAddress='{criteria}' OR UserName LIKE '%{criteria}%')";
                }

                if (request.Status > 0)
                    andClause += SQLParser.MakeSQL(" AND Status=%n", request.Status);

                switch (userType)
                {
                    case UserTypeEnum.SuperUser:
                        andClause += SQLParser.MakeSQL(" AND UserType IN(%n)", UserTypeEnum.SuperAdmin);
                        break;

                    case UserTypeEnum.SuperAdmin:
                        andClause += SQLParser.MakeSQL(" AND UserType IN(%n,%n)", UserTypeEnum.Administrator, UserTypeEnum.FieldLevelUser);
                        break;

                    case UserTypeEnum.Administrator:
                        andClause += SQLParser.MakeSQL(" AND UserType IN(%n)", UserTypeEnum.DistributorAdmin);
                        break;

                    case UserTypeEnum.FieldLevelUser:
                        andClause += SQLParser.MakeSQL(" AND UserType IN(%n,%n)", UserTypeEnum.DistributorUser, UserTypeEnum.CustomerUser);
                        break;

                    case UserTypeEnum.DistributorAdmin:
                        andClause += SQLParser.MakeSQL(" AND UserType IN(%n)", UserTypeEnum.DistributorUser);
                        break;

                    default:
                        break;
                }

                string sortField = request.SortField switch
                {
                    "userName" => "UserName",
                    "mobileNo" => "MobileNo",
                    "emailAddress" => "EmailAddress",
                    "statusDetail" => "Status",
                    "userTypeDetail" => "UserType",
                    _ => "LoginId",
                };

                string sortOrder = request.SortOrder switch
                {
                    "desc" => "DESC",
                    _ => "ASC",
                };

                string commandText = (request.Skip + request.PageSize) <= 0 ?
                  SQLParser.MakeSQL("SELECT UserId, LoginId, UserName, MobileNo, EmailAddress, Status, UserType, ISNULL(IsLocked,0), Password,"
                    + " COUNT(*) OVER() AS TotalRows FROM Users WHERE UserId!=-9%q ORDER BY %q %q", andClause, sortField, sortOrder)
                  : SQLParser.MakeSQL("SELECT A.UserId, A.LoginId, A.UserName, A.MobileNo, A.EmailAddress, A.Status, A.UserType, A.IsLocked,"
                    + " A.Password, A.TotalRows FROM(SELECT UserId, LoginId, UserName, MobileNo, EmailAddress, Status, ISNULL(IsLocked,0) IsLocked,"
                    + " UserType, Password, ROW_NUMBER() OVER(ORDER BY %q %q) AS RN, COUNT(*) OVER() AS TotalRows FROM Users WHERE UserId!=-9%q) A"
                    + " WHERE A.RN>%n AND A.RN<=%n", sortField, sortOrder, andClause, request.Skip, (request.Skip + request.PageSize));

                using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode);
                try
                {
                    int totalRows = 0;
                    using IDataReader dr = tc.ExecuteReader(commandText: commandText);
                    while (dr.Read())
                    {
                        UserSearch item = new()
                        {
                            UserId = dr.GetInt32(0),
                            LoginId = dr.GetString(1),
                            UserName = dr.GetString(2),
                            MobileNo = dr.GetString(3),
                            EmailAddress = dr.GetString(4),
                            Status = (StatusEnum)dr.GetInt16(5),
                            UserType = (UserTypeEnum)dr.GetInt16(6),
                            IsLocked = Convert.ToBoolean(dr.GetInt16(7))
                        };
                        item.AuthId = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{item.LoginId}~{dr.GetString(8)}"));
                        totalRows = dr.GetInt32(9);

                        response.Value.Add(item);
                    }
                    dr.Close();

                    response.TotalRows = totalRows;

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
        /// <param name="exceptUserId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<UserForceLogoutResponse> GetForceLogoutUsersAsync(int exceptUserId)
        {
            UserForceLogoutResponse response = new();
            try
            {
                using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode);
                try
                {
                    using IDataReader dr = tc.ExecuteReader(commandText: $"SELECT UserID, LoginID, UserName FROM Users WHERE UserId NOT IN(-9,{exceptUserId}) AND Status=16");
                    while (dr.Read())
                    {
                        UserForceLogout item = new()
                        {
                            UserId = dr.GetInt32(0),
                            LoginId = dr.GetString(1),
                            UserName = dr.GetString(2)
                        };

                        response.Value.Add(item);
                    }
                    dr.Close();

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
        /// <param name="userIds"></param>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> ForceLogoutNowAsync(List<int> userIds, string ipAddress)
        {
            bool returnValue;
            try
            {
                using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode, true);
                try
                {
                    SqlParameter[] p =
                    [
                        SqlHelperExtension.CreateInParam(pName: "@UserIDs", pType: SqlDbType.VarChar, pValue: string.Join(",",userIds.ToArray()), size: 8000),
                        SqlHelperExtension.CreateInParam(pName: "@IpAddress", pType: SqlDbType.VarChar, pValue: ipAddress, size: 20),
                    ];
                    _ = tc.ExecuteNonQuerySp(spName: "dbo.DoForceLogout", parameterValues: p);

                    tc.End();

                    returnValue = true;
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

            return returnValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<UserGetResponse> GetUserAsync(int userId)
        {
            UserGetResponse response = new() { ReturnStatus = 200 };
            try
            {
                using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode);
                try
                {
                    using (IDataReader dr = tc.ExecuteReader("SELECT LoginId, UserName, MobileNo, EmailAddress, UserType, IsLocked, Status,"
                      + " AccessStatus, NeverExpire, AuthReqAtlogin, AuthMethod, DBOnStartup, DAMultiLogin FROM Users WHERE UserId=%n", userId))
                    {
                        if (dr.Read())
                        {
                            response = new UserGetResponse
                            {
                                UserId = userId,
                                LoginId = dr.GetString(0),
                                UserName = dr.GetString(1),
                                MobileNo = dr.GetString(2),
                                EmailAddress = dr.GetString(3),
                                UserType = (UserTypeEnum)dr.GetInt16(4),
                                IsLocked = !dr.IsDBNull(5) && dr.GetInt16(5) > 0,
                                Status = (StatusEnum)dr.GetInt16(6),
                                AccessStatus = (AccessStatusEnum)dr.GetInt16(7),
                                NeverExpire = !dr.IsDBNull(8) && dr.GetInt16(8) != 0,
                                AuthRequiredAtlogin = !dr.IsDBNull(9) && dr.GetInt16(9) != 0,
                                AuthMethod = (AuthenticationMethodEnum)dr.GetInt16(10),
                                DbOnStartup = !dr.IsDBNull(11) && dr.GetInt16(11) != 0,
                                DisallowMultiLogin = !dr.IsDBNull(12) && dr.GetInt16(12) != 0,
                                ReturnStatus = 200
                            };
                        }
                        dr.Close();
                    }

                    response.GroupIds = new List<int>();
                    using (IDataReader dr = tc.ExecuteReader("SELECT GroupId FROM UserGroups WHERE UserID=%n", userId))
                    {
                        while (dr.Read())
                        {
                            response.GroupIds.Add(dr.GetInt32(0));
                        }
                        dr.Close();
                    }

                    tc.End();
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
        /// <param name="ipAddress"></param>
        /// <param name="userId"></param>
        /// <param name="logId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> LogoutAsync(string ipAddress, int userId, int logId)
        {
            bool returnValue = false;
            try
            {
                using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode, true);
                try
                {
                    SqlParameter[] p =
                    [
                        SqlHelperExtension.CreateInParam(pName: "@UserID", pType: SqlDbType.Int, pValue: userId),
                        SqlHelperExtension.CreateInParam(pName: "@Status", pType: SqlDbType.TinyInt, pValue: 3),
                        SqlHelperExtension.CreateInParam(pName: "@IpAddress", pType: SqlDbType.VarChar, pValue: ipAddress, size: 20),
                        SqlHelperExtension.CreateInParam(pName: "@LogID", pType: SqlDbType.Int, pValue: logId)
                    ];
                    _ = tc.ExecuteNonQuerySp(spName: "dbo.CreateAccessLog", parameterValues: p);
                    p = null;

                    tc.End();
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

            return returnValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="newPassword"></param>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> SendPasswordAsync(int userId, string newPassword, string ipAddress)
        {
            bool returnValue = false;

            try
            {
                newPassword = EncryptPassword(password: newPassword);
                using TransactionContext tc = await TransactionContext.BeginAsync(_settings.DefaultConnection.ConnectionNode, true);
                try
                {
                    SqlParameter[] p =
                    [
                        SqlHelperExtension.CreateInParam(pName: "@NewPwd", pType: SqlDbType.VarChar, pValue: newPassword, size: 65),
                        SqlHelperExtension.CreateInParam(pName: "LastPwds", pType: SqlDbType.VarChar, pValue: string.Empty, size: 660),
                        SqlHelperExtension.CreateInParam(pName: "@LastPwdChgDate", pType: SqlDbType.DateTime, pValue: DateTime.Now),
                        SqlHelperExtension.CreateInParam(pName: "@PwdExpireDate", pType: SqlDbType.DateTime, pValue: DateTime.Now),
                        SqlHelperExtension.CreateInParam(pName: "@IpAddress", pType: SqlDbType.VarChar, pValue: ipAddress, size: 20),
                        SqlHelperExtension.CreateInParam(pName: "@UserId", pType: SqlDbType.Int, pValue: userId),
                        SqlHelperExtension.CreateInParam(pName: "@ChangedBy", pType: SqlDbType.Int, pValue: userId),
                        SqlHelperExtension.CreateInParam(pName: "@ResetPwd", pType: SqlDbType.SmallInt, pValue: 1)
                    ];
                    _ = tc.ExecuteNonQuerySp(spName: "dbo.UpdateUserPassword", parameterValues: p);
                    
                    tc.End();

                    returnValue = true;
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

            return returnValue;
        }

        #region Private functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enfStgPwd"></param>
        /// <param name="minLen"></param>
        /// <param name="maxLen"></param>
        /// <param name="reservedWords"></param>
        /// <exception cref="Exception"></exception>
        private void ReadPwdParams(out bool enfStgPwd, out short minLen, out short maxLen, out string reservedWords)
        {
            try
            {
                enfStgPwd = false;
                minLen = maxLen = 0;
                reservedWords = string.Empty;
                using TransactionContext tc = TransactionContext.Begin(_settings.DefaultConnection.ConnectionNode);
                try
                {
                    using (IDataReader dr = tc.ExecuteReader("SELECT EnfStgPwd, PwdMinLen, PwdMaxLen, PwdRsvdWords FROM ThisSystem"))
                    {
                        if (dr.Read())
                        {
                            enfStgPwd = Convert.ToBoolean(dr.GetInt16(0));
                            minLen = dr.GetInt16(1);
                            maxLen = dr.GetInt16(2);
                            reservedWords = dr.IsDBNull(3) ? string.Empty : dr.GetString(3);
                        }
                        dr.Close();
                    }
                    tc.End();
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
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="password"></param>
        /// <param name="minLen"></param>
        /// <param name="maxLen"></param>
        /// <param name="enfStgPwd"></param>
        /// <param name="reservedWords"></param>
        /// <param name="daLastPwds"></param>
        /// <param name="pwdHistories"></param>
        /// <param name="actPwdHistories"></param>
        /// <exception cref="Exception"></exception>
        private void CheckPasswordHistory(string password, short minLen, short maxLen, bool enfStgPwd, string reservedWords, int daLastPwds, Queue<string> pwdHistories, List<string> actPwdHistories)
        {
            try
            {
                ValidatePwdPolicies(password: password, minLen: minLen, maxLen: maxLen, enfStgPwd: enfStgPwd, reservedWords: reservedWords);

                password = EncryptPassword(password: password);
                if (daLastPwds > 0 && actPwdHistories.Contains(password))
                    throw new Exception($"You cannot use this password, because it was used in last {daLastPwds} passwords.");

                if (pwdHistories.Count >= 5)
                    pwdHistories.Dequeue();

                pwdHistories.Enqueue(password);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="password"></param>
        /// <param name="minLen"></param>
        /// <param name="maxLen"></param>
        /// <param name="enfStgPwd"></param>
        /// <param name="reservedWords"></param>
        /// <exception cref="Exception"></exception>
        private static void ValidatePwdPolicies(string password, short minLen, short maxLen, bool enfStgPwd, string reservedWords)
        {
            try
            {
                if (string.IsNullOrEmpty(password) == false && string.IsNullOrEmpty(reservedWords) == false)
                {
                    List<string> words = new();
                    words.AddRange(reservedWords.Split(','));
                    foreach (string word in words)
                    {
                        if (password.Contains(word, StringComparison.InvariantCultureIgnoreCase))
                            throw new Exception($"[{word}] can not be used in password.");
                    }
                }

                if (minLen > 0 && password.Length < minLen)
                    throw new Exception($"Minimum length of Password is {minLen}");

                if (maxLen > 0 && password.Length > maxLen)
                    throw new Exception($"Maximum length of Password is {maxLen}");

                if (enfStgPwd && IsStrongPassword(password, minLen: minLen, maxLen: maxLen) == false)
                    throw new Exception("Must have Upper & Lower case character, a Number and a Special character.");
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="password"></param>
        /// <param name="minLen"></param>
        /// <param name="maxLen"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static bool IsStrongPassword(string password, int minLen = 8, int maxLen = 30)
        {
            try
            {
                return Global.StringFunctions.IsStrongPassword(password: password, minLen: minLen, maxLen: maxLen);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private string EncryptPassword(string password)
        {
            try
            {
                return Global.CipherFunctions.Encrypt(key: _settings.PwdSecretKey, data: password);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tc"></param>
        /// <param name="authValue"></param>
        /// <param name="validMinutes"></param>
        /// <param name="userId"></param>
        /// <exception cref="Exception"></exception>
        private static void SetAuthValue(TransactionContext tc, string authValue, int validMinutes, int userId)
        {
            try
            {
                SqlParameter[] p =
                [
                    SqlHelperExtension.CreateInParam(pName: "@AuthValue", pType: SqlDbType.VarChar, pValue: authValue, size: 10),
                    SqlHelperExtension.CreateInParam(pName: "@ValidMinutes", pType: SqlDbType.Int, pValue: validMinutes),
                    SqlHelperExtension.CreateInParam(pName: "@UserId", pType: SqlDbType.Int, pValue: userId)
                ];
                _ = tc.ExecuteNonQuerySp(spName: "dbo.SetAuthValue", parameterValues: p);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="data"></param>
        /// <param name="pmns"></param>
        /// <exception cref="Exception"></exception>
        private static void MakeHierarchy(GroupPermission parent, List<GroupPermission> data, List<GroupPermission> pmns)
        {
            try
            {
                IEnumerable<GroupPermission> children = data.Where(x => x.ParentId == parent.ModuleId && x.Visible && pmns.Where(y => y.AllowSelect).Select(z => z.ModuleId).Contains(x.ModuleId));
                parent.Children.AddRange(children);

                foreach (GroupPermission child in parent.Children)
                {
                    MakeHierarchy(parent: child, data: data, pmns: pmns);
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <exception cref="Exception"></exception>
        private void CopyHierarchy(GroupPermission src, MenuItem dst)
        {
            try
            {
                if (src.Children.Count > 0)
                    dst.Items = [];

                foreach (GroupPermission srcChild in src.Children)
                {
                    MenuItem dstChild = srcChild.Copy();
                    dstChild.Label = _menuSettings.GetItem(dstChild.ModuleId, dstChild.Label).Value;
                    dst.Items.Add(dstChild);

                    CopyHierarchy(src: srcChild, dst: dstChild);
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
        }

        #endregion
    }
}
