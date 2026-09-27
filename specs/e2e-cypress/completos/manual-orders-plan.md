# Implementation Plan: Cypress E2E Manual Orders

## Summary

Implement the manual-order-specific Cypress E2E coverage defined by `specs/e2e-cypress/manual-orders-spec.md` using the existing E2E foundation.

The implementation will add stable selectors to manual order UI components, add deterministic E2E seed support for manual order scenarios, and create a Cypress spec that covers source creation, order creation, dynamic rows, totals, editing, product linking, invalid submissions, and read-only protection.

## Specification Reference

- `specs/e2e-cypress/spec.md`
- `specs/e2e-cypress/manual-orders.md`
- `specs/e2e-cypress/manual-orders-spec.md`

## Current Architecture

The E2E foundation already exists:

- `tests/e2e/cypress.config.js`
- `tests/e2e/cypress/support/commands.js`
- `tests/e2e/cypress/e2e/authentication.cy.js`
- `tests/e2e/cypress/e2e/orders.cy.js`
- `src/App/E2E/E2EEndpoints.cs`
- `src/App/E2E/E2EDatabaseResetter.cs`
- `src/App/E2E/E2EIdentitySeeder.cs`
- `src/App/E2E/E2EOrderScenarioSeeder.cs`

Existing Cypress commands include:

- `cy.getByCy(selector)`
- `cy.resetE2E()`
- `cy.loginAs(role)`
- `cy.loginThroughUi(email, password)`
- `cy.seedOrdersScenario(scenario)`

Manual order application flow currently uses:

- `ManualOrdersController` for create, edit, source creation, link-items page, and link-item post.
- `ManualOrderService` for create/update/source/link behavior.
- `OrderQueryService` for order details used by link-items and details assertions.
- `ManualOrderCreatePage.razor` for the create/edit form and dynamic item JavaScript.
- `ManualOrderLinkItemsPage.razor` for linking order items to products.
- `OrderDetailsPage.razor` for post-create/edit/link verification.

Current gaps for this slice:

- `ManualOrderCreatePage.razor` lacks the required manual order `data-cy` selectors.
- `ManualOrderLinkItemsPage.razor` lacks the required link-product selectors.
- E2E scenario seeding currently supports order scenarios but does not provide focused manual order setup for editable/unlinked/manual form scenarios.
- Cypress support commands do not yet expose manual order scenario seeding.
- There is no Cypress spec dedicated to manual order workflows.

## Change Surface

### Direct Impact

- `src/UI/Components/ManualOrders/ManualOrderCreatePage.razor`
- `src/UI/Components/ManualOrders/ManualOrderLinkItemsPage.razor`
- `src/App/E2E/E2EEndpoints.cs`
- new or extended E2E seed support under `src/App/E2E`
- `tests/e2e/cypress/support/commands.js`
- new `tests/e2e/cypress/e2e/manual-orders.cy.js`

### Indirect Impact

- E2E database records for products, raw materials, bill of materials, order sources, orders, and order items.
- Existing anti-forgery-protected MVC forms.
- Existing order details selectors used as post-action assertions.
- Existing authorization policies on manual order write actions.

## Implementation Strategy

Reuse the existing E2E reset/login infrastructure and add manual-order scenario setup through E2E-only endpoints.

The seed support should create focused scenarios by name rather than exposing broad arbitrary database writes to Cypress. Each scenario should assume a reset database, seed the minimal supporting products/order sources/orders, and return identifiers Cypress needs to navigate to create, edit, details, or link-items pages.

Successful write flows should use rendered UI forms so anti-forgery behavior remains exercised. Read-only authorization checks may use direct authenticated `cy.request` calls to write endpoints because the objective is server-enforced rejection.

Selectors should be added directly to the static Razor components without changing visible text or layout.

## Data Flow

