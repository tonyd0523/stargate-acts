using Microsoft.AspNetCore.Http;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Data;
using StargateAPI.Tests.Helpers;

namespace StargateAPI.Tests.Commands
{
    public class CreateAstronautDutyTests
    {
        [Fact]
        public async Task Handle_CreatesNewDuty_ForNewAstronaut()
        {
            using var context = TestDbContextFactory.CreateWithData(); // has "John Glenn"
            var handler = new CreateAstronautDutyHandler(context);

            var request = new CreateAstronautDuty
            {
                Name = "John Glenn",
                Rank = "Colonel",
                DutyTitle = "Pilot",
                DutyStartDate = new DateTime(1998, 10, 29)
            };

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.True(result.Success);
            Assert.True(result.Id > 0);
            Assert.Single(context.AstronautDuties);
            Assert.Single(context.AstronautDetails);
        }

        [Fact]
        public async Task Handle_SetsCareerStartDate_WhenFirstDuty()
        {
            using var context = TestDbContextFactory.CreateWithData();
            var handler = new CreateAstronautDutyHandler(context);

            var startDate = new DateTime(1998, 10, 29);
            await handler.Handle(new CreateAstronautDuty
            {
                Name = "John Glenn",
                Rank = "Colonel",
                DutyTitle = "Pilot",
                DutyStartDate = startDate
            }, CancellationToken.None);

            var detail = context.AstronautDetails.Single();
            Assert.Equal(startDate.Date, detail.CareerStartDate);
        }

        [Fact]
        public async Task Handle_SetsPreviousDutyEndDate_WhenNewDutyAdded()
        {
            using var context = TestDbContextFactory.CreateWithAstronautData(); // Neil Armstrong with existing duty 2000-01-01
            var handler = new CreateAstronautDutyHandler(context);

            var newDutyStart = new DateTime(2005, 6, 15);
            await handler.Handle(new CreateAstronautDuty
            {
                Name = "Neil Armstrong",
                Rank = "General",
                DutyTitle = "Commander",
                DutyStartDate = newDutyStart
            }, CancellationToken.None);

            var previousDuty = context.AstronautDuties
                .OrderBy(d => d.DutyStartDate)
                .First();

            Assert.Equal(newDutyStart.AddDays(-1).Date, previousDuty.DutyEndDate);
        }

        [Fact]
        public async Task Handle_SetsCareerEndDate_WhenRetired()
        {
            using var context = TestDbContextFactory.CreateWithAstronautData(); // Neil Armstrong
            var handler = new CreateAstronautDutyHandler(context);

            var retireDate = new DateTime(2010, 3, 1);
            await handler.Handle(new CreateAstronautDuty
            {
                Name = "Neil Armstrong",
                Rank = "General",
                DutyTitle = "RETIRED",
                DutyStartDate = retireDate
            }, CancellationToken.None);

            var detail = context.AstronautDetails.Single();
            Assert.Equal(retireDate.AddDays(-1).Date, detail.CareerEndDate);
            Assert.Equal("RETIRED", detail.CurrentDutyTitle);
        }

        [Fact]
        public async Task Handle_NewAstronaut_RetiredImmediately_SetsCareerEndDateOneDayBefore()
        {
            using var context = TestDbContextFactory.CreateWithData(); // John Glenn, no duties
            var handler = new CreateAstronautDutyHandler(context);

            var retireDate = new DateTime(2000, 6, 1);
            await handler.Handle(new CreateAstronautDuty
            {
                Name = "John Glenn",
                Rank = "Colonel",
                DutyTitle = "RETIRED",
                DutyStartDate = retireDate
            }, CancellationToken.None);

            var detail = context.AstronautDetails.Single();
            Assert.Equal(retireDate.AddDays(-1).Date, detail.CareerEndDate);
        }

        [Fact]
        public void PreProcessor_ThrowsException_WhenPersonNotFound()
        {
            using var context = TestDbContextFactory.Create();
            var preProcessor = new CreateAstronautDutyPreProcessor(context);

            Assert.Throws<BadHttpRequestException>(() =>
                preProcessor.Process(new CreateAstronautDuty
                {
                    Name = "Unknown Person",
                    Rank = "Major",
                    DutyTitle = "Pilot",
                    DutyStartDate = DateTime.Today
                }, CancellationToken.None).GetAwaiter().GetResult());
        }

        [Fact]
        public void PreProcessor_ThrowsException_WhenDuplicateDutyExists()
        {
            using var context = TestDbContextFactory.CreateWithAstronautData(); // Neil Armstrong, duty on 2000-01-01
            var preProcessor = new CreateAstronautDutyPreProcessor(context);

            Assert.Throws<BadHttpRequestException>(() =>
                preProcessor.Process(new CreateAstronautDuty
                {
                    Name = "Neil Armstrong",
                    Rank = "Commander",
                    DutyTitle = "Pilot",
                    DutyStartDate = new DateTime(2000, 1, 1)
                }, CancellationToken.None).GetAwaiter().GetResult());
        }

