# Feature: Cypress E2E Manual Orders

## Summary

Add Cypress end-to-end coverage for manual order workflows in the ASP.NET MVC application.

The tests must validate source creation, manual order creation, dynamic item behavior, totals, editing, product linking, invalid submissions, and read-only authorization through the real browser-facing UI using deterministic E2E data.

## Context

The repository already contains the Cypress E2E foundation under `tests/e2e`, including database reset, E2E users, programmatic login helpers, authentication coverage, and order coverage.

Manual orders are exposed through `ManualOrdersController` and static Razor components:

- `/ManualOrders/Create` renders the manual order form and accepts create submissions.
- `/ManualOrders/Edit/{id}` renders the same form for editing an existing order.
- `/ManualOrders/CreateSource` creates an order source from the manual order page.
- `/ManualOrders/LinkItems/{id}` displays order items and allows linking each item to an internal product.
- `/ManualOrders/LinkItem` persists the selected internal product for an order item.

Manual orders are a critical entry point for downstream order details, production planning, inventory consumption, shipment, and finance workflows.

## Problem

Lower-level tests cover pieces of manual order behavior, but there is no browser-level verification that the form, dynamic JavaScript, model binding, anti-forgery-protected posts, role restrictions, and persisted effects work together.

Regressions in dynamic item indexing, currency input handling, source creation, edit binding, product linking, or write authorization could pass lower-level tests while breaking real user workflows.

## Goals

- Verify a write-authorized user can create an order source from the manual order page.
- Verify a write-authorized user can create a manual order with source, reference, shipping date, and multiple valid items.
- Verify dynamic item add/remove behavior preserves correct submitted item binding.
- Verify item totals and order totals update when quantity or price changes.
- Verify a write-authorized user can edit an existing manual order.
- Verify a write-authorized user can link an unlinked order item to an internal product.
- Verify invalid manual order data is rejected and remains on the form.
- Verify a `Consulta` user cannot create, edit, or link manual orders.

## Non-Goals

This feature does not include:

- exhaustive validation of every invalid field combination;
- direct testing of downstream production, shipment, or finance side effects;
- testing external marketplace integrations;
- changing the manual order user experience or visible layout;
- replacing existing domain, application, repository, or MVC tests.

## Actors

### Operador

Can read protected pages and execute operational write actions such as creating, editing, and linking manual orders.

### Admin

Can perform the same manual order workflows as `Operador`.

### Consulta

Can read protected pages but must not create, edit, or link manual orders.

## Preconditions

- The application runs with `ASPNETCORE_ENVIRONMENT=E2E`.
- Cypress can reset the E2E database before each test.
- E2E users for `Admin`, `Operador`, and `Consulta` exist.
- Scenario-specific products, order sources, and manual orders can be seeded deterministically.
- Successful mutation tests exercise real MVC forms with anti-forgery tokens.

## Functional Requirements

### FR-001 - Create source

A write-authorized user must be able to create an order source from the manual order page and then use that source in the manual order form.

### FR-002 - Create manual order

A write-authorized user must be able to create a manual order with source, reference, shipping date, and at least one valid item.

### FR-003 - Multiple item binding

When a manual order is submitted with multiple valid item rows, the saved order must contain the submitted rows with their descriptions, quantities, prices, and product links.

### FR-004 - Dynamic item add/remove

The manual order form must allow adding and removing item rows, and the submitted rows must bind correctly after client-side reindexing.

### FR-005 - Totals

The form must update item totals and order total when quantity or unit price changes.

### FR-006 - Edit manual order

A write-authorized user must be able to edit an existing manual order's reference, shipping date, and item fields when the order is still eligible for editing.

### FR-007 - Link item to product

A write-authorized user must be able to link an unlinked order item to an internal product.

### FR-008 - Validation

Missing source, missing shipping date, no valid item, invalid quantity, or invalid price must prevent saving.

### FR-009 - Read-only protection

A `Consulta` user must not be able to create, edit, create sources for, or link manual orders, including when attempting write URLs directly.

### FR-010 - Stable selectors

The UI elements exercised by the manual order E2E tests must expose the required `data-cy` selectors without changing visible behavior.

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
- `data-cy="flash-success"`
- `data-cy="flash-error"`
- `data-cy="order-details"`
- `data-cy="order-edit"`
- `data-cy="order-link-items"`

## User Flows

### Create source and manual order

1. Cypress resets the E2E database.
2. Cypress seeds at least one internal product.
3. Cypress authenticates as `Operador`.
4. User opens `/ManualOrders/Create`.
5. User creates a new order source.
6. System returns to the manual order form with the source available.
7. User fills source, reference, shipping date, and at least two item rows.
8. User submits the form.
9. System redirects to order details.
10. Details show the created reference, source, shipping date, and submitted items.

### Dynamic totals

1. User opens the manual order form.
2. User enters quantity and unit price in an item row.
3. System updates the item total and order total.
4. User adds and removes item rows.
5. System recalculates totals and preserves correct row indexing.

### Edit order

1. Cypress seeds an editable manual order.
2. User opens the order details page.
3. User opens the edit page.
4. User changes the reference and item quantity.
5. User saves the form.
6. Details show the updated reference and item quantity.

### Link item

