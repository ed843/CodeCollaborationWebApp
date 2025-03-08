using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;


namespace CodeCollaborationWebApp.Controllers
{
    using CodeCollaborationWebApp.Services;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Linq;

    [ApiController]
    [Route("api/room")]
    public class RoomController : ControllerBase
    {
        /// <summary>
        ///    The room service used to manage rooms
        /// </summary>
        private readonly IRoomService _roomService;

        private readonly ILogger<RoomController> _logger;

        public RoomController(IRoomService roomService, ILogger<RoomController> logger)
        {
            _roomService = roomService ?? throw new ArgumentNullException(nameof(roomService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        /// <summary>
        ///    Verifies if a room exists
        /// </summary>
        /// <param name="code">
        ///     The room code to verify
        /// </param>
        /// <returns>
        ///     <c>BadRequest</c> if the room code is invalid. 
        ///     Otherwise <c>Ok</c> with a JSON object containing the key <c>exists</c> and a boolean value indicating if the room exists
        /// </returns>
        [HttpGet("verify")]
        public IActionResult VerifyRoom([FromQuery] string code)
        {
            try
            {
                if (string.IsNullOrEmpty(code) || code.Length != 5)
                {
                    return BadRequest(new { exists = false, message = "Invalid room code format" });
                }

                bool roomExists = _roomService.RoomExists(code);
                return Ok(new { exists = roomExists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying room with code: {code}", code);
                return StatusCode(500, new { error = "An error occurred while verifying the room" });
            }
        }

        /// <summary>
        ///    Creates a new room
        /// </summary>
        /// <returns>
        ///     The room code of the newly created room
        /// </returns>
        [HttpGet("create")]
        public IActionResult CreateRoom()
        {
            try
            {
                string roomCode = _roomService.CreateRoom();
                _logger.LogInformation("Created new room with code: {roomCode}", roomCode);
                return Ok(new { roomCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating room");
                return StatusCode(500, new { error = "An error occurred while creating the room" });
            }
        }
    }
}
