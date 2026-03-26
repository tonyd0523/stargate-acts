using StargateAPI.Business.Queries;
using StargateAPI.Tests.Helpers;

namespace StargateAPI.Tests.Queries
{
    public class GetAstronautDutiesByNameTests
    {
        [Fact]
        public async Task Handle_ReturnsDuties_ForKnownAstronaut()
        {
            using var context = TestDbContextFactory.CreateWithAstronautData(); // Neil Armstrong, 1 duty
                var handler = new GetAstronautDutiesByNameHandler(context);

            var result = await handler.Handle(new GetAstronautDutiesByName { Name = "Neil Armstrong" }, CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(result.Person);
            Assert.Single(result.AstronautDuties);
        }

        [Fact]
        public async Task Handle_ReturnsNullPerson_AndEmptyDuties_WhenNotFound()
        {
            using var context = TestDbContextFactory.Create();
                var handler = new GetAstronautDutiesByNameHandler(context);

            var result = await handler.Handle(new GetAstronautDutiesByName { Name = "Unknown" }, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Null(result.Person);
            Assert.Empty(result.AstronautDuties);
        }

        [Fact]
        public async Task Handle_ReturnsDutiesOrderedByStartDateDescending()
        {
            using var context = TestDbContextFactory.CreateWithAstronautData();
    
            // Add a second duty manually
            var person = context.People.Single();
            context.AstronautDuties.Add(new StargateAPI.Business.Data.AstronautDuty
            {
                PersonId = person.Id,
                Rank = "General",
                DutyTitle = "Commander",
                DutyStartDate = new DateTime(2010, 5, 1)
            });
            context.SaveChanges();

            var handler = new GetAstronautDutiesByNameHandler(context);
            var result = await handler.Handle(new GetAstronautDutiesByName { Name = "Neil Armstrong" }, CancellationToken.None);

            Assert.Equal(2, result.AstronautDuties.Count);
            Assert.True(result.AstronautDuties[0].DutyStartDate > result.AstronautDuties[1].DutyStartDate);
        }

        [Fact]
        public async Task Handle_ReturnsCorrectPersonInfo_WithAstronautDetails()
        {
            using var context = TestDbContextFactory.CreateWithAstronautData();
                var handler = new GetAstronautDutiesByNameHandler(context);

            var result = await handler.Handle(new GetAstronautDutiesByName { Name = "Neil Armstrong" }, CancellationToken.None);

            Assert.NotNull(result.Person);
            Assert.Equal("Neil Armstrong", result.Person.Name);
            Assert.Equal("Commander", result.Person.CurrentRank);
            Assert.Equal("Pilot", result.Person.CurrentDutyTitle);
        }
    }
}
