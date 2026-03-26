using Microsoft.AspNetCore.Http;
using StargateAPI.Business.Commands;
using StargateAPI.Tests.Helpers;

namespace StargateAPI.Tests.Commands
{
    public class CreatePersonTests
    {
        [Fact]
        public async Task Handle_CreatesNewPerson_ReturnsId()
        {
            using var context = TestDbContextFactory.Create();
            var handler = new CreatePersonHandler(context);

            var result = await handler.Handle(new CreatePerson { Name = "Buzz Aldrin" }, CancellationToken.None);

            Assert.True(result.Success);
            Assert.True(result.Id > 0);
            Assert.Single(context.People);
            Assert.Equal("Buzz Aldrin", context.People.First().Name);
        }

        [Fact]
        public async Task Handle_PersistsCorrectName()
        {
            using var context = TestDbContextFactory.Create();
            var handler = new CreatePersonHandler(context);

            await handler.Handle(new CreatePerson { Name = "Sally Ride" }, CancellationToken.None);

            var person = context.People.Single();
            Assert.Equal("Sally Ride", person.Name);
        }

        [Fact]
        public void PreProcessor_ThrowsException_WhenPersonAlreadyExists()
        {
            using var context = TestDbContextFactory.CreateWithData(); // has "John Glenn"
            var preProcessor = new CreatePersonPreProcessor(context);

            Assert.Throws<BadHttpRequestException>(() =>
                preProcessor.Process(new CreatePerson { Name = "John Glenn" }, CancellationToken.None).GetAwaiter().GetResult());
        }

        [Fact]
        public async Task PreProcessor_DoesNotThrow_WhenPersonDoesNotExist()
        {
            using var context = TestDbContextFactory.Create();
            var preProcessor = new CreatePersonPreProcessor(context);

            var exception = await Record.ExceptionAsync(() =>
                preProcessor.Process(new CreatePerson { Name = "New Person" }, CancellationToken.None));

            Assert.Null(exception);
        }

        [Fact]
        public async Task Handle_MultiplePersons_EachHasUniqueId()
        {
            using var context = TestDbContextFactory.Create();
            var handler = new CreatePersonHandler(context);

            var result1 = await handler.Handle(new CreatePerson { Name = "Person One" }, CancellationToken.None);
            var result2 = await handler.Handle(new CreatePerson { Name = "Person Two" }, CancellationToken.None);

            Assert.NotEqual(result1.Id, result2.Id);
        }
    }
}
