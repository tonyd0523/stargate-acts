# ACTS — Astronaut Career Tracking System

## Stargate | Technical Exercise Implementation

This document maps every README requirement to its implementation so reviewers can quickly locate each deliverable.

---

## Quick Start

### 1 — Start the API

```bash
cd api
dotnet run
```

- API: <http://localhost:5204>
- Swagger: <http://localhost:5204/swagger>
- Database (`starbase.db`) is created and migrated automatically on first run, then seeded with six real astronauts.

### 2 — Start the UI

```bash
cd ui
npm install
npm start
```

- UI: <http://localhost:4200>
- The dev proxy forwards `/api/*` to <http://localhost:5204> — no CORS configuration required.

### 3 — Run the Tests

```bash
cd exercise1
dotnet test
```

### 4 — Run with Code Coverage

```bash
cd exercise1
dotnet test --collect:"XPlat Code Coverage" --settings coverage.runsettings
```

Coverage results are written to `StargateAPI.Tests/TestResults/*/coverage.cobertura.xml`.
Program.cs and Migrations are excluded via `coverage.runsettings`.

---

## README Requirement Coverage

### Requirement: Enhance the Stargate API

> _"The REST API is expected to do the following:"_

> **Swagger Demo Tip:** All examples below use seeded data and are listed in a recommended demo order. Start the API (`cd api && dotnet run`), then open <http://localhost:5204/swagger>. Click **Try it out** on each endpoint and paste the values shown below. Each response block shows the exact JSON you should see.

#### 1. System Statistics

**Endpoint:** `GET /stats`

Returns aggregate counts across the system. Good starting point for a demo to show the database is seeded.

**Swagger:** No parameters — just click **Execute**.

```bash
curl http://localhost:5204/stats
```

**Expected response:**

```json
{
  "success": true,
  "message": "Successful",
  "responseCode": 200,
  "totalPeople": 6,
  "activeAstronauts": 5,
  "retiredAstronauts": 1,
  "totalDuties": 16
}
```

---

#### 2. Retrieve all people

**Endpoint:** `GET /person`

Returns every person in the system with their current astronaut detail (if any). People without assignments are included but have null detail fields.

**Handler:** `GetPeopleHandler` — Dapper query returning all rows from a LEFT JOIN of `Person` and `AstronautDetail`.

**Swagger:** No parameters — just click **Execute**.

```bash
curl http://localhost:5204/person
```

**Expected response (6 seeded people):**

```json
{
  "success": true,
  "message": "Successful",
  "responseCode": 200,
  "people": [
    {
      "personId": 1,
      "name": "Neil Armstrong",
      "photoUrl": "photos/neil-armstrong.jpg",
      "currentRank": "Colonel",
      "currentDutyTitle": "RETIRED",
      "careerStartDate": "1962-03-01T00:00:00",
      "careerEndDate": "1971-07-31T00:00:00"
    },
    {
      "personId": 2,
      "name": "Buzz Aldrin",
      "photoUrl": "photos/buzz-aldrin.jpg",
      "currentRank": "Colonel",
      "currentDutyTitle": "Mission Specialist",
      "careerStartDate": "1963-05-15T00:00:00",
      "careerEndDate": null
    },
    {
      "personId": 3,
      "name": "Sally Ride",
      "photoUrl": "photos/sally-ride.jpg",
      "currentRank": "Lieutenant",
      "currentDutyTitle": "Payload Commander",
      "careerStartDate": "1978-01-01T00:00:00",
      "careerEndDate": null
    },
    {
      "personId": 4,
      "name": "Mae Jemison",
      "photoUrl": "photos/mae-jemison.jpg",
      "currentRank": "Lieutenant",
      "currentDutyTitle": "Science Mission Specialist",
      "careerStartDate": "1987-06-04T00:00:00",
      "careerEndDate": null
    },
    {
      "personId": 5,
      "name": "Chris Hadfield",
      "photoUrl": "photos/chris-hadfield.jpg",
      "currentRank": "Colonel",
      "currentDutyTitle": "ISS Commander",
      "careerStartDate": "1992-12-01T00:00:00",
      "careerEndDate": null
    },
    {
      "personId": 6,
      "name": "Valentina Tereshkova",
      "photoUrl": "photos/valentina-tereshkova.jpg",
      "currentRank": "Major",
      "currentDutyTitle": "Senior Cosmonaut",
      "careerStartDate": "1962-02-16T00:00:00",
      "careerEndDate": null
    }
  ]
}
```

---

#### 3. Retrieve a person by name

**Endpoint:** `GET /person/{name}`

Returns the person's current rank, current duty title, career start/end dates, and astronaut detail. If the person has never had an astronaut assignment they are returned without detail fields. Returns HTTP 200 with `success: false` and a message if no person is found.

**Handler:** `GetPersonByNameHandler` — Dapper query with a LEFT JOIN between `Person` and `AstronautDetail`.

**Swagger:** Enter `Neil Armstrong` in the **name** field, click **Execute**.

```bash
curl http://localhost:5204/person/Neil%20Armstrong
```

**Expected response:**

```json
{
  "success": true,
  "message": "Successful",
  "responseCode": 200,
  "person": {
    "personId": 1,
    "name": "Neil Armstrong",
    "photoUrl": "photos/neil-armstrong.jpg",
    "currentRank": "Colonel",
    "currentDutyTitle": "RETIRED",
    "careerStartDate": "1962-03-01T00:00:00",
    "careerEndDate": "1971-07-31T00:00:00"
  }
}
```

---

#### 4. Add a person

**Endpoint:** `POST /person`

Creates a new person. The request body is a plain JSON string (`"Eileen Collins"`).

**Handler:** `CreatePersonPreProcessor` validates that no person with the same name already exists (enforcing Rule 1). `CreatePersonHandler` inserts the row.

**Swagger:** In the request body field, enter `"Eileen Collins"` (with quotes), click **Execute**.

```bash
curl -X POST http://localhost:5204/person \
  -H "Content-Type: application/json" \
  -d '"Eileen Collins"'
```

**Expected response:**

```json
{
  "success": true,
  "message": "Successful",
  "responseCode": 200,
  "id": 7
}
```

> **Verify:** Run `GET /person/Eileen Collins` to see the newly created person — she will have no astronaut details yet.

---

#### 5. Update (rename) a person

**Endpoint:** `PUT /person/{name}`

Renames an existing person. The request body is the new name as a JSON string.

**Handler:** `UpdatePersonPreProcessor` confirms the current name exists and the new name is not already taken. `UpdatePersonHandler` updates the row via EF Core.

