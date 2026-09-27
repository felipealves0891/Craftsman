# Feature: E2E Order Import

## Summary

Validate the order import entry point and its integration with the orders workflow.

## Context

`ImportController` exposes `/Import` and `/Import/Run`, while the current implementation redirects import navigation and execution back to orders. `OrdersController.Import` performs the actual import action from the orders page.

## Goals

- Verify the import navigation route remains valid.
- Verify the import action redirects to the orders workflow according to current behavior.
- Verify the orders import button reports deterministic import results when a fake or seeded source is configured.
- Verify import writes require write authorization.

## Non-Goals

This feature does not call real marketplace APIs or validate every import normalization rule.

## Functional Requirements

### FR-001 - Import route

An authenticated read-authorized user must be able to visit `/Import` without receiving a 404.

### FR-002 - Import route behavior

The `/Import` page must redirect or route the user to the current orders import/listing experience.

### FR-003 - Run import

A write-authorized user must be able to trigger the current import action and see imported, skipped, or failed result information.

### FR-004 - Deterministic source

The E2E environment must use deterministic import data from a fake source, local fixture, or seed setup.

### FR-005 - Read-only protection

A `Consulta` user must not be able to execute import mutations.

## Scenarios

### Scenario: Import navigation reaches orders

Given an authenticated user is on the dashboard
When the user opens import navigation
Then the orders listing or import experience must be visible.

### Scenario: Import deterministic order

Given the E2E import source contains one new order
When an `Operador` triggers import
Then the orders page must show an import result
And the imported order must appear in the orders listing.

### Scenario: Read-only user cannot import

Given a `Consulta` user is authenticated
When the user attempts to run import
Then the operation must be rejected.

## Acceptance Criteria

- `/Import` remains a valid route.
- Import action is covered with deterministic data.
- Result messaging is visible after import.
- Read-only import mutation is blocked.

## Required Selectors

- `data-cy="nav-import"`
- `data-cy="orders-import"`
- `data-cy="import-result"`
- `data-cy="orders-list"`
