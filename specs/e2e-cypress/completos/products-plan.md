# Implementation Plan: Cypress E2E Product Catalog

## Summary

Implement the product-catalog-specific Cypress E2E coverage defined by `specs/e2e-cypress/completos/products-spec.md` using the existing E2E foundation.

The implementation will add stable selectors to product catalog UI, add deterministic E2E seed support for product scenarios, add a Cypress support command, and create a Cypress spec that covers listing, create, edit, validation, bill of materials, mappings, and read-only protection.

## Specification Reference

- `specs/e2e-cypress/spec.md`
- `specs/e2e-cypress/products.md`
- `specs/e2e-cypress/completos/products-spec.md`

## Current Architecture

The E2E foundation already exists:

- `tests/e2e/cypress.config.js`
- `tests/e2e/cypress/support/commands.js`
- `tests/e2e/cypress/e2e/authentication.cy.js`
- `tests/e2e/cypress/e2e/orders.cy.js`
- `tests/e2e/cypress/e2e/manual-orders.cy.js`
- `tests/e2e/scripts/run-e2e.js`
- `src/App/E2E/E2EEndpoints.cs`
- `src/App/E2E/E2EDatabaseResetter.cs`
- `src/App/E2E/E2EIdentitySeeder.cs`
- `src/App/E2E/E2EOrderScenarioSeeder.cs`
- `src/App/E2E/E2EManualOrderScenarioSeeder.cs`

Existing Cypress commands include:

- `cy.getByCy(selector)`
- `cy.resetE2E()`
- `cy.loginAs(role)`
- `cy.loginThroughUi(email, password)`
- `cy.seedOrdersScenario(scenario)`
- `cy.seedManualOrdersScenario(scenario)`

Product catalog application flow currently uses:

- `ProductsController` for listing, create/edit, bill of materials, and mappings.
- `ProductCatalogAppService` for product, bill of materials, raw material option, and mapping behavior.
- `ProductsIndexPage.razor` for the static product listing component.
- `Views/Products/Edit.cshtml` for create and edit.
- `Views/Products/BillOfMaterials.cshtml` for bill of materials and dynamic row JavaScript.
- `Views/Products/Mappings.cshtml` for product mappings.
- `ManualCatalogModels.cs` for product input, bill of materials input, and mapping input models.

Current gaps for this slice:

- Product views/components lack the required product `data-cy` selectors.
- Cypress support commands do not expose product catalog scenario seeding.
- E2E scenario seeding exists for orders and manual orders, but not for focused product catalog setup.
- There is no Cypress spec dedicated to product catalog workflows.

## Change Surface

### Direct Impact

- `src/UI/Components/Products/ProductsIndexPage.razor`
- `src/App/Views/Products/Edit.cshtml`
- `src/App/Views/Products/BillOfMaterials.cshtml`
- `src/App/Views/Products/Mappings.cshtml`
- `src/App/E2E/E2EEndpoints.cs`
- `src/App/Program.cs`
- new or extended E2E seed support under `src/App/E2E`
- `tests/e2e/cypress/support/commands.js`
- new `tests/e2e/cypress/e2e/products.cy.js`

### Indirect Impact

- E2E database records for products, raw materials, bill of materials, and mappings.
- Existing anti-forgery-protected MVC forms.
- Existing authorization policies on product catalog write actions.
- Product catalog cache invalidation through `ProductRepository`.

## Implementation Strategy

Reuse the existing E2E reset/login infrastructure and add product catalog scenario setup through an E2E-only endpoint.

The seed support should create focused scenarios by name rather than exposing broad arbitrary database writes to Cypress. Each scenario should assume a reset database, seed the minimal supporting product/raw material/mapping data, and return identifiers Cypress needs to navigate to product edit, bill of materials, and mappings pages.

Successful write flows should use rendered UI forms so anti-forgery behavior remains exercised. Read-only authorization checks may use direct authenticated `cy.request` calls to write endpoints because the objective is server-enforced rejection.

Selectors should be added directly to the Razor component/views without changing visible text or layout.

## Data Flow

```text
Cypress test
    -> cy.resetE2E()
    -> cy.seedProductsScenario("catalog")
    -> cy.loginAs("Operador")
    -> cy.visit("/Products")
    -> use rendered MVC/Razor forms
    -> ProductsController + anti-forgery + ProductCatalogAppService
    -> database effect
    -> redirected/rendered UI assertion on listing or mappings
```

