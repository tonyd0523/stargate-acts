using Dapper;
using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;
using System.Net;

namespace StargateAPI.Business.Commands
{
    /// <summary>
    /// Request to create a new astronaut duty assignment.
    /// </summary>
    public class CreateAstronautDuty : IRequest<CreateAstronautDutyResult>
    {
        /// <summary>Full name of the person receiving the duty.</summary>
        /// <example>Buzz Aldrin</example>
        public required string Name { get; set; }

        /// <summary>Military or agency rank during this duty.</summary>
        /// <example>Colonel</example>
        public required string Rank { get; set; }

        /// <summary>Title of the duty assignment. Use "RETIRED" to retire the astronaut.</summary>
        /// <example>Mission Commander</example>
        public required string DutyTitle { get; set; }

        /// <summary>Start date of the duty assignment.</summary>
        /// <example>2026-01-15</example>
        public DateTime DutyStartDate { get; set; }
    }

    public class CreateAstronautDutyPreProcessor : IRequestPreProcessor<CreateAstronautDuty>
    {
        private readonly StargateContext _context;

        public CreateAstronautDutyPreProcessor(StargateContext context)
        {
            _context = context;
        }

        public Task Process(CreateAstronautDuty request, CancellationToken cancellationToken)
        {
            var person = _context.People.AsNoTracking().FirstOrDefault(z => z.Name == request.Name);

            // FIX: Replaced generic "Bad Request" with a descriptive message. A 400 with no
            // context forces callers to guess what went wrong; a clear message tells them exactly.
            if (person is null) throw new BadHttpRequestException($"Person '{request.Name}' not found.");

            // FIX: The original checked for a matching DutyTitle + DutyStartDate globally across
            // ALL persons. That means if any astronaut anywhere ever held "Pilot" on 1965-07-01,
            // no other astronaut could ever be assigned "Pilot" on that date — which is wrong.
            // The check must be scoped to this specific person (z.PersonId == person.Id) to only
            // prevent adding a duplicate duty to the same individual.
            // var verifyNoPreviousDuty = _context.AstronautDuties.FirstOrDefault(z => z.DutyTitle == request.DutyTitle && z.DutyStartDate == request.DutyStartDate);
            var verifyNoPreviousDuty = _context.AstronautDuties.FirstOrDefault(z => z.PersonId == person.Id && z.DutyTitle == request.DutyTitle && z.DutyStartDate == request.DutyStartDate);

            if (verifyNoPreviousDuty is not null) throw new BadHttpRequestException($"'{request.Name}' already has a '{request.DutyTitle}' duty starting on {request.DutyStartDate:yyyy-MM-dd}.");

            // FIX: Validate that new duty start date is strictly after the most recent duty's start
            // date. The README rules state duties must be chronological ("A Person will only ever
            // hold one current Astronaut Duty"). Allowing a past-dated duty would silently corrupt
            // the timeline by back-dating DutyEndDate on the wrong "previous" record.
            var currentDuty = _context.AstronautDuties
                .AsNoTracking()
                .Where(z => z.PersonId == person.Id)
                .OrderByDescending(z => z.DutyStartDate)
                .FirstOrDefault();

            if (currentDuty != null && request.DutyStartDate.Date <= currentDuty.DutyStartDate.Date)
                throw new BadHttpRequestException($"New duty start date ({request.DutyStartDate:yyyy-MM-dd}) must be after the current duty start date ({currentDuty.DutyStartDate:yyyy-MM-dd}).");

            return Task.CompletedTask;
        }
    }

    public class CreateAstronautDutyHandler : IRequestHandler<CreateAstronautDuty, CreateAstronautDutyResult>
    {
        private readonly StargateContext _context;

