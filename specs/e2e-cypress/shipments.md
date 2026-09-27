# Feature: E2E Shipments

## Summary

Validate shipment creation, listing, filtering, and status updates.

## Context

`ShipmentsController` lists shipments, creates shipments for orders, and updates shipment status. Shipment behavior depends on existing orders and optional tracking codes.

## Goals

- Verify a shipment can be created for an eligible order.
- Verify the shipment list shows created shipments.
- Verify shipment status filtering works.
- Verify status updates are reflected in the list.
- Verify read-only users cannot mutate shipments.

## Non-Goals

This feature does not test real Correios or Loggi tracking APIs.

## Functional Requirements

### FR-001 - Shipment list

The shipment listing must display seeded and created shipments with order, tracking code, and status information.

### FR-002 - Create shipment

A write-authorized user must create a shipment for an eligible order with an optional tracking code.

### FR-003 - Create validation

Creating a shipment without an eligible order must be rejected.

### FR-004 - Filter by status

The shipment listing must filter by shipment status.

### FR-005 - Update status

A write-authorized user must update shipment status to supported statuses.

### FR-006 - Read-only protection

A `Consulta` user must not be able to create shipments or update shipment status.

## Scenarios

### Scenario: Create shipment

Given an eligible order exists
When the user creates a shipment with tracking code "TRACK-E2E"
Then the shipment list must show the order and tracking code.

### Scenario: Update shipment status

Given a shipment exists
When the user updates it to delivered
Then the shipment list filtered by delivered status must include the shipment.

### Scenario: Create shipment validation

Given no eligible order is selected
When the user submits the create shipment form
Then the form must reject the submission.

## Acceptance Criteria

- Shipment creation is covered.
- Shipment status filtering and updates are covered.
- Invalid create submission is covered.
- Read-only mutation attempts are blocked.

## Required Selectors

- `data-cy="shipments-list"`
- `data-cy="shipment-create"`
- `data-cy="shipment-order"`
- `data-cy="shipment-tracking-code"`
- `data-cy="shipment-save"`
- `data-cy="shipment-status-filter"`
- `data-cy="shipment-filter-submit"`
- `data-cy="shipment-row"`
- `data-cy="shipment-status-in-transit"`
- `data-cy="shipment-status-delivery-attempted"`
- `data-cy="shipment-status-delivered"`
- `data-cy="shipment-status-cancelled"`
