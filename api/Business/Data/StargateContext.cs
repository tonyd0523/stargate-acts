using Microsoft.EntityFrameworkCore;
using System.Data;

namespace StargateAPI.Business.Data
{
    public class StargateContext : DbContext
    {
        // Exposes the underlying ADO.NET connection for Dapper queries alongside EF Core.
        // Pattern used throughout: Dapper for reads (ad-hoc SQL, flexible projections,
        // multi-table JOINs), EF Core for writes (change tracking, SaveChanges). Both
        // use the same SQLite connection, so there are no transaction isolation concerns.
        public IDbConnection Connection => Database.GetDbConnection();
        public DbSet<Person> People { get; set; }
        public DbSet<AstronautDetail> AstronautDetails { get; set; }
        public DbSet<AstronautDuty> AstronautDuties { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        public StargateContext(DbContextOptions<StargateContext> options)
        : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(StargateContext).Assembly);

            // CHANGED: Uncommented SeedData — seed data now lives here so EF Core tracks
            // it in migrations rather than being applied at runtime via a separate seeder class.
            SeedData(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            // CHANGED: Replaced placeholder "John/Jane Doe" data with realistic astronaut records
            // that match the domain rules defined in the README.
            //
            // Key fixes from the original implementation:
            //   1. Hardcoded IDs are required by HasData() so EF can track inserts/deletes
            //      across migrations. The original used navigation properties which HasData()
            //      does not support.
            //   2. Replaced DateTime.Now with literal dates. DateTime.Now is evaluated at
            //      migration generation time, causing EF to detect a "change" on every
            //      `dotnet ef migrations add` and emit a spurious migration.
            //   3. Added AstronautDuty history per person so the career progression rules
            //      (DutyEndDate = next DutyStartDate - 1 day, RETIRED sets CareerEndDate)
            //      are reflected in the seed data.

            // ── People ────────────────────────────────────────────────────────────
            modelBuilder.Entity<Person>().HasData(
                new Person { Id = 1, Name = "Neil Armstrong", PhotoUrl = "photos/neil-armstrong.jpg" },
                new Person { Id = 2, Name = "Buzz Aldrin", PhotoUrl = "photos/buzz-aldrin.jpg" },
                new Person { Id = 3, Name = "Sally Ride", PhotoUrl = "photos/sally-ride.jpg" },
                new Person { Id = 4, Name = "Mae Jemison", PhotoUrl = "photos/mae-jemison.jpg" },
                new Person { Id = 5, Name = "Chris Hadfield", PhotoUrl = "photos/chris-hadfield.jpg" },
                new Person { Id = 6, Name = "Valentina Tereshkova", PhotoUrl = "photos/valentina-tereshkova.jpg" }
            );

            // ── Astronaut Duties ──────────────────────────────────────────────────
            // Duty invariants (per README rules):
            //   - Previous duty DutyEndDate = next duty DutyStartDate - 1 day
            //   - RETIRED duty: CareerEndDate = DutyStartDate - 1 day, no DutyEndDate
            modelBuilder.Entity<AstronautDuty>().HasData(

                // Neil Armstrong — retired
                new AstronautDuty { Id = 1,  PersonId = 1, Rank = "2LT",             DutyTitle = "Pilot",                    DutyStartDate = new DateTime(1962, 3, 1),   DutyEndDate = new DateTime(1965, 6, 30) },
                new AstronautDuty { Id = 2,  PersonId = 1, Rank = "1LT",             DutyTitle = "Flight Commander",          DutyStartDate = new DateTime(1965, 7, 1),   DutyEndDate = new DateTime(1969, 7, 19) },
                new AstronautDuty { Id = 3,  PersonId = 1, Rank = "Colonel",         DutyTitle = "Mission Commander",         DutyStartDate = new DateTime(1969, 7, 20),  DutyEndDate = new DateTime(1971, 7, 31) },
                new AstronautDuty { Id = 4,  PersonId = 1, Rank = "Colonel",         DutyTitle = "RETIRED",                   DutyStartDate = new DateTime(1971, 8, 1),   DutyEndDate = null },

                // Buzz Aldrin — active
                new AstronautDuty { Id = 5,  PersonId = 2, Rank = "2LT",             DutyTitle = "Pilot",                    DutyStartDate = new DateTime(1963, 5, 15),  DutyEndDate = new DateTime(1966, 8, 31) },
                new AstronautDuty { Id = 6,  PersonId = 2, Rank = "Captain",         DutyTitle = "LEM Pilot",                 DutyStartDate = new DateTime(1966, 9, 1),   DutyEndDate = new DateTime(1969, 7, 19) },
                new AstronautDuty { Id = 7,  PersonId = 2, Rank = "Colonel",         DutyTitle = "Mission Specialist",        DutyStartDate = new DateTime(1969, 7, 20),  DutyEndDate = null },

                // Sally Ride — active
                new AstronautDuty { Id = 8,  PersonId = 3, Rank = "Ensign",          DutyTitle = "Mission Specialist",        DutyStartDate = new DateTime(1978, 1, 1),   DutyEndDate = new DateTime(1983, 5, 31) },
                new AstronautDuty { Id = 9,  PersonId = 3, Rank = "Lieutenant",      DutyTitle = "Payload Commander",         DutyStartDate = new DateTime(1983, 6, 1),   DutyEndDate = null },

                // Mae Jemison — active
                new AstronautDuty { Id = 10, PersonId = 4, Rank = "Ensign",          DutyTitle = "Mission Specialist",        DutyStartDate = new DateTime(1987, 6, 4),   DutyEndDate = new DateTime(1992, 9, 11) },
                new AstronautDuty { Id = 11, PersonId = 4, Rank = "Lieutenant",      DutyTitle = "Science Mission Specialist", DutyStartDate = new DateTime(1992, 9, 12),  DutyEndDate = null },

                // Chris Hadfield — active
                new AstronautDuty { Id = 12, PersonId = 5, Rank = "2LT",             DutyTitle = "Mission Specialist",        DutyStartDate = new DateTime(1992, 12, 1),  DutyEndDate = new DateTime(1995, 10, 21) },
                new AstronautDuty { Id = 13, PersonId = 5, Rank = "Captain",         DutyTitle = "Mission Specialist",        DutyStartDate = new DateTime(1995, 10, 22), DutyEndDate = new DateTime(2013, 5, 12) },
                new AstronautDuty { Id = 14, PersonId = 5, Rank = "Colonel",         DutyTitle = "ISS Commander",             DutyStartDate = new DateTime(2013, 5, 13),  DutyEndDate = null },

                // Valentina Tereshkova — active
                new AstronautDuty { Id = 15, PersonId = 6, Rank = "Junior Lieutenant", DutyTitle = "Cosmonaut",               DutyStartDate = new DateTime(1962, 2, 16),  DutyEndDate = new DateTime(1963, 6, 15) },
                new AstronautDuty { Id = 16, PersonId = 6, Rank = "Major",             DutyTitle = "Senior Cosmonaut",        DutyStartDate = new DateTime(1963, 6, 16),  DutyEndDate = null }
            );

            // ── AstronautDetail (current snapshot per person) ─────────────────────
            // Reflects the most recent duty for each person per the domain rules.
            // CareerEndDate is set only for retired astronauts (= RetiredDutyStartDate - 1 day).
            modelBuilder.Entity<AstronautDetail>().HasData(
                new AstronautDetail { Id = 1, PersonId = 1, CurrentRank = "Colonel",           CurrentDutyTitle = "RETIRED",                    CareerStartDate = new DateTime(1962, 3, 1),   CareerEndDate = new DateTime(1971, 7, 31) },
                new AstronautDetail { Id = 2, PersonId = 2, CurrentRank = "Colonel",           CurrentDutyTitle = "Mission Specialist",          CareerStartDate = new DateTime(1963, 5, 15),  CareerEndDate = null },
                new AstronautDetail { Id = 3, PersonId = 3, CurrentRank = "Lieutenant",        CurrentDutyTitle = "Payload Commander",           CareerStartDate = new DateTime(1978, 1, 1),   CareerEndDate = null },
                new AstronautDetail { Id = 4, PersonId = 4, CurrentRank = "Lieutenant",        CurrentDutyTitle = "Science Mission Specialist",  CareerStartDate = new DateTime(1987, 6, 4),   CareerEndDate = null },
                new AstronautDetail { Id = 5, PersonId = 5, CurrentRank = "Colonel",           CurrentDutyTitle = "ISS Commander",              CareerStartDate = new DateTime(1992, 12, 1),  CareerEndDate = null },
                new AstronautDetail { Id = 6, PersonId = 6, CurrentRank = "Major",             CurrentDutyTitle = "Senior Cosmonaut",           CareerStartDate = new DateTime(1962, 2, 16),  CareerEndDate = null }
            );
        }
    }
}