## Existing Components to Reuse

- `cy.resetE2E()` and `cy.loginAs(...)`.
- Existing `ProductsController` routes and forms.
- Existing `ProductCatalogAppService` validation and persistence behavior.
- Existing product, raw material, bill of materials, and product mapping persistence entities.
- Existing `E2EOrderScenarioSeeder` and `E2EManualOrderScenarioSeeder` patterns for scenario endpoint shape, environment guard, deterministic ids, and response models.

## New Components

### E2E product catalog scenario seeder

Create a focused E2E-only service that seeds named product catalog scenarios and returns identifiers.

Expected scenarios:

- `listing`: at least one product with bill of materials data so the listing can verify seeded display fields.
- `editable-product`: an existing product that can be edited.
- `bom-support`: one product and at least two active raw materials for bill of materials tests.
- `mapping-support`: one product available for mapping.
- `validation-support`: minimal setup needed to open product create/edit pages and submit invalid product data.
- `readonly-support`: one product and raw material setup for direct `Consulta` write checks.

### E2E seed endpoint

Extend `/__e2e` with a guarded product catalog scenario endpoint, for example:

- `POST /__e2e/products/scenario`

The endpoint should accept a scenario name and return scenario metadata, including product ids, product names, raw material ids/names, and mapping support values where useful.

### Cypress product catalog command

Add a helper command:

- `cy.seedProductsScenario(name)`

Keep scenario setup readable and consistent with `cy.seedOrdersScenario(...)` and `cy.seedManualOrdersScenario(...)`.

## Files to Modify

### `src/UI/Components/Products/ProductsIndexPage.razor`

Add:

- `data-cy="products-list"` on the product table or table container;
- `data-cy="product-create"` on the create-product link;
- `data-cy="product-row"` on each product row;
- `data-cy="product-edit"` on each edit link;
- `data-cy="product-bom"` on each bill of materials link.

Repeated row selectors should be usable with Cypress row scoping through `.parents('[data-cy="product-row"]')` or `.within(...)`.

### `src/App/Views/Products/Edit.cshtml`

Add:

- `data-cy="product-name"` on the name input;
- `data-cy="product-status"` on the status select;
- `data-cy="product-production-duration"` on the production duration input;
- `data-cy="product-hourly-rate"` on the hourly rate input;
- `data-cy="product-save"` on the submit button.

Do not change form action, visible labels, validation behavior, or layout.

### `src/App/Views/Products/BillOfMaterials.cshtml`

Add:

- `data-cy="bom-add-row"` on the dynamic add-row button;
- `data-cy="bom-material"` on each raw material select;
- `data-cy="bom-quantity"` on each quantity input;
- `data-cy="bom-save"` on the submit button.

Keep the existing dynamic row JavaScript behavior. If cloned rows preserve `data-cy` attributes, Cypress can use `.eq(index)` or row scoping.

### `src/App/Views/Products/Mappings.cshtml`

Add:

- `data-cy="mapping-source"` on the source input;
- `data-cy="mapping-external-item"` on the external item input;
- `data-cy="mapping-product"` on the product select;
- `data-cy="mapping-save"` on the submit button.

If mapping-list row scoping becomes necessary, add a stable selector to mapping rows, but this is not required by the current specification.

### `src/App/E2E/E2EEndpoints.cs`

Map the product catalog scenario seed endpoint under `/__e2e`.

Keep the endpoint available only through the existing E2E endpoint mapping.

### `src/App/Program.cs`

Register the new product catalog scenario seeder in dependency injection.

### `tests/e2e/cypress/support/commands.js`

Add a Cypress command for product catalog scenario seeding.

### `tests/e2e/cypress/e2e/products.cy.js`

Add the product catalog E2E tests.

## Files to Create

### `src/App/E2E/E2EProductCatalogScenarioSeeder.cs`

Seeds deterministic scenario data for product catalog Cypress tests.

The service should use `AppDbContext` consistently with the current E2E setup style. Prefer direct deterministic persistence for setup because test setup is below the behavior under test and must be fast and explicit.

### Optional request/response models

If the endpoint would otherwise use anonymous shapes repeatedly, create small models such as:

- `E2EProductCatalogScenarioRequest`
- `E2EProductCatalogScenarioResponse`

## Files to Remove

None.

## Persistence Changes

No schema changes are required.

