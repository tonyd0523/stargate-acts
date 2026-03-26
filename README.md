# Stargate ACTS

## Astronaut Career Tracking System

A full-stack application for tracking astronaut careers, duty assignments, and personnel records. Built with a **.NET 10 API** backend and **Angular 19** frontend.

---

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) and npm

### 1 — Start the API

```bash
cd api
dotnet run
```

- API: http://localhost:5204
- Swagger UI: http://localhost:5204/swagger
- SQLite database is created, migrated, and seeded automatically on first run.

### 2 — Start the UI

```bash
cd ui
npm install
npm start
```

- UI: http://localhost:4200
- Dev proxy forwards `/api/*` to the API — no CORS needed.

### 3 — Run Tests

```bash
dotnet test StargateAPI.Tests
```

### 4 — Run Tests with Code Coverage

```bash
dotnet test StargateAPI.Tests --settings coverage.runsettings --collect:"XPlat Code Coverage"
```

### 5 — Generate HTML Coverage Report

```bash
export PATH="$HOME/.dotnet/tools:$PATH"
reportgenerator \
  "-reports:StargateAPI.Tests/TestResults/*/coverage.cobertura.xml" \
  "-targetdir:StargateAPI.Tests/TestResults/CoverageReport" \
  "-reporttypes:Html"
open StargateAPI.Tests/TestResults/CoverageReport/index.html
```

Requires `dotnet tool install -g dotnet-reportgenerator-globaltool` (one-time).

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| API | .NET 10, C#, MediatR (CQRS), EF Core (writes), Dapper (reads) |
| Database | SQLite (auto-created, auto-migrated) |
| UI | Angular 19, TypeScript, SCSS |
| Testing | xUnit, Moq, Microsoft.AspNetCore.Mvc.Testing, Coverlet |
| Coverage | Coverlet + ReportGenerator |

---

## Architecture

```
api/
├── Controllers/          Thin HTTP dispatchers — send MediatR requests, map results
├── Business/
│   ├── Commands/         Write operations (Create/Update Person, Create/Update Duty)
│   ├── Queries/          Read operations (Get People, Duties, Logs, Stats)
│   ├── Behaviors/        MediatR pipeline (LoggingBehavior — audit trail)
│   ├── Data/             EF Core entities, configurations, DbContext
│   ├── Dtos/             Dapper read models
│   └── Migrations/       EF Core migrations (schema + seed data)
├── wwwroot/photos/       Astronaut profile photos
└── Program.cs            App bootstrap, DI, middleware

ui/src/app/
├── app.ts                Root component (all state, signals, tab navigation)
├── app.html              Template (People, Search, Duties, Logs tabs)
├── app.scss              Space-themed HUD styles
└── astronaut.service.ts  HTTP service layer

StargateAPI.Tests/
├── Commands/             Handler + pre-processor tests
├── Queries/              Query handler tests
├── Controllers/          Controller tests (mocked MediatR)
├── Behaviors/            LoggingBehavior tests
├── Integration/          WebApplicationFactory + migration tests
├── Models/               Entity/DTO property tests
└── Helpers/              In-memory SQLite test factory
```

