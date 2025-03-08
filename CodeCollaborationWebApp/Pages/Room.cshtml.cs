using CodeCollaborationWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CodeCollaborationWebApp.Pages
{
    public class RoomModel : PageModel
    {
        private readonly IRoomService _roomService;

        public RoomModel(IRoomService roomService)
        {
            _roomService = roomService;
        }

        public IActionResult OnGet()
        {
            string roomCode = Request.Query["code"].ToString();

            if (string.IsNullOrEmpty(roomCode) || roomCode.Length != 5 || !_roomService.RoomExists(roomCode))
            {
                return RedirectToPage("/Index");
            }

            return Page();
        }
    }
}
