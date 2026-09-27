# Feature: Cypress E2E Product Catalog

## Summary

Add Cypress end-to-end coverage for product catalog workflows in the ASP.NET MVC application.

The tests must validate product listing, product create/edit, bill of materials maintenance, product mappings, validation behavior, and read-only authorization through the real browser-facing UI using deterministic E2E data.

## Context

The repository already contains the Cypress E2E foundation under `tests/e2e`, including database reset, E2E users, programmatic login helpers, authentication coverage, order coverage, and manual order coverage.

Products are exposed through `ProductsController` and Razor UI:

- `/Products` lists products and links to create, edit, bill of materials, and mappings.
- `/Products/Create` renders the product form.
- `/Products/Edit/{id}` renders the same form for editing an existing product.
- `/Products/BillOfMaterials/{id}` renders bill of materials rows and accepts dynamic row additions.
- `/Products/Mappings` displays and creates external item mappings.

Products are used by manual orders, imported orders, production planning, and inventory consumption, so browser-level coverage protects workflows that depend on catalog correctness.

## Problem

Lower-level tests cover pieces of product catalog behavior, but there is no browser-level verification that product forms, dynamic bill of materials rows, model binding, anti-forgery-protected posts, role restrictions, mappings, selectors, and persisted effects work together.

Regressions in product form fields, bill of materials integer binding, mapping creation, listing display, or write authorization could pass lower-level tests while breaking real user workflows.

## Goals

- Verify a write-authorized user can create a product.
- Verify a write-authorized user can edit an existing product.
- Verify product listing displays seeded and newly saved product data.
- Verify invalid product form submissions remain on the form with validation feedback.
- Verify a write-authorized user can save bill of materials rows using existing raw materials.
- Verify bill of materials dynamic row addition binds submitted rows correctly.
- Verify bill of materials quantities reject decimal values and accept positive whole numbers.
- Verify a write-authorized user can create an external product mapping.
- Verify a `Consulta` user cannot create, edit, save bill of materials, or create mappings.

## Non-Goals

This feature does not include:

- exhaustive validation of every invalid product field combination;
- direct testing of production planning internals;
- direct testing of manual order, import, inventory, shipment, or finance workflows;
- changing the visible product catalog user experience;
- replacing existing domain, application, repository, or MVC tests.

## Actors

### Operador

Can read protected pages and execute operational write actions such as creating and editing products, saving bill of materials, and creating mappings.

### Admin

Can perform the same product catalog workflows as `Operador`.

### Consulta

Can read protected product pages but must not create products, edit products, save bill of materials, or create mappings.

## Preconditions

- The application runs with `ASPNETCORE_ENVIRONMENT=E2E`.
- Cypress can reset the E2E database before each test.
- E2E users for `Admin`, `Operador`, and `Consulta` exist.
- Scenario-specific products and raw materials can be seeded deterministically.
- Successful mutation tests exercise real MVC forms with anti-forgery tokens.

## Functional Requirements

### FR-001 - Product listing

The product listing must show seeded and newly created products with name, status, production duration, hourly rate, and bill of materials count.

### FR-002 - Create product

A write-authorized user must be able to create a product with name, status, production duration, and hourly rate.

### FR-003 - Edit product

A write-authorized user must be able to edit an existing product and see the updated data in the product listing.

### FR-004 - Product validation

Invalid product forms must remain on the product form and display validation feedback without redirecting to the listing.

### FR-005 - Bill of materials

A write-authorized user must be able to save bill of materials rows for an existing product using existing raw materials.

### FR-006 - Dynamic bill of materials rows

When bill of materials rows are added dynamically in the browser, the submitted rows must bind correctly and be reflected in the saved bill of materials count.

### FR-007 - Bill of materials integer quantities

Bill of materials quantities must reject decimal values and accept positive whole numbers.

### FR-008 - Product mappings

A write-authorized user must be able to map an external source and external item id to an internal product.

### FR-009 - Read-only protection

A `Consulta` user must not be able to create products, edit products, save bill of materials, or create mappings, including when attempting write URLs directly.

### FR-010 - Stable selectors

The UI elements exercised by the product catalog E2E tests must expose the required `data-cy` selectors without changing visible behavior.

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

## User Flows

### Create product

1. Cypress resets the E2E database.
2. Cypress authenticates as `Operador`.
3. User opens `/Products`.
4. User opens the product create form.
5. User fills name, status, production duration, and hourly rate.
6. User submits the form.
7. System redirects to the product listing.
8. Listing shows the created product data.

### Edit product

1. Cypress seeds an existing product.
2. Cypress authenticates as `Operador`.
3. User opens `/Products`.
4. User opens the edit page for the seeded product.
5. User changes product fields.
6. User submits the form.
7. System redirects to the product listing.
8. Listing shows the updated product data.

### Save bill of materials

1. Cypress seeds a product and raw materials.
2. Cypress authenticates as `Operador`.
3. User opens the product bill of materials page.
4. User selects raw materials and enters whole-number quantities.
5. User adds a row dynamically when needed.
6. User submits the form.
7. System redirects to the product listing.
8. Listing shows the expected bill of materials item count.

### Create mapping

1. Cypress seeds a product.
2. Cypress authenticates as `Operador`.
3. User opens `/Products/Mappings`.
4. User fills source, external item id, and product selection.
5. User submits the form.
6. Mapping list shows source, external item id, and product name.