The E2E product catalog seeder will insert existing entities into the isolated E2E database after reset.

## API Contract Changes

Production contracts are unchanged.

New E2E-only test contract:

- `POST /__e2e/products/scenario`

This endpoint must not be available outside `ASPNETCORE_ENVIRONMENT=E2E`.

## UI Changes

Only invisible `data-cy` attributes are expected.

No visible text, layout, or workflow changes should be introduced by this slice.

## Configuration Changes

No configuration changes are expected.

## Dependencies

No new NuGet packages are expected.

No new npm packages are expected.

## Security Considerations

- E2E seed endpoints must remain E2E-only.
- Successful create/edit/bill-of-materials/mapping tests should submit real UI forms so anti-forgery tokens are exercised.
- Read-only write protection must verify server rejection through direct write attempts or UI attempts that reach the server.
- Do not weaken production authorization or anti-forgery behavior to make tests pass.

## Performance Considerations

Keep product catalog scenario seeds small and explicit.

Avoid seeding the full development dataset in every test.

## Observability

Use existing Cypress screenshots/artifacts for failures.

No new application logging is required.

## Testing Strategy

### Cypress Product Catalog Tests

Add `products.cy.js` with scenarios:

- list seeded products and verify status, duration, hourly rate, and bill of materials count;
- create a product through `/Products/Create` and verify it appears in `/Products`;
- edit an existing product and verify listing reflects updated values;
- submit invalid product data and verify the form remains visible with validation feedback;
- save bill of materials for an existing product using two raw materials, including a dynamically added row;
- submit a decimal bill of materials quantity and verify browser/model validation rejects it;
- create a product mapping and verify the mapping list shows source, external item id, and product name;
- verify `Consulta` is rejected from create product, edit product, save bill of materials, and create mapping write attempts.

### Existing .NET Tests

No new .NET behavior tests are required for Cypress-only assertions unless the seed service contains non-trivial setup logic that merits focused coverage.

Run the existing build/test suite after implementation if feasible.

### E2E Validation

Run the product catalog Cypress spec through the existing E2E command. If targeted execution is needed during development, run Cypress with the product spec path while the E2E app is started through the existing orchestration.

## Implementation Tasks

### Task 1 - Add product UI selectors

Affected:

- `ProductsIndexPage.razor`
- `Views/Products/Edit.cshtml`
- `Views/Products/BillOfMaterials.cshtml`
- `Views/Products/Mappings.cshtml`

Changes:

- add all required `data-cy` selectors to exercised elements;
- preserve visible UI.

Validation:

- product listing, create/edit form, bill of materials form, and mappings form render selectors required by the spec.

Dependencies:

None.

### Task 2 - Add E2E product catalog scenario seeding

Affected:

- new `E2EProductCatalogScenarioSeeder`;
- `E2EEndpoints.cs`;
- `Program.cs`;
- optional request/response models.

Changes:

- seed minimal deterministic product catalog scenarios;
- return product/raw material/mapping metadata Cypress needs;
- guard setup through existing E2E endpoint structure.

Validation:

- after reset, each scenario endpoint creates only the data needed and returns expected ids/names.

Dependencies:

None.

### Task 3 - Add Cypress product setup command

Affected:

- `tests/e2e/cypress/support/commands.js`

Changes:

- add `cy.seedProductsScenario(name)`;
- return response body to tests.

Validation:

- command can seed a scenario after `cy.resetE2E()`.

Dependencies:

Task 2.

### Task 4 - Implement listing, create, and edit Cypress coverage

Affected:

- new `products.cy.js`

Changes:

- verify seeded product listing display fields;
- create a product through the real form;
- edit an existing product through the real form.

Validation:

- `/Products` shows seeded, created, and edited product data with expected values.

Dependencies:

Tasks 1 through 3.

### Task 5 - Implement validation Cypress coverage

Affected:

- `products.cy.js`

Changes:

- submit invalid product form data;
- assert the browser remains on the form and validation feedback is present.

Validation:

- invalid submission does not redirect to `/Products`;
- invalid fields expose validation failure.

Dependencies:

Tasks 1 through 3.

### Task 6 - Implement bill of materials Cypress coverage

Affected:

- `products.cy.js`

Changes:

- seed product and raw materials;
- save two bill of materials rows, including a dynamically added row;
- verify product listing shows expected bill of materials count.

Validation:

- submitted rows bind correctly;
- listing shows `2 item(ns)` for the product.

