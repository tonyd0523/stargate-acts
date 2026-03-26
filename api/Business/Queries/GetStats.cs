// Returns aggregate counts for dashboard and health-check use.
// Four separate CountAsync calls are issued — one per metric. For SQLite at this scale
// that is acceptable; in production these should be combined into a single aggregated
// SQL query or served from a cache to reduce round-trips.
// "Active" is defined as any AstronautDetail where CurrentDutyTitle != "RETIRED".
using MediatR;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Queries
{
    public class GetStats : IRequest<GetStatsResult> { }

    public class GetStatsHandler : IRequestHandler<GetStats, GetStatsResult>
    {
        private readonly StargateContext _context;

        public GetStatsHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<GetStatsResult> Handle(GetStats request, CancellationToken cancellationToken)
        {
            var totalPeople = await _context.People.CountAsync(cancellationToken);
            var activeAstronauts = await _context.AstronautDetails
                .CountAsync(z => z.CurrentDutyTitle != "RETIRED", cancellationToken);
            var retiredAstronauts = await _context.AstronautDetails
                .CountAsync(z => z.CurrentDutyTitle == "RETIRED", cancellationToken);
            var totalDuties = await _context.AstronautDuties.CountAsync(cancellationToken);

            return new GetStatsResult
            {
                TotalPeople = totalPeople,
                ActiveAstronauts = activeAstronauts,
                RetiredAstronauts = retiredAstronauts,
                TotalDuties = totalDuties
            };
        }
    }

    /// <summary>
    /// Aggregate statistics about people and astronaut duties.
    /// </summary>
    public class GetStatsResult : BaseResponse
    {
        /// <summary>Total number of people in the system.</summary>
        /// <example>6</example>
        public int TotalPeople { get; set; }

        /// <summary>Number of astronauts with an active (non-retired) duty.</summary>
        /// <example>5</example>
        public int ActiveAstronauts { get; set; }

        /// <summary>Number of retired astronauts.</summary>
        /// <example>1</example>
        public int RetiredAstronauts { get; set; }

        /// <summary>Total number of duty records across all astronauts.</summary>
        /// <example>16</example>
        public int TotalDuties { get; set; }
    }
}