```text
Cypress test
    -> cy.resetE2E()
    -> cy.seedManualOrdersScenario("catalog")
    -> cy.loginAs("Operador")
    -> cy.visit("/ManualOrders/Create")
    -> fill and submit MVC form
    -> ManualOrdersController + anti-forgery + ManualOrderService
    -> database effect
    -> redirected/rendered UI assertion on order details
```

## Existing Components to Reuse

- `cy.resetE2E()` and `cy.loginAs(...)`.
- Existing `ManualOrdersController` routes and forms.
- Existing `ManualOrderService` validation and persistence behavior.
- Existing product/order/order-item persistence entities.
- Existing `OrderDetailsPage.razor` selectors:
  - `order-details`
  - `order-edit`
  - `order-link-items`
  - `flash-success`
  - `flash-error`
- Existing E2E `E2EOrderScenarioSeeder` catalog helpers may be reused or refactored only if it reduces duplication without broadening scope.

## New Components

### E2E manual order scenario seeder

Create a focused E2E-only service that seeds named manual order scenarios and returns identifiers.

Expected scenarios:

- `catalog`: at least one active product, supporting raw material, bill of materials, stock if useful, and optionally a baseline source.
- `editable-order`: an editable manual order with at least one item.
- `unlinked-order`: an order with an unlinked item and an active product available for linking.
- `validation-support`: supporting data needed to open the form and submit invalid data.
- `readonly-support`: an existing order/product setup for direct read-only write checks.

### E2E seed endpoint

Extend `/__e2e` with a guarded manual order scenario endpoint, for example:

- `POST /__e2e/manual-orders/scenario`

The endpoint should accept a scenario name and return scenario metadata, including order ids, item ids, product ids, product names, source names, and references where useful.

### Cypress manual order command

Add a helper command:

- `cy.seedManualOrdersScenario(name)`

Keep scenario setup readable and consistent with `cy.seedOrdersScenario(...)`.

## Files to Modify

### `src/UI/Components/ManualOrders/ManualOrderCreatePage.razor`

Add:

- `data-cy="order-source-create-name"` on the new-source input;
- `data-cy="order-source-create-submit"` on the new-source submit button;
- `data-cy="manual-order-source"` on the source select;
- `data-cy="manual-order-reference"` on the reference input;
- `data-cy="manual-order-shipping-date"` on the shipping date input;
- `data-cy="manual-order-add-item"` on the add-item button;
- `data-cy="manual-order-remove-item"` on each remove-item button;
- `data-cy="manual-order-item-product"` on each product select;
- `data-cy="manual-order-item-description"` on each item description field;
- `data-cy="manual-order-item-quantity"` on each quantity input;
- `data-cy="manual-order-item-unit-price"` on each unit price input;
- `data-cy="manual-order-item-total"` on each item total display;
- `data-cy="manual-order-total"` on the order total display;
- `data-cy="manual-order-save"` on the create/edit submit button.

Selectors on repeated item rows should be usable with Cypress `.eq(index)` or row scoping.

### `src/UI/Components/ManualOrders/ManualOrderLinkItemsPage.razor`

Add:

- stable row scoping if needed for each item row;
- `data-cy="manual-order-link-product"` on each product select used for linking;
- `data-cy="manual-order-link-submit"` on each link submit button.

### `src/App/E2E/E2EEndpoints.cs`

Map the manual order scenario seed endpoint under `/__e2e`.

Keep the endpoint available only through the existing E2E endpoint mapping.

### `tests/e2e/cypress/support/commands.js`

Add a Cypress command for manual order scenario seeding.

### `tests/e2e/cypress/e2e/manual-orders.cy.js`

Add the manual order E2E tests.

## Files to Create

### `src/App/E2E/E2EManualOrderScenarioSeeder.cs`

Seeds deterministic scenario data for manual order Cypress tests.

The service should use `AppDbContext` or existing repositories consistently with the current E2E setup style. Prefer direct deterministic persistence for setup because test setup is below the behavior under test and must be fast and explicit.

### Optional request/response models

If the endpoint would otherwise use anonymous shapes repeatedly, create small models such as:

