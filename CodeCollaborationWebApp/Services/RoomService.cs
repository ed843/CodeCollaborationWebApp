namespace CodeCollaborationWebApp.Services
{
    /// <summary>
    ///     This service manages the creation and tracking of rooms, as well as the association of users with rooms.
    /// </summary>
    public interface IRoomService
    {
        /// <summary>
        ///     Creates a new room with a room code
        /// </summary>
        /// <returns>
        ///     The room code of the newly created room
        /// </returns>
        string CreateRoom();

        /// <summary>
        ///     Checks if a room with the given code exists
        /// </summary>
        /// <param name="code">
        ///     The room code to check
        /// </param>
        /// <returns>
        ///     <c>true</c> if the room exists, <c>false</c> otherwise
        /// </returns>
        bool RoomExists(string code);

        /// <summary>
        ///     Adds a user to a room
        /// </summary>
        /// <param name="roomCode">
        ///     The room code to add the user to
        /// </param>
        /// <param name="connectionId">
        ///     The connection ID of the user to add
        /// </param>
        void AddUserToRoom(string roomCode, string connectionId);

        /// <summary>
        /// Removes a user from a room
        /// </summary>
        /// <param name="connectionId">
        /// The connection ID of the user to remove
        /// </param>
        void RemoveUserFromRoom(string connectionId);

        /// <summary>
        /// Checks if a room has no users
        /// </summary>
        /// <param name="roomCode">
        /// The room code to check
        /// </param>
        /// <returns>
        /// <c>true</c> if the room is empty, <c>false</c> otherwise
        /// </returns>
        bool IsRoomEmpty(string roomCode);

        /// <summary>
        /// Gets the number of users in a room
        /// </summary>
        /// <param name="roomCode">
        /// The room code to check
        /// </param>
        /// <returns>
        /// The number of users in the room, or <c>0</c> if the room doesn't exist
        /// </returns>
        int GetRoomUserCount(string roomCode);

        /// <summary>
        ///     Gets the room that a user is in
        /// </summary>
        /// <param name="connectionId">
        ///     The connection ID of the user
        /// </param>
        /// <returns>
        ///     The room code that the user is in, or <c>null</c> if the user is not in a room
        /// </returns>
        string? GetUserRoom(string connectionId);

        void StoreWhiteboardState(string roomCode, string whiteboardState);
        string GetWhiteboardState(string roomCode);

    }

    public class RoomService : IRoomService
    {
        /// <summary>
        ///    A dictionary of active rooms, with the room code as the key and a set of connection IDs as the value
        /// </summary>
        private static readonly Dictionary<string, HashSet<string>> _activeRooms = new Dictionary<string, HashSet<string>>();
        /// <summary>
        ///   A dictionary mapping connection IDs to room codes
        /// </summary>
        private static readonly Dictionary<string, string> _connectionRoomMap = new Dictionary<string, string>();
        /// <summary>
        ///   A dictionary storing whiteboard states for each room
        /// </summary>
        private static readonly Dictionary<string, string> _whiteboardStates = new Dictionary<string, string>();
        /// <summary>
        ///   A random number generator for generating room codes
        /// </summary>
        private static readonly Random _random = new Random();
        /// <summary>
        ///  A lock object for thread safety
        /// </summary>
        private static readonly object _lock = new object();

        public string CreateRoom()
        {
            lock (_lock)
            {
                // Generate a unique 5-letter room code
                string roomCode;
                do
                {
                    // Generate a 5-letter code (A-Z)
                    char[] code = new char[5];
                    for (int i = 0; i < 5; i++)
                    {
                        code[i] = (char)(_random.Next(26) + 'A');
                    }
                    roomCode = new string(code);
                } while (_activeRooms.ContainsKey(roomCode));

                // Add to active rooms with empty user set
                _activeRooms.Add(roomCode, new HashSet<string>());

                return roomCode;
            }
        }

        public bool RoomExists(string code)
        {
            if (string.IsNullOrEmpty(code))
                return false;


            lock (_lock)
            {
                // Convert to uppercase for case-insensitive comparison
                return _activeRooms.ContainsKey(code.ToUpper());
            }
        }

        public void AddUserToRoom(string roomCode, string connectionId)
        {
            if (string.IsNullOrEmpty(roomCode) || string.IsNullOrEmpty(connectionId))
                return;

            roomCode = roomCode.ToUpper();

            lock (_lock)
            {
                // If room does not exist, do nothing
                if (!_activeRooms.ContainsKey(roomCode))
                    return;

                // Add user to room
                _activeRooms[roomCode].Add(connectionId);
                _connectionRoomMap[connectionId] = roomCode;
            }
        }

        public void RemoveUserFromRoom(string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId))
                return;

            lock (_lock)
            {
                if (!_connectionRoomMap.ContainsKey(connectionId))
                    return;

                string roomCode = _connectionRoomMap[connectionId];
                _connectionRoomMap.Remove(connectionId);

                if (_activeRooms.ContainsKey(roomCode))
                {
                    _activeRooms[roomCode].Remove(connectionId);

                    // If room is empty, remove it
                    if (_activeRooms[roomCode].Count == 0)
                    {
                        _activeRooms.Remove(roomCode);
                    }
                }
            }
        }

        public bool IsRoomEmpty(string roomCode)
        {
            // Checks the hashmap if the room is empty
            // If the room does not exist, it is considered empty
            // If the room exists but has no users, it is considered empty
            if (string.IsNullOrEmpty(roomCode))
                return true;

            roomCode = roomCode.ToUpper();

            lock (_lock)
            {
                if (!_activeRooms.ContainsKey(roomCode))
                    return true;

                return _activeRooms[roomCode].Count == 0;
            }
        }

        public int GetRoomUserCount(string roomCode)
        {
            if (string.IsNullOrEmpty(roomCode))
                return 0;

            roomCode = roomCode.ToUpper();

            lock (_lock)
            {
                // returns 0 if the room does not exist
                if (!_activeRooms.ContainsKey(roomCode))
                    return 0;

                return _activeRooms[roomCode].Count;
            }
        }

        public string? GetUserRoom(string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId))
                return null;

            lock (_lock)
            {
                // If the connection ID is in the room storage, return the associated room code
                if (_connectionRoomMap.TryGetValue(connectionId, out string? roomCode))
                    return roomCode;
                // Otherwise, return null
                return null;
            }
        }

        public void StoreWhiteboardState(string roomCode, string whiteboardState)
        {
            if (string.IsNullOrEmpty(roomCode))
                return;

            roomCode = roomCode.ToUpper();

            lock (_lock)
            {
                // If room does not exist, do nothing
                if (!_activeRooms.ContainsKey(roomCode))
                    return;

                // If whiteboardState is null, remove the state if it exists
                if (whiteboardState == null)
                {
                    if (_whiteboardStates.ContainsKey(roomCode))
                    {
                        _whiteboardStates.Remove(roomCode);
                    }
                    return;
                }

                // Store or update the whiteboard state
                _whiteboardStates[roomCode] = whiteboardState;
            }
        }

        public string GetWhiteboardState(string roomCode)
        {
            if (string.IsNullOrEmpty(roomCode))
                return null;

            roomCode = roomCode.ToUpper();

            lock (_lock)
            {
                // Return the whiteboard state if it exists, otherwise null
                if (_whiteboardStates.TryGetValue(roomCode, out string state))
                    return state;
                return null;
            }
        }



    }
}
