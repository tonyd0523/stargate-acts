using Microsoft.AspNetCore.Mvc;
using StargateAPI.Controllers;
using System.Net;

namespace StargateAPI.Tests.Controllers
{
    public class ControllerBaseExtensionsTests
    {
        // Minimal concrete controller for testing the extension method
        private class FakeController : ControllerBase { }

        [Fact]
        public void GetResponse_ReturnsObjectResult_WithMatchingStatusCode()
        {
            var controller = new FakeController();
            var response = new BaseResponse
            {
                Success = true,
                Message = "OK",
                ResponseCode = (int)HttpStatusCode.OK
            };

            var result = controller.GetResponse(response) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.Same(response, result.Value);
        }

        [Fact]
        public void GetResponse_Returns500_WhenResponseCodeIs500()
        {
            var controller = new FakeController();
            var response = new BaseResponse
            {
                Success = false,
                Message = "Something broke",
                ResponseCode = (int)HttpStatusCode.InternalServerError
            };

            var result = controller.GetResponse(response) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
        }

        [Fact]
        public void GetResponse_Returns400_WhenResponseCodeIs400()
        {
            var controller = new FakeController();
            var response = new BaseResponse
            {
                Success = false,
                Message = "Bad request",
                ResponseCode = (int)HttpStatusCode.BadRequest
            };

            var result = controller.GetResponse(response) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }
    }
}
