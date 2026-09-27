# Feature: E2E Finance

## Summary

Validate financial settlement pending, generation, listing, filtering, and detail workflows.

## Context

`FinanceController` lists settlements, shows details, lists pending settlements, and generates financial settlement for eligible orders.

## Goals

- Verify pending orders appear in finance pending view.
- Verify settlement generation succeeds for eligible orders.
- Verify generated settlement details can be opened.
- Verify list filters by date range.
- Verify read-only users cannot generate settlements.

## Non-Goals

This feature does not exhaustively validate settlement calculation formulas already covered by application and domain tests.

## Functional Requirements

### FR-001 - Pending list

The finance pending page must display eligible orders awaiting settlement generation.

### FR-002 - Generate settlement

A write-authorized user must generate settlement for an eligible pending order.

### FR-003 - Generation failure

When settlement cannot be generated, the system must show the existing error behavior.

### FR-004 - Settlement list

The finance index must display generated settlements.

### FR-005 - Date filters

The finance index must filter settlements by `from` and `to` date.

### FR-006 - Settlement details

The details page must show settlement information for an order.

### FR-007 - Read-only protection

A `Consulta` user must not be able to generate settlements.

## Scenarios

### Scenario: Generate settlement

Given an order is eligible for financial settlement
When the user opens pending finance and generates settlement
Then the settlement details page must be visible
And the finance list must include the generated settlement.

### Scenario: Filter settlements by period

Given settlements exist inside and outside a date range
When the user filters finance by that range
Then only matching settlements must be visible.

### Scenario: Read-only user cannot generate

Given a `Consulta` user is authenticated
When the user attempts to generate a settlement
Then the operation must be rejected.

## Acceptance Criteria

- Pending, generation, listing, filtering, and detail pages are covered.
- Generation failure or unauthorized generation is covered.
- Financial assertions remain behavior-level and do not duplicate detailed formula tests.

## Required Selectors

- `data-cy="finance-list"`
- `data-cy="finance-pending-link"`
- `data-cy="finance-date-from"`
- `data-cy="finance-date-to"`
- `data-cy="finance-filter-submit"`
- `data-cy="finance-settlement-row"`
- `data-cy="finance-settlement-details-link"`
- `data-cy="finance-pending-list"`
- `data-cy="finance-generate-settlement"`
- `data-cy="finance-settlement-details"`
