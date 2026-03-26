using StargateAPI.Business.Queries;
using StargateAPI.Tests.Helpers;

namespace StargateAPI.Tests.Queries
{
    public class GetPeopleTests
    {
        [Fact]
        public async Task Handle_ReturnsEmptyList_WhenNoPeopleExist()
        {
            using var context = TestDbContextFactory.Create();
            // Open connection for Dapper
                var handler = new GetPeopleHandler(context);

            var result = await handler.Handle(new GetPeople(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Empty(result.People);
        }

        [Fact]
        public async Task Handle_ReturnsPeople_WithAstronautDetails()
        {
            using var context = TestDbContextFactory.CreateWithAstronautData(); // Neil Armstrong with details
                var handler = new GetPeopleHandler(context);

            var result = await handler.Handle(new GetPeople(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Single(result.People);
            var person = result.People[0];
            Assert.Equal("Neil Armstrong", person.Name);
            Assert.Equal("Commander", person.CurrentRank);
        }

        [Fact]
        public async Task Handle_ReturnsPeople_WithoutAstronautDetails()
        {
            using var context = TestDbContextFactory.CreateWithData(); // John Glenn, no duties
                var handler = new GetPeopleHandler(context);

            var result = await handler.Handle(new GetPeople(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Single(result.People);
            Assert.Equal("John Glenn", result.People[0].Name);
            Assert.Null(result.People[0].CareerStartDate);
        }
    }
}
