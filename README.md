# FleetDesk – Vessel Job Scheduler

Full-stack app for scheduling maintenance/inspection jobs on vessels and assigning crew, with
double-booking protection.

**Stack:** C#, .NET 8, ASP.NET Core Web API, Entity Framework Core, PostgreSQL, JWT auth with
role-based access, Angular 18 + TypeScript, xUnit, Docker Compose.

## Features
- JWT login/registration (BCrypt password hashing); roles: `Viewer`, `Dispatcher`, `Admin`
- Vessels, crew members and jobs (CRUD with role checks)
- Kanban-style job board (Planned / In progress / Completed / Cancelled) with search and vessel filter
- Crew assignment with **conflict detection**: a crew member cannot be assigned to overlapping
  active jobs (HTTP 409 with a clear message)
- Swagger UI at `/swagger`, health check at `/health`

## Run everything with Docker
```bash
docker compose up --build
```
- UI: http://localhost:4200
- API / Swagger: http://localhost:5080/swagger
- Seeded admin: `admin@fleetdesk.local` / `ChangeMe123!` (override with `ADMIN_EMAIL`, `ADMIN_PASSWORD`, `JWT_KEY`)

New accounts registered through the UI are `Viewer` (read-only). Sign in as the admin to create jobs.

## Run locally (without Docker for the apps)
```bash
# 1. Database
docker run -d --name fleetdesk-db -p 5432:5432 \
  -e POSTGRES_DB=fleetdesk -e POSTGRES_USER=fleetdesk -e POSTGRES_PASSWORD=fleetdesk postgres:16-alpine

# 2. API (http://localhost:5080)
cd api/FleetDesk.Api
dotnet run --urls http://localhost:5080

# 3. UI (http://localhost:4200, proxies /api to :5080)
cd web
npm install
npm start
```

## Tests
```bash
cd api/FleetDesk.Tests
dotnet test
```
Covers the scheduling conflict logic (overlap rules, cancelled/completed jobs, edit-in-place) and
JWT token creation.

## API overview
| Method | Route | Role |
|---|---|---|
| POST | `/api/auth/register`, `/api/auth/login` | public |
| GET | `/api/vessels`, `/api/crew`, `/api/jobs` | any signed-in user |
| POST | `/api/vessels`, `/api/crew`, `/api/jobs` | Dispatcher, Admin |
| PUT / PATCH | `/api/jobs/{id}`, `/api/jobs/{id}/status` | Dispatcher, Admin |
| POST / DELETE | `/api/jobs/{id}/assignments[/{crewId}]` | Dispatcher, Admin |
| DELETE | `/api/jobs/{id}`, `/api/vessels/{id}` | Admin |

## Notes
- The schema is created with `EnsureCreated()` on startup for simplicity. For production, switch to
  EF Core migrations (`dotnet ef migrations add Initial`).
- Change `Jwt:Key` and the seeded admin password before deploying anywhere real.
