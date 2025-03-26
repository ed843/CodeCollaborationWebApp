using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using CodeCollaborationWebApp.Services;
using Xunit.Sdk;

namespace CodeCollaborationWebApp.Tests
{
    public class RedisRoomServiceTests
    {
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly Mock<ILogger<RedisRoomService>> _mockLogger;
        private readonly RedisRoomService _redisRoomService;

        public RedisRoomServiceTests()
        {
            _mockCache = new Mock<IDistributedCache>();
            _mockLogger = new Mock<ILogger<RedisRoomService>>();
            _redisRoomService = new RedisRoomService(_mockCache.Object, _mockLogger.Object);
        }

        [Fact]
        public void StoreWhiteboardState_ValidRoomAndState_StateIsStored()
        {
            // Arrange
            string roomCode = "ABCDE";
            string whiteboardState = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAA";
            
            // Setup mock to return that room exists
            _mockCache.Setup(c => c.Get(It.Is<string>(s => s.Contains(roomCode))))
                .Returns(Encoding.UTF8.GetBytes("1"));

            // Act
            _redisRoomService.StoreWhiteboardState(roomCode, whiteboardState);

            // Assert
            _mockCache.Verify(c => c.Set(
                It.Is<string>(s => s.Contains(roomCode) && s.Contains("whiteboard")),
                It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == whiteboardState),
                It.IsAny<DistributedCacheEntryOptions>()
            ), Times.Once);
        }

        [Fact]
        public void StoreWhiteboardState_NullState_RemovesExistingState()
        {
            // Arrange
            string roomCode = "ABCDE";

            // Setup mock to return that room exists
            _mockCache.Setup(c => c.Get(It.Is<string>(s => s.Contains(roomCode))))
                .Returns(Encoding.UTF8.GetBytes("1"));

            // Act
            _redisRoomService.StoreWhiteboardState(roomCode, null);

            // Assert
            _mockCache.Verify(c => c.Remove(
                It.Is<string>(s => s.Contains(roomCode) && s.Contains("whiteboard"))
            ), Times.Once);
        }

        [Fact]
        public void StoreWhiteboardState_EmptyRoomCode_DoesNotStoreState()
        {
            // Arrange
            string emptyRoomCode = "";
            string whiteboardState = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAA";

            // Act
            _redisRoomService.StoreWhiteboardState(emptyRoomCode, whiteboardState);

            // Assert
            _mockCache.Verify(c => c.Set(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>()
            ), Times.Never);
        }

        [Fact]
        public void StoreWhiteboardState_NonExistentRoom_DoesNotStoreState()
        {
            // Arrange
            string nonExistentRoom = "NONEX";
            string whiteboardState = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAA";

            // Setup mock to return null for room existence check
            _mockCache.Setup(c => c.Get(It.Is<string>(s => s.Contains(nonExistentRoom) && !s.Contains("whiteboard"))))
                .Returns((byte[])null);

            // Act
            _redisRoomService.StoreWhiteboardState(nonExistentRoom, whiteboardState);

            // Assert
            // Verify that Get was called for room existence check
            _mockCache.Verify(c => c.Get(It.Is<string>(s => s.Contains(nonExistentRoom) && !s.Contains("whiteboard"))), Times.Once);

            // Verify that Set was not called for whiteboard state
            _mockCache.Verify(c => c.Set(
                It.Is<string>(s => s.Contains(nonExistentRoom) && s.Contains("whiteboard")),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>()
            ), Times.Never);
        }


        [Fact]
        public void GetWhiteboardState_ValidRoom_ReturnsState()
        {
            // Arrange
            string roomCode = "ABCDE";
            string expectedState = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAA";

            // Setup mock to return that room exists
            _mockCache.Setup(c => c.Get(It.Is<string>(s => s.Contains(roomCode) && !s.Contains("whiteboard"))))
                .Returns(Encoding.UTF8.GetBytes("1"));

            // Setup mock to return the whiteboard state
            _mockCache.Setup(c => c.Get(It.Is<string>(s => s.Contains(roomCode) && s.Contains("whiteboard"))))
                .Returns(Encoding.UTF8.GetBytes(expectedState));

            // Act
            string retrievedState = _redisRoomService.GetWhiteboardState(roomCode);

            // Assert
            Assert.Equal(expectedState, retrievedState);
        }

        [Fact]
        public void GetWhiteboardState_EmptyRoomCode_ReturnsNull()
        {
            // Arrange
            string emptyRoomCode = "";
            _mockCache.Reset(); // Reset all interactions

            // Act
            string retrievedState = _redisRoomService.GetWhiteboardState(emptyRoomCode);

            // Assert
            Assert.Null(retrievedState);
        }


        [Fact]
        public void GetWhiteboardState_NonExistentRoom_ReturnsNull()
        {
            // Arrange
            string nonExistentRoom = "NONEX";

            // Setup mock to return that room doesn't exist
            _mockCache.Setup(c => c.Get(It.Is<string>(s => s.Contains(nonExistentRoom) && !s.Contains("whiteboard"))))
                .Returns((byte[])null);

            // Setup mock for whiteboard state (should not be called, but just in case)
            _mockCache.Setup(c => c.Get(It.Is<string>(s => s.Contains(nonExistentRoom) && s.Contains("whiteboard"))))
                .Returns((byte[])null);

            // Act
            string retrievedState = _redisRoomService.GetWhiteboardState(nonExistentRoom);

            // Assert
            Assert.Null(retrievedState);

            // Verify that Get was called for room existence check
            _mockCache.Verify(c => c.Get(It.Is<string>(s => s.Contains(nonExistentRoom) && !s.Contains("whiteboard"))), Times.Once);

            // Verify that Get was not called for whiteboard state
            _mockCache.Verify(c => c.Get(It.Is<string>(s => s.Contains(nonExistentRoom) && s.Contains("whiteboard"))), Times.Never);
        }

        [Fact]
        public void StoreWhiteboardState_ExceptionThrown_LogsError()
        {
            // Arrange
            string roomCode = "ABCDE";
            string whiteboardState = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAA";

            // Setup mock to return that room exists
            _mockCache.Setup(c => c.Get(It.Is<string>(s => s.Contains(roomCode))))
                .Returns(Encoding.UTF8.GetBytes("1"));

            // Setup mock to throw exception when Set is called
            _mockCache.Setup(c => c.Set(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>()
            )).Throws(new Exception("Test exception"));

            // Act
            _redisRoomService.StoreWhiteboardState(roomCode, whiteboardState);

            // Assert - verify that error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public void GetWhiteboardState_ExceptionThrown_LogsErrorAndReturnsNull()
        {
            // Arrange
            string roomCode = "ABCDE";

            // Setup mock to return that room exists
            _mockCache.Setup(c => c.Get(It.Is<string>(s => s.Contains(roomCode) && !s.Contains("whiteboard"))))
                .Returns(Encoding.UTF8.GetBytes("1"));

            // Setup mock to throw exception when Get is called for whiteboard state
            _mockCache.Setup(c => c.Get(It.Is<string>(s => s.Contains(roomCode) && s.Contains("whiteboard"))))
                .Throws(new Exception("Test exception"));

            // Act
            string retrievedState = _redisRoomService.GetWhiteboardState(roomCode);

            // Assert
            Assert.Null(retrievedState);

            // Verify that error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }
    }
}
