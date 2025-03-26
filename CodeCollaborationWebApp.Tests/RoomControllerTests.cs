using System;
using System.Collections.Generic;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using CodeCollaborationWebApp.Controllers;
using CodeCollaborationWebApp.Services;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace CodeCollaborationWebApp.Tests
{
    public class RoomControllerTests
    {
        private RoomController CreateController(Mock<IRoomService> mockRoomService = null, Mock<ILogger<RoomController>> mockLogger = null)
        {
            mockRoomService ??= new Mock<IRoomService>();
            mockLogger ??= new Mock<ILogger<RoomController>>();

            return new RoomController(mockRoomService.Object, mockLogger.Object);
        }

        [Fact]
        public void Constructor_NullRoomService_ThrowsArgumentNullException()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<RoomController>>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RoomController(null, mockLogger.Object));
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            var mockRoomService = new Mock<IRoomService>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RoomController(mockRoomService.Object, null));
        }

        [Fact]
        public void VerifyRoom_ValidRoomExists_ReturnsOkWithExists()
        {
            // Arrange
            var mockRoomService = new Mock<IRoomService>();
            mockRoomService.Setup(s => s.RoomExists("ABCDE")).Returns(true);
            var controller = CreateController(mockRoomService);

            // Act
            var result = controller.VerifyRoom("ABCDE");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            var json = JsonConvert.SerializeObject(okResult.Value);
            Assert.Contains("\"exists\":true", json);
        }

        [Fact]
        public void VerifyRoom_ValidRoomDoesNotExist_ReturnsOkWithNotExists()
        {
            // Arrange
            var mockRoomService = new Mock<IRoomService>();
            mockRoomService.Setup(s => s.RoomExists("ABCDE")).Returns(false);
            var controller = CreateController(mockRoomService);

            // Act
            var result = controller.VerifyRoom("ABCDE");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            var json = JsonConvert.SerializeObject(okResult.Value);
            Assert.Contains("\"exists\":false", json);
        }


        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("ABC")]
        [InlineData("ABCDEF")]
        public void VerifyRoom_InvalidRoomCode_ReturnsBadRequest(string roomCode)
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.VerifyRoom(roomCode);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);

            // Serialize to JSON and check content
            var json = JsonConvert.SerializeObject(badRequestResult.Value);
            Assert.Contains("\"exists\":false", json);
            Assert.Contains("\"message\":\"Invalid room code format\"", json);
        }


        [Fact]
        public void VerifyRoom_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var mockRoomService = new Mock<IRoomService>();
            mockRoomService.Setup(s => s.RoomExists(It.IsAny<string>())).Throws(new Exception("Test exception"));
            var controller = CreateController(mockRoomService);

            // Act
            var result = controller.VerifyRoom("ABCDE");

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);

            var json = JsonConvert.SerializeObject(statusCodeResult.Value);
            Assert.Contains("\"error\":\"An error occurred while verifying the room\"", json);
        }


        [Fact]
        public void CreateRoom_Success_ReturnsOkWithRoomCode()
        {
            // Arrange
            var mockRoomService = new Mock<IRoomService>();
            mockRoomService.Setup(s => s.CreateRoom()).Returns("ABCDE");
            var controller = CreateController(mockRoomService);

            // Act
            var result = controller.CreateRoom();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            var json = JsonConvert.SerializeObject(okResult.Value);
            Assert.Contains("\"roomCode\":\"ABCDE\"", json);
        }


        [Fact]
        public void CreateRoom_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var mockRoomService = new Mock<IRoomService>();
            mockRoomService.Setup(s => s.CreateRoom()).Throws(new Exception("Test exception"));
            var controller = CreateController(mockRoomService);

            // Act
            var result = controller.CreateRoom();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);

            var json = JsonConvert.SerializeObject(statusCodeResult.Value);
            Assert.Contains("\"error\":\"An error occurred while creating the room\"", json);
        }


    }
}
