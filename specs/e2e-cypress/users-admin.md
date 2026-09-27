# Feature: E2E User Administration

## Summary

Validate admin-only user listing and user creation workflows.

## Context

`UsersController` is protected by `ApplicationPolicies.AdminOnly`. Admin users can list existing users and create new users with roles `Admin`, `Operador`, or `Consulta`.

## Goals

- Verify admins can view user administration.
- Verify admins can create users with valid credentials and role.
- Verify created users appear in the list.
- Verify non-admin users cannot access user administration.
- Verify invalid user creation is rejected.

## Non-Goals

This feature does not include editing users, deleting users, password reset, or role reassignment after creation.

## Functional Requirements

### FR-001 - User list

An admin user must see existing users with email and assigned roles.

### FR-002 - Create user

An admin user must create a user with email, password, and role.

### FR-003 - Validation

Invalid email, weak password, missing role, or duplicate email must prevent user creation.

### FR-004 - Role assignment

The selected role must be assigned to the created user and visible in the list.

### FR-005 - Admin-only access

`Operador` and `Consulta` users must not access user administration.

## Scenarios

### Scenario: Admin creates operator

Given an admin user is authenticated
When the admin creates a new user with role `Operador`
Then the users list must show the new email and `Operador` role.

### Scenario: Duplicate user rejected

Given a user already exists
When the admin creates another user with the same email
Then the create user form must show validation errors
And no duplicate user must be created.

### Scenario: Operator cannot access users

Given an `Operador` user is authenticated
When the user visits `/Users`
Then the request must be forbidden or show access denied.

## Acceptance Criteria

- Admin user list and create flow are covered.
- Duplicate or invalid creation is covered.
- Non-admin access is blocked.

## Required Selectors

- `data-cy="users-list"`
- `data-cy="user-create"`
- `data-cy="user-email"`
- `data-cy="user-password"`
- `data-cy="user-role"`
- `data-cy="user-save"`
- `data-cy="user-row"`
