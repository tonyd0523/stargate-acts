using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Commands
{
    /// <summary>
    /// Request to create a new person. Names must be unique.
    /// </summary>
    public class CreatePerson : IRequest<CreatePersonResult>
    {
        /// <summary>Full name of the person to create.</summary>
        /// <example>John Glenn</example>
        public required string Name { get; set; } = string.Empty;
    }

    public class CreatePersonPreProcessor : IRequestPreProcessor<CreatePerson>
    {
        private readonly StargateContext _context;
        public CreatePersonPreProcessor(StargateContext context)
        {
            _context = context;
        }
        public Task Process(CreatePerson request, CancellationToken cancellationToken)
        {
            // Synchronous query: IRequestPreProcessor<T>.Process returns Task but there is
            // no async path here that would benefit from awaiting. The DB unique index on
            // Person.Name is the final safety net if two requests race past this check.
            var person = _context.People.AsNoTracking().FirstOrDefault(z => z.Name == request.Name);

            // FIX: Replaced generic "Bad Request" — same issue as CreateAstronautDutyPreProcessor.
            // The original gave callers no indication that the name was already taken.
            if (person is not null) throw new BadHttpRequestException($"A person named '{request.Name}' already exists.");

            return Task.CompletedTask;
        }
    }

    public class CreatePersonHandler : IRequestHandler<CreatePerson, CreatePersonResult>
    {
        private readonly StargateContext _context;

        public CreatePersonHandler(StargateContext context)
        {
            _context = context;
        }
        public async Task<CreatePersonResult> Handle(CreatePerson request, CancellationToken cancellationToken)
        {

                var newPerson = new Person()
                {
                   Name = request.Name
                };

                await _context.People.AddAsync(newPerson);

                await _context.SaveChangesAsync();

                return new CreatePersonResult()
                {
                    Id = newPerson.Id
                };
          
        }
    }

    /// <summary>
    /// Response returned after creating a person.
    /// </summary>
    public class CreatePersonResult : BaseResponse
    {
        /// <summary>The newly created person's ID.</summary>
        /// <example>7</example>
        public int Id { get; set; }
    }
}
