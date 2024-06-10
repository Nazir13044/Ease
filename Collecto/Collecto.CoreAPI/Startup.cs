using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using AutoMapper;
using Collecto.CoreAPI.Configurations.AutoMapper.Profiles;
using Collecto.CoreAPI.Configurations.DI;
using Collecto.CoreAPI.Helper;
using Collecto.CoreAPI.Models.Global;
using Collecto.CoreAPI.Services.DbContexts;
using Collecto.CoreAPI.SignalRHub;
using Collecto.CoreAPI.Swagger;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Net;
using System.Reflection;
using System.Text;
namespace Collecto.CoreAPI
{
    /// <summary>
    /// 
    /// </summary>
    public class Startup
    {
        private readonly ILogger _logger;
        private readonly AppSettings _appSettings;

        /// <summary>
        /// 
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// 
        /// </summary>
        public IWebHostEnvironment HostingEnvironment { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="env"></param>
        /// <param name="logger"></param>
        public Startup(IConfiguration configuration, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            _logger = logger;
            HostingEnvironment = env;
            Configuration = configuration;
            _appSettings = Configuration.GetSection("AppSettings").Get<AppSettings>();

            _logger.LogDebug("Startup::Constructor::Settings loaded");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            _logger.LogTrace("Startup::ConfigureServices");

            try
            {
                if (_appSettings.IsValid())
                {
                    _logger.LogDebug("Startup::ConfigureServices::valid AppSettings");

                    #region App settings

                    services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
                    services.Configure<MenuSettings>(Configuration.GetSection("MenuSettings"));

                    #endregion

                    #region Controllers

                    services.AddControllers(options =>
                    {
                        options.Filters.Add(new ProducesAttribute("application/json"));
                    });


                    //For OAuth2
                    services.AddEndpointsApiExplorer();

                    //JSON Formatting and must be removed before production 
                    /*
                    services.AddControllers().AddJsonOptions(options =>
                    {
                        options.JsonSerializerOptions.PropertyNamingPolicy = null;
                        options.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
                    });
                    */

                    #endregion

                    #region API versioning

                    services.AddApiVersioning(options =>
                    {
                        options.ReportApiVersions = true;
                        options.DefaultApiVersion = new ApiVersion(1, 0);
                        options.AssumeDefaultVersionWhenUnspecified = true;
                        options.ApiVersionReader = new UrlSegmentApiVersionReader();
                    })
                    .AddMvc()                       // API versioning extensions for MVC Core
                    .AddApiExplorer();              // API version-aware API Explorer extensions

                    #endregion

                    #region CORS

                    string[] cors = _appSettings.CorsSetting.Split(',');
                    if (cors.Length == 1 && (string.IsNullOrEmpty(cors[0]) || cors[0].Length <= 0))
                    {
                        services.AddCors(options =>
                        {
                            options.DefaultPolicyName = "CORSPolicy";
                            options.AddDefaultPolicy(builder =>
                            {
                                builder.AllowAnyOrigin()
                                    .AllowAnyMethod()
                                    .AllowAnyHeader();
                            });
                        });
                    }
                    else
                    {
                        services.AddCors(options =>
                        {
                            options.DefaultPolicyName = "CORSPolicy";
                            options.AddDefaultPolicy(builder =>
                            {
                                builder.WithOrigins(cors)
                                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                                    .AllowAnyMethod()
                                    .AllowAnyHeader()
                                    .AllowCredentials();
                            });
                        });
                    }
                    #endregion

                    #region Versioned Api Explorer

                    //Format the version as "'v'major[.minor][-status]"
                    //services.AddVersionedApiExplorer(options =>
                    //{
                    //    options.GroupNameFormat = "'v'VVV";
                    //    options.SubstituteApiVersionInUrl = true;
                    //});

                    #endregion

                    #region Caching

                    services.AddMemoryCache();
                    if (string.IsNullOrEmpty(_appSettings.RedisSessionUrl) == false)
                    {
                        services.AddStackExchangeRedisCache(options =>
                        {
                            options.Configuration = _appSettings.RedisSessionUrl;
                        });
                    }

                    #endregion

                    #region Session

                    services.AddSession(options =>
                    {
                        options.IdleTimeout = TimeSpan.FromHours(12);
                        options.Cookie.Name = "Collecto.Net8.Session.Id";
                    });

                    #endregion

                    #region JWT Authentication

                    var key = Encoding.ASCII.GetBytes(_appSettings.JwtCryptoKey);
                    services.AddAuthentication(options =>
                    {
                        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                    //.AddOpenIdConnect(openIdConnectAuthenticationScheme, options =>
                    //{
                    //    options.Authority = authority;
                    //    var openIdConnectConfiguration = new OpenIdConnectConfiguration
                    //    {
                    //        TokenEndpoint = tokenEndpoint
                    //    };
                    //    options.Configuration = openIdConnectConfiguration;
                    //    options.ClientId = openIdConfig["ClientId"];
                    //    options.ClientSecret = openIdConfig["ClientSecret"];
                    //})
                    .AddJwtBearer(options =>
                    {
                        options.SaveToken = true;
                        options.RequireHttpsMetadata = false;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ClockSkew = TimeSpan.Zero,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = _appSettings.JwtIssuer,
                            ValidAudience = _appSettings.JwtAudience,
                            ValidateIssuer = _appSettings.JwtValidateIssuer,
                            IssuerSigningKey = new SymmetricSecurityKey(key),
                            ValidateAudience = _appSettings.JwtValidateAudience
                        };

                        options.Events = new JwtBearerEvents
                        {
                            OnAuthenticationFailed = context =>
                            {
                                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                                    context.Response.Headers.Add("Token-Expired", "true");

                                return System.Threading.Tasks.Task.CompletedTask;
                            }
                        };
                    });

                    #endregion

                    #region Password Monitoring

                    /*
                    services.Configure<IdentityOptions>(options =>
                    {
                        // Password settings.
                        options.Password.RequireDigit = true;
                        options.Password.RequireLowercase = true;
                        options.Password.RequireNonAlphanumeric = true;
                        options.Password.RequireUppercase = true;
                        options.Password.RequiredLength = 6;
                        options.Password.RequiredUniqueChars = 1;

                        // Lockout settings.
                        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                        options.Lockout.MaxFailedAccessAttempts = 5;
                        options.Lockout.AllowedForNewUsers = true;

                        // User settings.
                        options.User.AllowedUserNameCharacters ="abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                        options.User.RequireUniqueEmail = false;
                    });
                    */

                    #endregion

                    #region Cookie Settings

                    services.Configure<CookiePolicyOptions>(options =>
                    {
                        // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                        options.CheckConsentNeeded = context => true;
                        // requires using Microsoft.AspNetCore.Http;
                        options.MinimumSameSitePolicy = SameSiteMode.None;
                        options.ConsentCookieValue = "true";
                    });

                    /*
                     services.ConfigureApplicationCookie(options =>
                     {
                         // Cookie settings
                         options.Cookie.HttpOnly = true;
                         options.ExpireTimeSpan = TimeSpan.FromMinutes(5);

                         //options.LoginPath = "/Identity/Account/Login";
                         //options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                         //options.SlidingExpiration = true;
                     });
                     */

                    #endregion

                    #region Mvc
                    services.AddMvc();
                    #endregion

                    #region Antiforgery

                    services.AddAntiforgery(options =>
                    {
                        options.SuppressXFrameOptionsHeader = false;
                        options.HeaderName = "Collecto.X-XSRF-TOKEN";
                        options.Cookie.Name = "Collecto.X-XSRF-ANTIFORGERY";
                    });

                    #endregion

                    #region Auto Mapper Configurations

                    var mappingConfig = new MapperConfiguration(mc =>
                    {
                        mc.AddProfile(new APIMappingProfile());
                    });

                    IMapper mapper = mappingConfig.CreateMapper();
                    services.AddSingleton(mapper);

                    #endregion

                    #region SignalR

                    services.AddSignalR();
                    services.AddResponseCompression(opt =>
                    {
                        opt.MimeTypes = ["application/octet-stream"];
                    });
                    #endregion

                    #region Hangfire

                    if (string.IsNullOrEmpty(_appSettings.HangfireDb) == false)
                    {
                        services.AddHangfire(config =>
                        config.UseSqlServerStorage(_appSettings.HangfireDb, new SqlServerStorageOptions { SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5), QueuePollInterval = TimeSpan.Zero }));
                        services.AddHangfireServer();
                    }

                    #endregion

                    #region SWAGGER

                    if (_appSettings.Swagger.Enabled)
                    {
                        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
                        services.AddSwaggerGen(options =>
                        {
                            options.OperationFilter<SwaggerDefaultValues>();
                            options.IncludeXmlComments(filePath: XmlCommentsFilePath);
                        });
                    }

                    #endregion

                    # region Mappings

                    services.ConfigureMappings();

                    #endregion

                    #region Entity Framwork Service  

                    if (string.IsNullOrEmpty(_appSettings.EfCoreDb) == false)
                    {
                        services.AddDbContext<PrimaryDbContext>(config =>
                        {
                            config.UseSqlServer(_appSettings.EfCoreDb);
                        });
                    }
                    #endregion

                    #region Business Service  

                    services.ConfigureBusinessServices();

                    #endregion

                    _logger.LogDebug("Startup::ConfigureServices::ApiVersioning, Swagger and DI settings");
                }
                else
                {
                    _logger.LogDebug("Startup::ConfigureServices::invalid AppSettings");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="provider"></param>
        /// <param name="loggerFactory"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider, ILoggerFactory loggerFactory)
        {
            _logger.LogTrace("Startup::Configure");
            _logger.LogDebug(message: $"Startup::Configure::Environment:{env.EnvironmentName}");

            try
            {
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }
                else
                {
                    #region Exception Handling in Production mode

                    app.UseExceptionHandler(a => a.Run(async context =>
                    {
                        IExceptionHandlerPathFeature feature = context.Features.Get<IExceptionHandlerPathFeature>();
                        Exception exception = feature.Error;
                        HttpStatusCode code = HttpStatusCode.InternalServerError;

                        if (exception is ArgumentNullException)
                            code = HttpStatusCode.BadRequest;
                        else if (exception is ArgumentException)
                            code = HttpStatusCode.BadRequest;
                        else if (exception is UnauthorizedAccessException)
                            code = HttpStatusCode.Unauthorized;
                        else if (exception is WebException)
                        {
                            if ((exception as WebException).Response is HttpWebResponse response)
                                code = response.StatusCode;
                        }

                        _logger.LogError($"GLOBAL ERROR HANDLER::HTTP:{code}::{exception.Message}");

                        var result = JsonConvert.SerializeObject(exception, Formatting.Indented);

                        context.Response.Clear();
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(result);
                    }));

                    app.UseHsts();
                    app.UseHttpsRedirection();

                    #endregion
                }

                #region CORS

                app.UseCors("CORSPolicy");

                #endregion                

                #region Use Static Files

                app.UseStaticFiles();

                #endregion

                #region Routing

                app.UseRouting();

                #endregion

                #region Session

                app.UseSession();

                #endregion

                #region Cookie Policy

                //app.UseCookiePolicy();

                #endregion

                #region For Authentication

                app.UseAuthentication();
                app.UseAuthorization();

                #endregion

                #region X-Frame-Options

                //app.Use(async (context, next) =>
                //{
                //    context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
                //    await next();
                //});

                #endregion

                #region Hangfire

                if (string.IsNullOrEmpty(_appSettings.HangfireDb) == false)
                {
                    app.UseHangfireDashboard();
                }

                #endregion

                #region Logger

                if (string.IsNullOrEmpty(_appSettings.LoggerPath) == false)
                {
                    loggerFactory.AddFile(pathFormat: $@"{_appSettings.LoggerPath}\Log.txt", minimumLevel: (LogLevel)_appSettings.LoggerMinLevel);
                }

                #endregion

                #region Controllers and SignalR

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.MapHub<NotificationHub>("/signalRHub");
                });

                #endregion

                #region Request Localization

                app.UseRequestLocalization();

                #endregion

                #region Swagger

                if (_appSettings.IsValid())
                {
                    if (_appSettings.Swagger.Enabled)
                    {
                        app.UseSwagger();
                        app.UseSwaggerUI(options =>
                        {
                            foreach (var description in provider.ApiVersionDescriptions)
                            {
                                string sjp = string.IsNullOrWhiteSpace(options.RoutePrefix) ? "." : "..";
                                options.SwaggerEndpoint($"{sjp}/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                            }
                        });
                    }
                }

                #endregion

                app.Run(async (context) =>
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync("<!DOCTYPE html><html lang=\"en\"><head><title>Collecto API (.NetCore 8)</title></head><body><h3>Collecto API is Running</h3>");
                    await context.Response.WriteAsync($"<p>Request Url: {context.Request.GetDisplayUrl()}<p>");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        static string XmlCommentsFilePath
        {
            get
            {
                var basePath = AppContext.BaseDirectory;
                var fileName = typeof(Startup).GetTypeInfo().Assembly.GetName().Name + ".xml";
                return Path.Combine(basePath, fileName);
            }
        }
    }
}
