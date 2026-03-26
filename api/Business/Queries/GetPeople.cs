using Dapper;
using MediatR;
using StargateAPI.Business.Data;
using StargateAPI.Business.Dtos;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Queries
{
    public class GetPeople : IRequest<GetPeopleResult>
    {

    }

    public class GetPeopleHandler : IRequestHandler<GetPeople, GetPeopleResult>
    {
        // FIX: Was `public readonly` — a private field should never be public. Exposing the
        // DbContext on a handler allows any external code to access and modify database state
        // directly, bypassing all validation and business logic.
        private readonly StargateContext _context;
        public GetPeopleHandler(StargateContext context)
        {
            _context = context;
        }
        public async Task<GetPeopleResult> Handle(GetPeople request, CancellationToken cancellationToken)
        {
            var result = new GetPeopleResult();

            // FIX: Removed the unnecessary $ prefix. No values are interpolated into this query
            // so the $ serves no purpose. Leaving it in implies a variable may have been
            // accidentally removed, which is misleading to anyone reading the code.
            // LEFT JOIN: people without any duty assignments are still included.
            // A person can be registered before being assigned as an astronaut, so
            // an INNER JOIN would silently exclude newly created people from the list.
            var query = "SELECT a.Id as PersonId, a.Name, a.PhotoUrl, b.CurrentRank, b.CurrentDutyTitle, b.CareerStartDate, b.CareerEndDate FROM [Person] a LEFT JOIN [AstronautDetail] b on b.PersonId = a.Id";

            var people = await _context.Connection.QueryAsync<PersonAstronaut>(query);

            result.People = people.ToList();

            return result;
        }
    }

    /// <summary>
    /// Response containing all people and their astronaut details.
    /// </summary>
    public class GetPeopleResult : BaseResponse
    {
        /// <summary>List of all people with their current astronaut information.</summary>
        public List<PersonAstronaut> People { get; set; } = new List<PersonAstronaut> { };
    }
}
