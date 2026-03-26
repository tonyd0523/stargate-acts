// Exposes the audit log table with offset pagination. Audit entries accumulate on every
// MediatR request, so without pagination the response payload grows unbounded over time.
using MediatR;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Queries
{
    public class GetAuditLogs : IRequest<GetAuditLogsResult>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? Search { get; set; }
        public string SortBy { get; set; } = "date";
        public string SortDirection { get; set; } = "desc";
    }

    public class GetAuditLogsHandler : IRequestHandler<GetAuditLogs, GetAuditLogsResult>
    {
        private readonly StargateContext _context;

        public GetAuditLogsHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<GetAuditLogsResult> Handle(GetAuditLogs request, CancellationToken cancellationToken)
        {
            // FIX: Added input guards. Without these, a caller passing Page=0 produces a
            // negative Skip() value which throws an ArgumentOutOfRangeException, and PageSize=0
            // would return no results with no indication of why. Clamping to safe defaults
            // makes the endpoint resilient to bad query-string values.
            if (request.Page < 1) request.Page = 1;
            if (request.PageSize < 1) request.PageSize = 50;

            IQueryable<AuditLog> query = _context.AuditLogs;

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();
                query = query.Where(x => x.Message.Contains(search));
            }

            var total = await query.CountAsync(cancellationToken);

            // Apply sorting
            query = (request.SortBy?.ToLowerInvariant()) switch
            {
                "status" => request.SortDirection?.ToLowerInvariant() == "asc"
                    ? query.OrderBy(x => x.IsException)
                    : query.OrderByDescending(x => x.IsException),
                "message" => request.SortDirection?.ToLowerInvariant() == "asc"
                    ? query.OrderBy(x => x.Message)
                    : query.OrderByDescending(x => x.Message),
                _ => request.SortDirection?.ToLowerInvariant() == "asc"
                    ? query.OrderBy(x => x.CreatedDate)
                    : query.OrderByDescending(x => x.CreatedDate),
            };

            var logs = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new GetAuditLogsResult
            {
                Logs = logs,
                TotalCount = total,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }

    /// <summary>
    /// Paginated response of audit log entries.
    /// </summary>
    public class GetAuditLogsResult : BaseResponse
    {
        /// <summary>Audit log entries for the requested page.</summary>
        public List<AuditLog> Logs { get; set; } = new();

        /// <summary>Total number of audit log entries across all pages.</summary>
        /// <example>142</example>
        public int TotalCount { get; set; }

        /// <summary>Current page number (1-based).</summary>
        /// <example>1</example>
        public int Page { get; set; }

        /// <summary>Number of entries per page.</summary>
        /// <example>50</example>
        public int PageSize { get; set; }
    }
}
