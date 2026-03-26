using StargateAPI.Business.Data;
using StargateAPI.Business.Queries;
using StargateAPI.Tests.Helpers;

namespace StargateAPI.Tests.Queries
{
    public class GetAllAstronautDutiesTests
    {
        [Fact]
        public async Task Handle_ReturnsEmptyList_WhenNoDuties()
        {
            using var context = TestDbContextFactory.Create();
            var handler = new GetAllAstronautDutiesHandler(context);

            var result = await handler.Handle(new GetAllAstronautDuties(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Empty(result.AstronautDuties);
        }

        [Fact]
        public async Task Handle_ReturnsDuties_WithPersonNameJoined()
        {
            using var context = TestDbContextFactory.CreateWithAstronautData(); // Neil Armstrong, 1 duty
            var handler = new GetAllAstronautDutiesHandler(context);

            var result = await handler.Handle(new GetAllAstronautDuties(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Single(result.AstronautDuties);
            Assert.Equal("Neil Armstrong", result.AstronautDuties[0].PersonName);
            Assert.Equal("Pilot", result.AstronautDuties[0].DutyTitle);
        }

        [Fact]
        public async Task Handle_ReturnsDutiesAcrossMultiplePeople()
        {
            using var context = TestDbContextFactory.Create();

            var p1 = new Person { Name = "Person One" };
            var p2 = new Person { Name = "Person Two" };
            context.People.AddRange(p1, p2);
            context.SaveChanges();

            context.AstronautDuties.AddRange(
                new AstronautDuty { PersonId = p1.Id, Rank = "Colonel", DutyTitle = "Pilot",      DutyStartDate = new DateTime(2000, 1, 1) },
                new AstronautDuty { PersonId = p2.Id, Rank = "Captain", DutyTitle = "Navigator",  DutyStartDate = new DateTime(2001, 1, 1) }
            );
            context.SaveChanges();

            var handler = new GetAllAstronautDutiesHandler(context);
            var result = await handler.Handle(new GetAllAstronautDuties(), CancellationToken.None);

            Assert.Equal(2, result.AstronautDuties.Count);
            Assert.Contains(result.AstronautDuties, d => d.PersonName == "Person One");
            Assert.Contains(result.AstronautDuties, d => d.PersonName == "Person Two");
        }

        [Fact]
        public async Task Handle_ReturnsDutiesOrderedByStartDateDescending()
        {
            using var context = TestDbContextFactory.CreateWithAstronautData();
            var person = context.People.Single();

            context.AstronautDuties.Add(new AstronautDuty
            {
                PersonId = person.Id,
                Rank = "General",
                DutyTitle = "Commander",
                DutyStartDate = new DateTime(2010, 6, 1)
            });
            context.SaveChanges();

            var handler = new GetAllAstronautDutiesHandler(context);
            var result = await handler.Handle(new GetAllAstronautDuties(), CancellationToken.None);

            Assert.Equal(2, result.AstronautDuties.Count);
            Assert.True(result.AstronautDuties[0].DutyStartDate > result.AstronautDuties[1].DutyStartDate);
        }

        [Fact]
        public async Task Handle_ExcludesPeopleWithNoDuties()
        {
            // A person with no duties should not appear in the duties list at all
            // since the query is an INNER JOIN on AstronautDuty.
            using var context = TestDbContextFactory.CreateWithData(); // John Glenn, no duties
            var handler = new GetAllAstronautDutiesHandler(context);

            var result = await handler.Handle(new GetAllAstronautDuties(), CancellationToken.None);

            Assert.Empty(result.AstronautDuties);
        }
    }
}
