# Implementation Plan: Order Edit and Delete

## Summary

Add order editing and deletion through the existing MVC application. Reuse the manual order form shape where practical, extend the sales domain to replace order data, and add repository/service operations to reverse planned production effects and inventory consumption before updating or deleting orders.

## Specification Reference

This plan implements `specs/order-edit-delete/spec.md`.

## Current Architecture

Order flows currently use:

- `ManualOrdersController` and `ManualOrderService` for manual order creation and product linking;
- `OrdersController` and `OrderQueryService` for listing, details, import, and send-to-production;
- `Order`, `OrderItem`, and `OrderRepository` for sales persistence;
- `OrderProductionPlanningService` and `ProductionPlanner` for production tasks;
- `InventoryService` and `StockMovementRepository` for stock consumption;
- static Razor components under `src/UI/Components`.

Production planning creates outbound `StockMovement` records with `BusinessReference` set to the production task id. Existing repositories can add and update, but do not yet expose delete/cancel helpers or lookup by order/task reference.

## Change Surface

- `src/Domains/Sales/Entities/Order.cs`
- `src/Domains/Sales/Repositories/IOrderRepository.cs`
- `src/Infra/Repositories/OrderRepository.cs`
- `src/Domains/Production/Repositories/IProductionTaskRepository.cs`
- `src/Infra/Repositories/ProductionTaskRepository.cs`
- `src/Domains/Inventory/Repositories/IStockMovementRepository.cs`
- `src/Infra/Repositories/StockMovementRepository.cs`
- `src/App/Models/ManualOrderInputModel.cs`
- `src/App/Services/ManualOrderService.cs`
- `src/App/Controllers/ManualOrdersController.cs`
- `src/App/Views/ManualOrders/Create.cshtml`
- `src/App/Views/Orders/Details.cshtml`
- `src/UI/Components/ManualOrders/ManualOrderCreatePage.razor`
- `src/UI/Components/Orders/OrderDetailsPage.razor`
- Application tests around manual operations and view contracts.

## Implementation Strategy

Extend the current manual order service to handle an existing order id in the same input model. The service will load the existing order, validate eligibility, reverse planned production effects, replace order fields/items, persist the order, and try planning again. Deletion will use the same eligibility and reversal behavior, then delete the order.

Inventory reversal will be traceable: for each outbound production movement tied to an obsolete task, create an inbound compensating movement with the same raw material, quantity, and unit cost. Planned tasks will be cancelled or deleted through repository support; the repository should expose only the operation needed by application/domain services.

The edit UI will reuse `ManualOrderCreatePage` with mode-specific title, form action, and submit label. The details page will expose edit/delete actions and route delete through a POST form with anti-forgery protection.

## Data Flow

Edit:

```text
GET /ManualOrders/Edit/{id}
  -> ManualOrderService.GetInputAsync(id)
  -> ManualOrderCreatePage in edit mode

POST /ManualOrders/Edit/{id}
  -> ManualOrderService.UpdateAsync(id, input)
  -> validate eligibility
  -> reverse existing planned effects
  -> order.Replace(...)
  -> orderRepository.UpdateAsync(...)
  -> TryPlanAsync(order.Id)
  -> redirect to Orders/Details
```

Delete:

```text
POST /Orders/Delete/{id}
  -> ManualOrderService.DeleteAsync(id)
  -> validate eligibility
  -> reverse existing planned effects
  -> orderRepository.DeleteAsync(id)
  -> redirect to Orders/Index
```

## Existing Components to Reuse

- `ManualOrderInputModel` for edit form binding.
- `ManualOrderCreatePage` for the dynamic item form.
- `OrderRepository.UpdateAsync` item synchronization logic.
- `OrderProductionPlanningService.TryPlanAsync` for replanning after edits.
- `StockMovement` model for compensating inventory movements.
- Existing read/write authorization policies and anti-forgery conventions.

## New Components

No new broad service abstraction is required. Repository interfaces will be extended with targeted methods for delete, task lookup/deletion, and movement lookup by business reference.

## Files to Modify

### `Order.cs`

Add behavior to replace editable order data and reset status to `Normalized` when production planning is removed.

### `IOrderRepository` / `OrderRepository`

