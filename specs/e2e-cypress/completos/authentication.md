# Feature: E2E Authentication

## Summary

Validate login, logout, unauthorized access, and role-based access behavior through the browser.

This specification is the first executable slice of the broader Cypress E2E suite described in `specs/e2e-cypress/spec.md`.

## Context

`AccountController` exposes anonymous login, logout, and access denied endpoints. The application uses ASP.NET Identity, cookie authentication, fallback authentication requirements, and role policies for read, write, and admin behavior.

## Goals

- Verify a valid user can sign in and reach the dashboard.
- Verify invalid credentials remain on the login page with an error.
- Verify logout ends the session.
- Verify protected routes redirect anonymous users to login.
- Verify role restrictions are enforced.

## Non-Goals

This feature does not include password reset, lockout behavior, registration, or external identity providers.

It also does not require every role-protected route to be exhaustively tested in the authentication spec. Broader component coverage belongs to the component-specific E2E specifications.

## Actors

### Admin

Can authenticate, access the dashboard, access `/Users`, and perform write operations.

### Operador

Can authenticate and access read/write operational routes, but must not access `/Users`.

### Consulta

Can authenticate and access read routes, but must not perform write operations and must not access `/Users`.

### Anonymous User

Can access `/Account/Login` and `/Account/AccessDenied`, but must be redirected away from protected application routes.

## Preconditions

- The application is running with `ASPNETCORE_ENVIRONMENT=E2E`.
- The E2E database has been migrated and reset to a known state.
- E2E users exist for `Admin`, `Operador`, and `Consulta`.
- E2E user credentials are deterministic and must not be production credentials.

## Functional Requirements

### FR-001 - Login page

The login page must render email, password, remember-me, return URL, and submit controls with stable E2E selectors.

### FR-002 - Successful login

When a user submits valid credentials, the system must authenticate the user and redirect to the local return URL or dashboard.

### FR-003 - Invalid login

When a user submits invalid credentials, the system must not authenticate the user and must show the existing invalid-login error.

### FR-004 - Logout

When an authenticated user logs out, the system must end the session and redirect to login.

### FR-005 - Anonymous access

When an anonymous user visits protected routes, the system must redirect to login.

### FR-006 - Access denied

When an authenticated user visits a route outside their role, the system must show the existing access denied behavior.

### FR-007 - Authentication seed data

The E2E setup must seed deterministic users for `Admin`, `Operador`, and `Consulta` with their expected role assignments.

### FR-008 - Programmatic login support

The E2E suite must provide reusable Cypress commands for authenticated sessions by role.

The authentication UI tests must still exercise the real login form.

### FR-009 - Return URL behavior

When an anonymous user is redirected to login from a protected local route, a valid login must return the user to that local route.

External or non-local return URLs must not be used by E2E tests and must remain subject to the application's existing local redirect protection.

## Scenarios

### Scenario: Admin logs in

Given an admin E2E user exists
When the user logs in through `/Account/Login`
Then the dashboard must be visible.

### Scenario: Anonymous user is returned to requested route after login

Given no user is authenticated
When the user visits `/Users`
And the system redirects to `/Account/Login`
And the user logs in as an admin
Then the users administration page must be visible.

### Scenario: Invalid credentials

Given no authenticated user
When the user submits an unknown email or wrong password
Then the login page must display the invalid login message
And protected application content must not be visible.

### Scenario: Logout ends session

Given an authenticated user is on the dashboard
When the user logs out
Then the login page must be visible
And visiting `/Orders` must require authentication again.

### Scenario: Consulta cannot access admin users

Given a `Consulta` user is authenticated
When the user visits `/Users`
Then the system must show access denied or another existing forbidden response.

### Scenario: Consulta cannot execute write operation

Given a `Consulta` user is authenticated
When the user attempts a write operation directly
Then the system must reject the request according to the existing authorization behavior.

## Authorization

Authentication tests must use real login UI for login-specific coverage. Other E2E specs may use programmatic login commands.

The minimum role assertions for this spec are:

- anonymous users cannot access `/Orders`;
- `Admin` can access `/Users`;
- `Consulta` cannot access `/Users`;
- `Consulta` cannot perform a representative write operation;
- `Operador` can authenticate successfully and must not access `/Users`.

## Security

Login and logout flows must exercise the real anti-forgery-protected forms through the browser.

Programmatic login commands must follow application security conventions and must not bypass server-side authorization checks.

Any E2E-only setup capability used to seed users or reset the database must be unavailable outside the `E2E` environment.

## Compatibility

Existing authentication routes must remain valid:

- `GET /Account/Login`
- `POST /Account/Login`
- `POST /Account/Logout`
- `GET /Account/AccessDenied`

Adding `data-cy` selectors must not change visible UI behavior.

## Edge Cases

- Login is submitted with unknown credentials.
- Anonymous user opens a protected read route directly.
- Authenticated user opens an admin-only route without the `Admin` role.
- Read-only user sends a direct write request instead of relying on hidden UI controls.
- Logout form is submitted from an authenticated session.

## Acceptance Criteria

- Login succeeds for seeded E2E credentials.
- Login fails for invalid credentials.
- Logout prevents continued access to protected routes.
- Anonymous users are redirected away from protected routes.
- Role restrictions are verified for at least one admin-only route and one write operation.
- Programmatic login helpers exist for `Admin`, `Operador`, and `Consulta`.
- Required authentication selectors are present in rendered HTML.

## Required Selectors

- `data-cy="login-email"`
- `data-cy="login-password"`
- `data-cy="login-remember-me"`
- `data-cy="login-return-url"`
- `data-cy="login-submit"`
- `data-cy="logout-submit"`
- `data-cy="access-denied-message"`

## Assumptions

- `/Orders` is the representative protected read route for anonymous redirect coverage.
- `/Users` is the representative admin-only route.
- A write-protected route from the existing MVC controllers may be used for the representative `Consulta` write rejection.

## Open Questions

None blocking.