- `E2EManualOrderScenarioRequest`
- `E2EManualOrderScenarioResponse`

## Persistence Changes

No schema changes are required.

The E2E manual order seeder will insert existing entities into the isolated E2E database after reset.

## API Contract Changes

Production contracts are unchanged.

New E2E-only test contract:

- `POST /__e2e/manual-orders/scenario`

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
- Successful create/edit/link tests should submit real UI forms so anti-forgery tokens are exercised.
- Read-only write protection must verify server rejection through direct write attempts or UI attempts that reach the server.
- Do not weaken production authorization or anti-forgery behavior to make tests pass.

## Performance Considerations

Keep manual order scenario seeds small and explicit.

Avoid seeding the full development dataset in every test.

## Observability

Use existing Cypress screenshots/artifacts for failures.

No new application logging is required.

## Testing Strategy

### Cypress Manual Order Tests

Add `manual-orders.cy.js` with scenarios:

- create an order source from `/ManualOrders/Create` and verify it is available in the source select;
- create a manual order with two items and verify redirected order details;
- verify dynamic total calculation for quantity `2` and unit price `15,50`;
- verify dynamic add/remove behavior by submitting an order with only remaining valid rows;
- edit an existing manual order and verify details reflect changed reference and item quantity;
- link an unlinked item to an internal product and verify the association is visible;
- submit invalid order data and verify the form remains visible with validation feedback;
- verify `Consulta` is rejected from create source, create order, edit order, and link item write attempts.

### Existing .NET Tests

No new .NET behavior tests are required for Cypress-only assertions unless the seed service contains non-trivial setup logic that merits focused coverage.

Run the existing build/test suite after implementation if feasible.

### E2E Validation

Run the manual orders Cypress spec through the existing E2E command. If targeted execution is needed during development, run Cypress with the manual order spec path while the E2E app is started through the existing orchestration.

## Implementation Tasks

### Task 1 - Add manual order UI selectors

Affected:

- `ManualOrderCreatePage.razor`
- `ManualOrderLinkItemsPage.razor`

Changes:

- add all required `data-cy` selectors to exercised elements;
- preserve visible UI.

Validation:

- create/edit/link pages render selectors for source creation, order fields, item rows, totals, save, product link, and link submit.

Dependencies:

None.

### Task 2 - Add E2E manual order scenario seeding

Affected:

- new `E2EManualOrderScenarioSeeder`;
- `E2EEndpoints.cs`;
- optional request/response models.

Changes:

- seed minimal deterministic manual order scenarios;
- return product/order/item/source metadata Cypress needs;
- guard setup through existing E2E endpoint structure.

Validation:

- after reset, each scenario endpoint creates only the data needed and returns expected ids/names.

Dependencies:

None.

### Task 3 - Add Cypress manual order setup command

Affected:

- `tests/e2e/cypress/support/commands.js`

Changes:

- add `cy.seedManualOrdersScenario(name)`;
- return response body to tests.

Validation:

- command can seed a scenario after `cy.resetE2E()`.

Dependencies:

Task 2.

### Task 4 - Implement source and create Cypress coverage

Affected:

- new `manual-orders.cy.js`

Changes:

- test source creation from the manual order page;
- test create manual order with two items through the real form;
- assert details page shows source, reference, shipping date, and item data.

Validation:

- created order redirects to `/Orders/Details/{id}`;
- details contain submitted reference/source/items.

Dependencies:

Tasks 1 through 3.

### Task 5 - Implement dynamic rows and totals coverage

Affected:

- `manual-orders.cy.js`

Changes:

- test totals update for quantity and unit price;
- test add/remove item rows and save only remaining valid rows.

Validation:

- item total and order total display expected currency values;
- details show only submitted remaining item rows.

Dependencies:

Tasks 1 through 3.

### Task 6 - Implement edit Cypress coverage

Affected:

- `manual-orders.cy.js`

Changes:

- seed an editable order;
- open details and edit page;
- update reference and item quantity;
- save through the real edit form.

