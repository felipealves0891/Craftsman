# Feature: E2E Orders

## Summary

Validate order listing, import entry point, details, production submission, and deletion behavior.

## Context

`OrdersController` lists orders, imports orders from configured sources, shows details, sends orders to production, and deletes eligible orders. Orders connect catalog, inventory, production, shipments, and finance.

## Goals

- Verify seeded and manually created orders appear in the listing.
- Verify order details show item, production, shipment, and action sections.
- Verify import action reports results.
- Verify eligible orders can be sent to production.
- Verify eligible orders can be deleted.
- Verify write actions require write authorization.

## Non-Goals

This feature does not test external marketplace APIs directly.

## Functional Requirements

### FR-001 - Order listing

The order listing must display existing orders with useful identifying information and details links.

### FR-002 - Import orders

A write-authorized user must be able to trigger order import and see import outcome information.

### FR-003 - Order details

Order details must display order metadata, items, production tab, shipments tab, and relevant actions.

### FR-004 - Send to production

A write-authorized user must be able to send an eligible order to production.

### FR-005 - Production failure message

When an order cannot be sent to production, the UI must show the existing failure message.

### FR-006 - Delete eligible order

A write-authorized user must be able to delete an eligible order and return to the listing.

### FR-007 - Block unsafe delete

Orders with started/completed production or shipments must not be deleted successfully.

### FR-008 - Read-only protection

A `Consulta` user must not be able to import, send to production, or delete orders.

## Scenarios

### Scenario: View order details

Given an order with items exists
When the user opens the order details page
Then order metadata and item rows must be visible.

### Scenario: Send order to production

Given an order has linked products, valid bill of materials, and sufficient stock
When the user sends the order to production
Then the details page must show a success message
And the production area must include the generated task.

### Scenario: Send unplannable order to production

Given an order is missing product links or sufficient stock
When the user sends the order to production
Then the details page must show the existing failure message.

### Scenario: Delete order

Given an eligible order exists
When the user deletes the order
Then the order listing must not include it.

## Acceptance Criteria

- Order listing and details are covered.
- Import action behavior is covered with deterministic fake or seeded source data.
- Production submission success and failure are covered.
- Delete success and blocked-delete behavior are covered.
- Read-only write protection is covered.

## Required Selectors

- `data-cy="orders-list"`
- `data-cy="orders-import"`
- `data-cy="order-create"`
- `data-cy="order-row"`
- `data-cy="order-details-link"`
- `data-cy="order-details"`
- `data-cy="order-items-tab"`
- `data-cy="order-production-tab"`
- `data-cy="order-shipments-tab"`
- `data-cy="order-send-to-production"`
- `data-cy="order-delete"`
- `data-cy="order-edit"`
- `data-cy="order-link-items"`
- `data-cy="flash-success"`
- `data-cy="flash-error"`
