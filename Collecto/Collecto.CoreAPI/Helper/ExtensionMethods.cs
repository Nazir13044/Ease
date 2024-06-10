using ExifLib;
using System.Security.Claims;

namespace Collecto.CoreAPI.Helper
{
    /// <summary>
    /// 
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="principal"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static T GetClaimValue<T>(this ClaimsPrincipal principal, string type)
        {
            if (principal == null || principal.Identity == null || principal.Identity.IsAuthenticated == false)
                return typeof(T) == typeof(string) ? ((T)Convert.ChangeType(string.Empty, typeof(T))) : default;

            Claim claim = principal.Claims.Where(p => p.Type == type).SingleOrDefault();
            if (claim == null || string.IsNullOrEmpty(claim.Value) || string.IsNullOrWhiteSpace(claim.Value))
                return typeof(T) == typeof(string) ? ((T)Convert.ChangeType(string.Empty, typeof(T))) : default;

            return (T)Convert.ChangeType(claim.Value, typeof(T));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void Set<T>(this ISession session, string key, T value)
        {
            if (session is null)
                throw new ArgumentNullException(nameof(session));

            try
            {
                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(value);
                session.SetString(key, jsonString);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T GetObject<T>(this ISession session, string key)
        {
            if (session is null)
                throw new ArgumentNullException(nameof(session));

            try
            {
                var value = session.GetString(key);
                return value == null ? default : Newtonsoft.Json.JsonConvert.DeserializeObject<T>(value);
            }
            catch (System.Exception e)
            {
                throw new System.Exception(e.Message, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<bool> SetModulesToSession<T>(this ISession session, T value)
        {
            ArgumentNullException.ThrowIfNull(session);

            try
            {
                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(value);
                session.SetString(key: "Collecto.ModuleIds", value: jsonString);
                await session.CommitAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        public static async Task<bool> IsPermitted(this ISession session, string moduleId)
        {
            if (session is null)
                return false;

            await session.LoadAsync();
            string moduleIds = session.GetString(key: "Collecto.ModuleIds");
            if (string.IsNullOrEmpty(moduleIds))
            {
                return false;
            }

            try
            {
                List<string> value = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(moduleIds);
                return value.Contains(moduleId);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static bool SessionExpired(this ISession session)
        {
            if (session is null)
                return true;

            string moduleIds = session.GetString("Collecto.ModuleIds");
            if (string.IsNullOrEmpty(moduleIds))
                return true;

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="remoteConnection"></param>
        /// <returns></returns>
        public static string GetIpAddress(this ConnectionInfo remoteConnection)
        {
            if (remoteConnection is null)
                return string.Empty;

            if (remoteConnection.RemoteIpAddress is null)
                return string.Empty;

            return remoteConnection.RemoteIpAddress.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static double GetLatitude(this ExifReader reader)
        {
            return reader.GetCoordinate(ExifTags.GPSLatitude);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static double GetLongitude(this ExifReader reader)
        {
            return reader.GetCoordinate(ExifTags.GPSLongitude);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private static double GetCoordinate(this ExifReader reader, ExifTags type)
        {
            double coordinate = 0;
            if (reader.GetTagValue(type, out double[] coordinates))
                coordinate = coordinates[0] + (coordinates[1] / 60f) + (coordinates[2] / 3600f);

            return coordinate;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="exception"></param>
        public static void LogError(this ILogger logger, Exception exception)
        {
#pragma warning disable CA2254 // Template should be a static expression
            logger.LogError(exception: exception, message: string.Empty);
#pragma warning restore CA2254 // Template should be a static expression
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        public static void LogError(this ILogger logger, string message)
        {
            logger.LogError(message: message);
        }
    }
}
