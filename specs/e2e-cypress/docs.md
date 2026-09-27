# Feature: E2E Documentation

## Summary

Validate protected documentation access and navigation.

## Context

`DocsController` exposes documentation to authenticated users with the read policy. Documentation appears in the shared navigation.

## Goals

- Verify authenticated users with read access can open documentation.
- Verify anonymous users cannot access documentation directly.
- Verify documentation navigation link works.

## Non-Goals

This feature does not verify the full content of documentation pages.

## Functional Requirements

### FR-001 - Documentation access

Authenticated users with the read policy must access `/Docs`.

### FR-002 - Anonymous protection

Anonymous users must be redirected to login when visiting `/Docs`.

### FR-003 - Navigation

The shared navigation must provide a documentation link for authenticated users.

## Scenarios

### Scenario: Authenticated user opens docs

Given an authenticated user exists
When the user opens `/Docs`
Then the documentation page must be visible.

### Scenario: Anonymous docs access redirects

Given no user is authenticated
When the user visits `/Docs`
Then the login page must be visible.

## Acceptance Criteria

- Documentation page access is covered for authenticated users.
- Anonymous direct access is blocked.
- Navigation link reaches documentation.

## Required Selectors

- `data-cy="docs-page"`
- `data-cy="nav-docs"`
