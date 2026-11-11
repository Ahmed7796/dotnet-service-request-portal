# ServiceDesk API

An internal service-request API: employees submit requests, which then pass through a multi-step approval workflow scoped to their department.

[![CI](https://github.com/Ahmed7796/dotnet-service-request-portal/actions/workflows/ci.yml/badge.svg)](https://github.com/Ahmed7796/dotnet-service-request-portal/actions/workflows/ci.yml)

## What it does

- Employees submit **Equipment**, **Software access**, **Leave** and **Purchase** requests.
- Each request type follows its own ordered approval chain (for example, Department Head → IT Manager), and a step can be limited to the requester's department.
- The domain enforces who may act on each step: approvers can't approve their own requests, and a step is escalated to the General Manager when the requester holds that step's position.
- Approvers approve or reject, requesters cancel, and Admins fulfil approved requests. Every transition is written to an audit trail.
- JWT login, ProblemDetails errors (400/401/403/404/409), a `/health` endpoint, and Swagger UI.

## Quick start

**You only need Docker.** The SQL Server database, the migrations and the demo data all start with the stack.

```bash
git clone https://github.com/Ahmed7796/dotnet-service-request-portal.git
cd dotnet-service-request-portal

# 1. Create your .env from the template
cp .env.example .env

# 2. Generate a JWT signing key and write it into .env
#    (or run `openssl rand -base64 32` and paste the result after JWT_SECRET_KEY=)
sed -i.bak "s|^JWT_SECRET_KEY=.*|JWT_SECRET_KEY=$(openssl rand -base64 32)|" .env

# 3. Build and start the API and SQL Server
docker compose up --build
```

When the log shows `Now listening on: http://[::]:8080`:

- Swagger UI: **http://localhost:8080/swagger**
- Health: **http://localhost:8080/health** (returns `Healthy`)

The API runs in the `Development` environment inside Compose, so on startup it applies the EF Core migrations and seeds the demo data. `docker compose down -v` resets everything.

## Demo accounts

Every account uses the password **`ServiceDesk!2026`**. This is dev-only seed data, inserted at startup when the database is empty.

| Email | Role | Position | Department |
|---|---|---|---|
| requester@servicedesk.local | Requester | — | Operations |
| ops.head@servicedesk.local | Approver | DepartmentHead | Operations |
| finance.head@servicedesk.local | Approver | DepartmentHead | Finance |
| it.manager@servicedesk.local | Approver | ItManager | IT |
| hr.manager@servicedesk.local | Approver | HrManager | Human Resources |
| finance.manager@servicedesk.local | Approver | FinanceManager | Finance |
| gm@servicedesk.local | Approver | GeneralManager | Operations |
| admin@servicedesk.local | Admin | — | IT |

## Try it in 2 minutes

In Swagger UI (http://localhost:8080/swagger):

1. **Log in as the requester.** Use `POST /api/auth/login` with:
   ```json
   { "identifier": "requester@servicedesk.local", "password": "ServiceDesk!2026" }
   ```
   Copy `accessToken` from the response, click **Authorize** (top right), and paste it in. Paste the token only; Swagger adds `Bearer` for you.
2. **Submit an Equipment request.** Use `POST /api/service-requests` with:
   ```json
   { "type": "Equipment", "title": "New laptop", "description": "Current one is 6 years old" }
   ```
   You get **201 Created**, status `Submitted`, with two pending steps. Copy the `id`.
3. **See a deliberate 403.** While still logged in as the requester, call `POST /api/service-requests/{id}/approve` with `{}`. The response is **403** *"You cannot approve your own request."*
4. **Approve as the Ops Head.** Click **Authorize** → **Logout**, log in as `ops.head@servicedesk.local`, authorize with the new token, and approve. The response is **200**, status `InReview`.
5. **Approve as the IT Manager.** Repeat step 4 with `it.manager@servicedesk.local`. The response is **200**, status `Approved`.
6. **Fulfil as the Admin.** Repeat with `admin@servicedesk.local`, then call `POST /api/service-requests/{id}/fulfil`. The response is **200**, status `Fulfilled`.

Optional: approving the request again now returns **409**, and calling `fulfil` as anyone except an Admin returns **403**.

## Architecture

```
            ┌──────────────┐
            │     Api      │  controllers, JWT, ProblemDetails, Swagger, health
            └──┬────────┬──┘
               │        │
               ▼        ▼
┌──────────────┐    ┌────────────────┐
│ Application  │◄───│ Infrastructure │  EF Core, SQL Server, repositories, seeding
└──────┬───────┘    └────────────────┘
       │
       ▼
┌──────────────┐
│    Domain    │  entities, state machine, approval rules (no dependencies)
└──────────────┘
```

Arrows point toward the dependency, and Domain depends on nothing.

| Layer | What lives there |
|---|---|
| `ServiceDesk.Domain` | `ServiceRequest` aggregate (`Submit`, `Approve`, `Reject`, `Cancel`, `Fulfil`), `ApprovalStep`, `AuditLog`, `User`, `WorkflowTemplate`, `RequestStateMachine`, `EscalationPolicy`, domain exceptions |
| `ServiceDesk.Application` | `ServiceRequestService` use cases, DTOs with validation attributes, and the repository and unit-of-work interfaces |
| `ServiceDesk.Infrastructure` | `AppDbContext`, Fluent API configurations, migrations, repositories, `UnitOfWork`, `DbSeeder` |
| `ServiceDesk.Api` | `ServiceRequestsController`, `AuthController`, `JwtTokenService`, `DomainExceptionHandler`, `Program.cs` |

## Approval rules

**Workflows** (copied onto each request when it's submitted):

| Request type | Step 1 | Step 2 |
|---|---|---|
| Equipment | Department Head *(requester's department)* | IT Manager *(global)* |
| SoftwareAccess | IT Manager *(global)* | — |
| Leave | HR Manager *(global)* | — |
| Purchase | Department Head *(requester's department)* | Finance Manager *(global)* |

- **Scope:** a *requester's department* step can only be decided by someone in that position from the requester's department. A *global* step can be decided by anyone in that position.
- **Segregation of duties:** nobody can approve or reject their own request.
- **Escalation:** if the requester holds a step's position (for example, the Ops Head submits an Equipment request), that step goes to the **General Manager** with global scope. This happens at submission and is recorded in the audit trail.
- **Admin fallback:** when the requester holds a step's position and there's nobody to escalate to (the General Manager's own GM step), an **Admin** may decide that step. The audit entry says *"decided by Admin on behalf of GeneralManager"*. The seeded workflows have no GM step, so unit tests cover this path rather than the demo data.
- **State machine:**
  ```
  Submitted ─approve─► InReview ─approve final─► Approved ─fulfil─► Fulfilled
      │                   │
      └──reject/cancel────┴──► Rejected / Cancelled   (remaining steps → Skipped)
  ```
  A single-step workflow goes from `Submitted` straight to `Approved`. Only the original requester can cancel, and only an Admin can fulfil.

## Design decisions

- **Rich domain model:** the state and permission rules live in `ServiceRequest`. Controllers and services only load, call and save. Setters are private, and collections are exposed read-only over backing fields.
- **State machine as a transition table:** `RequestStateMachine` is a dictionary of `(status, trigger) → status`. Every legal move is listed in one place, and anything else throws `InvalidStateTransitionException` (409).
- **Workflow copied at submit:** a request copies its template's steps when it's created, so editing a template later never changes requests already in flight. The requester's department and position are copied too.
- **Audit in the same transaction:** each domain method appends its `AuditLog` entry, and both are saved by a single `SaveChangesAsync`, so a state change and its audit record can't diverge.
- **Optimistic concurrency:** `ServiceRequests.RowVersion` is a SQL Server `rowversion`. A lost update raises `DbUpdateConcurrencyException`, which becomes `ConcurrencyConflictException`, returned as **409**.
- **Guid v7 ids, assigned in code:** ids are time-ordered, which is friendly to clustered indexes, and they exist before the first save. EF is told `ValueGeneratedNever()` so that new child rows are inserted, not updated.
- **Error mapping:** `DomainExceptionHandler` turns exceptions into RFC 9457 ProblemDetails:

  | Status | Cause |
  |---|---|
  | 400 | validation or bad input |
  | 401 | missing or invalid token |
  | 403 | the domain or a role check refused the action |
  | 404 | not found |
  | 409 | invalid transition or concurrency conflict |

  Anything else is a generic 500 with no internals exposed.
- **Who you are vs. what you may do:** the JWT only says who the caller is (`sub`, `role`). Whether they may act on a given step is decided by the domain, which knows positions, departments and requesters. The `[Authorize(Roles = "Admin")]` check on fulfil is a coarse first gate.

## Known limitations / trade-offs

- **Senior requesters on department steps:** a senior requester (such as the General Manager) isn't escalated on a department-scoped step they don't hold. A GM's Equipment request is approved first by their own department's Department Head.
- **Startup migrations are dev-only:** migrations and seeding run at startup only in `Development`. Production would run migrations as a deployment step and never seed demo users.
- **One position per approver:** each approver holds exactly one position. Acting roles and delegation aren't modelled.

## Next steps

- An outbound webhook on status changes, using a `BackgroundService` + `Channel<T>` (a transactional outbox in production).
- Rate limiting on `/api/auth/login`.
- Integration tests with `WebApplicationFactory` + Testcontainers (SQL Server).
- An authority ranking, so that senior requesters escalate past junior approvers.
- Refresh tokens.

## Java / Spring Boot → .NET mapping

This is a .NET rebuild of the workflow core of a Spring Boot portal I built.

| Spring Boot | This project (.NET) |
|---|---|
| `@RestController` | `[ApiController]` + `ControllerBase` |
| Spring DI (`@Service`, constructor injection) | `IServiceCollection` (`AddScoped`/`AddSingleton`) + primary constructors |
| JPA / Hibernate repositories | EF Core `DbContext` + repository classes |
| `@Version` | `rowversion` via `IsRowVersion()` |
| Flyway / Liquibase | EF Core migrations (`dotnet ef migrations add`) |
| `@ControllerAdvice` / `@ExceptionHandler` | `IExceptionHandler` + ProblemDetails |
| Bean Validation (`@NotBlank`, `@Size`) | DataAnnotations (`[Required]`, `[MaxLength]`), automatic 400 |
| Spring Security + JWT resource server | `AddJwtBearer` + `[Authorize]` / `[Authorize(Roles = "Admin")]` |
| `application-{profile}.yml` | `appsettings.{Environment}.json` + environment variables |
| Actuator `/health` | `AddHealthChecks().AddDbContextCheck<AppDbContext>()` → `/health` |
| JUnit + Mockito | xUnit + NSubstitute |
| Maven / Gradle | `dotnet` CLI + NuGet |

## Tech stack

.NET 10 · ASP.NET Core · EF Core 10 · SQL Server 2022 · JWT Bearer · Serilog · OpenAPI + Swagger UI · xUnit · NSubstitute · Docker Compose · GitHub Actions

## Running tests

```bash
dotnet test
```

This needs the .NET 10 SDK. The tests are unit tests over the domain and application layers and don't need a database.