---

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/person` | List all people with astronaut details |
| `GET` | `/person/{name}` | Get person by name with full duty history |
| `POST` | `/person` | Create a new person |
| `PUT` | `/person/{name}` | Update a person's name |
| `POST` | `/person/{name}/photo` | Upload a profile photo |
| `GET` | `/astronautduty` | List all astronaut duties |
| `GET` | `/astronautduty/{name}` | Get duties for a specific person |
| `POST` | `/astronautduty` | Create a new astronaut duty |
| `PUT` | `/astronautduty/{id}` | Update an existing duty |
| `GET` | `/auditlog` | Paginated audit logs with search and sort |
| `GET` | `/stats` | System-wide statistics |

### Audit Log Query Parameters

| Param | Default | Description |
|-------|---------|-------------|
| `page` | 1 | Page number (1-based) |
| `pageSize` | 50 | Entries per page |
| `search` | — | Filter logs by message text |
| `sortBy` | `date` | Sort column: `date`, `status`, or `message` |
| `sortDirection` | `desc` | Sort order: `asc` or `desc` |

---

## UI Features

- **People tab** — Browse all personnel, view profiles with avatars, inline name editing
- **Search tab** — Search by name, view full astronaut career history and duty timeline
- **All Duties tab** — Browse all duty assignments across all astronauts
- **Audit Logs tab** — Searchable, sortable, paginated log of every API operation
- **Stats bar** — Real-time counts of people, active/retired astronauts, total duties
- **Photo upload** — Click an avatar to upload a profile photo
- **Edit mode** — Toggle between read-only and edit mode for data modifications
- **Space-themed HUD** — Animated sci-fi interface with cyan accents and scan-line effects

---

## Key Changes from Original

### Bug Fixes

- **CreatePersonPreProcessor** was never registered in MediatR — duplicate names silently hit the DB unique constraint instead of returning 400
- **UpdateAstronautDutyPreProcessor** was not registered — invalid duty IDs caused NullReferenceException instead of a clean error
- **GetAuditLogsHandler** was not registered — the audit log endpoint always returned 404
- Input guards added to pagination (Page < 1, PageSize < 1 clamped to safe defaults)

### New Features

- **UpdatePerson** command — rename a person with pre-validation (exists + unique name check)
- **UpdateAstronautDuty** command — edit existing duty assignments
- **Stats endpoint** (`GET /stats`) — aggregate counts for dashboard
- **Photo upload** (`POST /person/{name}/photo`) — profile pictures served from wwwroot
- **Audit log search, sort, and pagination** — server-side filtering by message text, sortable by date/status/message
- **Full Angular 19 UI** — single-page app with tabbed navigation

### Architecture Improvements

- Upgraded from .NET 8 to .NET 10
- Seed data moved into EF Core `HasData()` migrations (version-controlled, no runtime seeder)
- Added `Person.Name` unique index via migration
- Added `Person.PhotoUrl` column via migration
- MediatR assembly scanning replaces manual handler registration
- `LoggingBehavior` pipeline captures timing, serialized request params, and exception details

### Testing

- **129 passing tests** across 10 test classes
- **93%+ line coverage**, **100% branch coverage** on business logic
- Integration tests for `Program.cs` (app startup, all endpoints respond)
- Migration smoke tests (all migrations apply, schema verified, seed data present)
- Full coverage of all commands, queries, controllers, and the logging pipeline

---

## Business Rules

1. A Person is uniquely identified by their Name.
2. A Person who has not had an astronaut assignment will not have Astronaut records.
3. A Person will only ever hold one current Astronaut Duty Title, Start Date, and Rank at a time.
4. A Person's Current Duty will not have a Duty End Date.
5. A Person's Previous Duty End Date is set to the day before the New Astronaut Duty Start Date.
6. A Person is classified as 'Retired' when their Duty Title is 'RETIRED'.
7. A Person's Career End Date is one day before the Retired Duty Start Date.

---

## Seed Data

The database is seeded with six astronauts via EF Core migration:

| Name | Current Rank | Current Duty | Status |
|------|-------------|-------------|--------|
| Neil Armstrong | 1SG | RETIRED | Retired |
| Buzz Aldrin | SPC | Shuttle Pilot | Active |
| Sally Ride | MAJ | Mission Specialist | Active |
| Mae Jemison | CPT | Science Specialist | Active |
| Chris Hadfield | COL | Station Commander | Active |
| Valentina Tereshkova | 1LT | Cosmonaut Pilot | Active |

---

## Code Coverage Tool

- **Coverlet** (`coverlet.collector` NuGet package) — collects coverage during `dotnet test`, outputs `coverage.cobertura.xml`
- **ReportGenerator** (`dotnet-reportgenerator-globaltool`) — converts XML to browsable HTML report