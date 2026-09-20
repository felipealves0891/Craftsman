# Feature: Order Edit and Delete

## Summary

Allow users with write permission to edit and delete existing orders. The feature must keep the operational effects of order creation consistent: when an order changes or is removed, dependent production planning and inventory consumption must be recalculated or reversed.

## Context

Orders can currently be created manually and imported from external sources. Manual creation may immediately plan production when all items are linked to internal products. Production planning creates production tasks and consumes inventory through outbound stock movements tied to the production task identifier.

Existing users can view orders, link order items to internal products, and manually send an order to production. They cannot edit order data or delete an order after creation.

## Problem

Incorrect orders cannot be corrected or removed through the application. If an order that already affected production planning or inventory is changed outside the application, stock balances and production tasks can become inconsistent.

## Goals

- Allow editing order source, reference, shipping date, items, quantities, prices, and internal product links.
- Allow deleting an order.
- Recalculate or reverse production and inventory effects that were created from the order.
- Preserve existing validation, authorization, and CSRF protection patterns.
- Keep existing order listing and details behavior compatible.

## Non-Goals

This feature does not include:

- partial deletion of a single order item outside the edit form;
- editing shipments;
- editing financial settlements;
- editing external marketplace data at the source;
- restoring deleted orders;
- creating a full audit/history screen.

## Actors

### User

Can edit or delete an order when they have write permission.

### System

Updates order data, dependent production tasks, and inventory movements so balances reflect the final order state.

## Preconditions

- The order exists.
- The user is authenticated and authorized with the existing write policy for mutating order data.

## Functional Requirements

### FR-001 - Edit order form

The system must provide an edit flow for an existing order using the current order data as the initial form values.

### FR-002 - Save edited order

The system must allow saving changes to order source, external/reference id, shipping date, and order items.

### FR-003 - Validate edited order

The edited order must contain at least one populated item, valid quantities, valid prices, and a valid order source.

### FR-004 - Unique origin on edit

When source or reference changes, the resulting source/reference pair must remain unique across orders, excluding the order being edited.

### FR-005 - Recalculate production planning on edit

When an edited order previously has planned production effects, the system must reverse the prior production task inventory consumption, cancel or remove the prior planned tasks according to repository conventions, update the order, and attempt production planning again when all items have internal products and enough stock is available.

### FR-006 - Preserve editable unplanned orders

When an edited order cannot be replanned because items are not fully linked or stock is insufficient, the order update must still be saved and prior planned effects must not remain active.

### FR-007 - Delete order

The system must allow deleting an existing order.

### FR-008 - Reverse production and inventory on delete

Deleting an order must reverse active planned production inventory consumption and remove or cancel planned production tasks associated with the order.

### FR-009 - Block unsafe edit/delete states

The system must not edit or delete an order when associated production has already started or completed, or when the order has shipments.

### FR-010 - UI actions

The order listing or order detail page must expose edit and delete actions for orders that can be changed.

## User Flow

1. User opens an order.
2. System displays edit and delete actions when the order is eligible.
3. User opens the edit form.
4. System displays current order data.
5. User changes order data and submits.
6. System validates input.
7. System reverses previous planned production effects when present.
8. System saves the edited order.
9. System attempts to plan production again when eligible.
10. System returns the user to the order details page.

## Scenarios

### Scenario: Edit quantity recalculates stock

Given an order has a planned production task that consumed inventory
When the user changes an item quantity and saves the order
Then the system must reverse the old inventory consumption
And update the order item quantity
And create production planning effects for the new quantity when planning is possible.

### Scenario: Edit removes product link

Given an order has planned production effects
When the user edits the order and removes an internal product link from an item
Then the order must be saved
And the previous production effects must be reversed
And the order must not keep active production tasks.

### Scenario: Delete planned order

Given an order has planned production effects that have not started
When the user deletes the order
Then the system must reverse the inventory consumption
And remove or cancel the planned production tasks
And remove the order from normal order views.

### Scenario: Production already started

Given an order has a production task in progress
When the user attempts to edit or delete the order
Then the system must reject the operation.

## Business Rules

### BR-001 - Planned effects must match current order

Active production tasks and inventory consumption for an order must correspond to the current saved order items.

### BR-002 - Stock must be restored through traceable movements

Inventory consumed by production planning must be reversed with compensating stock movements rather than by silently editing historical movement quantities.

### BR-003 - In-progress operational work is not editable

Orders with started or completed production, or shipments, must not be edited or deleted through this feature.

### BR-004 - Imported and manual orders share edit/delete behavior

The edit and delete behavior must work for orders regardless of whether they were manually created or imported.

## Invariants

- An order must always contain at least one item.
- Stock balances must not include consumption from obsolete planned tasks after edit or delete.
- Existing authorization policies must remain enforced server-side.
- Existing anti-forgery validation must remain enforced for mutations.
- Deleting an order must not delete raw materials, products, or order sources.

## Validation

- Missing order source is invalid.
- Missing shipping date is invalid.
- Shipping date must be future when provided through the edit form.
- Empty item rows are ignored.
- At least one populated item is required.
- Populated items must have positive quantity, description, and non-negative unit price.
- Product ids, when provided, must reference existing products.

## Error Handling

Validation and business-rule failures must return the user to the edit form or order details page using the existing validation message patterns. Technical exception details must not be exposed to the user.

## Authorization

Viewing edit forms follows the existing read policy. Saving edits and deleting orders require the existing write policy.

## Security

All new mutation endpoints must validate anti-forgery tokens. User-provided form fields are untrusted and must use existing model binding and persistence APIs rather than string-built database commands.

## Compatibility

Existing order list, order details, manual order creation, import, product linking, and send-to-production flows must continue working. Existing URLs without edit/delete actions remain valid.

## Edge Cases

- Order does not exist.
- Edited source/reference duplicates another order.
- Edited item removes a product link.
- Edited item changes to a product with different bill of materials.
- Replanning fails due to insufficient stock.
- Order has no production tasks.
- Order has planned tasks only.
- Order has in-progress or completed tasks.
- Order has shipments.

## Dependencies

This feature depends on:

- existing order repositories;
- existing manual order input model and form component patterns;
- existing production planning;
- existing stock movement balance calculations;
- existing authorization and anti-forgery infrastructure.

## Constraints

- Use the current ASP.NET Core MVC/Razor component architecture.
- Do not introduce a new database dependency or external service.
- Preserve stock movement traceability.

## Acceptance Criteria

- A user can open an edit form for an existing eligible order.
- A user can save changes to order fields and items.
- Editing a planned order reverses old stock consumption and attempts to create new planning effects.
- Editing to an unplannable state saves the order and leaves no active obsolete production tasks.
- A user can delete an eligible order.
- Deleting a planned order reverses stock consumption and removes the order from normal views.
- Edit/delete is rejected for orders with started/completed production or shipments.
- Existing manual creation and send-to-production behavior continue to work.
- Relevant automated tests pass.

## Test Considerations

The implementation should verify:

- edit updates order fields and items;
- edit recalculates inventory when quantity changes;
- edit removes prior planning when product links are removed;
- delete reverses planned inventory consumption;
- edit/delete are blocked for in-progress production or shipments;
- duplicate source/reference is rejected;
- existing creation and planning tests continue passing.

## Open Questions

None blocking. This specification assumes planned production can be cancelled/removed, but production that has started or completed cannot be automatically reconciled by this feature.
