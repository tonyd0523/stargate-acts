using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;

namespace StargateAPI.Tests.Helpers
{
    public static class TestDbContextFactory
    {
        public static StargateContext Create()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<StargateContext>()
                .UseSqlite(connection)
                .Options;

            var context = new StargateContext(options);
            context.Database.EnsureCreated();

            // FIX: EnsureCreated() applies HasData() seed rows defined in StargateContext.SeedData()
            // directly from the model. Without this cleanup every test database starts pre-populated
            // with 6 astronauts and 16 duties, breaking any assertion that expects a clean or
            // specific row count (e.g. Assert.Single, Assert.Empty). Purging after schema creation
            // gives each test an empty, schema-valid database to work with.
            context.AstronautDuties.RemoveRange(context.AstronautDuties);
            context.AstronautDetails.RemoveRange(context.AstronautDetails);
            context.AuditLogs.RemoveRange(context.AuditLogs);
            context.People.RemoveRange(context.People);
            context.SaveChanges();
            context.ChangeTracker.Clear();

            return context;
        }

        public static StargateContext CreateWithData()
        {
            var context = Create();

            var person = new Person { Name = "John Glenn" };
            context.People.Add(person);
            context.SaveChanges();
            context.ChangeTracker.Clear();

            return context;
        }

        public static StargateContext CreateWithAstronautData()
        {
            var context = Create();

            var person = new Person { Name = "Neil Armstrong" };
            context.People.Add(person);
            context.SaveChanges();

            var detail = new AstronautDetail
            {
                PersonId = person.Id,
                CurrentRank = "Commander",
                CurrentDutyTitle = "Pilot",
                CareerStartDate = new DateTime(2000, 1, 1)
            };
            context.AstronautDetails.Add(detail);

            var duty = new AstronautDuty
            {
                PersonId = person.Id,
                Rank = "Commander",
                DutyTitle = "Pilot",
                DutyStartDate = new DateTime(2000, 1, 1)
            };
            context.AstronautDuties.Add(duty);
            context.SaveChanges();
            context.ChangeTracker.Clear();

            return context;
        }
    }
}