        public CreateAstronautDutyHandler(StargateContext context)
        {
            _context = context;
        }
        public async Task<CreateAstronautDutyResult> Handle(CreateAstronautDuty request, CancellationToken cancellationToken)
        {
            // FIX: Wrap all writes in an explicit transaction so that updating the previous
            // duty's DutyEndDate, upserting AstronautDetail, and inserting the new duty either
            // all succeed or all roll back. Without this, a failure partway through (e.g. during
            // insert of the new duty) would leave the previous duty's DutyEndDate committed —
            // corrupting the timeline with no matching successor duty.
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // WHY: String interpolation in SQL allows a crafted Name value to break out of the
            // query and execute arbitrary SQL (SQL injection). Parameterized queries let the
            // database driver safely escape user input so it is always treated as data, not code.
            // var query = $"SELECT * FROM [Person] WHERE '{request.Name}' = Name";
            var query = "SELECT * FROM [Person] WHERE Name = @Name";

            // WHY: QueryFirstOrDefaultAsync without a parameter object was used originally;
            // added parameter object to match the new parameterized query.
            var person = await _context.Connection.QueryFirstOrDefaultAsync<Person>(query, new { request.Name });

            // var query = $"SELECT * FROM [AstronautDetail] WHERE {person.Id} = PersonId";
            query = "SELECT * FROM [AstronautDetail] WHERE PersonId = @PersonId";

            var astronautDetail = await _context.Connection.QueryFirstOrDefaultAsync<AstronautDetail>(query, new { PersonId = person!.Id });

            if (astronautDetail == null)
            {
                astronautDetail = new AstronautDetail
                {
                    PersonId = person.Id,
                    CurrentDutyTitle = request.DutyTitle,
                    CurrentRank = request.Rank,
                    CareerStartDate = request.DutyStartDate.Date
                };
                if (request.DutyTitle == "RETIRED")
                {
                    // WHY: The README states "A Person's Career End Date is one day before the
                    // Retired Duty Start Date." The original set CareerEndDate = DutyStartDate,
                    // which is off by one day and violates that rule.
                    // astronautDetail.CareerEndDate = request.DutyStartDate.Date;
                    astronautDetail.CareerEndDate = request.DutyStartDate.AddDays(-1).Date;
                }

                await _context.AstronautDetails.AddAsync(astronautDetail);

            }
            else
            {
                astronautDetail.CurrentDutyTitle = request.DutyTitle;
                astronautDetail.CurrentRank = request.Rank;
                if (request.DutyTitle == "RETIRED")
                {
                    // WHY: Same off-by-one bug in the update path — same fix applied.
                    // astronautDetail.CareerEndDate = request.DutyStartDate.Date;
                    astronautDetail.CareerEndDate = request.DutyStartDate.AddDays(-1).Date;
                }
                _context.AstronautDetails.Update(astronautDetail);
            }

            // WHY: Same SQL injection risk as above. Also the original used non-standard casing
            // "Order By DutyStartDate Desc" which works in SQLite but is inconsistent style.
            // query = $"SELECT * FROM [AstronautDuty] WHERE {person.Id} = PersonId Order By DutyStartDate Desc";
            query = "SELECT * FROM [AstronautDuty] WHERE PersonId = @PersonId ORDER BY DutyStartDate DESC";

            var astronautDuty = await _context.Connection.QueryFirstOrDefaultAsync<AstronautDuty>(query, new { PersonId = person.Id });

            if (astronautDuty != null)
            {
                astronautDuty.DutyEndDate = request.DutyStartDate.AddDays(-1).Date;
                _context.AstronautDuties.Update(astronautDuty);
            }

            var newAstronautDuty = new AstronautDuty()
            {
                PersonId = person.Id,
                Rank = request.Rank,
                DutyTitle = request.DutyTitle,
                DutyStartDate = request.DutyStartDate.Date,
                DutyEndDate = null
            };

            await _context.AstronautDuties.AddAsync(newAstronautDuty);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new CreateAstronautDutyResult()
            {
                Id = newAstronautDuty.Id
            };
        }
    }

    /// <summary>
    /// Response returned after creating an astronaut duty.
    /// </summary>
    public class CreateAstronautDutyResult : BaseResponse
    {
        /// <summary>The newly created duty record ID.</summary>
        /// <example>17</example>
        public int? Id { get; set; }
    }
}
