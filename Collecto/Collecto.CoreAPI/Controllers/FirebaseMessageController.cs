using Asp.Versioning;
using Collecto.CoreAPI.Models.Objects;
using Collecto.CoreAPI.Models.Responses;
using Collecto.CoreAPI.Models.Responses.Systems;
using Collecto.CoreAPI.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Collecto.CoreAPI.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [ValidateAntiForgeryToken]
    [Route("api/v{version:apiVersion}/firebaseMessage")]
    public class FirebaseMessageController(IFirebaseMessageService service) : ControllerBase
    {
        private readonly IFirebaseMessageService _service = service;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [HttpPost("sendNotification")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BooleanResponse))]
        public async Task<IActionResult> SendPushNotification([FromBody] FirebaseMessage message)
        {
            BooleanResponse response = new() { ReturnStatus = StatusCodes.Status200OK };
            if (message == null)
            {
                response.ReturnStatus = StatusCodes.Status417ExpectationFailed;
                response.ReturnMessage.Add("Parameter value is null.");
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            try
            {
                response = await _service.SendNotification(message: message);
                return Ok(response);
            }
            catch
            {
                response.ReturnStatus = StatusCodes.Status500InternalServerError;
                response.ReturnMessage.Add("Cannot send message at this moment, please try after some times");
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet("getUsersForNotification")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UsersNotifiationResponse))]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(UsersNotifiationResponse))]
        public async Task<IActionResult> GetUsersForNotification()
        {
            UsersNotifiationResponse response = new() { ReturnStatus = StatusCodes.Status200OK };

            try
            {
                response = await _service.GetUsersForNotificationAsync();
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
