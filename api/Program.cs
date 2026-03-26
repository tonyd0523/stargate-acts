using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using StargateAPI.Business.Behaviors;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Queries;
using StargateAPI.Business.Data;

// Architecture:
//   CQRS-style API using MediatR. All domain logic lives in request handlers under
//   Business/Commands (writes) and Business/Queries (reads). Controllers are thin
//   dispatchers — they send a request via IMediator and map the result to HTTP.
//
//   Input validation runs in MediatR pre-processors (IRequestPreProcessor<T>) before
//   the handler executes. BadHttpRequestException from a pre-processor produces a 400.
//
//   Cross-cutting concerns (logging, audit) are handled by LoggingBehavior, a MediatR
//   pipeline behavior registered globally below.
//
//   Data access: EF Core for writes (change tracking), Dapper for reads (flexible
//   projections). Both share the same SQLite connection via StargateContext.Connection.

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Stargate ACTS API",
        Version = "v1",
        Description = "Astronaut Career Tracking System (ACTS) — manages people, astronaut duty assignments, and career history."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});
builder.Services.AddDbContext<StargateContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("StarbaseApiDatabase"))
           // WHY: Moving seed data into HasData() causes EF Core to emit a
           // PendingModelChangesWarning at startup if the migration hasn't been applied yet.
           // Suppressing it prevents a noisy warning during development without hiding
           // real schema issues — the Migrate() call below still applies any pending migrations.
           .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

builder.Services.AddMediatR(cfg =>
{
    cfg.AddRequestPreProcessor<CreateAstronautDutyPreProcessor>();

    // FIX: CreatePersonPreProcessor was never registered in the original project.
    // Without this, the duplicate-name guard in CreatePerson.cs never executes and
    // the first duplicate silently hits the DB unique-index constraint instead of
    // returning a clean 400 Bad Request.
    cfg.AddRequestPreProcessor<CreatePersonPreProcessor>();

    // WHY: UpdatePerson needs pre-validation (person must exist, new name must be unique)
    // before the handler runs. Following the same pre-processor pattern already used
    // by CreateAstronautDutyPreProcessor keeps validation consistent across commands.
    cfg.AddRequestPreProcessor<UpdatePersonPreProcessor>();

    // FIX: UpdateAstronautDutyPreProcessor was not registered. Without it the
    // "duty not found" check in UpdateAstronautDuty.cs never runs, causing a
    // NullReferenceException in the handler when an invalid ID is supplied.
    cfg.AddRequestPreProcessor<UpdateAstronautDutyPreProcessor>();

    // WHY: Registers all IRequestHandler, IRequestPreProcessor, and IPipelineBehavior implementations in the assembly.
    // This keeps the registration list clean and automatically includes new handlers and pre-processors as they are added, without needing to remember to register each one manually.
    // Note: MediatR's assembly scanning is not the most efficient, so in a larger project you might want to register handlers more selectively or use a third-party DI container with better assembly-scanning performance. But for this small project the convenience outweighs the cost.
    // FIX: GetAuditLogsHandler was not registered, so the GetAuditLogs query always returned a 404 Not Found instead of executing the handler logic and returning results.
    // WHY: The README requires an endpoint to retrieve audit logs with pagination, searching, and sorting. GetAuditLogsHandler implements this logic, but without registering it here, the corresponding controller action always returns 404 Not Found instead of executing the handler and returning results.
    cfg.RegisterServicesFromAssemblies(typeof(Program).Assembly);
});

// WHY: The README requires logging exceptions, successes, and storing logs in the database.
// A MediatR pipeline behavior is the cleanest way to satisfy this — it intercepts every
// command and query automatically without modifying individual handlers.
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StargateContext>();
    // WHY: Seed data was moved into EF Core's HasData() so it is version-controlled
    // alongside schema changes in migrations, rather than applied as a separate
    // runtime step that could run out of order or re-seed on every restart.
    // DatabaseSeeder.Seed(db);
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "Stargate ACTS API";
        options.DefaultModelsExpandDepth(1);
        options.EnableTryItOutByDefault();
    });
}

app.UseHttpsRedirection();

// ADDED: Serves astronaut photos from wwwroot/photos/ without a controller round-trip.
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Needed so WebApplicationFactory<Program> can reference this type from the test project.
public partial class Program { }
