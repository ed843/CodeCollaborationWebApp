namespace CodeCollaborationWebApp.Hubs
{
    using Microsoft.AspNetCore.SignalR;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.SignalR;
    using System;
    using System.Threading.Tasks;
    using CodeCollaborationWebApp.Services;

    public class CollaborationHub : Hub
    {
        private readonly IRoomService _roomService;

        public CollaborationHub(IRoomService roomService)
        {
            _roomService = roomService;
        }

        public async Task JoinRoom(string roomCode)
        {
            if (!_roomService.RoomExists(roomCode))
            {
                // Send error message to client
                await Clients.Caller.SendAsync("RoomNotFound");
                return;
            }

            // Add user to room tracking
            _roomService.AddUserToRoom(roomCode, Context.ConnectionId);

            // Add to SignalR group
            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);

            // Get current user count for this room
            int userCount = _roomService.GetRoomUserCount(roomCode);

            // Send the current user count to the newly joined user
            await Clients.Caller.SendAsync("UpdateUserCount", userCount);

            // Notify everyone else about the new user
            await Clients.OthersInGroup(roomCode).SendAsync("UserJoined", userCount);
        }

        public async Task SendWhiteboardUpdate(string roomCode, string updateData)
        {
            await Clients.OthersInGroup(roomCode).SendAsync("ReceiveWhiteboardUpdate", updateData);
        }

        public async Task SendWhiteboardClear(string roomCode)
        {
            await Clients.OthersInGroup(roomCode).SendAsync("ReceiveWhiteboardClear");
        }

        public async Task SendCodeUpdate(string roomCode, string code)
        {
            await Clients.OthersInGroup(roomCode).SendAsync("ReceiveCodeUpdate", code);
        }

        public async Task SendLanguageChange(string roomCode, string language)
        {
            await Clients.OthersInGroup(roomCode).SendAsync("ReceiveLanguageChange", language);
        }

        public async Task SendOutputUpdate(string roomCode, string output)
        {
            await Clients.OthersInGroup(roomCode).SendAsync("ReceiveOutputUpdate", output);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Get current connection ID
            var connectionId = Context.ConnectionId;

            // Get the room code before removing the user
            string roomCode = _roomService.GetUserRoom(connectionId);

            if (!string.IsNullOrEmpty(roomCode))
            {
                // Remove user from room
                _roomService.RemoveUserFromRoom(connectionId);

                // Get updated user count
                int userCount = _roomService.GetRoomUserCount(roomCode);

                if (userCount > 0)
                {
                    // Notify remaining users that someone left
                    await Clients.Group(roomCode).SendAsync("UserLeft", userCount);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }

}