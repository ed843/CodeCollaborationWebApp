using System;
using System.Collections.Generic;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using CodeCollaborationWebApp.Services;

namespace CodeCollaborationWebApp.Tests
{
    public class RoomServiceTests
    {
        private readonly RoomService _roomService;

        public RoomServiceTests()
        {
            _roomService = new RoomService();
        }

        [Fact]
        public void StoreWhiteboardState_ValidRoomAndState_StateIsStored()
        {
            // Arrange
            string roomCode = _roomService.CreateRoom();
            string whiteboardState = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAA";

            // Act
            _roomService.StoreWhiteboardState(roomCode, whiteboardState);
            string retrievedState = _roomService.GetWhiteboardState(roomCode);

            // Assert
            Assert.Equal(whiteboardState, retrievedState);
        }

        [Fact]
        public void StoreWhiteboardState_NullState_RemovesExistingState()
        {
            // Arrange
            string roomCode = _roomService.CreateRoom();
            string initialState = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAA";
            _roomService.StoreWhiteboardState(roomCode, initialState);

            // Act
            _roomService.StoreWhiteboardState(roomCode, null);
            string retrievedState = _roomService.GetWhiteboardState(roomCode);

            // Assert
            Assert.Null(retrievedState);
        }

        [Fact]
        public void StoreWhiteboardState_EmptyRoomCode_DoesNotThrowException()
        {
            // Arrange
            string emptyRoomCode = "";
            string whiteboardState = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAA";

            // Act & Assert
            var exception = Record.Exception(() => _roomService.StoreWhiteboardState(emptyRoomCode, whiteboardState));
            Assert.Null(exception);
        }

        [Fact]
        public void StoreWhiteboardState_NonExistentRoom_DoesNotStoreState()
        {
            // Arrange
            string nonExistentRoom = "ABCDE";
            string whiteboardState = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAA";

            // Act
            _roomService.StoreWhiteboardState(nonExistentRoom, whiteboardState);
            string retrievedState = _roomService.GetWhiteboardState(nonExistentRoom);

            // Assert
            Assert.Null(retrievedState);
        }

        [Fact]
        public void GetWhiteboardState_EmptyRoomCode_ReturnsNull()
        {
            // Arrange
            string emptyRoomCode = "";

            // Act
            string retrievedState = _roomService.GetWhiteboardState(emptyRoomCode);

            // Assert
            Assert.Null(retrievedState);
        }

        [Fact]
        public void GetWhiteboardState_NonExistentRoom_ReturnsNull()
        {
            // Arrange
            string nonExistentRoom = "NONEX";

            // Act
            string retrievedState = _roomService.GetWhiteboardState(nonExistentRoom);

            // Assert
            Assert.Null(retrievedState);
        }

        [Fact]
        public void StoreWhiteboardState_CaseInsensitiveRoomCode_StoresCorrectly()
        {
            // Arrange
            string roomCode = _roomService.CreateRoom();
            string lowerRoomCode = roomCode.ToLower();
            string whiteboardState = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAA";

            // Act
            _roomService.StoreWhiteboardState(lowerRoomCode, whiteboardState);
            string retrievedState = _roomService.GetWhiteboardState(roomCode);

            // Assert
            Assert.Equal(whiteboardState, retrievedState);
        }

        [Fact]
        public void GetWhiteboardState_CaseInsensitiveRoomCode_RetrievesCorrectly()
        {
            // Arrange
            string roomCode = _roomService.CreateRoom();
            string whiteboardState = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAA";
            _roomService.StoreWhiteboardState(roomCode, whiteboardState);

            // Act
            string retrievedState = _roomService.GetWhiteboardState(roomCode.ToLower());

            // Assert
            Assert.Equal(whiteboardState, retrievedState);
        }

        [Fact]
        public void StoreWhiteboardState_RoomIsRemoved_StateIsRemoved()
        {
            // Arrange
            string roomCode = _roomService.CreateRoom();
            string connectionId = "connection123";
            string whiteboardState = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAA";

            _roomService.AddUserToRoom(roomCode, connectionId);
            _roomService.StoreWhiteboardState(roomCode, whiteboardState);

            // Act
            _roomService.RemoveUserFromRoom(connectionId); // This should remove the room as it's the only user
            string retrievedState = _roomService.GetWhiteboardState(roomCode);

            // Assert
            Assert.Null(retrievedState);
        }

        [Fact]
        public void StoreWhiteboardState_LargeState_StoresCorrectly()
        {
            // Arrange
            string roomCode = _roomService.CreateRoom();
            // Create a large whiteboard state (10KB)
            string largeState = "data:image/png;base64," + new string('A', 10000);

            // Act
            _roomService.StoreWhiteboardState(roomCode, largeState);
            string retrievedState = _roomService.GetWhiteboardState(roomCode);

            // Assert
            Assert.Equal(largeState, retrievedState);
        }
    }
}
