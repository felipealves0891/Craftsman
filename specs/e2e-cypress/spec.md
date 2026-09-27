# Feature: Cypress End-to-End Test Suite

## Summary

Implement a Cypress end-to-end test suite that validates the main Craftsman user journeys through the real ASP.NET MVC application.

The suite must verify authentication, authorization, navigation, catalog, inventory, orders, production, shipments, finance, user administration, and documentation access using controlled E2E data.

## Context

The application exposes ASP.NET MVC controllers with Razor views and Razor components for the main operational areas. Existing automated tests cover domain, application, view, repository, and integration behavior, but there is no Cypress E2E infrastructure, no `E2E` appsettings file, and no `/health` readiness endpoint.

The reference prompt `prompts/exemplo-de-especificacao-testes-e2e.md` defines the intended E2E architecture and conventions.

## Problem

Critical user workflows are not currently verified from the browser through MVC, authentication, authorization, persistence, and application services. Regressions in routing, forms, anti-forgery integration, role restrictions, UI rendering, or cross-component workflows may pass lower-level tests.

## Goals

- Validate the application through realistic browser workflows.
- Keep tests independent, deterministic, and backed by controlled data.
- Provide stable test selectors for critical UI elements.
- Cover each application component at a behavior level proportional to business risk.
- Support local and CI execution through a repeatable E2E environment.

## Non-Goals

This feature does not include:

- replacing existing unit or integration tests;
- testing every validation combination through the browser;
- mocking the application backend with Cypress intercepts;
- using production data or production credentials;
- testing external providers directly in the E2E suite.

## Actors

### Admin

Can read, write, and administer users.

### Operador

Can read and write operational data, but cannot administer users.

### Consulta

Can read protected application pages, but cannot execute write operations.

### Anonymous User

Can access login and access denied pages, but must not access protected application routes.

## Preconditions

- The app can run with `ASPNETCORE_ENVIRONMENT=E2E`.
- The E2E environment uses an isolated database.
- External integrations are disabled or replaced by deterministic fakes.
- Test users for `Admin`, `Operador`, and `Consulta` can be seeded.

## Functional Requirements

### FR-001 - E2E environment

The system must support an `E2E` environment with isolated configuration, isolated database connection, deterministic identity seed values, disabled cache when needed for determinism, and disabled or fake external integrations.

### FR-002 - Readiness endpoint

The application must expose a readiness endpoint available to E2E startup orchestration.

The endpoint must return HTTP 200 only after the application can serve requests and access the configured database.

### FR-003 - Database reset

The Cypress suite must be able to reset the E2E database to a known state before each test or test group.

### FR-004 - Explicit seed data

Tests must create required data explicitly through E2E setup APIs, database tasks, or fixtures.

### FR-005 - Programmatic authentication

Tests outside the login UI specification must authenticate programmatically using reusable Cypress commands.

### FR-006 - Stable selectors

UI elements exercised by E2E tests must expose stable `data-cy` selectors.

Selectors must not depend on Bootstrap classes, layout classes, translated button text, or icon markup.

### FR-007 - Role coverage

The suite must validate representative read, write, and admin authorization behavior for `Admin`, `Operador`, and `Consulta`.

### FR-008 - Component coverage

The suite must include E2E specifications for authentication, dashboard/navigation, products, inventory, manual orders, orders, import, production, shipments, finance, users, and documentation.

### FR-009 - Failure artifacts

The E2E configuration must preserve useful artifacts for failing tests, including screenshots, videos when enabled, Cypress logs, and application logs in CI.

### FR-010 - Single-command execution

The repository must provide a single command that prepares the E2E environment, starts the application, waits for readiness, runs Cypress, and tears down transient dependencies where applicable.

## Business Rules

### BR-001 - Control state below, validate above

Tests must prepare state through setup helpers and validate behavior through the browser.

### BR-002 - No inter-test dependency

No test may rely on records created by a previous test.

### BR-003 - Backend remains real

Cypress must not systematically replace MVC endpoints or app services with intercepts. Intercepts may observe or synchronize requests.

### BR-004 - Security remains server-side

Authorization and anti-forgery behavior must remain enforced by the application, not only by hidden UI controls.

## Invariants

- E2E tests must never use production databases.
- Each test must be repeatable on a clean machine.
- Tests must not require manual login except login-specific tests.
- Tests must not use arbitrary sleeps such as `cy.wait(5000)`.
- Test data must be recognizable as E2E data.
- External integrations must not make the suite flaky.

## Data

The E2E suite must be able to seed at minimum:

- users by role;
- order sources;
- products;
- raw materials;
- bill of materials items;
- stock movements;
- orders with items and product links;
- production tasks;
- shipments;
- financial settlements or orders eligible for settlement generation.

## User Flow

1. E2E command starts dependencies.
2. Database migrations run against the E2E database.
3. Application starts with `ASPNETCORE_ENVIRONMENT=E2E`.
4. Test orchestration waits for readiness.
5. Cypress resets database state.
6. Cypress seeds scenario-specific data.
7. Cypress authenticates as the required role.
8. Cypress exercises the browser workflow.
9. Cypress validates UI state and relevant persisted effects.

## Scenarios

### Scenario: Protected route redirects anonymous user

Given no user is authenticated
When the user visits a protected application route
Then the system must redirect the user to the login page.

### Scenario: Write action is blocked for read-only user

Given a `Consulta` user is authenticated
When the user attempts a write operation
Then the system must reject the operation according to the existing authorization behavior.

### Scenario: Critical flow succeeds from setup data

Given required products, raw materials, order source, and stock have been seeded
When an `Operador` creates a manual order and sends it to production
Then the order details must show production information
And inventory effects must be reflected in balances or history.

## Authorization

The suite must verify:

- anonymous users cannot access protected pages;
- `Consulta` can access read pages but cannot perform writes;
- `Operador` can perform operational writes but cannot access user administration;
- `Admin` can access all protected areas including user administration.

## Security

Mutation workflows must exercise the real anti-forgery-protected forms through the browser or explicit authenticated requests that respect application security conventions.

E2E setup endpoints, if introduced, must only be available in the `E2E` environment.

## Compatibility

Existing application routes must remain valid. Adding `data-cy` attributes must not change visible UI behavior.

## Edge Cases

- Database reset fails.
- Application starts but database is unavailable.
- Seed creates duplicate business records.
- A test user lacks expected role assignment.
- Read-only user reaches a hidden write URL directly.
- External integration configuration is accidentally enabled.

## Acceptance Criteria

- Cypress is integrated under a dedicated E2E test directory.
- `ASPNETCORE_ENVIRONMENT=E2E` is supported.
- A readiness endpoint is available for orchestration.
- The E2E database can be reset automatically.
- Required users and scenario data can be seeded programmatically.
- Programmatic login helpers exist for `Admin`, `Operador`, and `Consulta`.
- Critical UI elements used by tests have `data-cy` selectors.
- Specs exist for every functional component listed in FR-008.
- The suite can run locally and in CI through one command.
- Failing tests produce diagnosable artifacts.

## Test Considerations

The initial suite should prioritize a small number of complete journeys before expanding detailed coverage. CRUD-like components should cover one successful create/update path, one validation failure, and one authorization assertion when applicable.

## Open Questions

None blocking. The implementation may choose direct database tasks, E2E-only endpoints, or a combination for reset and seed, as long as the behavior in this specification is preserved.
