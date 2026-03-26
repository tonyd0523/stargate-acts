// Allows correction of existing AstronautDuty records.
// When the updated duty is the current (open-ended) duty, AstronautDetail is also
// synced to keep the denormalized career snapshot consistent with the domain rules.
using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Commands
{
    /// <summary>
    /// Request to update an existing astronaut duty record.
    /// </summary>
    public class UpdateAstronautDuty : IRequest<UpdateAstronautDutyResult>
    {
        /// <summary>ID of the duty record to update (set from route parameter).</summary>
        /// <example>3</example>
        public int Id { get; set; }

        /// <summary>Updated rank during this duty.</summary>
        /// <example>Colonel</example>
        public required string Rank { get; set; }

        /// <summary>Updated duty title. Use "RETIRED" to retire the astronaut.</summary>
        /// <example>ISS Commander</example>
        public required string DutyTitle { get; set; }

        /// <summary>Updated start date of the duty.</summary>
        /// <example>2013-05-13</example>
        public DateTime DutyStartDate { get; set; }

        /// <summary>End date of the duty, or null if this is the current duty.</summary>
        /// <example>2025-12-31</example>
        public DateTime? DutyEndDate { get; set; }
    }

    public class UpdateAstronautDutyPreProcessor : IRequestPreProcessor<UpdateAstronautDuty>
    {
        private readonly StargateContext _context;

        public UpdateAstronautDutyPreProcessor(StargateContext context)
        {
            _context = context;
        }

        public Task Process(UpdateAstronautDuty request, CancellationToken cancellationToken)
        {
            var duty = _context.AstronautDuties.AsNoTracking().FirstOrDefault(z => z.Id == request.Id);
            if (duty is null) throw new BadHttpRequestException($"Duty with ID {request.Id} not found.");
            return Task.CompletedTask;
        }
    }

    public class UpdateAstronautDutyHandler : IRequestHandler<UpdateAstronautDuty, UpdateAstronautDutyResult>
    {
        private readonly StargateContext _context;

        public UpdateAstronautDutyHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<UpdateAstronautDutyResult> Handle(UpdateAstronautDuty request, CancellationToken cancellationToken)
        {
            // Re-fetches the duty already verified by the pre-processor to get a tracked
            // entity. The pre-processor pattern has no channel to pass the found record to
            // the handler, so a second DB query is unavoidable here.
            var duty = await _context.AstronautDuties.FirstOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

            duty!.Rank = request.Rank;
            duty.DutyTitle = request.DutyTitle;
            duty.DutyStartDate = request.DutyStartDate.Date;
            duty.DutyEndDate = request.DutyEndDate?.Date;

            // If this is the current duty (no end date), sync AstronautDetail so the
            // denormalized snapshot reflects the new rank, title, and retirement status.
            if (duty.DutyEndDate == null)
            {
                var detail = await _context.AstronautDetails
                    .FirstOrDefaultAsync(z => z.PersonId == duty.PersonId, cancellationToken);

                if (detail != null)
                {
                    detail.CurrentRank = request.Rank;
                    detail.CurrentDutyTitle = request.DutyTitle;
                    detail.CareerEndDate = request.DutyTitle == "RETIRED"
                        ? request.DutyStartDate.AddDays(-1).Date
                        : null;
                    _context.AstronautDetails.Update(detail);
                }
            }

            _context.AstronautDuties.Update(duty);
            await _context.SaveChangesAsync(cancellationToken);

            return new UpdateAstronautDutyResult { Id = duty.Id };
        }
    }

    /// <summary>
    /// Response returned after updating an astronaut duty.
    /// </summary>
    public class UpdateAstronautDutyResult : BaseResponse
    {
        /// <summary>The updated duty record ID.</summary>
        /// <example>3</example>
        public int Id { get; set; }
    }
}