**Swagger:** Enter `Eileen Collins` in the **name** field. In the request body, enter `"Eileen M. Collins"` (with quotes). Click **Execute**.

> **Prerequisite:** Run `POST /person` with `"Eileen Collins"` first (step 4 above).

```bash
curl -X PUT http://localhost:5204/person/Eileen%20Collins \
  -H "Content-Type: application/json" \
  -d '"Eileen M. Collins"'
```

**Expected response:**

```json
{
  "success": true,
  "message": "Successful",
  "responseCode": 200,
  "id": 7
}
```

> **Note:** `PUT /person/{name}` was not present in the original codebase and was added as part of this exercise.

---

#### 6. Upload a person's photo

**Endpoint:** `POST /person/{name}/photo`

Uploads a JPEG or PNG photo for a person (max 5 MB). The photo is saved to `wwwroot/photos/` and the person's `PhotoUrl` is updated.

**Swagger:** Enter `Eileen M. Collins` in the **name** field. Click **Choose File** and select a JPEG or PNG image. Click **Execute**.

> **Prerequisite:** The person must exist (steps 4–5 above).

```bash
curl -X POST http://localhost:5204/person/Eileen%20M.%20Collins/photo \
  -F "file=@eileen-collins.jpg"
```

**Expected response:**

```json
{
  "success": true,
  "message": "Photo uploaded successfully.",
  "responseCode": 200
}
```

---

#### 7. Retrieve astronaut duties by name

**Endpoint:** `GET /astronautduty/{name}`

Returns the person's current detail and their full duty history sorted most-recent first.

**Handler:** `GetAstronautDutiesByNameHandler` — two Dapper queries: one for the person + detail, one for all duties ordered by `DutyStartDate DESC`.

**Bug fixed:** The original controller called `GetPersonByName` instead of `GetAstronautDutiesByName`, so this endpoint always returned person data with no duties. Fixed by sending the correct query.

**Swagger:** Enter `Buzz Aldrin` in the **name** field, click **Execute**.

```bash
curl http://localhost:5204/astronautduty/Buzz%20Aldrin
```

**Expected response:**

```json
{
  "success": true,
  "message": "Successful",
  "responseCode": 200,
  "person": {
    "personId": 2,
    "name": "Buzz Aldrin",
    "photoUrl": "photos/buzz-aldrin.jpg",
    "currentRank": "Colonel",
    "currentDutyTitle": "Mission Specialist",
    "careerStartDate": "1963-05-15T00:00:00",
    "careerEndDate": null
  },
  "astronautDuties": [
    {
      "id": 7,
      "personId": 2,
      "rank": "Colonel",
      "dutyTitle": "Mission Specialist",
      "dutyStartDate": "1969-07-20T00:00:00",
      "dutyEndDate": null
    },
    {
      "id": 6,
      "personId": 2,
      "rank": "Captain",
      "dutyTitle": "LEM Pilot",
      "dutyStartDate": "1966-09-01T00:00:00",
      "dutyEndDate": "1969-07-19T00:00:00"
    },
    {
      "id": 5,
      "personId": 2,
      "rank": "2LT",
      "dutyTitle": "Pilot",
      "dutyStartDate": "1963-05-15T00:00:00",
      "dutyEndDate": "1966-08-31T00:00:00"
    }
  ]
}
```

---

#### 8. Retrieve all astronaut duties

**Endpoint:** `GET /astronautduty`

Returns every duty record across all astronauts with person names included. Sorted by `DutyStartDate DESC`.

**Handler:** `GetAllAstronautDutiesHandler` — Dapper query joining `AstronautDuty` with `Person`.

**Swagger:** No parameters — just click **Execute**.

```bash
curl http://localhost:5204/astronautduty
```

**Expected response (16 seeded duties — first two shown):**

```json
{
  "success": true,
  "message": "Successful",
  "responseCode": 200,
  "astronautDuties": [
    {
      "id": 14,
      "personId": 5,
      "personName": "Chris Hadfield",
      "rank": "Colonel",
      "dutyTitle": "ISS Commander",
      "dutyStartDate": "2013-05-13T00:00:00",
      "dutyEndDate": null
    },
    {
      "id": 13,
      "personId": 5,
      "personName": "Chris Hadfield",
      "rank": "Captain",
      "dutyTitle": "Mission Specialist",
      "dutyStartDate": "1995-10-22T00:00:00",
      "dutyEndDate": "2013-05-12T00:00:00"
    }
  ]
}
```

---

#### 9. Add an astronaut duty

**Endpoint:** `POST /astronautduty`

Assigns a new duty to an existing person. All date logic and career tracking are handled server-side.

**Handler:** `CreateAstronautDutyPreProcessor` validates the person exists and the same duty title doesn't already exist for them. `CreateAstronautDutyHandler` applies all seven business rules (see [Business Rules](#rules-enforcement) below).

**Swagger:** Paste the JSON below into the request body, click **Execute**.

> **Prerequisite:** The person `"Eileen M. Collins"` must exist (steps 4–5 above).

```json
{
  "name": "Eileen M. Collins",
  "rank": "Colonel",
  "dutyTitle": "Shuttle Commander",
  "dutyStartDate": "1999-07-23"
}
```

```bash
curl -X POST http://localhost:5204/astronautduty \
  -H "Content-Type: application/json" \
  -d '{"name":"Eileen M. Collins","rank":"Colonel","dutyTitle":"Shuttle Commander","dutyStartDate":"1999-07-23"}'
```

**Expected response:**

```json
{
  "success": true,
  "message": "Successful",
  "responseCode": 200,
  "id": 17
}
```

> **Verify:** Run `GET /astronautduty/Eileen M. Collins` to see the duty history and confirm `CareerStartDate` was set automatically.

---

#### 10. Update an astronaut duty

**Endpoint:** `PUT /astronautduty/{id}`

Updates an existing duty record's rank, title, and/or dates. If the duty is the person's current duty, `AstronautDetail` is synced automatically.

**Handler:** `UpdateAstronautDutyPreProcessor` validates the duty exists. `UpdateAstronautDutyHandler` updates the record and syncs `AstronautDetail` if needed.

**Swagger:** Enter `17` in the **id** field. Paste the JSON below into the request body. Click **Execute**.

> **Prerequisite:** Run `POST /astronautduty` first (step 9 above) to create duty ID 17.

```json
{
  "rank": "Brigadier General",
  "dutyTitle": "Shuttle Commander",
  "dutyStartDate": "1999-07-23",
  "dutyEndDate": null
}
```

