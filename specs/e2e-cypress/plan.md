# Implementation Plan: Cypress E2E Authentication Slice

## Summary

Introduce the Cypress E2E foundation required by `specs/e2e-cypress/spec.md` and implement the authentication and role coverage defined by `specs/e2e-cypress/authentication.md`.

This plan intentionally limits the first implementation slice to infrastructure plus authentication tests. Component specs for dashboard/navigation, products, inventory, manual orders, orders, import, production, shipments, finance, users, and documentation remain follow-up E2E coverage under the broader suite specification.

## Specification Reference

- `specs/e2e-cypress/spec.md`
- `specs/e2e-cypress/authentication.md`
- `prompts/exemplo-de-especificacao-testes-e2e.md`

## Current Architecture

The application is an ASP.NET MVC/Razor app in `src/App` with domain and persistence infrastructure split into `src/Domains` and `src/Infra`.

Authentication uses ASP.NET Identity with `ApplicationUser`, `IdentityRole<int>`, cookie auth, and these roles:

- `Admin`
- `Operador`
- `Consulta`

Authorization is configured in `src/App/Program.cs`:

- fallback policy requires authenticated users;
- `ApplicationPolicies.Read` allows all three roles;
- `ApplicationPolicies.Write` allows `Admin` and `Operador`;
- `ApplicationPolicies.AdminOnly` allows only `Admin`.

Current E2E gaps:

- no Cypress project exists;
- no `appsettings.E2E.json` exists;
- no E2E database reset/seed mechanism exists;
- no readiness endpoint exists;
- login/access-denied/logout selectors required by the spec are not present yet;
- `IdentitySeeder` seeds roles and one admin user only, so it does not satisfy the E2E role matrix by itself.

## Change Surface

### Direct Impact

- `src/App/Program.cs`
- `src/App/appsettings.E2E.json`
- `src/App/Views/Account/Login.cshtml`
- `src/App/Views/Shared/_Layout.cshtml`
- `src/UI/Components/Account/AccessDeniedPage.razor`
- new E2E-only reset/seed infrastructure under `src/App`
- new Cypress project under `tests/e2e`

### Indirect Impact

- Identity tables and application tables in the E2E database.
- Existing authorization attributes on MVC controllers.
- CI/local execution scripts.

## Implementation Strategy

Build the minimal E2E harness first, then add authentication tests against the real running app.

The app will support an `E2E` environment that applies migrations/seeding similarly to `Development`, but with isolated configuration and deterministic E2E users. E2E-only endpoints will provide database reset and seed capabilities because Cypress can call them consistently without duplicating EF schema knowledge in Node code.

The Cypress project will live under `tests/e2e`, separated from the ASP.NET projects. Tests will use the login UI only for login-specific scenarios. Reusable commands will use programmatic login for role setup in authorization/logout scenarios when the test objective is not the login form itself.

## Data Flow

```text
npm run test:e2e
    -> start E2E database dependency
    -> start ASP.NET app with ASPNETCORE_ENVIRONMENT=E2E
    -> wait for /health
    -> Cypress beforeEach calls E2E reset/seed helpers
    -> Cypress drives browser against MVC routes
    -> assertions verify UI redirects, access denied, and protected content
```

## Existing Components to Reuse

- ASP.NET Identity `UserManager`, `RoleManager`, and `SignInManager`.
- Existing `ApplicationRoles` and `ApplicationPolicies`.
- Existing MVC routes:
  - `/Account/Login`
  - `/Account/Logout`
  - `/Account/AccessDenied`
  - `/`
  - `/Orders`
  - `/Users`
- Existing anti-forgery behavior on login/logout forms.
- Existing PostgreSQL/EF Core persistence and migrations.

## New Components

### E2E endpoints

Add endpoints available only when `app.Environment.IsEnvironment("E2E")`:

- reset database state;
- seed deterministic users and roles;
- optionally return seeded credential metadata for Cypress.

These endpoints are needed so Cypress can control state below the browser while validating behavior through the real MVC UI.

### Readiness endpoint

Add `/health` to return `200` only when the application can resolve `AppDbContext` and connect to the configured database.

### Cypress support commands

Add reusable commands for:

- selecting `data-cy` attributes;
- resetting and seeding E2E state;
- logging in as `Admin`, `Operador`, or `Consulta`.

### E2E orchestration scripts

Add `package.json` scripts in `tests/e2e` for Cypress execution and a root-level single command if needed for the repository acceptance criteria.

## Files to Modify

### `src/App/Program.cs`

- Treat `E2E` as an environment that applies migrations and controlled seed setup.
- Map `/health`.
- Map E2E-only setup endpoints only in the `E2E` environment.

### `src/App/Views/Account/Login.cshtml`

- Add required `data-cy` selectors for email, password, remember-me, return URL, and submit controls.

### `src/App/Views/Shared/_Layout.cshtml`

