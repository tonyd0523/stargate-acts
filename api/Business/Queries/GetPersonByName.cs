using Dapper;
using MediatR;
using StargateAPI.Business.Data;
using StargateAPI.Business.Dtos;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Queries
{
    public class GetPersonByName : IRequest<GetPersonByNameResult>
    {
        public required string Name { get; set; } = string.Empty;
    }

    public class GetPersonByNameHandler : IRequestHandler<GetPersonByName, GetPersonByNameResult>
    {
        private readonly StargateContext _context;
        public GetPersonByNameHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<GetPersonByNameResult> Handle(GetPersonByName request, CancellationToken cancellationToken)
        {
            var result = new GetPersonByNameResult();

            // WHY: The original embedded the user-supplied name directly into the SQL string.
            // If a caller passed a name like "' OR '1'='1", they could manipulate the query
            // and return all rows. Parameterized queries prevent this entirely.
            // var query = $"SELECT a.Id as PersonId, a.Name, b.CurrentRank, b.CurrentDutyTitle, b.CareerStartDate, b.CareerEndDate FROM [Person] a LEFT JOIN [AstronautDetail] b on b.PersonId = a.Id WHERE '{request.Name}' = a.Name";
            // var person = await _context.Connection.QueryAsync<PersonAstronaut>(query);
            var query = "SELECT a.Id as PersonId, a.Name, a.PhotoUrl, b.CurrentRank, b.CurrentDutyTitle, b.CareerStartDate, b.CareerEndDate FROM [Person] a LEFT JOIN [AstronautDetail] b on b.PersonId = a.Id WHERE a.Name = @Name";

            // FIX: Replaced QueryAsync + .FirstOrDefault() with QueryFirstOrDefaultAsync.
            // Name has a unique index so this query returns at most one row. QueryAsync
            // allocates an IEnumerable for all results before we discard all but the first;
            // QueryFirstOrDefaultAsync stops after the first row and is semantically correct.
            // var person = await _context.Connection.QueryAsync<PersonAstronaut>(query, new { request.Name });
            // result.Person = person.FirstOrDefault();
            result.Person = await _context.Connection.QueryFirstOrDefaultAsync<PersonAstronaut>(query, new { request.Name });

            return result;
        }
    }

    /// <summary>
    /// Response containing a single person's details.
    /// </summary>
    public class GetPersonByNameResult : BaseResponse
    {
        /// <summary>The person and their current astronaut info, or null if not found.</summary>
        public PersonAstronaut? Person { get; set; }
    }
}
