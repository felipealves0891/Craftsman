# Feature: E2E Production

## Summary

Validate production task listing, filtering, and status advancement.

## Context

`ProductionController` lists production tasks with status/date filters and advances tasks through start, complete, or cancel actions. Tasks are commonly created by sending orders to production.

## Goals

- Verify production tasks appear after setup or order planning.
- Verify filters by status and planned date.
- Verify valid status advancement actions.
- Verify invalid or unauthorized advancement is blocked.

## Non-Goals

This feature does not test the production scheduling algorithm exhaustively.

## Functional Requirements

### FR-001 - Task listing

The production page must show tasks with order/product context, status, planned date, and available actions.

### FR-002 - Filter by status

The production page must filter tasks by selected status.

### FR-003 - Filter by planned date

The production page must filter tasks by planned date.

### FR-004 - Advance task

A write-authorized user must be able to start, complete, and cancel tasks when those actions are valid.

### FR-005 - Invalid transition

Invalid transitions must not update the task and must surface the existing error behavior.

### FR-006 - Read-only protection

A `Consulta` user must not be able to advance production tasks.

## Scenarios

### Scenario: Filter production tasks

Given tasks exist with different statuses and dates
When the user filters by status and planned date
Then only matching tasks must be visible.

### Scenario: Start and complete task

Given a planned production task exists
When the user starts the task and then completes it
Then the task status must update according to the existing workflow.

### Scenario: Cancel task

Given a task that can be cancelled exists
When the user cancels the task
Then the task must show the cancelled status or leave the active listing according to existing behavior.

## Acceptance Criteria

- Listing and filters are covered.
- At least one valid advancement flow is covered.
- Invalid or unauthorized advancement is covered.
- Tests use seeded tasks or a complete order-to-production setup.

## Required Selectors

- `data-cy="production-list"`
- `data-cy="production-status-filter"`
- `data-cy="production-date-filter"`
- `data-cy="production-filter-submit"`
- `data-cy="production-task-row"`
- `data-cy="production-task-start"`
- `data-cy="production-task-complete"`
- `data-cy="production-task-cancel"`