- Add `data-cy="logout-submit"` to the logout submit button.

### `src/UI/Components/Account/AccessDeniedPage.razor`

- Add `data-cy="access-denied-message"` to the existing access denied message.

### `src/App/appsettings.E2E.json`

- Add E2E-specific configuration with disabled external integrations, deterministic identity configuration, and connection string placeholder appropriate for environment override.

## Files to Create

### `src/App/E2E/E2EEndpoints.cs`

Maps E2E-only reset/seed endpoints.

### `src/App/E2E/E2EDatabaseResetter.cs`

Resets application and Identity tables to a known state for the E2E database.

### `src/App/E2E/E2EIdentitySeeder.cs`

Creates `Admin`, `Operador`, and `Consulta` users with deterministic credentials.

### `tests/e2e/package.json`

Defines Cypress dependencies and scripts.

### `tests/e2e/cypress.config.js`

Configures Cypress base URL, artifacts, env values, and node tasks if needed.

### `tests/e2e/cypress/support/e2e.js`

Loads shared Cypress support code.

### `tests/e2e/cypress/support/commands.js`

Defines `cy.getByCy`, `cy.resetE2E`, `cy.seedE2EUsers`, and role login commands.

### `tests/e2e/cypress/e2e/authentication.cy.js`

Implements login, invalid login, logout, anonymous redirect, and role authorization scenarios.

### E2E orchestration script

Create a small script if package scripts alone cannot reliably start the app, wait for health, run Cypress, and shut down the app on Windows.

## Files to Remove

None.

## Persistence Changes

No schema changes are expected.

The E2E reset mechanism will delete or truncate data in the isolated E2E database, then run the deterministic E2E seed. It must not run against non-E2E environments.

## API Contract Changes

Production API/UI contracts are unchanged.

New E2E-only endpoints are internal test contracts and must be unavailable outside `ASPNETCORE_ENVIRONMENT=E2E`.

## UI Changes

Only add `data-cy` attributes required by the spec:

- login email;
- login password;
- login remember-me;
- login return URL;
- login submit;
- logout submit;
- access denied message.

No visible UI behavior should change.

## Configuration Changes

Add `appsettings.E2E.json` with:

- isolated `CraftsmanDb` connection string supplied by environment or safe local default;
- external integrations disabled;
- cache disabled if needed for deterministic tests;
- E2E identity seed values.

## Dependencies

Add Cypress under `tests/e2e`.

If orchestration needs it, add small Node dev dependencies such as a readiness waiter/process runner. Prefer simple scripts and avoid broad tooling unless it removes real startup fragility.

No new NuGet dependency is required unless database reset cannot be implemented safely with existing EF Core/PostgreSQL capabilities.

## Security Considerations

- E2E setup endpoints must be mapped only in `E2E`.
- Reset logic must refuse to run unless the hosting environment is `E2E`.
- Test credentials must be deterministic but clearly non-production.
- Authentication tests must exercise real anti-forgery-protected login/logout forms.
- Authorization assertions must hit server routes directly, not only rely on hidden navigation.

## Performance Considerations

Database reset should be fast enough for `beforeEach` use in the initial authentication suite.

If full database reset becomes slow when later component specs are added, the reset implementation can evolve to snapshot/Respawn-style reset without changing test behavior.

## Observability

Cypress should keep screenshots on failure and support videos when enabled.

The E2E command should preserve app console logs in CI or make them visible in command output.

## Testing Strategy

### Cypress Authentication Tests

Cover:

- valid admin login through UI reaches dashboard;
- invalid credentials remain on login and show the existing invalid login error;
- anonymous visit to `/Orders` redirects to login;
- admin return URL flow from `/Users` lands on users page after login;
- logout ends the session and protected route requires login again;
- `Consulta` cannot access `/Users`;
- `Consulta` cannot execute a representative write operation;
- `Operador` can authenticate and cannot access `/Users`.

### Existing .NET Tests

Run existing .NET tests after implementation to catch regressions in controllers, authorization attributes, and rendering.

### Build Validation

Run the app build and the E2E command after implementation.

## Implementation Tasks

### Task 1 - Add E2E application environment

Affected:

- `src/App/Program.cs`
- `src/App/appsettings.E2E.json`

Changes:

- load E2E configuration through standard ASP.NET configuration;
- apply migrations in `E2E`;
- disable/fake external integrations via config;
- keep production behavior unchanged.

Validation:

- app starts with `ASPNETCORE_ENVIRONMENT=E2E`;
- migrations run against the E2E database.

Dependencies:

None.

### Task 2 - Add readiness endpoint

Affected:

- `src/App/Program.cs`

Changes:

- expose `/health`;
- verify database connectivity before returning success.

Validation:

- `/health` returns `200` when app and DB are ready;
- startup orchestration can wait on it.

Dependencies:

Task 1.

### Task 3 - Add E2E-only reset and seed endpoints

Affected:

