using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.SignalR;
using CodeCollaborationWebApp.Hubs;
using CodeCollaborationWebApp.Services;

namespace CodeCollaborationWebApp.Tests
{
    public class CollaborationHubTests
    {
        private readonly Mock<IHubCallerClients> _mockClients;
        private readonly Mock<ISingleClientProxy> _mockClientProxy;
        private readonly Mock<IGroupManager> _mockGroups;
        private readonly Mock<HubCallerContext> _mockContext;
        private readonly Mock<IRoomService> _mockRoomService;
        private readonly CollaborationHub _hub;

        public CollaborationHubTests()
        {
            _mockClients = new Mock<IHubCallerClients>();
            _mockClientProxy = new Mock<ISingleClientProxy>();
            _mockGroups = new Mock<IGroupManager>();
            _mockContext = new Mock<HubCallerContext>();
            _mockRoomService = new Mock<IRoomService>();

            // Setup caller and others
            _mockClients.Setup(clients => clients.Caller).Returns(_mockClientProxy.Object);
            _mockClients.Setup(clients => clients.OthersInGroup(It.IsAny<string>())).Returns(_mockClientProxy.Object);
            _mockClients.Setup(clients => clients.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);

            // Setup context
            _mockContext.Setup(c => c.ConnectionId).Returns("testConnectionId");

            // Create hub with mocks
            _hub = new CollaborationHub(_mockRoomService.Object)
            {
                Clients = _mockClients.Object,
                Groups = _mockGroups.Object,
                Context = _mockContext.Object
            };
        }

        [Fact]
        public async Task JoinRoom_ValidRoom_AddsUserToRoomAndSendsInitialState()
        {
            // Arrange
            string roomCode = "ABCDE";
            string connectionId = "testConnectionId";
            string whiteboardState = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAA";

            _mockRoomService.Setup(s => s.RoomExists(roomCode)).Returns(true);
            _mockRoomService.Setup(s => s.GetRoomUserCount(roomCode)).Returns(1);
            _mockRoomService.Setup(s => s.GetWhiteboardState(roomCode)).Returns(whiteboardState);

            // Act
            await _hub.JoinRoom(roomCode);

            // Assert
            _mockRoomService.Verify(s => s.AddUserToRoom(roomCode, connectionId), Times.Once);
            _mockGroups.Verify(g => g.AddToGroupAsync(connectionId, roomCode, It.IsAny<CancellationToken>()), Times.Once);

            // Verify initial state is sent
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "InitializeWhiteboard",
                    It.Is<object[]>(o => o.Length == 1 && o[0].ToString() == whiteboardState),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task JoinRoom_InvalidRoom_SendsRoomNotFoundMessage()
        {
            // Arrange
            string roomCode = "NONEX";

            _mockRoomService.Setup(s => s.RoomExists(roomCode)).Returns(false);

            // Act
            await _hub.JoinRoom(roomCode);

            // Assert
            _mockRoomService.Verify(s => s.AddUserToRoom(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockGroups.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

            // Verify RoomNotFound message is sent
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "RoomNotFound",
                    It.Is<object[]>(o => o.Length == 0),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task JoinRoom_NoWhiteboardState_DoesNotSendInitializeWhiteboard()
        {
            // Arrange
            string roomCode = "ABCDE";
            string connectionId = "testConnectionId";

            _mockRoomService.Setup(s => s.RoomExists(roomCode)).Returns(true);
            _mockRoomService.Setup(s => s.GetRoomUserCount(roomCode)).Returns(1);
            _mockRoomService.Setup(s => s.GetWhiteboardState(roomCode)).Returns((string)null);

            // Act
            await _hub.JoinRoom(roomCode);

            // Assert
            _mockRoomService.Verify(s => s.AddUserToRoom(roomCode, connectionId), Times.Once);

            // Verify InitializeWhiteboard is not sent
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "InitializeWhiteboard",
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.Never
            );
        }

        [Fact]
        public async Task SendWhiteboardUpdate_ValidRoom_SendsUpdateToOthers()
        {
            // Arrange
            string roomCode = "ABCDE";
            string updateData = "{\"x1\":100,\"y1\":100,\"x2\":200,\"y2\":200,\"color\":\"#000000\",\"tool\":\"pen\"}";

            // Act
            await _hub.SendWhiteboardUpdate(roomCode, updateData);

            // Assert
            _mockClients.Verify(c => c.OthersInGroup(roomCode), Times.Once);
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ReceiveWhiteboardUpdate",
                    It.Is<object[]>(o => o.Length == 1 && o[0].ToString() == updateData),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task SendWhiteboardState_ValidRoom_StoresState()
        {
            // Arrange
            string roomCode = "ABCDE";
            string whiteboardState = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAA";

            // Act
            await _hub.SendWhiteboardState(roomCode, whiteboardState);

            // Assert
            _mockRoomService.Verify(s => s.StoreWhiteboardState(roomCode, whiteboardState), Times.Once);
        }

        [Fact]
        public async Task SendWhiteboardClear_ValidRoom_ClearsStateAndNotifiesOthers()
        {
            // Arrange
            string roomCode = "ABCDE";

            // Act
            await _hub.SendWhiteboardClear(roomCode);

            // Assert
            _mockRoomService.Verify(s => s.StoreWhiteboardState(roomCode, null), Times.Once);
            _mockClients.Verify(c => c.OthersInGroup(roomCode), Times.Once);
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ReceiveWhiteboardClear",
                    It.Is<object[]>(o => o.Length == 0),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task OnDisconnectedAsync_UserInRoom_RemovesUserAndNotifiesOthers()
        {
            // Arrange
            string roomCode = "ABCDE";
            string connectionId = "testConnectionId";
            int remainingUsers = 1;

            _mockRoomService.Setup(s => s.GetUserRoom(connectionId)).Returns(roomCode);
            _mockRoomService.Setup(s => s.GetRoomUserCount(roomCode)).Returns(remainingUsers);

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert
            _mockRoomService.Verify(s => s.RemoveUserFromRoom(connectionId), Times.Once);
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "UserLeft",
                    It.Is<object[]>(o => o.Length == 1 && (int)o[0] == remainingUsers),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task OnDisconnectedAsync_UserNotInRoom_DoesNotRemoveUserOrNotifyOthers()
        {
            // Arrange
            string connectionId = "testConnectionId";

            _mockRoomService.Setup(s => s.GetUserRoom(connectionId)).Returns((string)null);

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert
            _mockRoomService.Verify(s => s.RemoveUserFromRoom(It.IsAny<string>()), Times.Never);
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "UserLeft",
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.Never
            );
        }



    }
}