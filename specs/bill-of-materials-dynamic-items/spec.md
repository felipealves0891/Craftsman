# Feature: Dynamic Bill of Materials Items

## Summary

Allow users editing a product bill of materials at `/Products/BillOfMaterials/{productId}` to add more material rows beyond the current five visible fields.

The quantity field for each material must accept only whole numbers, with no decimal places.

## Context

The bill of materials screen currently renders five editable material rows for a product. Users cannot add additional rows from the page, even though the existing save flow stores the submitted `Items` collection.

## Problem

Products that require more than five raw materials cannot have their full bill of materials entered through the current screen.

The quantity field currently supports decimal-style input, but the requested workflow requires whole-number quantities only.

## Goals

- Let the user add additional bill of materials rows from the existing screen.
- Ensure added rows are submitted, validated, and saved through the normal bill of materials flow.
- Restrict bill of materials quantities to integer values.
- Preserve the existing route and save behavior for users who need five or fewer rows.

## Non-Goals

This feature does not include:

- removing material rows from the screen;
- changing product or inventory domain calculations outside bill of materials input;
- changing the bill of materials route;
- changing duplicate material validation;
- introducing a new persistence model.

## Actors

### User

Edits a product bill of materials.

### System

Renders the bill of materials form, binds submitted rows, validates them, and saves the product bill of materials.

## Preconditions

- The product exists.
- The user can access `/Products/BillOfMaterials/{productId}`.
- Raw material options are available through the existing application flow.

## Functional Requirements

### FR-001 - Add row control

The bill of materials screen must provide an add control with a plus icon on the last table row.

### FR-002 - Dynamic rows are submitted

When the user adds a row, the new row must be included in the form submission using the same item structure as existing rows.

### FR-003 - Dynamic rows are saved

When a dynamically added row contains a raw material and a valid quantity, the existing save flow must persist it as part of the product bill of materials.

### FR-004 - Existing validation applies

Dynamically added rows must follow the same required material, positive quantity, and duplicate material validation as the existing rows.

### FR-005 - Integer quantities

Bill of materials quantity fields must accept and validate only integer quantities with no decimal places.

### FR-006 - Existing route compatibility

The existing `/Products/BillOfMaterials/{productId}` route must remain valid.

## Business Rules

### BR-001 - At least one bill of materials item

Saving a bill of materials must continue to require at least one valid item.

### BR-002 - No duplicate raw materials

Saving a bill of materials must continue to reject duplicate raw materials.

### BR-003 - Positive quantities

Saved bill of materials item quantities must be greater than zero.

## Invariants

- The feature must not bypass server-side validation.
- The feature must not rely on client-side behavior as the only validation layer.
- Existing products with five or fewer bill of materials items must keep working.
- Existing persisted bill of materials items must remain readable.

## User Flow

1. User opens `/Products/BillOfMaterials/{productId}`.
2. System displays existing bill of materials rows and empty rows up to the default initial set.
3. User clicks the plus icon on the last row.
4. System appends one empty bill of materials row.
5. User fills material and quantity.
6. User saves the form.
7. System validates and saves all valid submitted rows through the existing bill of materials save flow.

## Validation

- Empty rows are ignored as they are today.
- A row with material but no positive quantity is invalid.
- A row with quantity but no material is invalid.
- Quantity values must be positive whole numbers.
- Decimal quantity values such as `1.5` or `1,5` must be rejected.

## Security

The submitted rows are user-controlled input. Server-side validation must remain authoritative.

## Compatibility

- The existing GET and POST bill of materials route behavior must remain compatible.
- No database schema change is expected.
- Existing rows with decimal persisted quantities, if present, must still load without breaking the screen.

## Edge Cases

- Product currently has no bill of materials items.
- Product currently has exactly five bill of materials items.
- Product currently has more than five bill of materials items.
- User adds one extra row.
- User adds multiple extra rows.
- User submits an added row partially filled.
- User submits a decimal quantity.

## Acceptance Criteria

- The bill of materials table exposes a plus-icon add button on the last row.
- Clicking the add button appends a new material row with correctly indexed field names.
- Added rows participate in normal model binding and save behavior.
- The service can save more than five bill of materials items.
- The quantity field is rendered and validated as integer-only input.
- Decimal quantities are rejected server-side.
- Existing route `/Products/BillOfMaterials/{productId}` remains unchanged.
- Relevant automated tests pass.

## Open Questions

None.
