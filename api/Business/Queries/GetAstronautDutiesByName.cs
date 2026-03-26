using Dapper;
using MediatR;
using StargateAPI.Business.Data;
using StargateAPI.Business.Dtos;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Queries
{
    public class GetAstronautDutiesByName : IRequest<GetAstronautDutiesByNameResult>
    {
        public string Name { get; set; } = string.Empty;
    }

    public class GetAstronautDutiesByNameHandler : IRequestHandler<GetAstronautDutiesByName, GetAstronautDutiesByNameResult>
    {
        private readonly StargateContext _context;

        public GetAstronautDutiesByNameHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<GetAstronautDutiesByNameResult> Handle(GetAstronautDutiesByName request, CancellationToken cancellationToken)
        {

            var result = new GetAstronautDutiesByNameResult();

            // WHY: Same SQL injection risk as GetPersonByName — the name was interpolated
            // directly into the query string. Switched to a parameterized query for safety.
            // var query = $"SELECT a.Id as PersonId, a.Name, b.CurrentRank, b.CurrentDutyTitle, b.CareerStartDate, b.CareerEndDate FROM [Person] a LEFT JOIN [AstronautDetail] b on b.PersonId = a.Id WHERE \'{request.Name}\' = a.Name";
            // var person = await _context.Connection.QueryFirstOrDefaultAsync<PersonAstronaut>(query);
            var query = "SELECT a.Id as PersonId, a.Name, a.PhotoUrl, b.CurrentRank, b.CurrentDutyTitle, b.CareerStartDate, b.CareerEndDate FROM [Person] a LEFT JOIN [AstronautDetail] b on b.PersonId = a.Id WHERE a.Name = @Name";

            var person = await _context.Connection.QueryFirstOrDefaultAsync<PersonAstronaut>(query, new { request.Name });

            result.Person = person;

            // WHY: The original executed the duties query unconditionally, so if the person
            // was not found person.PersonId would throw a NullReferenceException. Guarding
            // on null also avoids an unnecessary DB round-trip for unknown names.
            // query = $"SELECT * FROM [AstronautDuty] WHERE {person.PersonId} = PersonId Order By DutyStartDate Desc";
            // var duties = await _context.Connection.QueryAsync<AstronautDuty>(query);
            // result.AstronautDuties = duties.ToList();
            if (person != null)
            {
                query = "SELECT * FROM [AstronautDuty] WHERE PersonId = @PersonId ORDER BY DutyStartDate DESC";
                var duties = await _context.Connection.QueryAsync<AstronautDuty>(query, new { PersonId = person.PersonId });
                result.AstronautDuties = duties.ToList();
            }

            return result;

        }
    }

    /// <summary>
    /// Response containing a person's astronaut details and their complete duty history.
    /// </summary>
    public class GetAstronautDutiesByNameResult : BaseResponse
    {
        /// <summary>The person and their current astronaut info, or null if not found.</summary>
        public PersonAstronaut? Person { get; set; }

        /// <summary>Chronological list of all duty assignments for this person.</summary>
        public List<AstronautDuty> AstronautDuties { get; set; } = new List<AstronautDuty>();
    }
}
