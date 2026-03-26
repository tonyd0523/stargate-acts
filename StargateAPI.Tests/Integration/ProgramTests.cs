using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;

namespace StargateAPI.Tests.Integration
{
    public class ProgramTests : IClassFixture<ProgramTests.StargateAppFactory>, IDisposable
    {
        private readonly HttpClient _client;
        private readonly StargateAppFactory _factory;

        public ProgramTests(StargateAppFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        [Fact]
        public async Task App_Starts_AndReturnsSuccessOnSwagger()
        {
            var response = await _client.GetAsync("/swagger/index.html");
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task App_PersonEndpoint_Returns200()
        {
            var response = await _client.GetAsync("/person");
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task App_AuditLogEndpoint_Returns200()
        {
            var response = await _client.GetAsync("/auditlog");
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task App_StatsEndpoint_Returns200()
        {
            var response = await _client.GetAsync("/stats");
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task App_AstronautDutyEndpoint_Returns200()
        {
            var response = await _client.GetAsync("/astronautduty");
            Assert.True(response.IsSuccessStatusCode);
        }

        public class StargateAppFactory : WebApplicationFactory<Program>
        {
            private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"stargate_test_{Guid.NewGuid()}.db");

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<StargateContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<StargateContext>(options =>
                        options.UseSqlite($"DataSource={_dbPath}"));
                });
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (File.Exists(_dbPath)) File.Delete(_dbPath);
            }
        }
    }
}