## Scenarios

### Scenario: Create product

Given an `Operador` user is authenticated
When the user creates product `Produto E2E`
Then the product listing must include `Produto E2E`
And the listing must show the submitted status, production duration, and hourly rate.

### Scenario: Edit product

Given a product exists
And an `Operador` user is authenticated
When the user edits the product name, status, production duration, and hourly rate
Then the product listing must show the updated values.

### Scenario: Invalid product form is rejected

Given an `Operador` user is authenticated
When the user submits an invalid product form
Then the system must remain on the product form
And validation feedback must be visible
And the product listing redirect must not occur.

### Scenario: Save bill of materials

Given a product and two raw materials exist
When the user opens the product bill of materials page and saves both materials with whole-number quantities
Then the product listing must show the expected bill of materials item count.

### Scenario: Reject decimal bill of materials quantity

Given a product and raw material exist
When the user submits quantity `1,5` or `1.5`
Then the form must reject the value.

### Scenario: Create mapping

Given a product exists
When the user maps source `Shopee` and external item `SKU-E2E` to the product
Then the mapping list must show the source, external item, and product name.

### Scenario: Read-only user cannot write

Given a `Consulta` user is authenticated
When the user attempts to create a product, edit a product, save bill of materials, or create a mapping
Then the system must reject the operation according to the existing authorization behavior.

## Business Rules

### BR-001 - E2E data is deterministic

Product catalog E2E tests must use deterministic setup data and must not depend on records created by previous tests.

### BR-002 - Browser validates behavior

Tests may prepare state through E2E setup helpers, but must validate product catalog workflows through the browser-visible application.

### BR-003 - Real authorization is exercised

Read-only protection must be verified against server-enforced write restrictions, not only by checking hidden controls.

### BR-004 - Bill of materials quantities are whole numbers

Bill of materials quantities must represent positive whole-number quantities in product catalog E2E workflows.

## Invariants

- Product catalog write actions must remain protected by the existing write policy.
- Product catalog write forms must continue using anti-forgery protection.
- E2E setup helpers must only be available in the `E2E` environment.
- Each Cypress test must be repeatable after database reset.
- Test selectors must not depend on CSS classes, translated labels, or icon markup.
- Adding `data-cy` attributes must not change visible UI behavior.

## Authorization

`Operador` and `Admin` may execute the product catalog write workflows covered by this specification.

`Consulta` may view protected product pages but must not create products, edit products, save bill of materials, or create product mappings.

## Security

Successful mutation tests must submit real MVC forms so anti-forgery behavior remains exercised.

Direct write attempts used for read-only authorization checks must be authenticated as `Consulta` and must verify the server rejects the operation.

E2E seed endpoints, if added, must be mapped only in the `E2E` environment.

## Compatibility

Existing routes must remain valid:

- `/Products`
- `/Products/Create`
- `/Products/Edit/{id}`
- `/Products/BillOfMaterials/{id}`
- `/Products/Mappings`

Adding `data-cy` attributes must not change visible UI behavior.

## Edge Cases

- Product list is empty after reset.
- Product name is missing.
- Production duration is missing, zero, or invalid.
- Hourly rate is missing or invalid.
- Bill of materials page is opened without raw materials.
- Decimal bill of materials quantity is submitted.
- Dynamically added bill of materials rows leave incorrect field indexes.
- Mapping is submitted with missing source, external item id, or product.
- Duplicate mapping is submitted.
- `Consulta` posts directly to write actions.

## Dependencies

This feature depends on:

- existing E2E reset and login helpers;
- existing product MVC routes and Razor views/components;
- existing product catalog application service;
- existing product, raw material, and product mapping persistence;
- existing authorization policies.

## Constraints

- Use Cypress in the existing `tests/e2e` project.
- Reuse existing E2E helpers where possible.
- Do not introduce visible UI changes except as necessary to expose stable selectors.
- Do not use arbitrary sleeps in Cypress tests.
- Do not mock the MVC backend with Cypress intercepts for behavior under test.

## Acceptance Criteria

- Product listing is covered.
- Product create workflow is covered.
- Product edit workflow is covered.
- Product validation failure is covered.
- Bill of materials save with multiple rows is covered.
- Dynamic bill of materials row addition is covered.
- Decimal bill of materials quantity rejection is covered.
- Product mapping creation is covered.
- Read-only write protection is covered.
- Required selectors exist on exercised UI elements.
- The Cypress product catalog spec can run through the existing E2E command.

## Test Considerations

Product catalog Cypress tests should favor complete, high-signal workflows over exhaustive validation permutations.

At minimum, the suite should cover:

- listing seeded products;
- creating a product;
- editing a product;
- invalid product form rejection;
- saving bill of materials with dynamically added rows;
- decimal quantity rejection;
- creating a mapping;
- `Consulta` rejection for product, bill of materials, and mapping write actions.

## Assumptions

- It is acceptable to add an E2E-only product catalog scenario endpoint, guarded by `ASPNETCORE_ENVIRONMENT=E2E`.
- Tests may use real UI forms for successful mutation flows and direct authenticated requests for read-only rejection checks.
- `Operador` is the default write-authorized actor for product catalog workflows.
- Existing lower-level tests remain responsible for exhaustive product domain and validation coverage.

## Open Questions

None blocking.
