using Microsoft.AspNetCore.Mvc;


namespace CodeCollaborationWebApp
{
    using CodeCollaborationWebApp;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Linq;

    [ApiController]
    [Route("api/room")]
    public class RoomController : ControllerBase
    {
        // Assuming you have a service or repository that tracks rooms
        private readonly IRoomService _roomService;

        public RoomController(IRoomService roomService)
        {
            _roomService = roomService;
        }

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

        // Your existing create endpoint
        [HttpGet("create")]
        public IActionResult CreateRoom()
        {
            string roomCode = _roomService.CreateRoom();
            return Ok(new { roomCode });
        }
    }
}
