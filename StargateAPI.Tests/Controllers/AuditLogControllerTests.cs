using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StargateAPI.Business.Queries;
using StargateAPI.Controllers;

namespace StargateAPI.Tests.Controllers
{
    public class AuditLogControllerTests
    {
        private readonly Mock<IMediator> _mediator = new();
        private AuditLogController CreateController() => new(_mediator.Object);

        [Fact]
        public async Task GetAuditLogs_ReturnsResult_WhenSuccessful()
        {
            var expected = new GetAuditLogsResult();
            _mediator.Setup(m => m.Send(It.IsAny<GetAuditLogs>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expected);

            var controller = CreateController();
            var result = await controller.GetAuditLogs(1, 50) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task GetAuditLogs_Returns500_WhenExceptionThrown()
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetAuditLogs>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("failure"));

            var controller = CreateController();
            var result = await controller.GetAuditLogs() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
        }
    }
}
