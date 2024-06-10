using Asp.Versioning;
using Collecto.CoreAPI.Models.Global;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Collecto.CoreAPI.Controllers.V1
{
    /// <summary>
    /// 
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/antiforgery")]
    public class AntiForgeryController : ControllerBase
    {
        private readonly bool _httpOnly, _secure;
        private readonly int _lifeTime, _sameSite;
        private readonly IAntiforgery _antiForgery;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="antiForgery"></param>
        /// <param name="appSettings"></param>
        public AntiForgeryController(IAntiforgery antiForgery, IOptions<AppSettings> appSettings)
        {
            _antiForgery = antiForgery;
            if (appSettings?.Value != null)
            {
                _secure = appSettings.Value.CookieSecure;
                _lifeTime = appSettings.Value.CookieLifeTime;
                _httpOnly = appSettings.Value.CookieHttpOnly;
                _sameSite = appSettings.Value.CookieSameSite;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet("refreshToken")]
        [IgnoreAntiforgeryToken]
        public IActionResult RefreshToken()
        {
            DateTime? expires = (_lifeTime > 0) ? DateTime.UtcNow.AddMinutes(_lifeTime) : null;

            AntiforgeryTokenSet tokens = _antiForgery.GetAndStoreTokens(HttpContext);
            Response.Cookies.Append("Collecto.X-XSRF-TOKEN", tokens.RequestToken, new CookieOptions
            {
                Secure = _secure,
                Expires = expires,
                HttpOnly = _httpOnly,
                SameSite = (SameSiteMode)_sameSite,
            });
            return NoContent();
        }
    }
}