Dependencies:

Tasks 1 through 3.

### Task 7 - Implement decimal quantity rejection coverage

Affected:

- `products.cy.js`

Changes:

- enter decimal bill of materials quantity such as `1,5` or `1.5`;
- assert the field is invalid or the server-side form remains on the bill of materials page with validation feedback.

Validation:

- decimal quantity is rejected;
- no successful redirect to `/Products` occurs for invalid quantity.

Dependencies:

Tasks 1 through 3.

### Task 8 - Implement product mapping Cypress coverage

Affected:

- `products.cy.js`

Changes:

- seed a product;
- create a mapping through `/Products/Mappings`;
- verify mapping list shows source, external item id, and product name.

Validation:

- mapping persists and is visible after redirect/reload.

Dependencies:

Tasks 1 through 3.

### Task 9 - Implement read-only coverage

Affected:

- `products.cy.js`

Changes:

- authenticate as `Consulta`;
- verify direct write attempts are rejected for create/edit product, bill of materials, and mappings.

Validation:

- write requests redirect to access denied or otherwise match existing authorization behavior.

Dependencies:

Tasks 1 through 3.

### Task 10 - Validate implementation

Affected:

- repository build/test and Cypress execution.

Changes:

- run relevant .NET build/tests;
- run the product catalog Cypress spec or full E2E command.

Validation:

- build succeeds;
- existing tests remain green or deviations are documented;
- product catalog Cypress spec passes.

Dependencies:

Tasks 1 through 9.

## Requirement Traceability

| Requirement | Implementation |
|---|---|
| `products-spec.md` FR-001 | Tasks 1, 2, 3, and 4 |
| `products-spec.md` FR-002 | Tasks 1, 3, and 4 |
| `products-spec.md` FR-003 | Tasks 1, 2, 3, and 4 |
| `products-spec.md` FR-004 | Tasks 1, 3, and 5 |
| `products-spec.md` FR-005 | Tasks 1, 2, 3, and 6 |
| `products-spec.md` FR-006 | Tasks 1, 2, 3, and 6 |
| `products-spec.md` FR-007 | Tasks 1, 2, 3, and 7 |
| `products-spec.md` FR-008 | Tasks 1, 2, 3, and 8 |
| `products-spec.md` FR-009 | Tasks 2, 3, and 9 |
| `products-spec.md` FR-010 | Task 1 |
| `spec.md` FR-003 | Tasks 2 and 3 |
| `spec.md` FR-004 | Tasks 2 and 3 |
| `spec.md` FR-005 | Existing E2E command plus Task 3 |
| `spec.md` FR-006 | Task 1 |
| `spec.md` FR-007 | Task 9 |
| `spec.md` FR-008 | Tasks 4 through 9 |

## Backward Compatibility

Existing application behavior and routes remain unchanged.

The only UI changes are stable test attributes.

E2E-only endpoints are unavailable outside the E2E environment.

## Migration Strategy

No database migration is required.

## Risks

### Bill of materials dynamic row binding

The bill of materials page clones existing rows and rewrites field names and ids.

Mitigation:

- keep the current JavaScript behavior intact;
- scope Cypress interactions by repeated selectors with `.eq(index)`;
- verify persisted count on `/Products` after saving.

### Browser validation differs by input type

Decimal rejection may be enforced by pattern/input validity before server model binding.

Mitigation:

- assert field invalidity where the browser prevents submit;
- otherwise assert the server returns to the form with validation feedback.

### Scenario seed drift

Direct E2E seed data can drift from domain invariants.

Mitigation:

- keep scenarios small;
- use existing entity shapes and current persistence configuration;
- validate through the real UI behavior after seeding.

### Product catalog cache

Product listing uses repository cache when configured.

Mitigation:

- preserve existing E2E cache behavior;
- rely on repository cache invalidation for UI-driven writes;
- ensure scenario seeding happens immediately after `cy.resetE2E()` and before product listing assertions.

## Assumptions

- The existing E2E foundation is accepted as the base for this slice.
- Direct scenario seeding through E2E-only endpoints is acceptable because Cypress validates behavior above that setup layer.
- `Operador` is the default write-authorized actor for product catalog workflows.
- `Consulta` direct write checks may use `cy.request` with `failOnStatusCode: false`.
- Product catalog tests can assert formatted values as rendered by the current `pt-BR` request culture.

## Open Technical Questions

None blocking.