Validation:

- details show updated reference and quantity.

Dependencies:

Tasks 1 through 3.

### Task 7 - Implement link-items Cypress coverage

Affected:

- `manual-orders.cy.js`

Changes:

- seed an unlinked order item and active product;
- open link-items page;
- select product and submit link form.

Validation:

- link-items page or details page shows the product association.

Dependencies:

Tasks 1 through 3.

### Task 8 - Implement invalid form and read-only coverage

Affected:

- `manual-orders.cy.js`

Changes:

- submit invalid manual order data and assert rejection;
- authenticate as `Consulta` and verify direct write attempts are rejected for source creation, create, edit, and link item.

Validation:

- invalid create remains on the form with validation feedback;
- read-only write requests redirect to access denied or otherwise match existing authorization behavior.

Dependencies:

Tasks 1 through 3.

### Task 9 - Validate implementation

Affected:

- repository build/test and Cypress execution.

Changes:

- run relevant .NET build/tests;
- run the manual order Cypress spec or full E2E command.

Validation:

- build succeeds;
- existing tests remain green or deviations are documented;
- manual order Cypress spec passes.

Dependencies:

Tasks 1 through 8.

## Requirement Traceability

| Requirement | Implementation |
|---|---|
| `manual-orders-spec.md` FR-001 | Tasks 1, 2, 3, and 4 |
| `manual-orders-spec.md` FR-002 | Tasks 1, 2, 3, and 4 |
| `manual-orders-spec.md` FR-003 | Tasks 1, 2, 3, 4, and 5 |
| `manual-orders-spec.md` FR-004 | Tasks 1, 2, 3, and 5 |
| `manual-orders-spec.md` FR-005 | Tasks 1 and 5 |
| `manual-orders-spec.md` FR-006 | Tasks 1, 2, 3, and 6 |
| `manual-orders-spec.md` FR-007 | Tasks 1, 2, 3, and 7 |
| `manual-orders-spec.md` FR-008 | Tasks 1, 2, 3, and 8 |
| `manual-orders-spec.md` FR-009 | Tasks 2, 3, and 8 |
| `manual-orders-spec.md` FR-010 | Task 1 |
| `spec.md` FR-003 | Tasks 2 and 3 |
| `spec.md` FR-004 | Tasks 2 and 3 |
| `spec.md` FR-005 | Existing E2E command plus Task 3 |
| `spec.md` FR-006 | Task 1 |
| `spec.md` FR-007 | Task 8 |
| `spec.md` FR-008 | Tasks 4 through 8 |

## Backward Compatibility

Existing application behavior and routes remain unchanged.

The only UI changes are stable test attributes.

E2E-only endpoints are unavailable outside the E2E environment.

## Migration Strategy

No database migration is required.

## Risks

### Dynamic row selector ambiguity

Repeated item rows use the same selector names.

Mitigation:

- scope assertions and actions to row containers or use `.eq(index)`;
- add row-level selectors only if needed for reliable scoping.

### Scenario seed drift

Direct E2E seed data can drift from domain invariants.

Mitigation:

- keep scenarios small;
- use existing entity shapes and current persistence configuration;
- validate through the real UI behavior after seeding.

### Anti-forgery handling

Write actions require anti-forgery tokens.

Mitigation:

- successful mutation tests submit real rendered forms.

### Locale-sensitive currency input

The manual form accepts `pt-BR` decimal input and displays BRL currency.

Mitigation:

- use `15,50` in the browser field and assert the rendered `R$ 31,00` total.

## Assumptions

- The existing E2E foundation is accepted as the base for this slice.
- Direct scenario seeding through E2E-only endpoints is acceptable because Cypress validates behavior above that setup layer.
- `Operador` is the default write-authorized actor for manual order workflows.
- `Consulta` direct write checks may use `cy.request` with `failOnStatusCode: false`.
- Existing order details selectors can be reused for post-create/edit assertions.

## Open Technical Questions

None blocking.
