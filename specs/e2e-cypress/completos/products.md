# Feature: E2E Product Catalog

## Summary

Validate product catalog workflows including product creation/editing, bill of materials maintenance, and external item mappings.

## Context

`ProductsController` supports product listing, create/edit, bill of materials, and mappings. Product workflows depend on raw materials for bill of materials and are later used by manual orders and production planning.

## Goals

- Verify users can create and edit products.
- Verify product list reflects saved product data.
- Verify bill of materials rows can be saved, including dynamically added rows.
- Verify product mappings can be created.
- Verify read-only users cannot mutate catalog data.

## Non-Goals

This feature does not test production planning internals or every product validation rule through Cypress.

## Functional Requirements

### FR-001 - Product listing

The product listing must show seeded and newly created products with status, production duration, hourly rate, and bill of materials count.

### FR-002 - Create product

A write-authorized user must be able to create a product with name, status, production duration, and hourly rate.

### FR-003 - Edit product

A write-authorized user must be able to edit an existing product and see the updated data in the listing.

### FR-004 - Product validation

Invalid product forms must remain on the form and display validation feedback.

### FR-005 - Bill of materials

A write-authorized user must be able to save bill of materials rows for an existing product using existing raw materials.

### FR-006 - Bill of materials integer quantities

Bill of materials quantities must reject decimal values and accept positive whole numbers.

### FR-007 - Product mappings

A write-authorized user must be able to map an external source/item id to an internal product.

### FR-008 - Read-only protection

A `Consulta` user must not be able to create, edit, save bill of materials, or create mappings.

## Scenarios

### Scenario: Create product

Given an `Operador` user is authenticated
When the user creates product "Produto E2E"
Then the product listing must include "Produto E2E".

### Scenario: Save bill of materials

Given a product and two raw materials exist
When the user opens the product bill of materials and saves both materials with whole-number quantities
Then the product listing must show the expected bill of materials item count.

### Scenario: Reject decimal bill of materials quantity

Given a product and raw material exist
When the user submits quantity `1,5` or `1.5`
Then the form must reject the value.

### Scenario: Create mapping

Given a product exists
When the user maps source "Shopee" and external item "SKU-E2E" to the product
Then the mapping list must show the source, external item, and product name.

## Acceptance Criteria

- Product create, edit, bill of materials, and mappings workflows pass through the browser.
- Validation failure keeps user input visible.
- Read-only users cannot complete write operations.
- All exercised controls have stable `data-cy` selectors.

## Required Selectors

- `data-cy="products-list"`
- `data-cy="product-create"`
- `data-cy="product-name"`
- `data-cy="product-status"`
- `data-cy="product-production-duration"`
- `data-cy="product-hourly-rate"`
- `data-cy="product-save"`
- `data-cy="product-row"`
- `data-cy="product-edit"`
- `data-cy="product-bom"`
- `data-cy="bom-add-row"`
- `data-cy="bom-material"`
- `data-cy="bom-quantity"`
- `data-cy="bom-save"`
- `data-cy="mapping-source"`
- `data-cy="mapping-external-item"`
- `data-cy="mapping-product"`
- `data-cy="mapping-save"`
