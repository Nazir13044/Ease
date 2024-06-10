using Collecto.CoreAPI.Models.Global;
using StackExchange.Redis;
using System.Configuration;

namespace Collecto.CoreAPI.Helper
{

    /// <summary>
    /// 
    /// </summary>
    internal static class CacheConfiguration
    {
        /// <summary>
        /// Gets the Redis URL from the configuration.
        /// </summary>
        internal static string RedisUrl { get; }

        /// <summary>
        /// Initializes the <see cref="CacheConfiguration"/> class by loading configuration settings.
        /// </summary>
        static CacheConfiguration()
        {
            try
            {
                IConfiguration configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

                RedisUrl = configuration.GetSection("AppSettings").Get<AppSettings>().RedisCacheUrl;
            }
            catch (FileNotFoundException e)
            {
                throw new ConfigurationErrorsException("The configuration file 'appsettings.json' was not found.", e);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal class CacheConnection
    {
        private static readonly string redisUrl;
        private static readonly Lazy<ConnectionMultiplexer> lazyConnection;
        static CacheConnection()
        {
            try
            {
                redisUrl = CacheConfiguration.RedisUrl;
                if (string.IsNullOrEmpty(redisUrl) == false)
                {
                    lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
                    {
                        return ConnectionMultiplexer.Connect(redisUrl);
                    });
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
        internal static ConnectionMultiplexer Connection => lazyConnection?.Value;

        /// <summary>
        /// 
        /// </summary>
        internal static bool RedisCacheEnbled => string.IsNullOrEmpty(redisUrl) == false;
    }
}