- new `src/App/E2E/*` files;
- `src/App/Program.cs`.

Changes:

- reset isolated database state;
- seed `Admin`, `Operador`, and `Consulta` users;
- guard all endpoints to `E2E` only.

Validation:

- reset produces a known database state;
- seeded users can authenticate with expected roles;
- endpoints are not mapped outside `E2E`.

Dependencies:

Task 1.

### Task 4 - Add required authentication selectors

Affected:

- `Login.cshtml`;
- `_Layout.cshtml`;
- `AccessDeniedPage.razor`.

Changes:

- add stable `data-cy` attributes required by `authentication.md`;
- preserve current visible UI.

Validation:

- rendered login, logout, and access denied elements can be selected by Cypress.

Dependencies:

None.

### Task 5 - Add Cypress project

Affected:

- `tests/e2e`.

Changes:

- add package/config/support structure;
- configure base URL and artifacts;
- add selector and role login commands;
- add reset/seed helpers.

Validation:

- Cypress can open/run against a running E2E app;
- commands authenticate roles deterministically.

Dependencies:

Tasks 2 and 3 for full execution.

### Task 6 - Implement authentication spec

Affected:

- `tests/e2e/cypress/e2e/authentication.cy.js`.

Changes:

- implement scenarios from `authentication.md`;
- avoid arbitrary waits;
- use real UI for login-specific tests;
- use direct protected routes for authorization checks.

Validation:

- authentication Cypress spec passes repeatedly on a reset E2E database.

Dependencies:

Tasks 3, 4, and 5.

### Task 7 - Add single-command E2E execution

Affected:

- root or `tests/e2e` package scripts;
- optional orchestration script.

Changes:

- prepare/start dependencies;
- start ASP.NET app with `ASPNETCORE_ENVIRONMENT=E2E`;
- wait for `/health`;
- run Cypress;
- stop transient processes.

Validation:

- one command runs the authentication E2E slice locally.

Dependencies:

Tasks 1 through 6.

### Task 8 - Validate implementation

Affected:

- repository build and tests.

Changes:

- run `dotnet build`;
- run relevant `dotnet test`;
- run E2E command.

Validation:

- build succeeds;
- existing test suite remains green or deviations are documented;
- Cypress authentication suite passes.

Dependencies:

Tasks 1 through 7.

## Requirement Traceability

| Requirement | Implementation |
|---|---|
| `spec.md` FR-001 | Tasks 1 and 7 |
| `spec.md` FR-002 | Task 2 |
| `spec.md` FR-003 | Task 3 |
| `spec.md` FR-004 | Task 3 |
| `spec.md` FR-005 | Task 5 |
| `spec.md` FR-006 | Task 4 |
| `spec.md` FR-007 | Tasks 3 and 6 |
| `spec.md` FR-009 | Tasks 5 and 7 |
| `spec.md` FR-010 | Task 7 |
| `authentication.md` FR-001 | Task 4 |
| `authentication.md` FR-002 | Task 6 |
| `authentication.md` FR-003 | Task 6 |
| `authentication.md` FR-004 | Task 6 |
| `authentication.md` FR-005 | Task 6 |
| `authentication.md` FR-006 | Task 6 |
| `authentication.md` FR-007 | Task 3 |
| `authentication.md` FR-008 | Task 5 |
| `authentication.md` FR-009 | Task 6 |

## Backward Compatibility

Existing application routes remain valid.

The visible login, logout, and access denied UI should not change except for invisible `data-cy` attributes.

E2E-only endpoints do not affect production because they are mapped only in the `E2E` environment.

## Migration Strategy

No database migration is expected.

The E2E database will use existing EF Core migrations during startup.

## Risks

### Reset safety

Database reset is destructive by design.

Mitigation:

- require `E2E` environment checks in endpoint mapping and reset service;
- use an isolated connection string;
- use recognizable E2E database naming.

### Anti-forgery programmatic login

Programmatic login via POST must respect anti-forgery if it uses MVC form endpoints.

Mitigation:

- for non-login specs, either use an E2E-only authenticated session helper or fetch and submit the real login form token;
- keep login UI coverage through the browser.

### Startup orchestration on Windows

Process startup/teardown can be fragile in npm scripts.

Mitigation:

- prefer a small explicit orchestration script if simple package scripts become hard to maintain.

## Assumptions

- PostgreSQL is available for local E2E execution, either through existing Docker infrastructure or a new E2E compose/service.
- The first implementation slice is authentication plus E2E foundation, not the full component suite.
- Adding `data-cy` attributes is acceptable when it does not alter visible UI.

## Open Technical Questions

- Should the first implementation add a separate `docker-compose.e2e.yml`, or reuse the existing PostgreSQL service with a separate `craftsman_e2e` database?
- Should programmatic login use E2E-only session endpoints or the real login form token flow?
- Should the single command live at the repository root or under `tests/e2e` only?
