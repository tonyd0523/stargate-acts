// Purpose:
//   Centralizes logging for MediatR request handling so individual handlers remain
//   focused on domain logic. Provides consistent timing, success, and failure logging.
//
// Behavior:
//   - Logs to two sinks on every request:
//       ILogger  — structured runtime observability; respects log-level filtering.
//       AuditLog — durable DB record for traceability, compliance, and incident review.
//   - Request is serialized before handler execution to capture inbound state.
//   - ElapsedMs measures handler execution time only (serialization excluded).
//   - OperationCanceledException is treated as normal flow-control; no failure audit entry is written.
//   - On failure, ChangeTracker.Clear() runs before the audit write to prevent
//     partial state from the failed handler being persisted.
//   - Audit writes use CancellationToken.None so they persist even if the request is cancelled.
//
// Tradeoffs:
//   - Audit writes occur on the request path and add database latency.
//     Higher-throughput systems may prefer a queue or outbox pattern.
//   - Full request payloads are serialized; sensitive fields must be excluded or redacted.
//   - ChangeTracker.Clear() removes all tracked entities in scope, which is safe for
//     request-scoped unit-of-work but broader than a targeted reset.
//   - AuditLog.Message is unbounded; large payloads or verbose exceptions can create large rows.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using StargateAPI.Business.Data;

namespace StargateAPI.Business.Behaviors
{
    /// <summary>
    /// Static logging state for <see cref="LoggingBehavior{TRequest,TResponse}"/>.
    /// Kept in a non-generic class because static fields on generic types are duplicated
    /// per closed type — one copy of these delegates would exist per registered
    /// TRequest/TResponse pair if they lived directly on the behavior.
    /// EventId range 10_100–10_199 is reserved for this behavior.
    /// </summary>
    internal static class LoggingBehaviorLog
    {
        internal static readonly JsonSerializerOptions JsonOptions;

        static LoggingBehaviorLog()
        {
            JsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };
            // Pre-warms STJ's type-metadata cache and locks the object against mutation
            // (readonly only prevents reference reassignment, not property changes).
            // FIX: .NET 10 requires a TypeInfoResolver before MakeReadOnly(). The
            // populateMissingResolver overload auto-assigns the default resolver.
            JsonOptions.MakeReadOnly(populateMissingResolver: true);
        }

        // Pre-compiled delegates: no allocations when the log level is disabled (CA1848).
        internal static readonly Action<ILogger, string, long, string, Exception?> Success =
            LoggerMessage.Define<string, long, string>(
                LogLevel.Information,
                new EventId(10_100, "RequestSucceeded"),
                "{RequestName} succeeded in {ElapsedMs}ms | {Params}");

        internal static readonly Action<ILogger, string, long, string, string, Exception?> Failure =
            LoggerMessage.Define<string, long, string, string>(
                LogLevel.Error,
                new EventId(10_101, "RequestFailed"),
                "{RequestName} failed after {ElapsedMs}ms: {Message} | {Params}");

        internal static readonly Action<ILogger, bool, Exception?> AuditFailure =
            LoggerMessage.Define<bool>(
                LogLevel.Error,
                new EventId(10_102, "AuditLogFailed"),
                "Failed to write audit log (IsException={IsException})");

        /// <summary>
        /// Best-effort JSON serialization for log messages.
        /// <see cref="JsonException"/> and <see cref="NotSupportedException"/> are the
        /// only exceptions thrown by <see cref="JsonSerializer"/>; both produce a
        /// diagnostic string. All other exceptions propagate so unexpected failures
        /// stay visible. The <c>object?</c> overload is intentional — it uses the
        /// runtime type, capturing the concrete class rather than a declared interface.
        /// </summary>
        internal static string TrySerialize(object? request)
        {
            try
            {
                return JsonSerializer.Serialize(request, JsonOptions);
            }
            catch (JsonException)
            {
                return "<serialization-failed:json>";
            }
            catch (NotSupportedException)
            {
                return "<serialization-failed:unsupported-type>";
            }
        }
    }

    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly StargateContext _context;
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(
            StargateContext context,
            ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // FullName is unambiguous across assemblies; Name alone can collide.
            var requestName = typeof(TRequest).FullName ?? typeof(TRequest).Name;

            // Captured before the handler runs so the log reflects inbound state.
            // Skipped entirely if both log levels are disabled to avoid serialization cost.
            var requestParams = (_logger.IsEnabled(LogLevel.Information) || _logger.IsEnabled(LogLevel.Error))
                ? LoggingBehaviorLog.TrySerialize(request)
                : string.Empty;

            // Started after serialization so ElapsedMs measures handler time only.
            var sw = Stopwatch.StartNew();

            try
            {
                var response = await next();
                sw.Stop();

                LoggingBehaviorLog.Success(_logger, requestName, sw.ElapsedMilliseconds, requestParams, null);

                // Handler succeeded — audit must persist even if the client already disconnected.
                await WriteAuditAsync(
                    $"{requestName} succeeded in {sw.ElapsedMilliseconds}ms | {requestParams}",
                    isException: false,
                    ct: CancellationToken.None);

                return response;
            }
            catch (OperationCanceledException)
            {
                // Cancellation is normal flow-control, not an application error.
                // TaskCanceledException is a subclass and is caught here too.
                sw.Stop();
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();

                LoggingBehaviorLog.Failure(_logger, requestName, sw.ElapsedMilliseconds, ex.Message, requestParams, ex);

                // Prevent partial state from the failed handler being flushed by the
                // audit write's SaveChangesAsync. Clears the entire tracker — see the
                // top-of-file note for the scope tradeoff.
                _context.ChangeTracker.Clear();

                // Token may already be cancelled; audit must still persist.
                await WriteAuditAsync(
                    $"{requestName} failed after {sw.ElapsedMilliseconds}ms: {ex.Message} | {requestParams}",
                    isException: true,
                    ct: CancellationToken.None);

                throw;
            }
        }

        /// <summary>
        /// Failures are logged and swallowed so a broken audit path never alters
        /// the outcome already returned (or thrown) to the caller.
        /// </summary>
        private async Task WriteAuditAsync(string message, bool isException, CancellationToken ct)
        {
            try
            {
                // Add suffices; AddAsync is only needed for async value generators
                // (e.g. HiLo sequences), not standard auto-increment PKs.
                _context.AuditLogs.Add(new AuditLog
                {
                    CreatedDate = DateTime.UtcNow,
                    Message = message,
                    IsException = isException,
                });
                await _context.SaveChangesAsync(ct);
            }
            catch (Exception logEx)
            {
                LoggingBehaviorLog.AuditFailure(_logger, isException, logEx);
            }
        }
    }
}
