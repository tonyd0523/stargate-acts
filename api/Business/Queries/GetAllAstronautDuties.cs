// Returns all duty records across all people, joined with the person's name for display.
// Uses INNER JOIN (unlike GetPeople which uses LEFT JOIN) because this endpoint is
// specifically about duty records — people with no duties have nothing to show here.
using Dapper;
using MediatR;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Queries
{
    public class GetAllAstronautDuties : IRequest<GetAllAstronautDutiesResult> { }

    /// <summary>
    /// An astronaut duty record with the associated person's name.
    /// </summary>
    public class AstronautDutyWithPerson
    {
        /// <summary>Duty record ID.</summary>
        /// <example>1</example>
        public int Id { get; set; }

        /// <summary>Person ID associated with this duty.</summary>
        /// <example>1</example>
        public int PersonId { get; set; }

        /// <summary>Full name of the person.</summary>
        /// <example>Neil Armstrong</example>
        public string PersonName { get; set; } = string.Empty;

        /// <summary>Rank during this duty.</summary>
        /// <example>Colonel</example>
        public string Rank { get; set; } = string.Empty;

        /// <summary>Title of the duty assignment.</summary>
        /// <example>Mission Commander</example>
        public string DutyTitle { get; set; } = string.Empty;

        /// <summary>Start date of the duty.</summary>
        /// <example>1969-07-20</example>
        public DateTime DutyStartDate { get; set; }

        /// <summary>End date of the duty, or null if this is the current assignment.</summary>
        public DateTime? DutyEndDate { get; set; }
    }

    public class GetAllAstronautDutiesHandler : IRequestHandler<GetAllAstronautDuties, GetAllAstronautDutiesResult>
    {
        private readonly StargateContext _context;

        public GetAllAstronautDutiesHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<GetAllAstronautDutiesResult> Handle(GetAllAstronautDuties request, CancellationToken cancellationToken)
        {
            var query = @"SELECT ad.Id, ad.PersonId, p.Name AS PersonName, ad.Rank, ad.DutyTitle, ad.DutyStartDate, ad.DutyEndDate
                          FROM [AstronautDuty] ad
                          INNER JOIN [Person] p ON p.Id = ad.PersonId
                          ORDER BY ad.DutyStartDate DESC";

            var duties = await _context.Connection.QueryAsync<AstronautDutyWithPerson>(query);

            return new GetAllAstronautDutiesResult
            {
                AstronautDuties = duties.ToList()
            };
        }
    }

    /// <summary>
    /// Response containing all astronaut duties across all people.
    /// </summary>
    public class GetAllAstronautDutiesResult : BaseResponse
    {
        /// <summary>List of all duty records with associated person names.</summary>
        public List<AstronautDutyWithPerson> AstronautDuties { get; set; } = new();
    }
}