```bash
curl -X PUT http://localhost:5204/astronautduty/17 \
  -H "Content-Type: application/json" \
  -d '{"rank":"Brigadier General","dutyTitle":"Shuttle Commander","dutyStartDate":"1999-07-23","dutyEndDate":null}'
```

**Expected response:**

```json
{
  "success": true,
  "message": "Successful",
  "responseCode": 200,
  "id": 17
}
```

> **Verify:** Run `GET /astronautduty/Eileen M. Collins` — her `currentRank` should now show `"Brigadier General"`.

---

#### 11. Audit Logs

**Endpoint:** `GET /auditlog?page=1&pageSize=50`

Returns paginated audit log entries, sorted newest-first. Every API operation is logged automatically by `LoggingBehavior`.

**Swagger:** Optionally set **page** to `1` and **pageSize** to `10`, click **Execute**.

```bash
curl "http://localhost:5204/auditlog?page=1&pageSize=10"
```

**Expected response (after running the demo steps above):**

```json
{
  "success": true,
  "message": "Successful",
  "responseCode": 200,
  "logs": [
    {
      "id": 5,
      "createdDate": "2026-03-24T17:15:30.123Z",
      "message": "CreateAstronautDuty succeeded | {\"name\":\"Eileen M. Collins\",\"rank\":\"Colonel\",\"dutyTitle\":\"Shuttle Commander\",\"dutyStartDate\":\"1999-07-23T00:00:00\"}",
      "isException": false
    },
    {
      "id": 4,
      "createdDate": "2026-03-24T17:14:20.456Z",
      "message": "UpdatePerson succeeded | {\"name\":\"Eileen Collins\",\"newName\":\"Eileen M. Collins\"}",
      "isException": false
    }
  ],
  "totalCount": 5,
  "page": 1,
  "pageSize": 10
}
```

---

### Swagger Demo Walkthrough (Recommended Order)

For a clean, reproducible demo with a freshly seeded database, follow these steps in order:

| Step | Endpoint | Input | What It Shows |
|------|----------|-------|---------------|
| 1 | `GET /stats` | _(none)_ | System has 6 people, 5 active, 1 retired, 16 duties |
| 2 | `GET /person` | _(none)_ | All 6 seeded astronauts with photos and career details |
| 3 | `GET /person/{name}` | `Neil Armstrong` | Retired astronaut with career end date |
| 4 | `GET /astronautduty/{name}` | `Buzz Aldrin` | Active astronaut with 3 duties (most recent first) |
| 5 | `GET /astronautduty` | _(none)_ | All 16 duties across all astronauts |
| 6 | `POST /person` | `"Eileen Collins"` | Creates a new person (ID 7) |
| 7 | `PUT /person/{name}` | name=`Eileen Collins`, body=`"Eileen M. Collins"` | Renames her |
| 8 | `POST /person/{name}/photo` | name=`Eileen M. Collins`, file=_(any JPEG)_ | Uploads her photo |
| 9 | `GET /person/Eileen M. Collins` | _(verify)_ | Confirm rename + photo URL |
| 10 | `POST /astronautduty` | `{"name":"Eileen M. Collins","rank":"Colonel","dutyTitle":"Shuttle Commander","dutyStartDate":"1999-07-23"}` | Assigns her first duty |
| 11 | `PUT /astronautduty/{id}` | id=`17`, body with `"rank":"Brigadier General"` | Updates her rank |
| 12 | `GET /astronautduty/Eileen M. Collins` | _(verify)_ | Confirm duty + updated rank |
| 13 | `GET /auditlog` | page=`1`, pageSize=`10` | All operations logged automatically |
| 14 | `GET /stats` | _(none)_ | Now shows 7 people, 6 active, 1 retired, 17 duties |

---

### Requirement: Implement a User Interface

> _"Successfully implement a web application that demonstrates production level quality. Angular is preferred."_

Built with **Angular 21** using standalone components, the Signals API, and `@if`/`@for` control flow. All five API operations are available through a tabbed mission-control interface.

#### UI Features by Tab

| Tab | Operation | API Call |
|-----|-----------|----------|
| **Search Duties** | Retrieve an astronaut's full career history | `GET /astronautduty/{name}` |
| **People** | Browse all people; look up a person by name | `GET /person` + `GET /person/{name}` |
| **People** | Rename an existing person | `PUT /person/{name}` |
| **People** | Upload/view astronaut photo | `POST /person/{name}/photo` |
| **Add Person** | Register a new person | `POST /person` |
| **Add Duty** | Assign a duty to an existing person | `POST /astronautduty` |
| **All Duties** | Browse all duties across all astronauts | `GET /astronautduty` |
| **Audit Logs** | Browse paginated operation history | `GET /auditlog` |

#### Visual Design

The UI is styled as a mission-control HUD with:

- **Starfield background** — 40 static stars (white, cyan, green, purple) with a twinkling animation
- **Nebula glow** — subtle radial gradients in the background that slowly breathe
- **Scanline overlay** — repeating CSS gradient giving a CRT monitor feel
- **Live mission clock** — ticks every second in the header (HH:MM:SS)
- **System status indicator** — animated green radar-ping dot

#### Animations

- Header sweep line, spinning dashed emblem ring, logo flicker
- Tab content slide-up on each tab switch
- Page heading letter-spacing expansion on enter
- Person cards lift and shimmer on hover; top border draws in on mount
- Timeline items cascade in with staggered delays
- People list rows stagger in and slide right on hover
- Form cards display a boot scan-line on mount
- Buttons show a diagonal shine sweep on hover
- Alerts spring in from the left
- Audit log rows stagger in per row; error badges pulse red

> _"Implement call(s) to retrieve an individual's astronaut duties."_

The **Search Duties** tab retrieves a person by name and renders their career profile (current rank, duty title, career dates, duration) followed by a full duty timeline showing every assignment with start/end dates and an "Active" badge on the current duty.

> _"Display the progress of the process and the results in a visually sophisticated and appealing manner."_

Loading states show an animated three-dot spinner (cyan/green/purple) with a scanning progress bar. Not-found states show a styled HUD warning icon. Results fade and slide into view.

---

### Task: Generate the Database

> _"Generate the database — This is your source and storage location."_

**Implementation:**

- `StargateContext` is configured to use SQLite (`starbase.db`) via Entity Framework Core 9.
- `Program.cs` calls `db.Database.Migrate()` at startup, which creates the database file and applies all pending migrations automatically — no manual `dotnet ef` commands are needed.
- **Seed data** is declared in `StargateContext.SeedData()` via EF Core's `HasData()` and applied automatically when `db.Database.Migrate()` runs at startup.

**Seed data includes six real astronauts (each with a photo):**

