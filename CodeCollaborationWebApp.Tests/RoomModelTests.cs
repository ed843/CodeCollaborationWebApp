using CodeCollaborationWebApp.Pages;
using CodeCollaborationWebApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Xunit;

namespace CodeCollaborationWebApp.Tests
{
    public class RoomModelTests
    {
        private readonly Mock<IRoomService> _mockRoomService;
        private readonly RoomModel _roomModel;

        public RoomModelTests()
        {
            _mockRoomService = new Mock<IRoomService>();
            _roomModel = new RoomModel(_mockRoomService.Object);
        }

        [Fact]
        public void OnGet_ValidRoomCode_ReturnsPageResult()
        {
            // Arrange
            const string validRoomCode = "ABCDE";
            _mockRoomService.Setup(s => s.RoomExists(validRoomCode)).Returns(true);
            _roomModel.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _roomModel.HttpContext.Request.QueryString = new QueryString($"?code={validRoomCode}");

            // Act
            var result = _roomModel.OnGet();

            // Assert
            Assert.IsType<PageResult>(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("ABC")]
        [InlineData("ABCDEF")]
        public void OnGet_InvalidRoomCode_ReturnsRedirectToPageResult(string invalidRoomCode)
        {
            // Arrange
            _roomModel.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _roomModel.HttpContext.Request.QueryString = new QueryString($"?code={invalidRoomCode}");

            // Act
            var result = _roomModel.OnGet();

            // Assert
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Index", redirectResult.PageName);
        }

        [Fact]
        public void OnGet_NonExistentRoomCode_ReturnsRedirectToPageResult()
        {
            // Arrange
            const string nonExistentRoomCode = "ABCDE";
            _mockRoomService.Setup(s => s.RoomExists(nonExistentRoomCode)).Returns(false);
            _roomModel.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _roomModel.HttpContext.Request.QueryString = new QueryString($"?code={nonExistentRoomCode}");

            // Act
            var result = _roomModel.OnGet();

            // Assert
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Index", redirectResult.PageName);
        }
    }
}
