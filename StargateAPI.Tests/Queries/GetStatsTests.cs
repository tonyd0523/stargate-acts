using StargateAPI.Business.Data;
using StargateAPI.Business.Queries;
using StargateAPI.Tests.Helpers;

namespace StargateAPI.Tests.Queries
{
    public class GetStatsTests
    {
        [Fact]
        public async Task Handle_ReturnsAllZeros_WhenDatabaseIsEmpty()
        {
            using var context = TestDbContextFactory.Create();
            var handler = new GetStatsHandler(context);

            var result = await handler.Handle(new GetStats(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(0, result.TotalPeople);
            Assert.Equal(0, result.ActiveAstronauts);
            Assert.Equal(0, result.RetiredAstronauts);
            Assert.Equal(0, result.TotalDuties);
        }

        [Fact]
        public async Task Handle_CountsTotalPeople_IncludingNonAstronauts()
        {
            using var context = TestDbContextFactory.CreateWithData(); // John Glenn, no duties
            var handler = new GetStatsHandler(context);

            var result = await handler.Handle(new GetStats(), CancellationToken.None);

            Assert.Equal(1, result.TotalPeople);
            Assert.Equal(0, result.ActiveAstronauts);
            Assert.Equal(0, result.RetiredAstronauts);
        }

        [Fact]
        public async Task Handle_CountsActiveAstronauts_ExcludingRetired()
        {
            using var context = TestDbContextFactory.Create();

            var active  = new Person { Name = "Active Astronaut" };
            var retired = new Person { Name = "Retired Astronaut" };
            context.People.AddRange(active, retired);
            context.SaveChanges();

            context.AstronautDetails.AddRange(
                new AstronautDetail { PersonId = active.Id,  CurrentRank = "Colonel", CurrentDutyTitle = "Pilot",   CareerStartDate = new DateTime(2000, 1, 1) },
                new AstronautDetail { PersonId = retired.Id, CurrentRank = "Colonel", CurrentDutyTitle = "RETIRED",  CareerStartDate = new DateTime(1990, 1, 1), CareerEndDate = new DateTime(2005, 12, 31) }
            );
            context.SaveChanges();

            var handler = new GetStatsHandler(context);
            var result = await handler.Handle(new GetStats(), CancellationToken.None);

            Assert.Equal(2, result.TotalPeople);
            Assert.Equal(1, result.ActiveAstronauts);
            Assert.Equal(1, result.RetiredAstronauts);
        }

        [Fact]
        public async Task Handle_CountsTotalDuties_AcrossAllPeople()
        {
            using var context = TestDbContextFactory.Create();

            var p1 = new Person { Name = "Person One" };
            var p2 = new Person { Name = "Person Two" };
            context.People.AddRange(p1, p2);
            context.SaveChanges();

            context.AstronautDuties.AddRange(
                new AstronautDuty { PersonId = p1.Id, Rank = "Colonel", DutyTitle = "Pilot",      DutyStartDate = new DateTime(2000, 1, 1) },
                new AstronautDuty { PersonId = p1.Id, Rank = "General", DutyTitle = "Commander",  DutyStartDate = new DateTime(2005, 1, 1) },
                new AstronautDuty { PersonId = p2.Id, Rank = "Captain", DutyTitle = "Navigator",  DutyStartDate = new DateTime(2003, 1, 1) }
            );
            context.SaveChanges();

            var handler = new GetStatsHandler(context);
            var result = await handler.Handle(new GetStats(), CancellationToken.None);

            Assert.Equal(3, result.TotalDuties);
        }
    }
}
