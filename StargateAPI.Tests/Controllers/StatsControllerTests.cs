using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StargateAPI.Business.Queries;
using StargateAPI.Controllers;

namespace StargateAPI.Tests.Controllers
{
    public class StatsControllerTests
    {
        private readonly Mock<IMediator> _mediator = new();
        private StatsController CreateController() => new(_mediator.Object);

        [Fact]
        public async Task GetStats_ReturnsResult_WhenSuccessful()
        {
            var expected = new GetStatsResult
            {
                TotalPeople = 5,
                ActiveAstronauts = 4,
                RetiredAstronauts = 1,
                TotalDuties = 10
            };
            _mediator.Setup(m => m.Send(It.IsAny<GetStats>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expected);

            var controller = CreateController();
            var result = await controller.GetStats() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.Same(expected, result.Value);
        }

        [Fact]
        public async Task GetStats_Returns500_WhenExceptionThrown()
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetStats>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("db down"));

            var controller = CreateController();
            var result = await controller.GetStats() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
            var body = result.Value as BaseResponse;
            Assert.NotNull(body);
            Assert.False(body.Success);
            Assert.Contains("db down", body.Message);
        }
    }
}
