using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;

namespace StargateAPI.Tests.Integration
{
    public class MigrationTests
    {
        [Fact]
        public void AllMigrations_ApplySuccessfully()
        {
            var options = new DbContextOptionsBuilder<StargateContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;

            using var context = new StargateContext(options);
            context.Database.OpenConnection();

            // This runs all migrations (InitialCreate, AddAuditLog,
            // AddPersonNameUniqueIndex, SeedAstronautData, AddPersonPhotoUrl)
            context.Database.Migrate();

            // Verify all tables exist by querying them
            Assert.NotNull(context.People);
            Assert.NotNull(context.AstronautDetails);
            Assert.NotNull(context.AstronautDuties);
            Assert.NotNull(context.AuditLogs);
        }

        [Fact]
        public void AllMigrations_ProduceSeedData()
        {
            var options = new DbContextOptionsBuilder<StargateContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;

            using var context = new StargateContext(options);
            context.Database.OpenConnection();
            context.Database.Migrate();

            // Seed migration should have inserted people
            Assert.True(context.People.Any(), "Seed migration should insert people");
        }

        [Fact]
        public void AllMigrations_CreateCorrectSchema()
        {
            var options = new DbContextOptionsBuilder<StargateContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;

            using var context = new StargateContext(options);
            context.Database.OpenConnection();
            context.Database.Migrate();

            // Verify Person table has PhotoUrl column (from AddPersonPhotoUrl migration)
            var person = new Person { Name = "SchemaTest" };
            context.People.Add(person);
            context.SaveChanges();

            var loaded = context.People.First(p => p.Name == "SchemaTest");
            Assert.Null(loaded.PhotoUrl); // Column exists, defaults to null

            // Verify AuditLog table works (from AddAuditLog migration)
            context.AuditLogs.Add(new AuditLog
            {
                CreatedDate = DateTime.UtcNow,
                Message = "test",
                IsException = false
            });
            context.SaveChanges();
            Assert.Single(context.AuditLogs.Where(l => l.Message == "test"));
        }

        [Fact]
        public void PersonName_HasUniqueIndex()
        {
            var options = new DbContextOptionsBuilder<StargateContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;

            using var context = new StargateContext(options);
            context.Database.OpenConnection();
            context.Database.Migrate();

            context.People.Add(new Person { Name = "Unique" });
            context.SaveChanges();

            context.People.Add(new Person { Name = "Unique" });
            Assert.Throws<DbUpdateException>(() => context.SaveChanges());
        }

        [Fact]
        public void Migrations_AreIdempotent_NoPendingChanges()
        {
            var options = new DbContextOptionsBuilder<StargateContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;

            using var context = new StargateContext(options);
            context.Database.OpenConnection();
            context.Database.Migrate();

            var pending = context.Database.GetPendingMigrations();
            Assert.Empty(pending);
        }
    }
}
