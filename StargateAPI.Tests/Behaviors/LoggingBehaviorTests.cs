using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StargateAPI.Business.Behaviors;
using StargateAPI.Business.Data;
using StargateAPI.Tests.Helpers;

namespace StargateAPI.Tests.Behaviors
{
    // Minimal request/response types used only within these tests
    public class TestRequest  : IRequest<TestResponse> { public string Value { get; set; } = ""; }
    public class TestResponse { public bool Success   { get; set; } = true; }

    // Causes JsonException due to circular reference (MaxDepth exceeded)
    public class CircularRef { public CircularRef? Self { get; set; } }

    // Triggers NotSupportedException during serialization via a custom converter
    [JsonConverter(typeof(UnsupportedTypeConverter))]
    public class UnsupportedType { public string Value { get; set; } = "test"; }

    public class UnsupportedTypeConverter : JsonConverter<UnsupportedType>
    {
        public override UnsupportedType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => null;
        public override void Write(Utf8JsonWriter writer, UnsupportedType value, JsonSerializerOptions options)
            => throw new NotSupportedException("Intentional test failure");
    }

    public class LoggingBehaviorTests
    {
        private static LoggingBehavior<TestRequest, TestResponse> CreateBehavior(StargateContext context) =>
            new(context, new Mock<ILogger<LoggingBehavior<TestRequest, TestResponse>>>().Object);

        [Fact]
        public async Task Handle_WritesSuccessAuditLog_WhenRequestSucceeds()
        {
            using var context = TestDbContextFactory.Create();
            var behavior = CreateBehavior(context);

            await behavior.Handle(
                new TestRequest { Value = "ok" },
                () => Task.FromResult(new TestResponse()),
                CancellationToken.None);

            var log = context.AuditLogs.Single();
            Assert.False(log.IsException);
            Assert.Contains("TestRequest", log.Message);
        }

        [Fact]
        public async Task Handle_WritesExceptionAuditLog_WhenRequestFails()
        {
            using var context = TestDbContextFactory.Create();
            var behavior = CreateBehavior(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                behavior.Handle(
                    new TestRequest { Value = "bad" },
                    () => Task.FromException<TestResponse>(new InvalidOperationException("test failure")),
                    CancellationToken.None));

            var log = context.AuditLogs.Single();
            Assert.True(log.IsException);
            Assert.Contains("test failure", log.Message);
        }

        [Fact]
        public async Task Handle_RethrowsOriginalException_AfterLogging()
        {
            using var context = TestDbContextFactory.Create();
            var behavior = CreateBehavior(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                behavior.Handle(
                    new TestRequest(),
                    () => Task.FromException<TestResponse>(new InvalidOperationException("rethrow check")),
                    CancellationToken.None));

            Assert.Equal("rethrow check", ex.Message);
        }

        [Fact]
        public async Task Handle_ReturnsResponse_FromNextDelegate()
        {
            using var context = TestDbContextFactory.Create();
            var behavior = CreateBehavior(context);
            var expected = new TestResponse { Success = true };

            var result = await behavior.Handle(
                new TestRequest(),
                () => Task.FromResult(expected),
                CancellationToken.None);

            Assert.Same(expected, result);
        }

        [Fact]
        public async Task Handle_StillWritesAuditLog_EvenIfContextHasPendingChanges()
        {
            // Verifies that ChangeTracker.Clear() on the exception path prevents a partially
            // modified context from blocking the audit log write.
            using var context = TestDbContextFactory.Create();
            var behavior = CreateBehavior(context);

            // Simulate a handler that staged changes then threw
            var person = new Person { Name = "Staged But Not Saved" };
            context.People.Add(person); // staged, not saved

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                behavior.Handle(
                    new TestRequest(),
                    () => Task.FromException<TestResponse>(new InvalidOperationException("failure after staging")),
                    CancellationToken.None));

            // The staged person should have been cleared; the audit log should be written
            Assert.Equal(0, context.People.Count());
            Assert.Single(context.AuditLogs);
            Assert.True(context.AuditLogs.Single().IsException);
        }

        [Fact]
        public async Task Handle_RethrowsOperationCanceledException_WithoutAuditLog()
        {
            // OperationCanceledException is normal flow-control and should NOT produce an audit entry.
            using var context = TestDbContextFactory.Create();
            var behavior = CreateBehavior(context);

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                behavior.Handle(
                    new TestRequest(),
                    () => Task.FromException<TestResponse>(new OperationCanceledException("cancelled")),
                    CancellationToken.None));

            Assert.Empty(context.AuditLogs);
        }

        [Fact]
        public async Task Handle_RethrowsTaskCanceledException_WithoutAuditLog()
        {
            // TaskCanceledException is a subclass of OperationCanceledException
            using var context = TestDbContextFactory.Create();
            var behavior = CreateBehavior(context);

            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                behavior.Handle(
                    new TestRequest(),
                    () => Task.FromException<TestResponse>(new TaskCanceledException()),
                    CancellationToken.None));

            Assert.Empty(context.AuditLogs);
        }

        [Fact]
        public void TrySerialize_ReturnsJson_ForSimpleObject()
        {
            var result = LoggingBehaviorLog.TrySerialize(new { Foo = "bar" });
            Assert.Contains("bar", result);
        }

        [Fact]
        public void TrySerialize_ReturnsNull_ForNullInput()
        {
            var result = LoggingBehaviorLog.TrySerialize(null);
            Assert.Equal("null", result);
        }

        [Fact]
        public void TrySerialize_ReturnsFallback_WhenJsonExceptionThrown()
        {
            // Create an object with a circular reference to trigger JsonException
            var circular = new CircularRef();
            circular.Self = circular;

            var result = LoggingBehaviorLog.TrySerialize(circular);
            Assert.Equal("<serialization-failed:json>", result);
        }

        [Fact]
        public void TrySerialize_ReturnsFallback_WhenNotSupportedExceptionThrown()
        {
            // Trigger NotSupportedException by serializing an object with a converter
            // that throws NotSupportedException
            var result = LoggingBehaviorLog.TrySerialize(new UnsupportedType());
            Assert.Equal("<serialization-failed:unsupported-type>", result);
        }

        [Fact]
        public async Task Handle_AuditWriteFailure_DoesNotThrow()
        {
            // Create a context, then close/dispose its underlying connection so that
            // WriteAuditAsync's SaveChangesAsync will fail. The behavior should swallow
            // the error and still return the handler's response.
            var context = TestDbContextFactory.Create();
            var behavior = CreateBehavior(context);

            // Close the DB connection to force WriteAuditAsync to fail
            context.Database.GetDbConnection().Close();

            // Despite audit failure, the handler response should still be returned
            var response = await behavior.Handle(
                new TestRequest { Value = "test" },
                () => Task.FromResult(new TestResponse()),
                CancellationToken.None);

            Assert.True(response.Success);
        }

        [Fact]
        public async Task Handle_AuditWriteFailure_OnExceptionPath_StillRethrows()
        {
            // Same scenario but on the exception path: audit write fails, but the
            // original exception should still be thrown to the caller.
            var context = TestDbContextFactory.Create();
            var behavior = CreateBehavior(context);

            context.Database.GetDbConnection().Close();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                behavior.Handle(
                    new TestRequest(),
                    () => Task.FromException<TestResponse>(new InvalidOperationException("original error")),
                    CancellationToken.None));

            Assert.Equal("original error", ex.Message);
        }
    }
}