        [Fact]
        public void PreProcessor_DoesNotThrow_WhenDifferentPersonHasSameDutyTitleAndDate()
        {
            // Regression test for the global-scope bug: the original pre-processor checked
            // DutyTitle + DutyStartDate across ALL persons. That meant if any astronaut held
            // "Pilot" on 2000-01-01, no other astronaut could ever be assigned the same. The
            // fix scopes the check to the specific person so two different people can share
            // a duty title and start date without conflict.
            using var context = TestDbContextFactory.CreateWithAstronautData(); // Neil Armstrong, Pilot, 2000-01-01

            var newPerson = new StargateAPI.Business.Data.Person { Name = "John Glenn" };
            context.People.Add(newPerson);
            context.SaveChanges();

            var preProcessor = new CreateAstronautDutyPreProcessor(context);

            // John Glenn with same title + date as Neil Armstrong should NOT throw
            var exception = preProcessor.Process(new CreateAstronautDuty
            {
                Name = "John Glenn",
                Rank = "Colonel",
                DutyTitle = "Pilot",
                DutyStartDate = new DateTime(2000, 1, 1) // same as Neil's duty
            }, CancellationToken.None).Exception;

            Assert.Null(exception);
        }

        [Fact]
        public async Task Handle_UpdatesCurrentRankAndTitle_OnSubsequentDuty()
        {
            using var context = TestDbContextFactory.CreateWithAstronautData();
            var handler = new CreateAstronautDutyHandler(context);

            await handler.Handle(new CreateAstronautDuty
            {
                Name = "Neil Armstrong",
                Rank = "General",
                DutyTitle = "Mission Commander",
                DutyStartDate = new DateTime(2005, 1, 1)
            }, CancellationToken.None);

            var detail = context.AstronautDetails.Single();
            Assert.Equal("General", detail.CurrentRank);
            Assert.Equal("Mission Commander", detail.CurrentDutyTitle);
        }

        [Fact]
        public void PreProcessor_ThrowsException_WhenNewDutyStartDateBeforeCurrentDuty()
        {
            // Fix #3: Validates that duties must be chronological — a new duty's start date
            // must be strictly after the current duty's start date to prevent timeline corruption.
            using var context = TestDbContextFactory.CreateWithAstronautData(); // Neil Armstrong, duty starts 2000-01-01
            var preProcessor = new CreateAstronautDutyPreProcessor(context);

            var ex = Assert.Throws<BadHttpRequestException>(() =>
                preProcessor.Process(new CreateAstronautDuty
                {
                    Name = "Neil Armstrong",
                    Rank = "General",
                    DutyTitle = "Commander",
                    DutyStartDate = new DateTime(1999, 6, 15) // Before current duty start
                }, CancellationToken.None).GetAwaiter().GetResult());

            Assert.Contains("must be after", ex.Message);
        }

        [Fact]
        public void PreProcessor_ThrowsException_WhenNewDutyStartDateEqualToCurrentDuty()
        {
            // Edge case: same start date as current duty (different title) should also be rejected
            // since it would create two duties starting on the same date.
            using var context = TestDbContextFactory.CreateWithAstronautData(); // Neil Armstrong, duty starts 2000-01-01
            var preProcessor = new CreateAstronautDutyPreProcessor(context);

            var ex = Assert.Throws<BadHttpRequestException>(() =>
                preProcessor.Process(new CreateAstronautDuty
                {
                    Name = "Neil Armstrong",
                    Rank = "General",
                    DutyTitle = "Commander", // Different title, same date
                    DutyStartDate = new DateTime(2000, 1, 1)
                }, CancellationToken.None).GetAwaiter().GetResult());

            Assert.Contains("must be after", ex.Message);
        }

        [Fact]
        public void PreProcessor_DoesNotThrow_WhenNewDutyStartDateAfterCurrentDuty()
        {
            using var context = TestDbContextFactory.CreateWithAstronautData(); // Neil Armstrong, duty starts 2000-01-01
            var preProcessor = new CreateAstronautDutyPreProcessor(context);

            var exception = preProcessor.Process(new CreateAstronautDuty
            {
                Name = "Neil Armstrong",
                Rank = "General",
                DutyTitle = "Commander",
                DutyStartDate = new DateTime(2005, 1, 1) // After current duty
            }, CancellationToken.None).Exception;

            Assert.Null(exception);
        }

        [Fact]
        public async Task Handle_AllChangesAreAtomic_WithinTransaction()
        {
            // Fix #4: Verify that the handler writes (update previous duty end date,
            // upsert astronaut detail, insert new duty) all succeed together.
            using var context = TestDbContextFactory.CreateWithAstronautData();
            var handler = new CreateAstronautDutyHandler(context);

            var newStart = new DateTime(2005, 6, 15);
            var result = await handler.Handle(new CreateAstronautDuty
            {
                Name = "Neil Armstrong",
                Rank = "General",
                DutyTitle = "Commander",
                DutyStartDate = newStart
            }, CancellationToken.None);

            Assert.True(result.Success);

            // All three writes must be committed together:
            var duties = context.AstronautDuties.OrderBy(d => d.DutyStartDate).ToList();
            Assert.Equal(2, duties.Count);
            // Previous duty's end date was updated
            Assert.Equal(newStart.AddDays(-1).Date, duties[0].DutyEndDate);
            // New duty was inserted
            Assert.Equal("Commander", duties[1].DutyTitle);
            Assert.Null(duties[1].DutyEndDate);
            // Astronaut detail was updated
            var detail = context.AstronautDetails.Single();
            Assert.Equal("Commander", detail.CurrentDutyTitle);
        }
    }
}
