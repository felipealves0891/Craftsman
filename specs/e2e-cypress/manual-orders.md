# Feature: E2E Manual Orders

## Summary

Validate manual order creation, editing, dynamic item behavior, totals, source creation, and product linking workflows.

## Context

`ManualOrdersController` supports creating and editing orders, creating order sources, and linking order items to internal products. Manual orders are a critical entry point for downstream production, shipment, and finance workflows.

## Goals

- Verify a user can create an order source.
- Verify a user can create a manual order with one or more items.
- Verify dynamic item add/remove and total calculation behavior.
- Verify a user can edit an existing manual order.
- Verify a user can link order items to internal products.
- Verify invalid order data is rejected.

## Non-Goals

This feature does not verify every downstream production or finance side effect.

## Functional Requirements

### FR-001 - Create source

A write-authorized user must be able to add an order source from the manual order page.

### FR-002 - Create manual order

A write-authorized user must create a manual order with source, reference, shipping date, and at least one valid item.

### FR-003 - Dynamic items

The manual order form must allow adding and removing item rows, and submitted rows must bind correctly.

### FR-004 - Totals

The form must update item and order totals when quantity or price changes.

### FR-005 - Edit manual order

A write-authorized user must edit existing order fields and items.

### FR-006 - Link item to product

A write-authorized user must link an unlinked order item to an internal product.

### FR-007 - Validation

Missing source, missing shipping date, no valid item, invalid quantity, or invalid price must prevent saving.

### FR-008 - Read-only protection

A `Consulta` user must not be able to create, edit, or link manual orders.

## Scenarios

### Scenario: Create manual order with two items

Given products and an order source exist
When the user creates a manual order with two items
Then the order details page must show the reference, source, shipping date, and both items.

### Scenario: Dynamic totals update

Given the manual order form is open
When the user enters quantity 2 and unit price 15,50
Then the item total and order total must show the calculated value.

### Scenario: Edit manual order

Given a manual order exists
When the user edits the reference and item quantity
Then the order details page must show the updated data.

### Scenario: Link unlinked item

Given an order has an unlinked item and a product exists
When the user links the item to the product
Then the link items page or order details page must show the internal product association.

## Acceptance Criteria

- Manual order creation and editing are covered.
- Dynamic form behavior is covered at least once.
- Product linking is covered.
- Invalid form submission is covered.
- Read-only write protection is covered.

## Required Selectors

- `data-cy="order-source-create-name"`
- `data-cy="order-source-create-submit"`
- `data-cy="manual-order-source"`
- `data-cy="manual-order-reference"`
- `data-cy="manual-order-shipping-date"`
- `data-cy="manual-order-add-item"`
- `data-cy="manual-order-remove-item"`
- `data-cy="manual-order-item-product"`
- `data-cy="manual-order-item-description"`
- `data-cy="manual-order-item-quantity"`
- `data-cy="manual-order-item-unit-price"`
- `data-cy="manual-order-item-total"`
- `data-cy="manual-order-total"`
- `data-cy="manual-order-save"`
- `data-cy="manual-order-link-product"`
- `data-cy="manual-order-link-submit"`
