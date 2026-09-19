# Implementation Plan: Dynamic Bill of Materials Items

## Summary

Extend the existing bill of materials form instead of introducing a new endpoint. The view will add client-side row insertion with sequential `Items[index]` field names, and the input model/service will enforce integer quantities server-side.

## Specification Reference

`specs/bill-of-materials-dynamic-items/spec.md`

## Current Architecture

The flow is:

`ProductsController.BillOfMaterials`
-> `ProductCatalogAppService.GetBillOfMaterialsInputAsync`
-> `BillOfMaterialsInputModel`
-> `Views/Products/BillOfMaterials.cshtml`
-> `ProductsController.BillOfMaterials` POST
-> `ProductCatalogAppService.SaveBillOfMaterialsAsync`
-> `Product.ReplaceBillOfMaterials`
-> repository/unit of work.

The existing domain and repository already support an arbitrary number of submitted bill of materials items.

## Change Surface

- `src/App/Models/ManualCatalogModels.cs`
- `src/App/Services/ProductCatalogAppService.cs`
- `src/App/Views/Products/BillOfMaterials.cshtml`
- `tests/Craftsman.Tests/Application/UsabilityTests.cs`
- `tests/Craftsman.Tests/Application/ManualOperationsTests.cs`

## Implementation Strategy

Keep the initial five rows for compatibility, but remove the functional limit by adding a button in the last table row that appends another row.

Use existing MVC model binding conventions by generating `Items[index].RawMaterialId` and `Items[index].QuantityPerUnit` names for appended rows. Server-side validation will treat `QuantityPerUnit` as an `int`, preserving the domain's decimal quantity storage by converting the integer to the existing `BillOfMaterialsItem` decimal constructor argument.

## Files to Modify

### `src/App/Models/ManualCatalogModels.cs`

Change bill of materials item quantity input from `decimal` to `int` and apply a positive integer range.

### `src/App/Services/ProductCatalogAppService.cs`

Keep loading existing items into the input model. Update empty-row filtering and save mapping to use the integer input. Retain the existing default of at least five rows.

### `src/App/Views/Products/BillOfMaterials.cshtml`

Render integer quantity inputs and a plus-icon add button in the last row. Add narrowly scoped JavaScript that appends rows with correct indexes and keeps the add button on the last row.

### Tests

Update view assertions for integer input behavior and add service coverage for saving more than five bill of materials items and rejecting decimal input at the model/service validation boundary where practical.

## Files to Create

None.

## Persistence Changes

No database schema changes are required. Existing domain quantities remain decimal-capable, but this UI input path restricts new submitted bill of materials quantities to integers.

## API Contract Changes

The route remains `/Products/BillOfMaterials/{productId}` for GET and POST. The form field names remain under `Items[index]`.

## UI Changes

- Add a compact button with a plus icon to the last bill of materials row.
- Newly added rows contain the same material select and quantity input fields.
- Quantity inputs use integer-oriented browser hints.

## Security Considerations

Client-side row insertion is convenience only. Server-side model binding and validation remain authoritative.

## Testing Strategy

- View/content tests verify the add-row button, row template behavior, and integer input attributes.
- Application service tests verify saving more than five bill of materials items.
- Existing duplicate and domain validation tests continue covering invalid materials and duplicate rows.
- Build and relevant tests validate the implementation.

## Implementation Tasks

### Task 1 - Update input model quantity semantics

Change `BillOfMaterialsItemInputModel.QuantityPerUnit` to integer input and positive-range validation.

### Task 2 - Update bill of materials save/load flow

Adapt the service to map integer quantities to domain items while preserving the existing empty-row and five-default-row behavior.

### Task 3 - Update Razor view

Render integer fields, add the plus button on the last row, and implement row insertion with sequential field indexes.

### Task 4 - Update tests

Add and update tests for dynamic rows, integer input, and more-than-five save behavior.

### Task 5 - Validate

Run `dotnet build` and relevant tests.

## Requirement Traceability

| Requirement | Implementation |
|---|---|
| FR-001 | Task 3 |
| FR-002 | Task 3 |
| FR-003 | Tasks 2 and 4 |
| FR-004 | Tasks 1, 2 and existing domain validation |
| FR-005 | Tasks 1, 3 and 4 |
| FR-006 | Task 3 and validation |
| BR-001 | Existing domain validation, Tasks 2 and 4 |
| BR-002 | Existing domain validation |
| BR-003 | Tasks 1 and 2 |

## Backward Compatibility

Existing route and form structure remain compatible. Existing products with zero, five, or more than five bill of materials items continue to render.

## Risks

- Incorrect dynamic field indexes would cause appended rows not to bind.
- Client-side validation may not automatically attach to appended fields, so server-side validation must remain sufficient.

## Open Technical Questions

None.
