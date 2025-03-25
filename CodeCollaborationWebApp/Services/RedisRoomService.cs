using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeCollaborationWebApp.Services
{
    public class RedisRoomService : IRoomService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisRoomService> _logger;
        private readonly Random _random = new Random();
        private const string RoomPrefix = "room:";
        private const string UserPrefix = "user:";

        private const string ChunkCountPrefix = "room:chunk:count:";
        private const string ChunkDataPrefix = "room:chunk:data:";

        public RedisRoomService(IDistributedCache cache, ILogger<RedisRoomService> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Test Redis connection on startup
            try
            {
                _cache.GetString("test-connection");
                _logger.LogInformation("Redis connection successfully established");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Redis cache on startup");
                // Don't throw here - we'll log the error but allow the app to start
            }
        }

        public string CreateRoom()
        {
            try
            {
                string roomCode;
                bool roomExists;

                // Generate a unique 5-letter room code
                do
                {
                    // Generate a 5-letter code (A-Z)
                    char[] code = new char[5];
                    for (int i = 0; i < 5; i++)
                    {
                        code[i] = (char)(_random.Next(26) + 'A');
                    }
                    roomCode = new string(code);

                    // Check if the room already exists
                    roomExists = RoomExists(roomCode);

                } while (roomExists);

                // Create empty room and store it
                var roomUsers = new HashSet<string>();
                var serializedData = JsonConvert.SerializeObject(roomUsers);

                _cache.SetString(
                    $"{RoomPrefix}{roomCode}",
                    serializedData,
                    new DistributedCacheEntryOptions
                    {
                        // Set expiration time
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                    }
                );

                _logger.LogInformation("Created new room with code: {RoomCode}", roomCode);
                return roomCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating room");
                throw; // Rethrow to let the controller handle it
            }
        }

        public bool RoomExists(string code)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                    return false;

                // Convert to uppercase for case-insensitive comparison
                code = code.ToUpper();

                // Check if room exists in cache
                var roomData = _cache.GetString($"{RoomPrefix}{code}");
                return !string.IsNullOrEmpty(roomData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if room exists: {RoomCode}", code);
                return false; // Assume room doesn't exist if we can't check
            }
        }

        public void AddUserToRoom(string roomCode, string connectionId)
        {
            try
            {
                if (string.IsNullOrEmpty(roomCode) || string.IsNullOrEmpty(connectionId))
                    return;

                roomCode = roomCode.ToUpper();

                // Get current room data
                var roomData = _cache.GetString($"{RoomPrefix}{roomCode}");
                if (string.IsNullOrEmpty(roomData))
                    return; // Room doesn't exist

                // Deserialize room users
                var roomUsers = JsonConvert.DeserializeObject<HashSet<string>>(roomData);

                // Add user to room
                roomUsers.Add(connectionId);

                // Save updated room data
                _cache.SetString(
                    $"{RoomPrefix}{roomCode}",
                    JsonConvert.SerializeObject(roomUsers),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                    }
                );

                // Map user to room
                _cache.SetString(
                    $"{UserPrefix}{connectionId}",
                    roomCode,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                    }
                );

                _logger.LogDebug("Added user {ConnectionId} to room {RoomCode}", connectionId, roomCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user {ConnectionId} to room {RoomCode}", connectionId, roomCode);
            }
        }

        public void RemoveUserFromRoom(string connectionId)
        {
            try
            {
                if (string.IsNullOrEmpty(connectionId))
                    return;

                // Get user's room
                var roomCode = _cache.GetString($"{UserPrefix}{connectionId}");
                if (string.IsNullOrEmpty(roomCode))
                    return; // User not in any room

                // Remove user mapping
                _cache.Remove($"{UserPrefix}{connectionId}");

                // Get room data
                var roomData = _cache.GetString($"{RoomPrefix}{roomCode}");
                if (string.IsNullOrEmpty(roomData))
                    return; // Room doesn't exist (shouldn't happen)

                // Deserialize room users
                var roomUsers = JsonConvert.DeserializeObject<HashSet<string>>(roomData);

                // Remove user from room
                roomUsers.Remove(connectionId);

                // If room is empty, remove it
                if (roomUsers.Count == 0)
                {
                    _cache.Remove($"{RoomPrefix}{roomCode}");
                    _logger.LogInformation("Removed empty room {RoomCode}", roomCode);
                }
                else
                {
                    // Save updated room data
                    _cache.SetString(
                        $"{RoomPrefix}{roomCode}",
                        JsonConvert.SerializeObject(roomUsers),
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                        }
                    );
                }

                _logger.LogDebug("Removed user {ConnectionId} from room {RoomCode}", connectionId, roomCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user {ConnectionId} from room", connectionId);
            }
        }

        public bool IsRoomEmpty(string roomCode)
        {
            try
            {
                if (string.IsNullOrEmpty(roomCode))
                    return true;

                roomCode = roomCode.ToUpper();

                // Get room data
                var roomData = _cache.GetString($"{RoomPrefix}{roomCode}");
                if (string.IsNullOrEmpty(roomData))
                    return true; // Room doesn't exist

                // Deserialize room users
                var roomUsers = JsonConvert.DeserializeObject<HashSet<string>>(roomData);

                return roomUsers.Count == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if room {RoomCode} is empty", roomCode);
                return true; // Assume room is empty if we can't check
            }
        }

        public int GetRoomUserCount(string roomCode)
        {
            try
            {
                if (string.IsNullOrEmpty(roomCode))
                    return 0;

                roomCode = roomCode.ToUpper();

                // Get room data
                var roomData = _cache.GetString($"{RoomPrefix}{roomCode}");
                if (string.IsNullOrEmpty(roomData))
                    return 0; // Room doesn't exist

                // Deserialize room users
                var roomUsers = JsonConvert.DeserializeObject<HashSet<string>>(roomData);

                return roomUsers.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user count for room {RoomCode}", roomCode);
                return 0; // Return 0 if we can't get the count
            }
        }

        public string GetUserRoom(string connectionId)
        {
            try
            {
                if (string.IsNullOrEmpty(connectionId))
                    return null;

                // Get user's room directly from cache
                return _cache.GetString($"{UserPrefix}{connectionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting room for user {ConnectionId}", connectionId);
                return null; // Return null if we can't get the room
            }
        }

        // Add these methods to RedisRoomService.cs
        public void StoreWhiteboardState(string roomCode, string whiteboardState)
        {
            try
            {
                if (string.IsNullOrEmpty(roomCode))
                    return;

                roomCode = roomCode.ToUpper();

                _cache.SetString(
                    $"{RoomPrefix}{roomCode}:whiteboard",
                    whiteboardState,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                    }
                );

                _logger.LogDebug("Stored whiteboard state for room {RoomCode}", roomCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing whiteboard state for room {RoomCode}", roomCode);
            }
        }

        public string GetWhiteboardState(string roomCode)
        {
            try
            {
                if (string.IsNullOrEmpty(roomCode))
                    return null;

                roomCode = roomCode.ToUpper();

                return _cache.GetString($"{RoomPrefix}{roomCode}:whiteboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting whiteboard state for room {RoomCode}", roomCode);
                return null;
            }
        }

    }
}