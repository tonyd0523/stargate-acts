using Microsoft.AspNetCore.Http;
using StargateAPI.Business.Commands;
using StargateAPI.Tests.Helpers;

namespace StargateAPI.Tests.Commands
{
    public class UpdatePersonTests
    {
        [Fact]
        public async Task Handle_UpdatesPersonName_WhenPersonExists()
        {
            using var context = TestDbContextFactory.CreateWithData(); // "John Glenn"
            var handler = new UpdatePersonHandler(context);

            var result = await handler.Handle(new UpdatePerson { Name = "John Glenn", NewName = "John H. Glenn" }, CancellationToken.None);

            Assert.True(result.Success);
            Assert.True(result.Id > 0);
            Assert.Equal("John H. Glenn", context.People.Single().Name);
        }

        [Fact]
        public void PreProcessor_ThrowsException_WhenPersonNotFound()
        {
            using var context = TestDbContextFactory.Create();
            var preProcessor = new UpdatePersonPreProcessor(context);

            Assert.Throws<BadHttpRequestException>(() =>
                preProcessor.Process(new UpdatePerson { Name = "Nobody", NewName = "Somebody" }, CancellationToken.None).GetAwaiter().GetResult());
        }

        [Fact]
        public void PreProcessor_ThrowsException_WhenNewNameAlreadyExists()
        {
            using var context = TestDbContextFactory.Create();
            context.People.Add(new StargateAPI.Business.Data.Person { Name = "Person A" });
            context.People.Add(new StargateAPI.Business.Data.Person { Name = "Person B" });
            context.SaveChanges();
            context.ChangeTracker.Clear();

            var preProcessor = new UpdatePersonPreProcessor(context);

            Assert.Throws<BadHttpRequestException>(() =>
                preProcessor.Process(new UpdatePerson { Name = "Person A", NewName = "Person B" }, CancellationToken.None).GetAwaiter().GetResult());
        }

        [Fact]
        public async Task PreProcessor_DoesNotThrow_WhenValidRename()
        {
            using var context = TestDbContextFactory.CreateWithData(); // "John Glenn"
            var preProcessor = new UpdatePersonPreProcessor(context);

            var exception = await Record.ExceptionAsync(() =>
                preProcessor.Process(new UpdatePerson { Name = "John Glenn", NewName = "John H. Glenn" }, CancellationToken.None));

            Assert.Null(exception);
        }
    }
}
