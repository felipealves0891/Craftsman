# Feature: Cypress E2E Orders

## Summary

Add Cypress end-to-end coverage for the order workflows in the ASP.NET MVC application.

The tests must validate order listing, order details, import, send-to-production, deletion, blocked deletion, and write authorization using deterministic E2E data through the real browser-facing UI.

## Context

The repository already contains the Cypress E2E foundation under `tests/e2e`, including database reset, E2E users, programmatic login helpers, and the authentication suite.

Orders are exposed through `OrdersController` and static Razor components:

- `/Orders` lists orders and exposes import and manual-order entry points.
- `/Orders/Details/{id}` displays order metadata, items, production, shipments, edit/delete actions, and send-to-production.
- `/Orders/Import`, `/Orders/SendToProduction/{id}`, and `/Orders/Delete/{id}` are write actions protected by the write policy and anti-forgery validation.

Current Cypress coverage does not yet exercise order-specific behavior.

## Problem

Lower-level tests cover pieces of order behavior, but there is no browser-level verification that order pages, forms, role restrictions, selectors, anti-forgery-protected actions, and persisted effects work together.

Regressions in order UI wiring or authorization could pass unit and integration tests while breaking real user workflows.

## Goals

- Verify that seeded and manually created orders appear in the order listing.
- Verify that order details show metadata, item rows, production, shipment sections, and relevant actions.
- Verify that import reports outcome information using deterministic E2E source data.
- Verify that eligible orders can be sent to production and show the resulting production task.
- Verify that unplannable orders show the existing production failure message.
- Verify that eligible orders can be deleted from the details page and disappear from the listing.
- Verify that orders with started/completed production or shipments are not deleted successfully.
- Verify that `Consulta` users cannot import, send to production, or delete orders.

## Non-Goals

This feature does not include:

- direct testing of external marketplace APIs;
- exhaustive validation of manual order creation fields;
- replacing domain/application tests for production planning, inventory, shipment, or deletion internals;
- broad styling or layout changes;
- testing every order status transition through Cypress.

## Actors

### Operador

Can read orders and execute operational write actions such as import, send-to-production, and delete when the order is eligible.

### Admin

Can perform the same order workflows as `Operador`.

### Consulta

Can read order pages but must not execute order write actions.

## Preconditions

- The application runs with `ASPNETCORE_ENVIRONMENT=E2E`.
- Cypress can call the existing E2E reset endpoint before each test.
- E2E users for `Admin`, `Operador`, and `Consulta` exist.
- Scenario-specific order, product, bill-of-materials, stock, production, and shipment data can be seeded deterministically.

## Functional Requirements

### FR-001 - Order listing

The order listing must display existing seeded or created orders with useful identifying information and details links.

### FR-002 - Import orders

A write-authorized user must be able to trigger order import and see deterministic import outcome information.

### FR-003 - Order details

Order details must display order metadata, item rows, production tab, shipments tab, and relevant order actions.

### FR-004 - Send to production

A write-authorized user must be able to send an eligible order to production.

### FR-005 - Production success result

After a successful production submission, the details page must show the existing success message and include the generated production task in the production area.

### FR-006 - Production failure message

When an order cannot be sent to production because it is missing product links, stock, valid bill of materials, or another existing planning precondition, the UI must show the existing failure message.

### FR-007 - Delete eligible order

A write-authorized user must be able to delete an eligible order and return to the listing.

### FR-008 - Block unsafe delete

Orders with started/completed production or shipments must not be deleted successfully.

### FR-009 - Read-only protection

A `Consulta` user must not be able to import, send to production, or delete orders, including when attempting write URLs directly.

### FR-010 - Stable selectors

The UI elements exercised by the order E2E tests must expose the required `data-cy` selectors without changing visible behavior.

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

## User Flows

### Listing and details

1. Cypress resets the E2E database.
2. Cypress seeds an order with items.
3. Cypress authenticates as a write-authorized user.
4. User opens `/Orders`.
5. System displays the order in the listing with a details link.
6. User opens details.
7. System displays order metadata, items, production tab, shipments tab, and actions.

### Send to production

1. Cypress seeds a plannable order with linked products, bill of materials, and sufficient stock.
2. Cypress authenticates as a write-authorized user.
3. User opens the order details page.
4. User sends the order to production.
5. System redirects back to details.
6. System displays the success message and generated production task.

