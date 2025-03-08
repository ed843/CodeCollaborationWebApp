using Microsoft.AspNetCore.Mvc;


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

        public RoomController(IRoomService roomService)
        {
            _roomService = roomService;
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
            if (string.IsNullOrEmpty(code) || code.Length != 5)
            {
                return BadRequest(new { exists = false, message = "Invalid room code format" });
            }

            bool roomExists = _roomService.RoomExists(code);

            return Ok(new { exists = roomExists });
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
            string roomCode = _roomService.CreateRoom();
            return Ok(new { roomCode });
        }
    }
}
