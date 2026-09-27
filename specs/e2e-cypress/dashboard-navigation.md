# Feature: E2E Dashboard and Navigation

## Summary

Validate that authenticated users can navigate from the dashboard and sidebar to the main Craftsman application areas.

## Context

`HomeController` renders the dashboard through `HomeDashboard`. The shared layout exposes navigation groups for orders, catalog/inventory, execution, finance, docs, and admin users.

## Goals

- Verify the dashboard loads for authenticated users.
- Verify primary dashboard cards and sidebar links route to the expected pages.
- Verify navigation honors the current user's authorization.

## Non-Goals

This feature does not verify detailed behavior inside each destination page.

## Functional Requirements

### FR-001 - Dashboard loads

The dashboard must render for authenticated users with summary cards and quick links.

### FR-002 - Primary navigation

Navigation must reach orders, new order, import, products, mappings, raw materials, balances, movements, production, shipments, finance, docs, and users when authorized.

### FR-003 - Authorization-aware navigation

Admin-only navigation must not provide unauthorized users with a successful admin workflow.

## Scenarios

### Scenario: Admin navigates through all major areas

Given an admin user is authenticated
When the user opens the dashboard and follows each primary navigation link
Then each destination page must load successfully.

### Scenario: Consulta opens read-only areas

Given a `Consulta` user is authenticated
When the user opens dashboard links for read pages
Then read pages must load
And write-only actions must not complete successfully.

## Acceptance Criteria

- Dashboard route `/` or `/Home/Index` loads for authenticated users.
- Main navigation links resolve without 404 errors.
- User administration navigation is only usable by admin users.

## Required Selectors

- `data-cy="dashboard"`
- `data-cy="nav-orders"`
- `data-cy="nav-manual-order-create"`
- `data-cy="nav-import"`
- `data-cy="nav-products"`
- `data-cy="nav-product-mappings"`
- `data-cy="nav-inventory"`
- `data-cy="nav-inventory-balances"`
- `data-cy="nav-inventory-movements"`
- `data-cy="nav-production"`
- `data-cy="nav-shipments"`
- `data-cy="nav-finance"`
- `data-cy="nav-docs"`
- `data-cy="nav-users"`