### Delete order

1. Cypress seeds an eligible order.
2. Cypress authenticates as a write-authorized user.
3. User opens details.
4. User deletes the order.
5. System redirects to the order listing.
6. System no longer displays the deleted order.

## Scenarios

### Scenario: View order listing and details

Given an order with items exists
When the user opens the order listing
Then the order must be visible with identifying information
And a details link must be available
When the user opens the order details page
Then order metadata and item rows must be visible.

### Scenario: Import orders

Given deterministic E2E import source data exists
And the user has write authorization
When the user triggers order import
Then the listing must show import outcome information
And imported orders must be visible when import succeeds.

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

### Scenario: Block unsafe delete

Given an order has a shipment or started/completed production
When the user attempts to delete the order
Then the delete must not succeed
And the order must remain visible.

### Scenario: Read-only user cannot write

Given a `Consulta` user is authenticated
When the user attempts to import, send to production, or delete an order
Then the system must reject the operation according to the existing authorization behavior.

## Business Rules

### BR-001 - E2E data is deterministic

Order E2E tests must use deterministic setup data and must not depend on data created by previous tests.

### BR-002 - Browser validates behavior

Tests may prepare state through E2E setup helpers, but must validate the requested order workflows through the browser-visible application.

### BR-003 - Real authorization is exercised

Read-only protection must be verified against server-enforced write restrictions, not only by checking whether buttons are hidden.

### BR-004 - Existing messages remain source of truth

Production success, production failure, delete success, and blocked-delete messages must use the application's existing user-facing behavior.

## Invariants

- Order write actions must remain protected by the existing write policy.
- Order write forms must continue using anti-forgery protection.
- E2E setup helpers must only be available in the `E2E` environment.
- Each Cypress test must be repeatable after database reset.
- Test selectors must not depend on CSS classes, translated labels, or icon markup.

## Authorization

`Operador` and `Admin` may execute the order write workflows covered by this specification.

`Consulta` may view order listing and details, but must not import, send to production, or delete orders.

## Security

The tests must exercise real MVC pages and forms for successful write flows.

Direct write attempts used for read-only authorization checks must be authenticated as `Consulta` and must verify the server response rejects the operation.

E2E seed endpoints, if added, must be mapped only in the `E2E` environment.

## Compatibility

Existing order routes must remain valid:

- `/Orders`
- `/Orders/Details/{id}`
- `/Orders/Import`
- `/Orders/SendToProduction/{id}`
- `/Orders/Delete/{id}`

Adding `data-cy` attributes must not change visible UI behavior.

## Edge Cases

- Order list is empty after reset.
- Imported order already exists and is skipped.
- Order details has no production tasks.
- Order details has production tasks.
- Order details has shipments.
- Order cannot be planned.
- Delete fails because production has started or shipments exist.
- Read-only user posts directly to write actions.

## Dependencies

This feature depends on:

- existing E2E reset and login helpers;
- existing order MVC routes and Razor components;
- existing order import pipeline;
- existing production planning behavior;
- existing order deletion behavior;
- existing authorization policies.

## Constraints

- Use Cypress in the existing `tests/e2e` project.
- Reuse existing E2E helpers where possible.
- Do not call external marketplace APIs.
- Do not introduce visible UI changes except as necessary to expose stable selectors.

## Acceptance Criteria

- Order listing and details are covered by Cypress.
- Import action behavior is covered with deterministic fake or seeded source data.
- Production submission success and failure are covered.
- Delete success and blocked-delete behavior are covered.
- Read-only write protection is covered.
- Required selectors exist on exercised UI elements.
- The Cypress order spec can run through the existing E2E command.

## Test Considerations

Order Cypress tests should favor complete, high-signal workflows over exhaustive field validation.

At minimum, the suite should cover:

- listing and details;
- import outcome;
- send to production success;
- send to production failure;
- delete success;
- blocked delete;
- read-only rejection for import, send-to-production, and delete.

## Assumptions

- It is acceptable to add E2E-only seed endpoints for scenario data, guarded by `ASPNETCORE_ENVIRONMENT=E2E`.
- Tests may use the real UI for successful mutation flows and direct authenticated requests for read-only rejection checks.
- Existing lower-level tests remain responsible for exhaustive production planning and deletion internals.

## Open Questions

None blocking.
