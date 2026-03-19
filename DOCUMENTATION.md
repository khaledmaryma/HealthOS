## Laboratory Information System (LIS) — Project Documentation

This document records setup steps, modules, and operational notes for the LIS solution.

### Overview
- **Solution**: `LIS.sln`
- **Backend**: `LIS.Api` (.NET 9, ASP.NET Core, EF Core)
- **Frontend**: `LIS.Web` (Angular)

### Technology Stack
- **Server**: ASP.NET Core 9
- **ORM**: Entity Framework Core
- **Database**: Configured via EF Core Migrations
- **Client**: Angular

### Project Structure
- `LIS.Api/`
  - `Program.cs` — application entry and DI setup
  - `Data/LISDbContext.cs` — EF Core DbContext
  - `Models/Patient.cs` — domain model(s)
  - `Migrations/` — EF Core migrations
  - `appsettings.json`, `appsettings.Development.json` — configuration
  - `Properties/launchSettings.json` — profiles
- `LIS.Web/`
  - `src/` — Angular app source
  - `angular.json`, `tsconfig*.json` — Angular/TS config
  - `package.json` — dependencies and scripts

---

## Setup and Run

### Prerequisites
- .NET 9 SDK
- Node.js (LTS) and npm
- Angular CLI (globally) — optional for local dev: `npm i -g @angular/cli`
- A SQL database engine supported by EF Core (e.g., SQL Server, SQLite). Connection string configured in `LIS.Api/appsettings*.json`.

### Backend (LIS.Api)
1. Restore dependencies:
   - `dotnet restore`
2. Apply database migrations (create/update DB):
   - `dotnet ef database update` (from `LIS.Api/`)
3. Run the API:
   - `dotnet run --project LIS.Api`
4. Configuration:
   - Edit `LIS.Api/appsettings.Development.json` for local DB connection and logging.

### Frontend (LIS.Web)
1. Install dependencies:
   - From `LIS.Web/`: `npm install`
2. Start the dev server:
   - From `LIS.Web/`: `npm start` or `npm run start`
3. Open the app:
   - Navigate to `http://localhost:4200` (default Angular dev server)

---

## Modules and Components

### Backend Modules
- **Data Access**
  - `Data/LISDbContext.cs`: Database context configuration
  - `Migrations/`: Schema evolution and versioning
- **Domain Models**
  - `Models/Patient.cs`: Patient entity
- **API Surface**
  - Endpoints/controllers will appear under `LIS.Api` root (e.g., `Controllers/`).

#### Entity Framework Migrations
- Initial migration present:
  - `20251007073326_InitialCreate` (see `Migrations/`)
- Commands:
  - Add: `dotnet ef migrations add <Name> --project LIS.Api`
  - Update DB: `dotnet ef database update --project LIS.Api`
  - Remove last: `dotnet ef migrations remove --project LIS.Api`

### Frontend Modules
- **App Shell**
  - `src/app/app.ts`, `app.html`, `app.scss`: root component
  - `app.routes.ts`: routing configuration
  - `app.config.ts`: application providers/config
- **Global Assets**
  - `src/styles.scss`, `public/`

---

## Configuration

### Backend Configuration Files
- `appsettings.json` (base)
- `appsettings.Development.json` (local overrides)

Typical keys:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "<your-connection-string>"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Frontend Environment
- Angular uses the default environment provided by the build system (no explicit `environment.ts` files in snapshot). Add if needed for API base URLs.

---

## Build, Test, and Run

### Backend
- Build: `dotnet build LIS.Api`
- Run: `dotnet run --project LIS.Api`
- Swagger/OpenAPI: If enabled by templates, accessible at `/swagger` when running in Development.

### Frontend
- Dev: `npm start` (in `LIS.Web/`)
- Build: `npm run build`
- Unit tests: `npm test`

---

## API Endpoints (to be expanded)
- List endpoints here as they are added. Example format:
  - `GET /api/patients` — list patients
  - `POST /api/patients` — create patient
  - `GET /api/patients/{id}` — get patient by id

---

## Development Workflow
1. Create/modify models in `LIS.Api/Models/`.
2. Update `LISDbContext` and add EF migration.
3. Apply migration to local database.
4. Implement controllers/endpoints.
5. Consume endpoints from Angular services/components.
6. Update this document with new modules/steps.

---

## Troubleshooting
- EF tools not found: install with `dotnet tool install --global dotnet-ef`.
- Connection issues: verify connection string in `appsettings.Development.json`.
- Angular port conflicts: run `npm start -- --port=4300`.

---

## Change Log
- 2025-10-07: Created initial documentation scaffold.



