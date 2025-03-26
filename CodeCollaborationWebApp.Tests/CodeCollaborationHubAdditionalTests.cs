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
    public class CollaborationHubAdditionalTests
    {
        private (CollaborationHub hub, Mock<IHubCallerClients> mockClients, Mock<IClientProxy> mockClientProxy,
                 Mock<ISingleClientProxy> mockSingleClientProxy, Mock<IGroupManager> mockGroups,
                 Mock<HubCallerContext> mockContext, Mock<IRoomService> mockRoomService)
        CreateHubWithMocks()
        {
            var mockClients = new Mock<IHubCallerClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            var mockSingleClientProxy = new Mock<ISingleClientProxy>();
            var mockGroups = new Mock<IGroupManager>();
            var mockContext = new Mock<HubCallerContext>();
            var mockRoomService = new Mock<IRoomService>();

            mockClients.Setup(clients => clients.Caller).Returns(mockSingleClientProxy.Object);
            mockClients.Setup(clients => clients.OthersInGroup(It.IsAny<string>())).Returns(mockClientProxy.Object);
            mockClients.Setup(clients => clients.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
            mockContext.Setup(c => c.ConnectionId).Returns("testConnectionId");

            var hub = new CollaborationHub(mockRoomService.Object)
            {
                Clients = mockClients.Object,
                Groups = mockGroups.Object,
                Context = mockContext.Object
            };

            return (hub, mockClients, mockClientProxy, mockSingleClientProxy, mockGroups, mockContext, mockRoomService);
        }

        [Fact]
        public async Task SendCodeUpdate_ValidRoom_SendsUpdateToOthers()
        {
            // Arrange
            var (hub, _, mockClientProxy, _, _, _, _) = CreateHubWithMocks();
            string roomCode = "ABCDE";
            string code = "console.log('Hello World');";

            // Act
            await hub.SendCodeUpdate(roomCode, code);

            // Assert
            mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ReceiveCodeUpdate",
                    It.Is<object[]>(o => o.Length == 1 && o[0].ToString() == code),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task SendLanguageChange_ValidRoom_SendsUpdateToOthers()
        {
            // Arrange
            var (hub, _, mockClientProxy, _, _, _, _) = CreateHubWithMocks();
            string roomCode = "ABCDE";
            string language = "javascript";

            // Act
            await hub.SendLanguageChange(roomCode, language);

            // Assert
            mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ReceiveLanguageChange",
                    It.Is<object[]>(o => o.Length == 1 && o[0].ToString() == language),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task SendOutputUpdate_ValidRoom_SendsUpdateToOthers()
        {
            // Arrange
            var (hub, _, mockClientProxy, _, _, _, _) = CreateHubWithMocks();
            string roomCode = "ABCDE";
            string output = "Hello World";

            // Act
            await hub.SendOutputUpdate(roomCode, output);

            // Assert
            mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ReceiveOutputUpdate",
                    It.Is<object[]>(o => o.Length == 1 && o[0].ToString() == output),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task JoinRoom_RoomWithWhiteboardState_SendsInitializeWhiteboard()
        {
            // Arrange
            var (hub, _, _, mockSingleClientProxy, _, _, mockRoomService) = CreateHubWithMocks();
            string roomCode = "ABCDE";
            string whiteboardState = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAA";

            mockRoomService.Setup(s => s.RoomExists(roomCode)).Returns(true);
            mockRoomService.Setup(s => s.GetRoomUserCount(roomCode)).Returns(1);
            mockRoomService.Setup(s => s.GetWhiteboardState(roomCode)).Returns(whiteboardState);

            // Act
            await hub.JoinRoom(roomCode);

            // Assert
            mockSingleClientProxy.Verify(
                c => c.SendCoreAsync(
                    "InitializeWhiteboard",
                    It.Is<object[]>(o => o.Length == 1 && o[0].ToString() == whiteboardState),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task JoinRoom_RoomWithoutWhiteboardState_DoesNotSendInitializeWhiteboard()
        {
            // Arrange
            var (hub, _, _, mockSingleClientProxy, _, _, mockRoomService) = CreateHubWithMocks();
            string roomCode = "ABCDE";

            mockRoomService.Setup(s => s.RoomExists(roomCode)).Returns(true);
            mockRoomService.Setup(s => s.GetRoomUserCount(roomCode)).Returns(1);
            mockRoomService.Setup(s => s.GetWhiteboardState(roomCode)).Returns((string)null);

            // Act
            await hub.JoinRoom(roomCode);

            // Assert
            mockSingleClientProxy.Verify(
                c => c.SendCoreAsync(
                    "InitializeWhiteboard",
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.Never
            );
        }

        [Fact]
        public async Task OnDisconnectedAsync_LastUserInRoom_DoesNotSendUserLeft()
        {
            // Arrange
            var (hub, _, mockClientProxy, _, _, _, mockRoomService) = CreateHubWithMocks();
            string roomCode = "ABCDE";
            string connectionId = "testConnectionId";

            mockRoomService.Setup(s => s.GetUserRoom(connectionId)).Returns(roomCode);
            mockRoomService.Setup(s => s.GetRoomUserCount(roomCode)).Returns(0); // Last user left

            // Act
            await hub.OnDisconnectedAsync(null);

            // Assert
            mockClientProxy.Verify(
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
