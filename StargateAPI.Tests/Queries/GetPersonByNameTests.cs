using StargateAPI.Business.Queries;
using StargateAPI.Tests.Helpers;

namespace StargateAPI.Tests.Queries
{
    public class GetPersonByNameTests
    {
        [Fact]
        public async Task Handle_ReturnsPerson_WhenFound()
        {
            using var context = TestDbContextFactory.CreateWithAstronautData(); // Neil Armstrong
                var handler = new GetPersonByNameHandler(context);

            var result = await handler.Handle(new GetPersonByName { Name = "Neil Armstrong" }, CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(result.Person);
            Assert.Equal("Neil Armstrong", result.Person.Name);
        }

        [Fact]
        public async Task Handle_ReturnsNull_WhenPersonNotFound()
        {
            using var context = TestDbContextFactory.Create();
                var handler = new GetPersonByNameHandler(context);

            var result = await handler.Handle(new GetPersonByName { Name = "Unknown Person" }, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Null(result.Person);
        }

        [Fact]
        public async Task Handle_ReturnsAstronautDetails_WhenPersonIsAstronaut()
        {
            using var context = TestDbContextFactory.CreateWithAstronautData();
                var handler = new GetPersonByNameHandler(context);

            var result = await handler.Handle(new GetPersonByName { Name = "Neil Armstrong" }, CancellationToken.None);

            Assert.NotNull(result.Person);
            Assert.Equal("Commander", result.Person.CurrentRank);
            Assert.Equal("Pilot", result.Person.CurrentDutyTitle);
        }

        [Fact]
        public async Task Handle_ReturnsEmptyAstronautFields_WhenPersonHasNoAssignments()
        {
            using var context = TestDbContextFactory.CreateWithData(); // John Glenn, no duties
                var handler = new GetPersonByNameHandler(context);

            var result = await handler.Handle(new GetPersonByName { Name = "John Glenn" }, CancellationToken.None);

            Assert.NotNull(result.Person);
            Assert.Null(result.Person.CareerStartDate);
        }
    }
}
