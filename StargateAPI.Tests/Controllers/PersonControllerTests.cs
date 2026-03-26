using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
    public class PersonControllerTests
    {
        private readonly Mock<IMediator> _mediator = new();

        private PersonController CreateController()
        {
            return new PersonController(_mediator.Object, null!, null!);
        }

        private PersonController CreateControllerWithContext(StargateContext context, string? webRootPath = null)
        {
            var env = new Mock<IWebHostEnvironment>();
            env.Setup(e => e.WebRootPath).Returns(webRootPath ?? Path.GetTempPath());
            return new PersonController(_mediator.Object, context, env.Object);
        }

        [Fact]
        public async Task GetPeople_ReturnsResult_WhenSuccessful()
        {
            var expected = new GetPeopleResult();
            _mediator.Setup(m => m.Send(It.IsAny<GetPeople>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expected);

            var controller = CreateController();
            var result = await controller.GetPeople() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task GetPeople_Returns500_WhenExceptionThrown()
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetPeople>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("fail"));

            var controller = CreateController();
            var result = await controller.GetPeople() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
        }

        [Fact]
        public async Task GetPersonByName_ReturnsResult_WhenSuccessful()
        {
            var expected = new GetPersonByNameResult();
            _mediator.Setup(m => m.Send(It.IsAny<GetPersonByName>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expected);

            var controller = CreateController();
            var result = await controller.GetPersonByName("Neil Armstrong") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task GetPersonByName_Returns500_WhenExceptionThrown()
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetPersonByName>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("not found"));

            var controller = CreateController();
            var result = await controller.GetPersonByName("Nobody") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
        }

        [Fact]
        public async Task CreatePerson_ReturnsResult_WhenSuccessful()
        {
            var expected = new CreatePersonResult { Id = 7 };
            _mediator.Setup(m => m.Send(It.IsAny<CreatePerson>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expected);

            var controller = CreateController();
            var result = await controller.CreatePerson("New Person") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(7, ((CreatePersonResult)result.Value!).Id);
        }

        [Fact]
        public async Task CreatePerson_Returns500_WhenExceptionThrown()
        {
            _mediator.Setup(m => m.Send(It.IsAny<CreatePerson>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("duplicate"));

            var controller = CreateController();
            var result = await controller.CreatePerson("Duplicate") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
        }

        [Fact]
        public async Task UpdatePerson_ReturnsResult_WhenSuccessful()
        {
            var expected = new UpdatePersonResult { Id = 1 };
            _mediator.Setup(m => m.Send(It.IsAny<UpdatePerson>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expected);

            var controller = CreateController();
            var result = await controller.UpdatePerson("Old Name", "New Name") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task UpdatePerson_Returns500_WhenExceptionThrown()
        {
            _mediator.Setup(m => m.Send(It.IsAny<UpdatePerson>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("conflict"));

            var controller = CreateController();
            var result = await controller.UpdatePerson("Old", "New") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
        }

        [Fact]
        public async Task DeletePerson_Returns404_WhenPersonNotFound()
        {
            using var context = TestDbContextFactory.Create();
            var controller = CreateControllerWithContext(context);

            var result = await controller.DeletePerson("Nobody") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task DeletePerson_Returns200_WhenPersonExists()
        {
            using var context = TestDbContextFactory.CreateWithData(); // "John Glenn"
            var controller = CreateControllerWithContext(context);

            var result = await controller.DeletePerson("John Glenn") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.Empty(context.People);
        }

        [Fact]
        public async Task DeletePerson_DeletesDutiesAndDetail_WhenPersonIsAstronaut()
        {
            using var context = TestDbContextFactory.CreateWithAstronautData(); // "Neil Armstrong" with duty + detail
            var controller = CreateControllerWithContext(context);

            var result = await controller.DeletePerson("Neil Armstrong") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.Empty(context.People);
            Assert.Empty(context.AstronautDuties);
            Assert.Empty(context.AstronautDetails);
        }

        [Fact]
        public async Task DeletePerson_DeletesPhotoFile_WhenPhotoExists()
        {
            using var context = TestDbContextFactory.CreateWithData(); // "John Glenn"
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                // Set up a photo file on disk
                var photosDir = Path.Combine(tempDir, "photos");
                Directory.CreateDirectory(photosDir);
                var photoPath = Path.Combine(photosDir, "john-glenn.jpg");
                File.WriteAllBytes(photoPath, new byte[] { 0xFF, 0xD8 });

                // Set person's PhotoUrl
                var person = context.People.Single();
                person.PhotoUrl = "photos/john-glenn.jpg";
                context.SaveChanges();
                context.ChangeTracker.Clear();

                var controller = CreateControllerWithContext(context, tempDir);
                var result = await controller.DeletePerson("John Glenn") as ObjectResult;

                Assert.NotNull(result);
                Assert.Equal(200, result.StatusCode);
                Assert.False(File.Exists(photoPath), "Photo file should be deleted");
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task UploadPhoto_Returns400_WhenNoFile()
        {
            using var context = TestDbContextFactory.Create();
            var controller = CreateControllerWithContext(context);

            var result = await controller.UploadPhoto("Test", null!) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task UploadPhoto_Returns400_WhenFileExceedsSizeLimit()
        {
            using var context = TestDbContextFactory.Create();
            var controller = CreateControllerWithContext(context);

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(6 * 1024 * 1024); // 6 MB
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");

            var result = await controller.UploadPhoto("Test", mockFile.Object) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task UploadPhoto_Returns400_WhenInvalidContentType()
        {
            using var context = TestDbContextFactory.Create();
            var controller = CreateControllerWithContext(context);

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");

            var result = await controller.UploadPhoto("Test", mockFile.Object) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task UploadPhoto_Returns404_WhenPersonNotFound()
        {
            using var context = TestDbContextFactory.Create();
            var controller = CreateControllerWithContext(context);

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");

            var result = await controller.UploadPhoto("Nobody", mockFile.Object) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task UploadPhoto_Returns200_WhenUploadSucceeds()
        {
            using var context = TestDbContextFactory.CreateWithData(); // "John Glenn"
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var controller = CreateControllerWithContext(context, tempDir);

            try
            {
                var content = new byte[] { 0xFF, 0xD8, 0xFF }; // minimal JPEG bytes
                var stream = new MemoryStream(content);
                var mockFile = new Mock<IFormFile>();
                mockFile.Setup(f => f.Length).Returns(content.Length);
                mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
                mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                        .Returns((Stream s, CancellationToken _) => stream.CopyToAsync(s));

                var result = await controller.UploadPhoto("John Glenn", mockFile.Object) as ObjectResult;

                Assert.NotNull(result);
                Assert.Equal(200, result.StatusCode);
                var person = context.People.Single();
                Assert.Equal("photos/john-glenn.jpg", person.PhotoUrl);
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task UploadPhoto_Returns500_WhenExceptionThrown()
        {
            using var context = TestDbContextFactory.CreateWithData(); // "John Glenn"
            var controller = CreateControllerWithContext(context);

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
            // CopyToAsync will throw, simulating a disk write failure
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new IOException("disk full"));

            var result = await controller.UploadPhoto("John Glenn", mockFile.Object) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
            Assert.Contains("disk full", ((BaseResponse)result.Value!).Message);
        }

        [Fact]
        public async Task DeletePerson_Returns500_WhenExceptionThrown()
        {
            // Close the connection to force an exception during SaveChangesAsync
            var context = TestDbContextFactory.CreateWithData();
            var controller = CreateControllerWithContext(context);

            // Add a person then close the connection so the delete's SaveChangesAsync fails
            context.Database.GetDbConnection().Close();

            var result = await controller.DeletePerson("John Glenn") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
        }
    }
}
