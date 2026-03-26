using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Data;
using StargateAPI.Business.Queries;
using StargateAPI.Controllers;
using StargateAPI.Tests.Helpers;

namespace StargateAPI.Tests.Controllers
{
    public class AstronautDutyControllerTests
    {
        private readonly Mock<IMediator> _mediator = new();

        private AstronautDutyController CreateController()
        {
            return new AstronautDutyController(_mediator.Object, null!);
        }

        private AstronautDutyController CreateControllerWithContext(StargateContext context)
        {
            return new AstronautDutyController(_mediator.Object, context);
        }

        [Fact]
        public async Task GetAllAstronautDuties_ReturnsResult_WhenSuccessful()
        {
            var expected = new GetAllAstronautDutiesResult();
            _mediator.Setup(m => m.Send(It.IsAny<GetAllAstronautDuties>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expected);

            var controller = CreateController();
            var result = await controller.GetAllAstronautDuties() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task GetAllAstronautDuties_Returns500_WhenExceptionThrown()
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetAllAstronautDuties>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("fail"));

            var controller = CreateController();
            var result = await controller.GetAllAstronautDuties() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
        }

        [Fact]
        public async Task GetAstronautDutiesByName_ReturnsResult_WhenSuccessful()
        {
            var expected = new GetAstronautDutiesByNameResult();
            _mediator.Setup(m => m.Send(It.IsAny<GetAstronautDutiesByName>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expected);

            var controller = CreateController();
            var result = await controller.GetAstronautDutiesByName("Buzz Aldrin") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task GetAstronautDutiesByName_Returns500_WhenExceptionThrown()
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetAstronautDutiesByName>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("err"));

            var controller = CreateController();
            var result = await controller.GetAstronautDutiesByName("Nobody") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
        }

        [Fact]
        public async Task CreateAstronautDuty_ReturnsResult_WhenSuccessful()
        {
            var expected = new CreateAstronautDutyResult { Id = 42 };
            _mediator.Setup(m => m.Send(It.IsAny<CreateAstronautDuty>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expected);

            var controller = CreateController();
            var result = await controller.CreateAstronautDuty(new CreateAstronautDuty
            {
                Name = "Neil Armstrong",
                Rank = "Colonel",
                DutyTitle = "Commander",
                DutyStartDate = DateTime.Today
            }) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(42, ((CreateAstronautDutyResult)result.Value!).Id);
        }

        [Fact]
        public async Task CreateAstronautDuty_Returns500_WhenExceptionThrown()
        {
            _mediator.Setup(m => m.Send(It.IsAny<CreateAstronautDuty>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("validation failed"));

            var controller = CreateController();
            var result = await controller.CreateAstronautDuty(new CreateAstronautDuty
            {
                Name = "Test",
                Rank = "Test",
                DutyTitle = "Test",
                DutyStartDate = DateTime.Today
            }) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
        }

        [Fact]
        public async Task UpdateAstronautDuty_ReturnsResult_WhenSuccessful()
        {
            var expected = new UpdateAstronautDutyResult { Id = 3 };
            _mediator.Setup(m => m.Send(It.IsAny<UpdateAstronautDuty>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expected);

            var controller = CreateController();
            var result = await controller.UpdateAstronautDuty(3, new UpdateAstronautDuty
            {
                Rank = "Colonel",
                DutyTitle = "ISS Commander",
                DutyStartDate = DateTime.Today
            }) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task UpdateAstronautDuty_Returns500_WhenExceptionThrown()
        {
            _mediator.Setup(m => m.Send(It.IsAny<UpdateAstronautDuty>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("not found"));

            var controller = CreateController();
            var result = await controller.UpdateAstronautDuty(99, new UpdateAstronautDuty
            {
                Rank = "Test",
                DutyTitle = "Test",
                DutyStartDate = DateTime.Today
            }) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
        }

        [Fact]
        public async Task DeleteAstronautDuty_Returns404_WhenDutyNotFound()
        {
            using var context = TestDbContextFactory.Create();
            var controller = CreateControllerWithContext(context);

            var result = await controller.DeleteAstronautDuty(999) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task DeleteAstronautDuty_Returns200_WhenDutyDeleted()
        {
            using var context = TestDbContextFactory.CreateWithAstronautData();
            var controller = CreateControllerWithContext(context);
            var dutyId = context.AstronautDuties.Single().Id;

            var result = await controller.DeleteAstronautDuty(dutyId) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.Empty(context.AstronautDuties);
        }

        [Fact]
        public async Task DeleteAstronautDuty_RemovesDetail_WhenLastDutyDeleted()
        {
            using var context = TestDbContextFactory.CreateWithAstronautData();
            var controller = CreateControllerWithContext(context);
            var dutyId = context.AstronautDuties.Single().Id;

            await controller.DeleteAstronautDuty(dutyId);

            // Only duty was deleted + it was current, so detail should be removed
            Assert.Empty(context.AstronautDetails);
        }

        [Fact]
        public async Task DeleteAstronautDuty_ReopsPreviousDuty_WhenCurrentDeleted()
        {
            // Setup: two duties for the same person
            using var context = TestDbContextFactory.CreateWithAstronautData(); // Neil Armstrong, one duty
            var handler = new CreateAstronautDutyHandler(context);
            await handler.Handle(new CreateAstronautDuty
            {
                Name = "Neil Armstrong",
                Rank = "General",
                DutyTitle = "Commander",
                DutyStartDate = new DateTime(2005, 1, 1)
            }, CancellationToken.None);

            var controller = CreateControllerWithContext(context);
            var currentDuty = context.AstronautDuties
                .OrderByDescending(d => d.DutyStartDate)
                .First();

            var result = await controller.DeleteAstronautDuty(currentDuty.Id) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);

            // Previous duty should be reopened (DutyEndDate = null)
            var remainingDuty = context.AstronautDuties.Single();
            Assert.Null(remainingDuty.DutyEndDate);

            // Detail should reflect the previous duty
            var detail = context.AstronautDetails.Single();
            Assert.Equal("Pilot", detail.CurrentDutyTitle);
        }

        [Fact]
        public async Task DeleteAstronautDuty_Returns500_WhenExceptionThrown()
        {
            var context = TestDbContextFactory.CreateWithAstronautData();
            var dutyId = context.AstronautDuties.Single().Id;

            // Close connection to force SaveChangesAsync to fail
            context.Database.GetDbConnection().Close();

            var controller = CreateControllerWithContext(context);
            var result = await controller.DeleteAstronautDuty(dutyId) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
        }
    }
}