1. Cypress seeds an order with an unlinked item and an internal product.
2. User opens the link-items page.
3. User selects an internal product for the unlinked item.
4. User submits the link form.
5. Link-items page or order details show the internal product association.

## Scenarios

### Scenario: Create manual order with two items

Given an internal product exists
And a write-authorized user is authenticated
When the user creates a source and a manual order with two items
Then the order details page must show the reference, source, shipping date, and both items.

### Scenario: Dynamic totals update

Given the manual order form is open
When the user enters quantity `2` and unit price `15,50`
Then the item total and order total must show the calculated value.

### Scenario: Add and remove item rows

Given the manual order form is open
When the user adds two item rows and removes one row
Then the submitted order must contain only the remaining valid rows.

### Scenario: Edit manual order

Given an editable manual order exists
When the user edits the reference and item quantity
Then the order details page must show the updated data.

### Scenario: Link unlinked item

Given an order has an unlinked item and an internal product exists
When the user links the item to the product
Then the link-items page or order details page must show the internal product association.

### Scenario: Invalid manual order is rejected

Given the manual order form is open
When the user submits missing or invalid required order data
Then the system must remain on the form
And validation feedback must be visible
And no order details redirect must occur.

### Scenario: Read-only user cannot write

Given a `Consulta` user is authenticated
When the user attempts to create a source, create an order, edit an order, or link an item
Then the system must reject the operation according to the existing authorization behavior.

## Business Rules

### BR-001 - E2E data is deterministic

Manual order E2E tests must use deterministic setup data and must not depend on records created by previous tests.

### BR-002 - Browser validates behavior

Tests may prepare state through E2E setup helpers, but must validate manual order workflows through the browser-visible application.

### BR-003 - Real authorization is exercised

Read-only protection must be verified against server-enforced write restrictions, not only by checking hidden controls.

### BR-004 - Dynamic rows must submit correctly

Client-side row additions and removals must preserve ASP.NET model binding names and indexes for submitted item rows.

## Invariants

- Manual order write actions must remain protected by the existing write policy.
- Manual order write forms must continue using anti-forgery protection.
- E2E setup helpers must only be available in the `E2E` environment.
- Each Cypress test must be repeatable after database reset.
- Test selectors must not depend on CSS classes, translated labels, or icon markup.
- Adding `data-cy` attributes must not change visible UI behavior.

## Authorization

`Operador` and `Admin` may execute the manual order write workflows covered by this specification.

`Consulta` may read protected pages but must not create sources, create orders, edit orders, or link products to order items.

## Security

Successful mutation tests must submit real MVC forms so anti-forgery behavior remains exercised.

Direct write attempts used for read-only authorization checks must be authenticated as `Consulta` and must verify the server rejects the operation.

E2E seed endpoints, if added, must be mapped only in the `E2E` environment.

## Compatibility

Existing routes must remain valid:

- `/ManualOrders/Create`
- `/ManualOrders/CreateSource`
- `/ManualOrders/Edit/{id}`
- `/ManualOrders/LinkItems/{id}`
- `/ManualOrders/LinkItem`
- `/Orders/Details/{id}`

Adding `data-cy` attributes must not change visible UI behavior.

## Edge Cases

- No order sources exist after reset.
- No products exist after reset.
- A duplicate order source is submitted.
- Manual order has no valid item rows.
- Quantity is missing, zero, or invalid.
- Unit price is missing, zero, or invalid.
- Removed dynamic rows leave non-contiguous client-side indexes before reindexing.
- Product link is attempted for a missing product.
- `Consulta` posts directly to write actions.

## Dependencies

This feature depends on:

- existing E2E reset and login helpers;
- existing manual order MVC routes and Razor components;
- existing order details page;
- existing product catalog persistence;
- existing authorization policies;
- existing Cypress order/detail selectors where already available.

## Constraints

- Use Cypress in the existing `tests/e2e` project.
- Reuse existing E2E helpers where possible.
- Do not introduce visible UI changes except as necessary to expose stable selectors.
- Do not use arbitrary sleeps in Cypress tests.
- Do not mock the MVC backend with Cypress intercepts for behavior under test.

## Acceptance Criteria

- Manual order source creation is covered.
- Manual order creation with multiple items is covered.
- Dynamic item add/remove behavior is covered.
- Dynamic total calculation is covered.
- Manual order editing is covered.
- Product linking is covered.
- Invalid form submission is covered.
- Read-only write protection is covered.
- Required selectors exist on exercised UI elements.
- The Cypress manual order spec can run through the existing E2E command.

## Test Considerations

Manual order Cypress tests should favor complete, high-signal workflows over exhaustive validation permutations.

At minimum, the suite should cover:

- create source;
- create manual order with two items;
- add/remove item rows;
- total calculation;
- edit existing manual order;
- link unlinked item to product;
- invalid form rejection;
- `Consulta` rejection for create/edit/link write actions.

## Assumptions

- It is acceptable to add E2E-only seed endpoints for products and manual order scenarios, guarded by `ASPNETCORE_ENVIRONMENT=E2E`.
- Tests may use real UI forms for successful mutation flows and direct authenticated requests for read-only rejection checks.
- `Operador` is the default write-authorized actor for manual order workflows.
- Existing lower-level tests remain responsible for exhaustive domain validation.

## Open Questions

None blocking.
