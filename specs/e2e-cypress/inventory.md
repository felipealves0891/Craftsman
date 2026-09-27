# Feature: E2E Inventory

## Summary

Validate raw material, stock movement, stock balance, alert, and history workflows.

## Context

`InventoryController` supports raw material listing/create/edit, stock movements, balances, and history. Inventory data is used by product bill of materials and production planning.

## Goals

- Verify raw materials can be created and edited.
- Verify stock movements update balances and history.
- Verify outbound movements require a reason.
- Verify stock alert states appear when balances reach configured limits.
- Verify read-only users cannot mutate inventory.

## Non-Goals

This feature does not exhaustively test stock calculation logic already covered by lower-level tests.

## Functional Requirements

### FR-001 - Raw material listing

The raw material listing must display seeded and newly created materials.

### FR-002 - Create raw material

A write-authorized user must create a material with name, unit of measure, status, minimum stock, and critical stock.

### FR-003 - Edit raw material

A write-authorized user must edit material metadata and thresholds.

### FR-004 - Register inbound movement

Inbound movement must increase the displayed balance and appear in movement history.

### FR-005 - Register outbound movement

Outbound movement must decrease the displayed balance and require a reason.

### FR-006 - Balances and alerts

Balances must show warning or critical indicators when thresholds are reached.

### FR-007 - Read-only protection

A `Consulta` user must not be able to save materials or movements.

## Scenarios

### Scenario: Create raw material

Given an `Operador` user is authenticated
When the user creates raw material "Linha E2E"
Then the raw material listing must include "Linha E2E".

### Scenario: Inbound movement updates balance

Given a raw material exists with no movements
When the user registers an inbound movement of 10 units
Then balances must show 10 units for that raw material
And history must include the inbound movement.

### Scenario: Outbound movement requires reason

Given a raw material exists with stock
When the user submits an outbound movement without reason
Then the form must reject the movement.

### Scenario: Critical alert appears

Given a raw material has critical and minimum stock thresholds
When stock balance reaches the critical threshold
Then the balances page or stock alert component must display the critical alert.

## Acceptance Criteria

- Raw material create/edit workflows are covered.
- Movement, balance, and history pages are connected by E2E assertions.
- Outbound validation is covered.
- Read-only mutation attempts are blocked.

## Required Selectors

- `data-cy="inventory-list"`
- `data-cy="raw-material-create"`
- `data-cy="raw-material-name"`
- `data-cy="raw-material-unit"`
- `data-cy="raw-material-status"`
- `data-cy="raw-material-minimum-stock"`
- `data-cy="raw-material-critical-stock"`
- `data-cy="raw-material-save"`
- `data-cy="stock-movement-material"`
- `data-cy="stock-movement-type"`
- `data-cy="stock-movement-quantity"`
- `data-cy="stock-movement-unit-cost"`
- `data-cy="stock-movement-reason"`
- `data-cy="stock-movement-save"`
- `data-cy="inventory-balances"`
- `data-cy="inventory-history"`
- `data-cy="stock-alerts"`
