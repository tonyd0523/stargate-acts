using StargateAPI.Business.Data;
using StargateAPI.Business.Queries;
using StargateAPI.Tests.Helpers;

namespace StargateAPI.Tests.Queries
{
    public class GetAuditLogsTests
    {
        [Fact]
        public async Task Handle_ReturnsEmptyList_WhenNoLogs()
        {
            using var context = TestDbContextFactory.Create();
            var handler = new GetAuditLogsHandler(context);

            var result = await handler.Handle(new GetAuditLogs(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Empty(result.Logs);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task Handle_ReturnsAllLogs_WhenCountBelowPageSize()
        {
            using var context = TestDbContextFactory.Create();
            context.AuditLogs.AddRange(
                new AuditLog { CreatedDate = DateTime.UtcNow, Message = "Log 1", IsException = false },
                new AuditLog { CreatedDate = DateTime.UtcNow, Message = "Log 2", IsException = false }
            );
            context.SaveChanges();

            var handler = new GetAuditLogsHandler(context);
            var result = await handler.Handle(new GetAuditLogs { Page = 1, PageSize = 50 }, CancellationToken.None);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Logs.Count);
        }

        [Fact]
        public async Task Handle_ReturnsCorrectPage_WhenPaginating()
        {
            using var context = TestDbContextFactory.Create();
            for (var i = 1; i <= 5; i++)
                context.AuditLogs.Add(new AuditLog { CreatedDate = DateTime.UtcNow.AddMinutes(-i), Message = $"Log {i}", IsException = false });
            context.SaveChanges();

            var handler = new GetAuditLogsHandler(context);

            var page1 = await handler.Handle(new GetAuditLogs { Page = 1, PageSize = 2 }, CancellationToken.None);
            var page2 = await handler.Handle(new GetAuditLogs { Page = 2, PageSize = 2 }, CancellationToken.None);

            Assert.Equal(5, page1.TotalCount);
            Assert.Equal(2, page1.Logs.Count);
            Assert.Equal(2, page2.Logs.Count);
            // Pages must not overlap
            Assert.DoesNotContain(page1.Logs, l => page2.Logs.Any(l2 => l2.Id == l.Id));
        }

        [Fact]
        public async Task Handle_ReturnsLogsOrderedByCreatedDateDescending()
        {
            using var context = TestDbContextFactory.Create();
            var older = new AuditLog { CreatedDate = DateTime.UtcNow.AddHours(-2), Message = "Older", IsException = false };
            var newer = new AuditLog { CreatedDate = DateTime.UtcNow,              Message = "Newer", IsException = false };
            context.AuditLogs.AddRange(older, newer);
            context.SaveChanges();

            var handler = new GetAuditLogsHandler(context);
            var result = await handler.Handle(new GetAuditLogs(), CancellationToken.None);

            Assert.Equal("Newer", result.Logs[0].Message);
            Assert.Equal("Older", result.Logs[1].Message);
        }

        [Fact]
        public async Task Handle_ClampsPageToOne_WhenPageIsZeroOrNegative()
        {
            using var context = TestDbContextFactory.Create();
            context.AuditLogs.Add(new AuditLog { CreatedDate = DateTime.UtcNow, Message = "Test", IsException = false });
            context.SaveChanges();

            var handler = new GetAuditLogsHandler(context);
            var result = await handler.Handle(new GetAuditLogs { Page = 0, PageSize = 50 }, CancellationToken.None);

            // Should not throw and should return results as if page=1
            Assert.Single(result.Logs);
        }

        [Fact]
        public async Task Handle_ClampsPageSizeToFifty_WhenPageSizeIsZeroOrNegative()
        {
            using var context = TestDbContextFactory.Create();
            context.AuditLogs.Add(new AuditLog { CreatedDate = DateTime.UtcNow, Message = "Test", IsException = false });
            context.SaveChanges();

            var handler = new GetAuditLogsHandler(context);
            var result = await handler.Handle(new GetAuditLogs { Page = 1, PageSize = 0 }, CancellationToken.None);

            // Should not throw; clamped to default page size of 50, so the 1 log is returned
            Assert.Single(result.Logs);
        }

        [Fact]
        public async Task Handle_FiltersLogsBySearchTerm()
        {
            using var context = TestDbContextFactory.Create();
            context.AuditLogs.AddRange(
                new AuditLog { CreatedDate = DateTime.UtcNow, Message = "CreatePerson succeeded | name=John", IsException = false },
                new AuditLog { CreatedDate = DateTime.UtcNow, Message = "CreateAstronautDuty succeeded | title=Pilot", IsException = false },
                new AuditLog { CreatedDate = DateTime.UtcNow, Message = "CreatePerson failed | name=Jane", IsException = true }
            );
            context.SaveChanges();

            var handler = new GetAuditLogsHandler(context);
            var result = await handler.Handle(new GetAuditLogs { Search = "CreatePerson" }, CancellationToken.None);

            Assert.Equal(2, result.Logs.Count);
            Assert.All(result.Logs, l => Assert.Contains("CreatePerson", l.Message));
            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public async Task Handle_ReturnsAllLogs_WhenSearchIsEmpty()
        {
            using var context = TestDbContextFactory.Create();
            context.AuditLogs.AddRange(
                new AuditLog { CreatedDate = DateTime.UtcNow, Message = "Log A", IsException = false },
                new AuditLog { CreatedDate = DateTime.UtcNow, Message = "Log B", IsException = false }
            );
            context.SaveChanges();

            var handler = new GetAuditLogsHandler(context);
            var result = await handler.Handle(new GetAuditLogs { Search = "  " }, CancellationToken.None);

            Assert.Equal(2, result.Logs.Count);
        }

        [Fact]
        public async Task Handle_SortsByDateAscending()
        {
            using var context = TestDbContextFactory.Create();
            context.AuditLogs.AddRange(
                new AuditLog { CreatedDate = DateTime.UtcNow.AddHours(-2), Message = "Older", IsException = false },
                new AuditLog { CreatedDate = DateTime.UtcNow, Message = "Newer", IsException = false }
            );
            context.SaveChanges();

            var handler = new GetAuditLogsHandler(context);
            var result = await handler.Handle(new GetAuditLogs { SortBy = "date", SortDirection = "asc" }, CancellationToken.None);

            Assert.Equal("Older", result.Logs[0].Message);
            Assert.Equal("Newer", result.Logs[1].Message);
        }

        [Fact]
        public async Task Handle_SortsByStatus()
        {
            using var context = TestDbContextFactory.Create();
            context.AuditLogs.AddRange(
                new AuditLog { CreatedDate = DateTime.UtcNow, Message = "Success", IsException = false },
                new AuditLog { CreatedDate = DateTime.UtcNow, Message = "Failure", IsException = true }
            );
            context.SaveChanges();

            var handler = new GetAuditLogsHandler(context);
            var result = await handler.Handle(new GetAuditLogs { SortBy = "status", SortDirection = "desc" }, CancellationToken.None);

            Assert.True(result.Logs[0].IsException);
            Assert.False(result.Logs[1].IsException);
        }

        [Fact]
        public async Task Handle_SortsByMessage()
        {
            using var context = TestDbContextFactory.Create();
            context.AuditLogs.AddRange(
                new AuditLog { CreatedDate = DateTime.UtcNow, Message = "Bravo", IsException = false },
                new AuditLog { CreatedDate = DateTime.UtcNow, Message = "Alpha", IsException = false }
            );
            context.SaveChanges();

            var handler = new GetAuditLogsHandler(context);
            var result = await handler.Handle(new GetAuditLogs { SortBy = "message", SortDirection = "asc" }, CancellationToken.None);

            Assert.Equal("Alpha", result.Logs[0].Message);
            Assert.Equal("Bravo", result.Logs[1].Message);
        }

        [Fact]
        public async Task Handle_SearchAndPaginationWorkTogether()
        {
            using var context = TestDbContextFactory.Create();
            for (var i = 1; i <= 5; i++)
                context.AuditLogs.Add(new AuditLog { CreatedDate = DateTime.UtcNow.AddMinutes(-i), Message = $"CreatePerson | name=Person{i}", IsException = false });
            context.AuditLogs.Add(new AuditLog { CreatedDate = DateTime.UtcNow, Message = "CreateDuty | title=Pilot", IsException = false });
            context.SaveChanges();

            var handler = new GetAuditLogsHandler(context);
            var result = await handler.Handle(new GetAuditLogs { Search = "CreatePerson", Page = 1, PageSize = 2 }, CancellationToken.None);

            Assert.Equal(5, result.TotalCount);
            Assert.Equal(2, result.Logs.Count);
        }
    }
}
