using Collecto.CoreAPI.Helper;
using Collecto.CoreAPI.Services.Contracts;
using Collecto.CoreAPI.Services.Contracts.Systems;
using Collecto.CoreAPI.Services.Services;
using Collecto.CoreAPI.Services.Services.Systems;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Reflection;

namespace Collecto.CoreAPI.Configurations.DI
{
    /// <summary>
    /// 
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        public static void ConfigureBusinessServices(this IServiceCollection services)
        {
            if (services == null)
                return;

            services.AddSingleton<ICollectoCache, CollectoCache>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

            services.AddTransient<IFirebaseMessageService, FirebaseMessageService>();
            services.AddTransient<IUserService, UserService>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        public static void ConfigureMappings(this IServiceCollection services)
        {
            services?.AddAutoMapper(Assembly.GetExecutingAssembly());
        }
    }
}
