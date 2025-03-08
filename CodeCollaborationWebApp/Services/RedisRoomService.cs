using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeCollaborationWebApp.Services
{
    public class RedisRoomService : IRoomService
    {
        private readonly IDistributedCache _cache;
        private readonly Random _random = new Random();
        private const string RoomPrefix = "room:";
        private const string UserPrefix = "user:";

        public RedisRoomService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public string CreateRoom()
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
                var roomData = _cache.GetString($"{RoomPrefix}{roomCode}");
                roomExists = !string.IsNullOrEmpty(roomData);

            } while (roomExists);

            // Create empty room and store it
            var roomUsers = new HashSet<string>();
            _cache.SetString(
                $"{RoomPrefix}{roomCode}",
                JsonConvert.SerializeObject(roomUsers),
                new DistributedCacheEntryOptions
                {
                    // Set expiration time - adjust as needed
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                }
            );

            return roomCode;
        }

        public bool RoomExists(string code)
        {
            if (string.IsNullOrEmpty(code))
                return false;

            // Convert to uppercase for case-insensitive comparison
            code = code.ToUpper();

            // Check if room exists in cache
            var roomData = _cache.GetString($"{RoomPrefix}{code}");
            return !string.IsNullOrEmpty(roomData);
        }

        public void AddUserToRoom(string roomCode, string connectionId)
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
        }

        public void RemoveUserFromRoom(string connectionId)
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
        }

        public bool IsRoomEmpty(string roomCode)
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

        public int GetRoomUserCount(string roomCode)
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

        public string GetUserRoom(string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId))
                return null;

            // Get user's room directly from cache
            return _cache.GetString($"{UserPrefix}{connectionId}");
        }
    }
}