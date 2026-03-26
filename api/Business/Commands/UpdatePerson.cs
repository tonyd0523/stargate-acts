// Renames a person, enforcing two invariants in the pre-processor before the handler runs:
//   1. The person being renamed must exist.
//   2. The new name must not already be taken.
// Separating validation into the pre-processor keeps the handler free of guard clauses.
using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Commands
{
    /// <summary>
    /// Request to rename an existing person.
    /// </summary>
    public class UpdatePerson : IRequest<UpdatePersonResult>
    {
        /// <summary>Current name of the person.</summary>
        /// <example>John Glenn</example>
        public required string Name { get; set; }

        /// <summary>New name to assign.</summary>
        /// <example>John H. Glenn</example>
        public required string NewName { get; set; }
    }

    public class UpdatePersonPreProcessor : IRequestPreProcessor<UpdatePerson>
    {
        private readonly StargateContext _context;
        public UpdatePersonPreProcessor(StargateContext context)
        {
            _context = context;
        }
        public Task Process(UpdatePerson request, CancellationToken cancellationToken)
        {
            // Two separate reads without a transaction: a concurrent request could claim
            // the target name between these checks and the handler's write. The unique
            // index on Person.Name is the final safety net and will throw on conflict.
            var person = _context.People.AsNoTracking().FirstOrDefault(z => z.Name == request.Name);
            if (person is null) throw new BadHttpRequestException($"Person '{request.Name}' not found.");

            var nameConflict = _context.People.AsNoTracking().FirstOrDefault(z => z.Name == request.NewName);
            if (nameConflict is not null) throw new BadHttpRequestException($"A person named '{request.NewName}' already exists.");

            return Task.CompletedTask;
        }
    }

    public class UpdatePersonHandler : IRequestHandler<UpdatePerson, UpdatePersonResult>
    {
        private readonly StargateContext _context;

        public UpdatePersonHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<UpdatePersonResult> Handle(UpdatePerson request, CancellationToken cancellationToken)
        {
            var person = await _context.People.FirstOrDefaultAsync(z => z.Name == request.Name, cancellationToken);

            person!.Name = request.NewName;

            await _context.SaveChangesAsync(cancellationToken);

            return new UpdatePersonResult { Id = person.Id };
        }
    }

    /// <summary>
    /// Response returned after renaming a person.
    /// </summary>
    public class UpdatePersonResult : BaseResponse
    {
        /// <summary>The updated person's ID.</summary>
        /// <example>7</example>
        public int Id { get; set; }
    }
}
