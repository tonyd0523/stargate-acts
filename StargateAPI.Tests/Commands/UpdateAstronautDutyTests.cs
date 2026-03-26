using Microsoft.AspNetCore.Http;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Data;
using StargateAPI.Tests.Helpers;

namespace StargateAPI.Tests.Commands
{
    public class UpdateAstronautDutyTests
    {
        [Fact]
        public async Task Handle_UpdatesRankAndTitle()
        {
            using var context = TestDbContextFactory.CreateWithAstronautData();
            var handler = new UpdateAstronautDutyHandler(context);
            var duty = context.AstronautDuties.Single();

            var result = await handler.Handle(new UpdateAstronautDuty
            {
                Id = duty.Id,
                Rank = "General",
                DutyTitle = "Mission Commander",
                DutyStartDate = duty.DutyStartDate
            }, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(duty.Id, result.Id);
            var updated = context.AstronautDuties.Single();
            Assert.Equal("General", updated.Rank);
            Assert.Equal("Mission Commander", updated.DutyTitle);
        }

        [Fact]
        public async Task Handle_SyncsAstronautDetail_WhenDutyRemainsOpen()
        {
            // When the updated duty has no end date (still current), AstronautDetail
            // must be kept in sync so the snapshot reflects the latest duty values.
            using var context = TestDbContextFactory.CreateWithAstronautData();
            var handler = new UpdateAstronautDutyHandler(context);
            var duty = context.AstronautDuties.Single(); // open-ended

            await handler.Handle(new UpdateAstronautDuty
            {
                Id = duty.Id,
                Rank = "General",
                DutyTitle = "ISS Commander",
                DutyStartDate = duty.DutyStartDate,
                DutyEndDate = null
            }, CancellationToken.None);

            var detail = context.AstronautDetails.Single();
            Assert.Equal("General", detail.CurrentRank);
            Assert.Equal("ISS Commander", detail.CurrentDutyTitle);
        }

        [Fact]
        public async Task Handle_DoesNotSyncAstronautDetail_WhenDutyIsClosed()
        {
            // Closing a previously-open duty (by supplying an end date) should NOT
            // update AstronautDetail — the current snapshot should remain unchanged.
            using var context = TestDbContextFactory.CreateWithAstronautData();
            var handler = new UpdateAstronautDutyHandler(context);
            var duty = context.AstronautDuties.Single();

            await handler.Handle(new UpdateAstronautDuty
            {
                Id = duty.Id,
                Rank = "General",
                DutyTitle = "Mission Commander",
                DutyStartDate = duty.DutyStartDate,
                DutyEndDate = new DateTime(2005, 1, 1) // closing the duty
            }, CancellationToken.None);

            var detail = context.AstronautDetails.Single();
            Assert.Equal("Commander", detail.CurrentRank);   // unchanged
            Assert.Equal("Pilot", detail.CurrentDutyTitle);  // unchanged
        }

        [Fact]
        public async Task Handle_SetsCareerEndDate_WhenRetired()
        {
            // README: "A Person's Career End Date is one day before the Retired Duty Start Date."
            using var context = TestDbContextFactory.CreateWithAstronautData();
            var handler = new UpdateAstronautDutyHandler(context);
            var duty = context.AstronautDuties.Single();
            var retireDate = new DateTime(2010, 3, 1);

            await handler.Handle(new UpdateAstronautDuty
            {
                Id = duty.Id,
                Rank = "General",
                DutyTitle = "RETIRED",
                DutyStartDate = retireDate,
                DutyEndDate = null
            }, CancellationToken.None);

            var detail = context.AstronautDetails.Single();
            Assert.Equal("RETIRED", detail.CurrentDutyTitle);
            Assert.Equal(retireDate.AddDays(-1).Date, detail.CareerEndDate);
        }

        [Fact]
        public async Task Handle_ClearsCareerEndDate_WhenChangedFromRetired()
        {
            // If a RETIRED duty is corrected to a non-retired title the CareerEndDate
            // must be cleared so the person is no longer shown as retired.
            using var context = TestDbContextFactory.CreateWithAstronautData();
            var handler = new UpdateAstronautDutyHandler(context);
            var duty = context.AstronautDuties.Single();

            // First retire the astronaut
            await handler.Handle(new UpdateAstronautDuty
            {
                Id = duty.Id,
                Rank = "General",
                DutyTitle = "RETIRED",
                DutyStartDate = new DateTime(2010, 1, 1),
                DutyEndDate = null
            }, CancellationToken.None);

            // Then correct it back to an active duty
            await handler.Handle(new UpdateAstronautDuty
            {
                Id = duty.Id,
                Rank = "General",
                DutyTitle = "ISS Commander",
                DutyStartDate = new DateTime(2010, 1, 1),
                DutyEndDate = null
            }, CancellationToken.None);

            var detail = context.AstronautDetails.Single();
            Assert.Null(detail.CareerEndDate);
        }

        [Fact]
        public void PreProcessor_ThrowsException_WhenDutyNotFound()
        {
            using var context = TestDbContextFactory.Create();
            var preProcessor = new UpdateAstronautDutyPreProcessor(context);

            Assert.Throws<BadHttpRequestException>(() =>
                preProcessor.Process(new UpdateAstronautDuty
                {
                    Id = 999,
                    Rank = "Colonel",
                    DutyTitle = "Pilot",
                    DutyStartDate = DateTime.Today
                }, CancellationToken.None).GetAwaiter().GetResult());
        }

        [Fact]
        public async Task PreProcessor_DoesNotThrow_WhenDutyExists()
        {
            using var context = TestDbContextFactory.CreateWithAstronautData();
            var duty = context.AstronautDuties.Single();
            var preProcessor = new UpdateAstronautDutyPreProcessor(context);

            var exception = await Record.ExceptionAsync(() =>
                preProcessor.Process(new UpdateAstronautDuty
                {
                    Id = duty.Id,
                    Rank = "Colonel",
                    DutyTitle = "Pilot",
                    DutyStartDate = DateTime.Today
                }, CancellationToken.None));

            Assert.Null(exception);
        }
    }
}
