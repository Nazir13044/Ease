using Asp.Versioning;
using Collecto.CoreAPI.Helper;
using Collecto.CoreAPI.Models.Requests;
using Collecto.CoreAPI.Models.Requests.Setups;
using Collecto.CoreAPI.Models.Responses;
using Collecto.CoreAPI.Models.Responses.Setups;
using Collecto.CoreAPI.Services.Contracts.Setups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Collecto.CoreAPI.Controllers.V1
{
    /// <summary>
    /// 
    /// </summary>                              
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [ValidateAntiForgeryToken]
    [Route("api/v{version:apiVersion}/authModules")]
    public class AuthModulesController(IAuthModulesService service, ICollectoCache cache) : ControllerBase
    {
        private readonly ICollectoCache _cache = cache;
        private readonly IAuthModulesService _service = service;
        private readonly DateTimeOffset _options = Helper.Helper.CreateCollectoCacheOptions();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthSummariesResponse))]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(AuthSummariesResponse))]
        [HttpGet("getAuthSummaries")]
        public async Task<IActionResult> GetAuthSummaries([FromQuery] AuthSummaryRequest request)
        {
            AuthSummariesResponse response = new() { ReturnStatus = StatusCodes.Status200OK };
            if (request == null)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Parameter value is null.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            try
            {
                int systemId = HttpContext.User.GetClaimValue<int>("SystemID");
                int userId = HttpContext.User.GetClaimValue<int>("UserId");
                int subsystemId = HttpContext.User.GetClaimValue<int>("SubSystemID");
                response = await _service.GetAuthSummariesAsync(systemId: systemId, subsystemId: subsystemId, userId: userId, status: request.Status, entryModule: 0);
                response.ReturnStatus = StatusCodes.Status200OK;

                return Ok(response);
            }
            catch (Exception ex)
            {
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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthDetailsResponse))]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(AuthDetailsResponse))]
        [HttpGet("getAuthDeatils")]
        public async Task<IActionResult> GetAuthDeatils([FromQuery] AuthDetailRequest request)
        {
            AuthDetailsResponse response = new() { ReturnStatus = StatusCodes.Status200OK };
            if (request == null)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Parameter value is null.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            try
            {
                int userId = HttpContext.User.GetClaimValue<int>("UserId");
                int systemId = HttpContext.User.GetClaimValue<int>("SystemID");
                int subsystemId = HttpContext.User.GetClaimValue<int>("SubSystemID");
                response = await _service.GetAuthDetailsAsync(moduleId: request.ModuleId, systemId: systemId, subsystemId: subsystemId, status: request.Status);
                response.ReturnStatus = StatusCodes.Status200OK;

                return Ok(response);
            }
            catch (Exception ex)
            {
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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BooleanResponse))]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(BooleanResponse))]
        [HttpPost("updateAuthStatus")]
        public async Task<IActionResult> UpdateAuthStatus([FromBody] AuthUpdateRequest request)
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
                string ids = string.Join(',', request.Ids);
                string ipAddress = HttpContext.Connection.GetIpAddress();
                int userId = HttpContext.User.GetClaimValue<int>("UserId");
                response.Value = await _service.UpdateAuthStatusAsync(moduleId: request.ModuleId, ipAddress: ipAddress, remarks: request.Remarks, status: request.Status, userId: userId, ids: ids);
                response.ReturnStatus = StatusCodes.Status200OK;

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.ReturnStatus = StatusCodes.Status500InternalServerError;
                response.ReturnMessage.Add(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// /
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthModulesBasicResponse))]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(AuthModulesBasicResponse))]
        [HttpGet("getAuthModulesBasic")]
        public async Task<IActionResult> GetAuthModulesBasic([FromQuery] ValueAndStatusSearchRequest request)
        {
            AuthModulesBasicResponse response = new() { ReturnStatus = StatusCodes.Status200OK };
            if (request == null)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Parameter value is null.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            try
            {
                int systemId = HttpContext.User.GetClaimValue<int>("SystemID");
                int subsystemId = HttpContext.User.GetClaimValue<int>("SubsystemID");
                string key = $"GetAuthModulesBasic~{systemId}~{subsystemId}~{request.Status}";
                if (_cache.TryGetValue(key: key, value: out response) == false)
                {
                    response = await _service.GetAuthModulesBasicAsync();

                    //Cache
                    _ = _cache.Set(key: key, value: response, options: _options);
                }

                response.ReturnStatus = StatusCodes.Status200OK;

                return Ok(response);
            }
            catch (Exception ex)
            {
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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthDetailsResponse))]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(AuthDetailsResponse))]
        [HttpGet("getInactiveDetails")]
        public async Task<IActionResult> GetInactiveDetails([FromQuery] InactiveDetailRequest request)
        {
            AuthDetailsResponse response = new() { ReturnStatus = StatusCodes.Status200OK };
            if (request == null)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Parameter value is null.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            try
            {
                int userId = HttpContext.User.GetClaimValue<int>("UserId");
                int systemId = HttpContext.User.GetClaimValue<int>("SystemID");
                response = await _service.GetInactiveDetailsAsync(request: request, systemId: systemId, userId: userId);
                response.ReturnStatus = StatusCodes.Status200OK;

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.ReturnStatus = StatusCodes.Status500InternalServerError;
                response.ReturnMessage.Add(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }
    }
}
