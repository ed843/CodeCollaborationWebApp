namespace CodeCollaborationWebApp
{
    public interface IRoomService
    {
        string CreateRoom();
        bool RoomExists(string code);
        void AddUserToRoom(string roomCode, string connectionId);
        void RemoveUserFromRoom(string connectionId);
        bool IsRoomEmpty(string roomCode);
        int GetRoomUserCount(string roomCode);
        string GetUserRoom(string connectionId);
    }

    public class RoomService : IRoomService
    {
        // In-memory storage for active rooms
        private static readonly Dictionary<string, HashSet<string>> _activeRooms = new Dictionary<string, HashSet<string>>();
        // Track which room a connection belongs to
        private static readonly Dictionary<string, string> _connectionRoomMap = new Dictionary<string, string>();
        private static readonly Random _random = new Random();
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
                if (!_activeRooms.ContainsKey(roomCode))
                    return;

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
                if (!_activeRooms.ContainsKey(roomCode))
                    return 0;

                return _activeRooms[roomCode].Count;
            }
        }

        public string GetUserRoom(string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId))
                return null;

            lock (_lock)
            {
                if (_connectionRoomMap.TryGetValue(connectionId, out string roomCode))
                    return roomCode;

                return null;
            }
        }
    }
}