Add delete support and any lookup needed for uniqueness excluding the current order.

### `IProductionTaskRepository` / `ProductionTaskRepository`

Add lookup by order id and delete/cancel support for planned tasks.

### `IStockMovementRepository` / `StockMovementRepository`

Add lookup by business reference so production consumption movements can be compensated.

### `ManualOrderService`

Add:

- loading existing order into input model;
- update behavior;
- delete behavior;
- eligibility checks;
- production/stock reversal helper.

### `ManualOrdersController`

Add GET/POST edit actions.

### `OrdersController`

Add POST delete action.

### Views/components

Add edit-mode parameters to manual order form and edit/delete actions to order details.

## Files to Create

None expected beyond the specification and plan.

## Persistence Changes

No schema changes are required. Existing stock movements remain append-only from a business perspective; reversals are represented as new movements.

## API Contract Changes

New MVC routes:

- `GET /ManualOrders/Edit/{id}`
- `POST /ManualOrders/Edit/{id}`
- `POST /Orders/Delete/{id}`

Existing routes remain unchanged.

## UI Changes

- Order details page displays an edit link for eligible orders.
- Order details page displays a delete form for eligible orders.
- Manual order form supports create and edit mode text/action.

## Security Considerations

POST edit and delete actions require the existing write policy and anti-forgery token. Server-side eligibility checks must protect against hidden button bypasses.

## Performance Considerations

Repository methods may initially scan lists similarly to existing services. Filtering should happen in EF queries where practical. No new large data load beyond existing production task listing patterns is required.

## Testing Strategy

### Unit/Domain Tests

- Order replacement preserves id/created time and updates items.
- Unsafe status transitions remain protected.

### Application Tests

- Update planned order quantity recalculates stock.
- Update planned order to unlinked item reverses old stock and removes active tasks.
- Delete planned order reverses stock and removes order.
- Edit/delete are blocked when production is in progress.
- Duplicate origin on edit is rejected.

### View Contract Tests

- Details page exposes edit/delete actions and anti-forgery token.
- Manual order component supports edit action/labels.

## Implementation Tasks

### Task 1 - Extend domain and repositories

Add order replacement, order delete, production task lookup/delete, and stock movement lookup by business reference.

### Task 2 - Add production effect reversal

In `ManualOrderService`, add eligibility validation and reversal of planned task stock consumption through compensating inbound movements.

### Task 3 - Implement edit and delete application flows

Add `GetInputAsync`, `UpdateAsync`, and `DeleteAsync` to `ManualOrderService`; wire controller actions and redirects.

### Task 4 - Update UI

Reuse the manual order form for edit mode and expose edit/delete actions on order details.

### Task 5 - Add tests and validate

Add behavior tests for edit/delete/recalculation and run build plus relevant tests.

## Requirement Traceability

| Requirement | Implementation |
|---|---|
| FR-001 | Tasks 3 and 4 |
| FR-002 | Tasks 1, 3, and 4 |
| FR-003 | Task 3 |
| FR-004 | Task 3 |
| FR-005 | Tasks 1, 2, and 3 |
| FR-006 | Tasks 2 and 3 |
| FR-007 | Tasks 1, 3, and 4 |
| FR-008 | Tasks 1, 2, and 3 |
| FR-009 | Tasks 2 and 3 |
| FR-010 | Task 4 |

## Backward Compatibility

Existing create, import, listing, details, linking, and send-to-production flows remain compatible. New form parameters are optional/defaulted so the manual creation component can still render in create mode.

## Migration Strategy

No database migration is required.

## Risks

### Inventory reversal traceability

The system must avoid deleting historical stock movements. Compensating movements reduce data loss risk and keep balances auditable.

### Started production

Automatic reversal after production starts would be ambiguous. The implementation blocks edit/delete for started or completed tasks.

### Shipments

Deleting shipped operational context can orphan shipment records. The implementation blocks edit/delete when shipments exist.

## Assumptions

- Planned production tasks can be removed or cancelled before work starts.
- Replanning failure after edit is acceptable as long as the order is saved and obsolete effects are reversed.
- Manual and imported orders should use the same edit/delete mechanics.

## Open Technical Questions

None blocking.