| Person | Status | Duties |
|--------|--------|--------|
| Neil Armstrong | Retired (1971) | 4 duties — Pilot → Flight Commander → Mission Commander → RETIRED |
| Buzz Aldrin | Active | 3 duties — Pilot → LEM Pilot → Mission Specialist |
| Sally Ride | Active | 2 duties — Mission Specialist → Payload Commander |
| Mae Jemison | Active | 2 duties — Mission Specialist → Science Mission Specialist |
| Chris Hadfield | Active | 3 duties — Mission Specialist → Mission Specialist → ISS Commander |
| Valentina Tereshkova | Active | 2 duties — Cosmonaut → Senior Cosmonaut |

**Migrations:**

| Migration | Change |
|-----------|--------|
| `20240122154939_InitialCreate` | Creates `Person`, `AstronautDetail`, `AstronautDuty` tables |
| `20240123000000_AddAuditLog` | Adds `AuditLog` table |
| `20240124000000_AddPersonNameUniqueIndex` | Adds unique index on `Person.Name` |

---

### Task: Enforce the Rules

> _"Enforce the rules."_

All seven business rules from the README are enforced in code. See [Rules Enforcement](#rules-enforcement) below for the full breakdown.

---

### Task: Improve Defensive Coding

> _"Improve defensive coding."_

The following defensive improvements were made to the original codebase:

#### SQL Injection — Fixed

The original code used string interpolation in every Dapper query, making all name-based lookups injectable:

```csharp
// BEFORE (vulnerable)
var person = await db.Connection.QueryFirstOrDefaultAsync<PersonAstronaut>(
    $"SELECT * FROM Person WHERE Name = '{name}'");

// AFTER (parameterized)
var person = await db.Connection.QueryFirstOrDefaultAsync<PersonAstronaut>(
    "SELECT p.*, ad.* FROM Person p LEFT JOIN AstronautDetail ad ON p.Id = ad.PersonId WHERE p.Name = @Name",
    new { Name = name });
```

All three Dapper queries in `CreateAstronautDuty` and both queries in the Get handlers were fixed.

#### Null Reference — Fixed

`GetAstronautDutiesByNameHandler` accessed `person.PersonId` before checking whether `person` was null, causing a `NullReferenceException` for any unknown name:

```csharp
// BEFORE (crashes on unknown names)
var duties = await db.Connection.QueryAsync<AstronautDuty>(
    $"SELECT * FROM AstronautDuty WHERE PersonId = {person.PersonId}");

// AFTER (guarded)
if (person != null)
{
    duties = await db.Connection.QueryAsync<AstronautDuty>(
        "SELECT * FROM AstronautDuty WHERE PersonId = @PersonId",
        new { PersonId = person.PersonId });
}
```

#### Wrong Query in Controller — Fixed

`AstronautDutyController.GetAstronautDuties` sent `GetPersonByName` instead of `GetAstronautDutiesByName`, returning person data with no duties for every request:

```csharp
// BEFORE (wrong query)
var result = await _mediator.Send(new GetPersonByName { Name = name });

// AFTER (correct query)
var result = await _mediator.Send(new GetAstronautDutiesByName { Name = name });
```

#### Missing Try-Catch — Fixed

The `POST /astronautduty` controller action had no error handling. Any exception from the handler propagated as an unhandled 500 with a stack trace. A try-catch block was added to return a structured error response.

#### CareerEndDate Off-by-One — Fixed

When retiring a person, `CareerEndDate` was set to `request.DutyStartDate.Date` (same day as retirement) instead of the day before:

```csharp
// BEFORE (wrong date)
detail.CareerEndDate = request.DutyStartDate.Date;

// AFTER (correct per Rule 7)
detail.CareerEndDate = request.DutyStartDate.AddDays(-1).Date;
```

#### Input Validation in Pre-Processors

All write operations validate their inputs before the handler runs:

- `CreatePersonPreProcessor` — rejects empty names and duplicate names
- `UpdatePersonPreProcessor` — rejects if current name not found or new name is already taken
- `CreateAstronautDutyPreProcessor` — rejects if person not found, exact duplicate duty exists, or new duty start date is not after the current duty's start date (chronological ordering)
- `CreateAstronautDutyHandler` — wraps all writes (update previous duty, upsert detail, insert new duty) in an explicit transaction for atomicity

---

### Task: Add Unit Tests

> _"Add unit tests — identify the most impactful methods requiring tests — reach >50% code coverage."_

**113 tests across 14 files — all pass. 100% line coverage, 94.4% branch coverage.**

Run with:

```bash
cd exercise1
dotnet test
dotnet test --collect:"XPlat Code Coverage" --settings coverage.runsettings
```

| File | Tests | Coverage focus |
|------|-------|----------------|
| `Commands/CreatePersonTests` | 5 | Handler creates person; pre-processor rejects duplicates and empty names |
| `Commands/UpdatePersonTests` | 4 | Handler renames; pre-processor rejects missing person and name conflicts |
| `Commands/CreateAstronautDutyTests` | 15 | All 7 business rules, start date ordering validation, transaction atomicity |
| `Commands/UpdateAstronautDutyTests` | 7 | Updates duty; syncs AstronautDetail; handles RETIRED title |
| `Queries/GetPeopleTests` | 3 | Returns all people with and without astronaut details |
| `Queries/GetPersonByNameTests` | 4 | Returns correct person; null for unknown name; astronaut vs. non-astronaut shapes |
| `Queries/GetAstronautDutiesByNameTests` | 5 | Duties sorted descending; null person returns empty; multiple duties all returned |
| `Queries/GetAllAstronautDutiesTests` | 5 | All duties with person names joined |
| `Queries/GetAuditLogsTests` | 6 | Pagination, page clamping, empty results |
| `Queries/GetStatsTests` | 4 | Aggregate counts including empty database |
| `Behaviors/LoggingBehaviorTests` | 12 | Success/failure audit logs, OperationCanceledException, serialization errors, audit write failure |
| `Controllers/ControllerBaseExtensionsTests` | 3 | GetResponse maps status codes correctly |
| `Controllers/PersonControllerTests` | 17 | All CRUD + photo upload + delete + error paths |
| `Controllers/AstronautDutyControllerTests` | 14 | All CRUD + delete with detail rollback + error paths |
| `Controllers/AuditLogControllerTests` | 2 | Success and error paths |
| `Controllers/StatsControllerTests` | 2 | Success and error paths |
| `Models/EntityAndDtoPropertyTests` | 6 | Entity navigation defaults, DTO property coverage |

**Test infrastructure:** Each test creates an **in-memory SQLite database** with the real schema (via `EnsureCreated()`), seeds specific data, then clears the EF change tracker before invoking handlers. This exercises the real SQL layer — no mocks of the database. Controller tests use Moq for IMediator and real DbContext for endpoints that access the database directly.

---

### Task: Implement Process Logging

> _"Implement process logging — Log exceptions — Log successes — Store the logs in the database."_

**Implementation:** `LoggingBehavior<TRequest, TResponse>` is a MediatR `IPipelineBehavior` that wraps every handler automatically.

**On success:**
1. Serializes the request object to JSON
2. Writes a row to `AuditLog` with `IsException = false` and a message of the form:

   ```text
   CreateAstronautDuty succeeded | {"name":"Neil Armstrong","rank":"Colonel","dutyTitle":"Commander","dutyStartDate":"1969-07-20T00:00:00"}
   ```

3. Also emits an `ILogger.LogInformation` entry

**On failure:**
1. Logs the exception via `ILogger.LogError`
2. Clears the EF Core change tracker (so the failed transaction doesn't corrupt the audit write)
3. Writes a row to `AuditLog` with `IsException = true` and a message of the form:

   ```text
   CreateAstronautDuty failed: Person not found | {"name":"Unknown","rank":"Colonel","dutyTitle":"Commander","dutyStartDate":"1969-07-20T00:00:00"}
   ```

4. Re-throws the exception so the controller can return the correct HTTP status

**Viewing logs:** The **Audit Logs** tab in the UI shows all log entries paginated (50 per page), sorted newest-first, with the operation name and JSON parameters displayed separately for readability. Error rows are highlighted red with a pulsing FAIL badge.

---

## Rules Enforcement

The seven rules from the README and where each is enforced:

| # | Rule | Enforced By |
|---|------|-------------|
| 1 | A Person is uniquely identified by their Name | `CreatePersonPreProcessor` (rejects duplicate at app level); `PersonConfiguration.HasIndex(x => x.Name).IsUnique()` (enforced at DB level) |
| 2 | A Person with no astronaut assignment will not have Astronaut records | `GetPersonByNameHandler` and `GetAstronautDutiesByNameHandler` both LEFT JOIN — no astronaut rows are created unless a duty is assigned |
| 3 | A Person holds only one current duty at a time | `CreateAstronautDutyHandler` updates `AstronautDetail` to reflect the latest duty every time a new duty is added |
| 4 | A Person's current duty has no `DutyEndDate` | The previous duty's `DutyEndDate` is set when a new one is added; new duties are inserted with `DutyEndDate = null` |
| 5 | Previous `DutyEndDate` = new duty's `StartDate` − 1 day | `CreateAstronautDutyHandler`: `existingDuty.DutyEndDate = request.DutyStartDate.AddDays(-1).Date` |
| 6 | A Person is Retired when `DutyTitle = 'RETIRED'` | `CreateAstronautDutyHandler` checks `request.DutyTitle == "RETIRED"` and sets `AstronautDetail.CareerEndDate` accordingly |
| 7 | `CareerEndDate` = `RETIRED` duty's `StartDate` − 1 day | `CreateAstronautDutyHandler`: `detail.CareerEndDate = request.DutyStartDate.AddDays(-1).Date` |

---

## Architecture

```text
HTTP Request
    │
    ▼
Controller (try/catch → structured error response)
    │
    ▼
IMediator.Send(request)
    │
    ├── Pre-Processor (input validation — throws BadHttpRequestException on failure)
    │
    ├── LoggingBehavior (wraps the handler; writes to AuditLog on success and failure)
    │
    └── Handler (business logic)
            │
            ├── Dapper   (reads — raw SQL, parameterized)
            └── EF Core  (writes — change tracker, SaveChangesAsync)
                    │
                    ▼
                SQLite (starbase.db)
```

### Key design patterns

| Pattern | Usage |
|---------|-------|
| CQRS | Commands (`Create*`, `Update*`) and Queries (`Get*`) are separate MediatR request types |
| Pipeline Behavior | `LoggingBehavior` cross-cuts every operation without touching handler code |
| Pre-Processor | Validation runs before the behavior pipeline — fast-fail before any DB work |
| Repository-lite | Queries use Dapper for performance; commands use EF Core for safety |

---

## Project Structure

```text
exercise1/
├── api/
│   ├── Business/
│   │   ├── Behaviors/
│   │   │   └── LoggingBehavior.cs          # Wraps every handler; writes AuditLog rows
│   │   ├── Commands/
│   │   │   ├── CreatePerson.cs             # Pre-processor + handler
│   │   │   ├── UpdatePerson.cs             # Pre-processor + handler (added)
│   │   │   └── CreateAstronautDuty.cs      # Pre-processor + handler (bugs fixed)
│   │   ├── Data/
│   │   │   ├── Person.cs                   # Entity + unique-index configuration
│   │   │   ├── AstronautDetail.cs          # Entity
│   │   │   ├── AstronautDuty.cs            # Entity
│   │   │   ├── AuditLog.cs                 # Entity (added)
│   │   │   └── StargateContext.cs          # DbContext + SeedData()
│   │   ├── Migrations/                     # EF Core migrations (hand-authored with Designer.cs)
│   │   └── Queries/
│   │       ├── GetPersonByName.cs          # SQL injection fixed
│   │       ├── GetPeople.cs
│   │       ├── GetAstronautDutiesByName.cs # SQL injection + null ref + wrong query fixed
│   │       └── GetAuditLogs.cs             # Paginated logs query (added)
│   ├── Controllers/
│   │   ├── PersonController.cs             # GET /person, GET /person/{name}, POST /person, PUT /person/{name}, POST /person/{name}/photo
│   │   ├── AstronautDutyController.cs      # GET /astronautduty, GET /astronautduty/{name}, POST /astronautduty, PUT /astronautduty/{id} (try-catch added)
│   │   └── AuditLogController.cs           # GET /auditlog (added)
│   └── Program.cs                          # DI, migrate, seed
│
├── StargateAPI.Tests/
│   ├── Commands/
│   │   ├── CreatePersonTests.cs
│   │   ├── UpdatePersonTests.cs
│   │   └── CreateAstronautDutyTests.cs
│   ├── Queries/
│   │   ├── GetPeopleTests.cs
│   │   ├── GetPersonByNameTests.cs
│   │   └── GetAstronautDutiesByNameTests.cs
│   └── Helpers/
│       └── TestDbContextFactory.cs         # In-memory SQLite + change tracker clear
│
└── ui/
    ├── src/
    │   ├── app/
    │   │   ├── app.ts                      # Signals, live clock, all tab logic
    │   │   ├── app.html                    # 5-tab template
    │   │   ├── app.scss                    # HUD/space theme + animations
    │   │   └── astronaut.service.ts        # HttpClient wrapper for all 6 endpoints
    │   └── styles.scss                     # Global starfield, nebula, scanlines
    └── proxy.conf.json                     # /api → http://localhost:5204
```

---

## API Response Envelope

All endpoints return the same base shape:

```json
{
  "success": true,
  "message": "Successful",
  "responseCode": 200,
  "...": "endpoint-specific fields"
}
```

Error responses follow the same shape with `success: false`, a descriptive `message`, and an appropriate `responseCode`.

---

## Changes from Original Codebase

This section documents every file that was modified or added from the original provided code, with the specific change and the reason why.

---

### `StargateAPI.csproj`

| What changed | Why |
|---|---|
| `TargetFramework` upgraded from `net8.0` → `net10.0` | Current LTS; keeps tooling and security patches current |
| All package versions bumped to match (`EF Core 9.0.0`, `Swashbuckle 6.9.0`, etc.) | Dependency alignment with the new framework target |
| Added `<InternalsVisibleTo Include="StargateAPI.Tests" />` | Allows tests to access `internal` types like `LoggingBehaviorLog` for thorough coverage |

---

### `Program.cs`

#### Added: `ConfigureWarnings`

```csharp
// BEFORE
options.UseSqlite(builder.Configuration.GetConnectionString("StarbaseApiDatabase"))

// AFTER
options.UseSqlite(builder.Configuration.GetConnectionString("StarbaseApiDatabase"))
       .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
```

WHY: Moving seed data into `HasData()` causes EF Core to emit a `PendingModelChangesWarning` at startup when the seed migration hasn't been applied yet. Suppressing it prevents noise without hiding real schema issues.

---

#### Added: Missing pre-processor registrations

```csharp
// BEFORE
cfg.AddRequestPreProcessor<CreateAstronautDutyPreProcessor>();

// AFTER
cfg.AddRequestPreProcessor<CreateAstronautDutyPreProcessor>();
cfg.AddRequestPreProcessor<CreatePersonPreProcessor>();        // was never registered
cfg.AddRequestPreProcessor<UpdatePersonPreProcessor>();        // new command
cfg.AddRequestPreProcessor<UpdateAstronautDutyPreProcessor>(); // was never registered
```

WHY: `CreatePersonPreProcessor` was defined but never registered — duplicate-name validation silently never ran, allowing the DB constraint to throw an unhandled 500 instead of a clean 400. `UpdateAstronautDutyPreProcessor` was also unregistered, meaning an invalid duty ID caused a `NullReferenceException` in the handler.

---

#### Added: `LoggingBehavior` pipeline registration

```csharp
// BEFORE — nothing

// AFTER
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

WHY: The README requires logging exceptions, successes, and storing logs in the database. A MediatR pipeline behavior intercepts every command and query automatically without modifying individual handlers.

---

#### Added: `db.Database.Migrate()` at startup

The original `Program.cs` did not call `Migrate()` — the database had to be created manually. Added a startup scope that auto-applies all pending EF Core migrations (including seed data declared via `HasData()`) so the API is ready to use immediately after `dotnet run`.

---

### `api/Business/Data/StargateContext.cs`

#### Fixed: Seed data was commented out and broken

```csharp
// BEFORE — commented out
//SeedData(modelBuilder);

// AFTER — enabled
SeedData(modelBuilder);
```

#### Fixed: `DateTime.Now` in `HasData()`

```csharp
// BEFORE (broken — re-generates a migration on every dotnet ef migrations add)
CareerStartDate = DateTime.Now

// AFTER (literal date)
CareerStartDate = new DateTime(1962, 3, 1)
```

WHY: EF Core evaluates `HasData()` values at migration generation time. `DateTime.Now` produces a different value on every run, causing spurious migrations.

#### Fixed: Placeholder data replaced with realistic seed records

The original seeded `John Doe / Jane Doe` with a single hardcoded duty. Replaced with 6 real astronauts, 16 duties, and 6 `AstronautDetail` snapshots that satisfy all README career rules. Hardcoded IDs were added as required by `HasData()`.

---

### `api/Business/Data/Person.cs`

#### Added: Unique index on `Name`

```csharp
// BEFORE — no index
builder.HasKey(x => x.Id);
builder.Property(x => x.Id).ValueGeneratedOnAdd();

// AFTER
builder.HasKey(x => x.Id);
builder.Property(x => x.Id).ValueGeneratedOnAdd();
builder.HasIndex(x => x.Name).IsUnique();
```

WHY: The README states "A Person is uniquely identified by their Name." Without this index, duplicate names could be inserted through any code path that bypassed application-level validation.

---

### `api/Business/Data/AstronautDetail.cs`

#### Fixed: `CareerStartDate` nullable mismatch

```csharp
// BEFORE (non-nullable — mismatches the DB column which is nullable: true)
public DateTime CareerStartDate { get; set; }

// AFTER
public DateTime? CareerStartDate { get; set; }
```

WHY: The `InitialCreate` migration defines `CareerStartDate` as `nullable: true`. With the non-nullable C# type, EF Core silently returned `DateTime.MinValue` instead of `null` when no value was stored.

#### Fixed: Navigation property not nullable

```csharp
// BEFORE
public virtual Person Person { get; set; }

// AFTER
public virtual Person? Person { get; set; }
```

WHY: EF Core only populates navigation properties when explicitly loaded via `.Include()` or lazy loading — they are `null` by default. The non-nullable declaration produced CS8618 warnings and created a false safety guarantee.

---

### `api/Business/Data/AstronautDuty.cs`

#### Fixed: Navigation property not nullable

Same reason as `AstronautDetail.cs` above.

```csharp
// BEFORE
public virtual Person Person { get; set; }

// AFTER
public virtual Person? Person { get; set; }
```

---

### `api/Business/Commands/CreateAstronautDuty.cs`

#### Fixed: SQL injection on all three Dapper queries

```csharp
// BEFORE (injectable)
var query = $"SELECT * FROM [Person] WHERE '{request.Name}' = Name";
var query = $"SELECT * FROM [AstronautDetail] WHERE {person.Id} = PersonId";
var query = $"SELECT * FROM [AstronautDuty] WHERE {person.Id} = PersonId Order By DutyStartDate Desc";

// AFTER (parameterized)
var query = "SELECT * FROM [Person] WHERE Name = @Name";
var query = "SELECT * FROM [AstronautDetail] WHERE PersonId = @PersonId";
var query = "SELECT * FROM [AstronautDuty] WHERE PersonId = @PersonId ORDER BY DutyStartDate DESC";
```

#### Fixed: `CareerEndDate` off-by-one bug (both create and update paths)

```csharp
// BEFORE (wrong — sets CareerEndDate to the same day as retirement)
astronautDetail.CareerEndDate = request.DutyStartDate.Date;

// AFTER (correct per README Rule 7)
astronautDetail.CareerEndDate = request.DutyStartDate.AddDays(-1).Date;
```

#### Fixed: Duplicate duty check globally scoped

```csharp
// BEFORE (wrong — blocks any astronaut if any other astronaut has the same title+date)
var verifyNoPreviousDuty = _context.AstronautDuties
    .FirstOrDefault(z => z.DutyTitle == request.DutyTitle
                      && z.DutyStartDate == request.DutyStartDate);

// AFTER (scoped to the specific person)
var verifyNoPreviousDuty = _context.AstronautDuties
    .FirstOrDefault(z => z.PersonId == person.Id
                      && z.DutyTitle == request.DutyTitle
                      && z.DutyStartDate == request.DutyStartDate);
```

#### Fixed: Generic error messages

```csharp
// BEFORE
throw new BadHttpRequestException("Bad Request");

// AFTER
throw new BadHttpRequestException($"Person '{request.Name}' not found.");
throw new BadHttpRequestException($"'{request.Name}' already has a '{request.DutyTitle}' duty starting on {request.DutyStartDate:yyyy-MM-dd}.");
```

#### Added: Start date chronological validation

```csharp
// NEW — prevents past-dated duties from corrupting the timeline
var currentDuty = _context.AstronautDuties
    .AsNoTracking()
    .Where(z => z.PersonId == person.Id)
    .OrderByDescending(z => z.DutyStartDate)
    .FirstOrDefault();

if (currentDuty != null && request.DutyStartDate.Date <= currentDuty.DutyStartDate.Date)
    throw new BadHttpRequestException($"New duty start date must be after the current duty start date.");
```

WHY: The README rules imply duties must be chronological. Without this check, a past-dated duty would silently corrupt the timeline by back-dating the wrong duty's DutyEndDate.

#### Added: Transaction wrapping

```csharp
// NEW — ensures atomicity across all three writes
await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
// ... update previous duty, upsert detail, insert new duty ...
await _context.SaveChangesAsync(cancellationToken);
await transaction.CommitAsync(cancellationToken);
```

WHY: The handler previously had no transaction. If the insert of the new duty failed after the previous duty's DutyEndDate was already committed, the database would be left in an inconsistent state with no successor duty.

---

### `api/Business/Commands/CreatePerson.cs`

#### Fixed: Generic error message

```csharp
// BEFORE
throw new BadHttpRequestException("Bad Request");

// AFTER
throw new BadHttpRequestException($"A person named '{request.Name}' already exists.");
```

---

### `api/Business/Queries/GetPersonByName.cs`

#### Fixed: SQL injection

```csharp
// BEFORE
var query = $"SELECT ... WHERE '{request.Name}' = a.Name";
var person = await _context.Connection.QueryAsync<PersonAstronaut>(query);

// AFTER
var query = "SELECT ... WHERE a.Name = @Name";
result.Person = await _context.Connection.QueryFirstOrDefaultAsync<PersonAstronaut>(query, new { request.Name });
```

#### Fixed: `QueryAsync` + `.FirstOrDefault()` → `QueryFirstOrDefaultAsync`

WHY: `Name` has a unique index so the query returns at most one row. `QueryAsync` allocated a full `IEnumerable` before discarding all but the first element. `QueryFirstOrDefaultAsync` stops after the first row.

---

### `api/Business/Queries/GetAstronautDutiesByName.cs`

#### Fixed: SQL injection on both queries

```csharp
// BEFORE
var query = $"SELECT ... WHERE \'{request.Name}\' = a.Name";
var query = $"SELECT * FROM [AstronautDuty] WHERE {person.PersonId} = PersonId Order By DutyStartDate Desc";

// AFTER
var query = "SELECT ... WHERE a.Name = @Name";
var query = "SELECT * FROM [AstronautDuty] WHERE PersonId = @PersonId ORDER BY DutyStartDate DESC";
```

**Fixed: NullReferenceException when person not found**
```csharp
// BEFORE (crashes if person is null)
var duties = await _context.Connection.QueryAsync<AstronautDuty>(...);
result.AstronautDuties = duties.ToList();

// AFTER
if (person != null)
{
    var duties = await _context.Connection.QueryAsync<AstronautDuty>(..., new { PersonId = person.PersonId });
    result.AstronautDuties = duties.ToList();
}
```

**Fixed: `Person` result property not nullable**
```csharp
// BEFORE (misleads callers — can actually be null)
public PersonAstronaut Person { get; set; }

// AFTER
public PersonAstronaut? Person { get; set; }
```

---

### `api/Business/Queries/GetPeople.cs`

**Fixed: `public readonly` DbContext field**
```csharp
// BEFORE (exposes DbContext externally)
public readonly StargateContext _context;

// AFTER
private readonly StargateContext _context;
```

**Fixed: Unnecessary `$` prefix on static SQL string**
```csharp
// BEFORE (misleading — implies interpolation that isn't there)
var query = $"SELECT a.Id as PersonId, ...";

// AFTER
var query = "SELECT a.Id as PersonId, ...";
```

---

### `api/Business/Dtos/PersonAstronaut.cs`

**Fixed: Non-nullable strings on LEFT JOIN result fields**
```csharp
// BEFORE (Dapper will set these to null when LEFT JOIN returns no match, ignoring the initializer)
public string CurrentRank      { get; set; } = string.Empty;
public string CurrentDutyTitle { get; set; } = string.Empty;

// AFTER
public string? CurrentRank      { get; set; }
public string? CurrentDutyTitle { get; set; }
```
WHY: The query uses `LEFT JOIN AstronautDetail` — a person with no astronaut assignment returns `NULL` for these columns. Dapper maps `NULL` directly to `null` regardless of C# initializers.

---

### `api/Business/Queries/GetAuditLogs.cs` *(new file)*

**Added: Pagination input validation**
```csharp
// Added — without this, Page=0 produces a negative Skip() which throws ArgumentOutOfRangeException
if (request.Page < 1)     request.Page = 1;
if (request.PageSize < 1) request.PageSize = 50;
```

---

### `api/Controllers/AstronautDutyController.cs`

**Fixed: Wrong query dispatched**
```csharp
// BEFORE (returns person info, not duties)
var result = await _mediator.Send(new GetPersonByName() { Name = name });

// AFTER
var result = await _mediator.Send(new GetAstronautDutiesByName() { Name = name });
```

**Added: `GET /astronautduty` endpoint** — returns all duties across all persons with person names joined. The original controller had no listing endpoint.

**Added: `PUT /astronautduty/{id}` endpoint** — updates an existing duty record and syncs `AstronautDetail` when the duty is the current one.

---

### `api/Controllers/PersonController.cs`

**Added: `PUT /person/{name}` endpoint** — renames a person. Not present in the original.

---

### `StargateAPI.Tests/Helpers/TestDbContextFactory.cs`

**Fixed: Seed data leaked into every test database**
```csharp
// ADDED — after EnsureCreated() applies HasData() rows, purge them so
// every test starts with an empty, schema-valid database
context.AstronautDuties.RemoveRange(context.AstronautDuties);
context.AstronautDetails.RemoveRange(context.AstronautDetails);
context.AuditLogs.RemoveRange(context.AuditLogs);
context.People.RemoveRange(context.People);
context.SaveChanges();
context.ChangeTracker.Clear();
```
WHY: `EnsureCreated()` applies `HasData()` seed rows directly from the model. After adding 6 astronauts and 16 duties as seed data, every test database started pre-populated, breaking assertions like `Assert.Single(context.People)` and `Assert.Empty(result.People)`.

---

### New Files Added

| File | Purpose |
|---|---|
| `api/Business/Commands/UpdatePerson.cs` | Rename a Person — pre-processor, handler, result |
| `api/Business/Commands/UpdateAstronautDuty.cs` | Correct an existing duty — pre-processor, handler, result |
| `api/Business/Behaviors/LoggingBehavior.cs` | MediatR pipeline behavior — writes every request outcome to `AuditLog` |
| `api/Business/Data/AuditLog.cs` | `AuditLog` entity and EF Core configuration |
| `api/Business/Queries/GetAllAstronautDuties.cs` | Returns all duties across all persons with person name joined |
| `api/Business/Queries/GetAuditLogs.cs` | Paginated audit log retrieval |
| `api/Business/Queries/GetStats.cs` | System-wide counts (people, active, retired, duties) |
| `api/Controllers/AuditLogController.cs` | `GET /auditlog` — exposes the audit log table |
| `api/Controllers/StatsController.cs` | `GET /stats` — exposes system statistics |
| `StargateAPI.Tests/Commands/UpdateAstronautDutyTests.cs` | 7 tests for `UpdateAstronautDuty` |
| `StargateAPI.Tests/Queries/GetAllAstronautDutiesTests.cs` | 5 tests for `GetAllAstronautDuties` |
| `StargateAPI.Tests/Queries/GetAuditLogsTests.cs` | 6 tests for `GetAuditLogs` including pagination validation |
| `StargateAPI.Tests/Queries/GetStatsTests.cs` | 4 tests for `GetStats` |
| `StargateAPI.Tests/Behaviors/LoggingBehaviorTests.cs` | 12 tests for `LoggingBehavior` — success/failure audit, cancellation, serialization errors, audit write failure |
| `StargateAPI.Tests/Controllers/ControllerBaseExtensionsTests.cs` | 3 tests for `GetResponse` status code mapping |
| `StargateAPI.Tests/Controllers/PersonControllerTests.cs` | 17 tests — all CRUD actions, photo upload, delete with cascade, error paths |
| `StargateAPI.Tests/Controllers/AstronautDutyControllerTests.cs` | 14 tests — all CRUD actions, delete with detail rollback, error paths |
| `StargateAPI.Tests/Controllers/AuditLogControllerTests.cs` | 2 tests — success and error paths |
| `StargateAPI.Tests/Controllers/StatsControllerTests.cs` | 2 tests — success and error paths |
| `StargateAPI.Tests/Models/EntityAndDtoPropertyTests.cs` | 6 tests for entity nav properties and DTO property coverage |
| `coverage.runsettings` | Coverlet configuration excluding Migrations and Program.cs from coverage |

---

### `api/Business/Behaviors/LoggingBehavior.cs`

#### Fixed: .NET 10 `MakeReadOnly()` incompatibility

```csharp
// BEFORE (throws TypeInitializationException on .NET 10)
JsonOptions.MakeReadOnly();

// AFTER
JsonOptions.MakeReadOnly(populateMissingResolver: true);
```

WHY: .NET 10 requires a `TypeInfoResolver` to be set before `MakeReadOnly()` can be called. The `populateMissingResolver: true` overload auto-assigns the default resolver, making it forward-compatible.

---

### `ui/src/app/app.ts`

#### Known Gap: Hardcoded credentials (client-side only)

The UI edit-mode gate uses plaintext credentials:

```typescript
if (this.loginUsername() === 'tonyd' && this.loginPassword() === 'GodMode')
```

This is a **client-side convenience guard only** — not real authentication. Anyone who inspects the JS bundle can bypass it. In production this would be replaced with a proper auth flow (OAuth 2.0 / JWT) backed by server-side enforcement. The API itself currently has no auth layer, which is also a gap. A security comment was added in the source to make this tradeoff explicit.

---



**Person**

| Column | Type | Notes |
|--------|------|-------|
| Id | INTEGER PK | Auto-increment |
| Name | TEXT | **Unique index** (Rule 1) |
| PhotoUrl | TEXT | Relative path to photo (nullable) |

**AstronautDetail** — one row per person who has ever had an assignment

| Column | Type | Notes |
|--------|------|-------|
| Id | INTEGER PK | |
| PersonId | INTEGER FK | References Person |
| CurrentRank | TEXT | Updated on every new duty |
| CurrentDutyTitle | TEXT | Updated on every new duty |
| CareerStartDate | DATETIME | Set on first duty assignment |
| CareerEndDate | DATETIME | Null unless retired (Rule 7) |

**AstronautDuty** — one row per assignment

| Column | Type | Notes |
|--------|------|-------|
| Id | INTEGER PK | |
| PersonId | INTEGER FK | References Person |
| Rank | TEXT | |
| DutyTitle | TEXT | `'RETIRED'` triggers retirement logic (Rule 6) |
| DutyStartDate | DATETIME | |
| DutyEndDate | DATETIME | **Null = current duty** (Rule 4) |

**AuditLog** — one row per API operation

| Column | Type | Notes |
|--------|------|-------|
| Id | INTEGER PK | |
| CreatedDate | DATETIME | UTC |
| Message | TEXT | `"{Operation} succeeded\|{request JSON}"` |
| IsException | INTEGER | 0 = success, 1 = failure |
