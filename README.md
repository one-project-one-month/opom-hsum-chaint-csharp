# HsumChaint_CSharp

## 1) Architecture overview (Phase 1)

This refactor keeps all public API contracts (routes, DTOs, and HTTP response behavior) intact while moving internal code to a feature-oriented structure.

### Current solution topology
- `HsumChaint.API`
- `HsumChaint.Domain`
- `HsumChaint.Database`
- `HsumChaint.Shared`

### Feature slices (non-breaking)
- `HsumChaint.API/Features/{Auth,User,Notification}/...`
- `HsumChaint.Domain/Features/{Auth,User,Notification}/...`
- `HsumChaint.Database/Models/...`

### Cross-layer boundaries
- API handles HTTP entry only and delegates to Domain services.
- Domain handles feature orchestration, validation, auth/token flow, notification sending, and DTO-entity mapping.
- Database contains EF Core database-first `AppDbContext` and table models only.
- Shared stores cross-project configuration contracts and enums.

### Startup/Composition refactor
- `Program.cs` now focuses on host bootstrap + middleware + endpoint mapping.
- All registration is split into extension methods:
  - `HsumChaint.API/Extensions/DependencyInjectionExtensions.cs`
    - `AddApiServices`
    - `AddFirebaseConfiguration`
    - `AddJwtAuthentication`
  - `HsumChaint.Domain/Extensions/DependencyInjectionExtensions.cs`
  - `HsumChaint.Database/Extensions/DependencyInjectionExtensions.cs`
- Stage-aware config loading:
  - `HsumChaint.API/Extensions/StageConfigurationExtensions.cs`
  - `HsumChaint.API/Config/appsettings.json`
  - `HsumChaint.API/Config/custom-settings-{stage}.json` (for now: `custom-settings-development.json`)
  - `Program` calls `builder.AddStageConfig()` immediately after creation.

## 2) Feature flow diagram

```mermaid
flowchart LR
  Request["HTTP Request"] --> Controllers["API Feature Controller<br/>api/v1/Auth, api/User, api/Notifications"]
  Controllers --> Service["Domain Feature Service<br/>IAuthService / IUserService / INotificationService"]
  Service --> Db[(Database<br/>EF Core AppDbContext)]
  Db --> Models["Database-first table models"]
  Service --> Provider["Domain Notification Provider"]
  Service --> Mapper["AutoMapper Profiles"]
  Service --> Response["ApplicationCommonResponseModel / DTOs"]
  Response --> Controllers --> Result["HTTP Response"]
```

## 3) Migration / onboarding note

### What moved
- API controllers moved from `HsumChaint.API/Controllers/*` into:
  - `HsumChaint.API/Features/Auth/Controllers`
  - `HsumChaint.API/Features/User/Controllers`
  - `HsumChaint.API/Features/Notification/Controllers`
- Application service logic and DTOs moved under feature folders in `HsumChaint.Domain/Features/...`.
- Repository logic was folded into Domain services so feature services use `AppDbContext` directly.
- EF Core database-first context and table models moved to `HsumChaint.Database/Models/...`.
- Shared enums/options moved from `HsumChaint.Common` to `HsumChaint.Shared`.

### Where to add new features
- Add each feature under:
  - `HsumChaint.API/Features/<Feature>/Controllers`
  - `HsumChaint.Domain/Features/<Feature>/{DTOs,ServiceInterfaces,Services}`
  - `HsumChaint.Database/Models` only when EF database-first scaffolding adds or refreshes table models.
- Register new services and database wiring through extension methods in:
  - `HsumChaint.API/Extensions/DependencyInjectionExtensions.cs`
  - `HsumChaint.Domain/Extensions/DependencyInjectionExtensions.cs`
  - `HsumChaint.Database/Extensions/DependencyInjectionExtensions.cs`

### Shared config placement
- Default/shared values: `HsumChaint.API/Config/appsettings.json`
- Stage-specific override values: `HsumChaint.API/Config/custom-settings-<stage>.json`
- Environment values continue to work through standard .NET configuration providers.

## 4) Development commands
- Build: `dotnet build HsumChaint.slnx`
- Tests: `dotnet test HsumChaint.Tests/HsumChaint.Tests.csproj`
- Run API: `dotnet run --project HsumChaint.API`

## 5) Legacy notes
- `dotnet ef dbcontext scaffold ...` command kept below for database scaffolding tasks.
- `docker compose up --build`
