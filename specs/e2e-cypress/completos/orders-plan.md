# Implementation Plan: Cypress E2E Orders

## Summary

Implement the order-specific Cypress E2E coverage defined by `specs/e2e-cypress/orders-spec.md` using the existing E2E foundation.

The implementation will add stable selectors to order UI components, add deterministic E2E seed support for order scenarios, and create a Cypress spec that covers listing, details, import, production submission, deletion, blocked deletion, and read-only protection.

## Specification Reference

- `specs/e2e-cypress/orders-spec.md`
- `specs/e2e-cypress/orders.md`
- `specs/e2e-cypress/spec.md`

## Current Architecture

The E2E foundation already exists:

- `tests/e2e/cypress.config.js`
- `tests/e2e/cypress/support/commands.js`
- `tests/e2e/cypress/e2e/authentication.cy.js`
- `src/App/E2E/E2EEndpoints.cs`
- `src/App/E2E/E2EDatabaseResetter.cs`
- `src/App/E2E/E2EIdentitySeeder.cs`

Existing Cypress commands include:

- `cy.getByCy(selector)`
- `cy.resetE2E()`
- `cy.loginAs(role)`
- `cy.loginThroughUi(email, password)`

Order application flow currently uses:

- `OrdersController` for listing, import, details, send-to-production, and delete.
- `OrderQueryService` for list/detail view models.
- `ManualOrderService` for deletion.
- `OrderProductionPlanningService` for production planning.
- `OrdersIndexPage.razor` and `OrderDetailsPage.razor` for static rendered UI.

Current gaps for this slice:

- order components lack most required `data-cy` selectors;
- E2E endpoints do not seed order scenario data;
- there is no Cypress order spec;
- import outcome data is currently provided by the configured in-memory order source, but reset removes product/catalog/stock data unless reseeded for scenarios.

## Change Surface

### Direct Impact

- `src/UI/Components/Orders/OrdersIndexPage.razor`
- `src/UI/Components/Orders/OrderDetailsPage.razor`
- `src/App/E2E/E2EEndpoints.cs`
- new E2E seed support under `src/App/E2E`
- `tests/e2e/cypress/support/commands.js`
- new `tests/e2e/cypress/e2e/orders.cy.js`

### Indirect Impact

- E2E database records for products, raw materials, bill of materials, stock movements, orders, production tasks, and shipments.
- Existing `OrdersController` write endpoints and anti-forgery forms.
- Existing import pipeline behavior.

## Implementation Strategy

Reuse the existing E2E reset/login infrastructure and add order scenario setup through E2E-only endpoints.

The seed support should create focused scenarios by name rather than exposing broad arbitrary database writes to Cypress. Each scenario should reset or assume reset state, seed the minimal supporting catalog/inventory/order records, and return the identifiers Cypress needs to navigate to details pages.

Successful write flows should use the rendered UI forms so anti-forgery behavior remains exercised. Read-only authorization checks may use direct authenticated `cy.request` calls to server write endpoints because the objective is server-enforced rejection.

Selectors should be added directly to the static Razor components without changing visible text or layout.

## Data Flow

```text
Cypress test
    -> cy.resetE2E()
    -> cy.seedOrdersScenario("plannable-order")
    -> cy.loginAs("Operador")
    -> cy.visit("/Orders/Details/{id}")
    -> click order UI action
    -> MVC controller + anti-forgery + app service
    -> database effect
    -> redirected/rendered UI assertion
```

## Existing Components to Reuse

- `cy.resetE2E()` and `cy.loginAs(...)`.
- Existing `OrdersController` routes and forms.
- Existing `InMemoryOrderSource` or equivalent deterministic source behavior for import.
- Existing domain/persistence model for products, bill of materials, stock, orders, production tasks, and shipments.
- Existing application messages:
  - `Pedido enviado para producao.`
  - `Pedido nao pode ser enviado para producao. Verifique vinculos, estoque e tarefas ja existentes.`
  - `Pedido excluido.`
  - blocked delete messages from `ManualOrderService`.

## New Components

### E2E order scenario seeder

Create a focused E2E-only service that seeds named order scenarios and returns identifiers.

Expected scenarios:

- `listing-details`: an order with item rows and no production or shipments.
- `plannable-order`: product, bill of materials, sufficient stock, and linked order item.
- `unplannable-order`: order missing product link or stock needed for planning.
- `deletable-order`: eligible order that can be removed.
- `blocked-delete`: order with shipment or started/completed production so delete is rejected.
- `import-support`: supporting catalog/product mapping/stock data needed for deterministic import.

### E2E seed endpoint

Extend `/__e2e` with a guarded order scenario endpoint, for example:

- `POST /__e2e/orders/scenario`

The endpoint should accept a scenario name and return scenario metadata, including order ids and external order references.

### Cypress order commands

Add helper command(s), for example:

- `cy.seedOrdersScenario(name)`

Keep selectors and test logic readable in the spec.

## Files to Modify

### `src/UI/Components/Orders/OrdersIndexPage.razor`

Add:

- `data-cy="orders-import"` on the import submit button or form control used by Cypress;
- `data-cy="order-create"` on the manual order link;
- `data-cy="orders-list"` on the table/list container;
- `data-cy="order-row"` on each order row;
- `data-cy="order-details-link"` on each details link;
- selector coverage for import outcome if assertions need a stable target.

### `src/UI/Components/Orders/OrderDetailsPage.razor`

Add:

- `data-cy="order-details"` on the details container;
- `data-cy="order-edit"` on the edit link;
- `data-cy="order-delete"` on the delete submit button;
- `data-cy="order-send-to-production"` on the production submit button;
- `data-cy="order-link-items"` on the link-items action;
- `data-cy="order-items-tab"` on the items tab;
- `data-cy="order-production-tab"` on the production tab;
- `data-cy="order-shipments-tab"` on the shipments tab;
- `data-cy="flash-success"` on success alerts;
- `data-cy="flash-error"` on error alerts.

### `src/App/E2E/E2EEndpoints.cs`

Map the order scenario seed endpoint under `/__e2e`.

Keep the endpoint available only through the existing E2E endpoint mapping.

### `tests/e2e/cypress/support/commands.js`

Add a Cypress command for order scenario seeding.

### `tests/e2e/cypress/e2e/orders.cy.js`

Add the order E2E tests.

## Files to Create

### `src/App/E2E/E2EOrderScenarioSeeder.cs`

Seeds deterministic scenario data for order Cypress tests.

The service should use `AppDbContext` or existing repositories consistently with the E2E setup style. Prefer direct deterministic persistence for setup, because test setup is below the behavior under test and must be fast and explicit.

### Optional request/response models

If the endpoint would otherwise use anonymous shapes repeatedly, create small models such as:

- `E2EOrderScenarioRequest`
- `E2EOrderScenarioResponse`

## Persistence Changes

No schema changes are required.

The E2E order seeder will insert existing entities into the isolated E2E database after reset.

## API Contract Changes

Production contracts are unchanged.

New E2E-only test contract:

- `POST /__e2e/orders/scenario`

This endpoint must not be available outside `ASPNETCORE_ENVIRONMENT=E2E`.

## UI Changes

Only invisible `data-cy` attributes are expected.

No visible text, layout, or workflow changes should be introduced by this slice.

## Configuration Changes

No configuration changes are expected unless the import scenario needs a new E2E-specific flag.

Prefer using the existing in-memory order source behavior already registered for non-Shopee execution.

## Dependencies

No new NuGet packages are expected.

No new npm packages are expected.

## Security Considerations

- E2E seed endpoints must remain E2E-only.
- Successful write tests should submit the real UI forms so anti-forgery tokens are exercised.
- Read-only write protection must verify server rejection through direct write attempts or UI attempts that reach the server.
- Do not weaken production authorization or anti-forgery behavior to make tests pass.

## Performance Considerations

Keep scenario seeds small and explicit.

Avoid seeding the full development dataset in every test unless a scenario specifically benefits from it.

## Observability

Use existing Cypress screenshots/artifacts for failures.

No new application logging is required.

## Testing Strategy

### Cypress Order Tests

Add `orders.cy.js` with scenarios:

- listing shows seeded order and opens details;
- details shows metadata, item rows, tabs, and actions;
- import reports deterministic outcome and lists imported order data;
- plannable order can be sent to production and displays success plus production task;
- unplannable order displays the existing failure message;
- eligible order can be deleted and disappears from listing;
- blocked-delete order remains after deletion attempt and shows an error;
- `Consulta` is rejected from import, send-to-production, and delete.

### Existing .NET Tests

No new .NET behavior tests are required for the Cypress-only assertions unless the seed service contains non-trivial setup logic that merits focused coverage.

Run the existing build/test suite after implementation if feasible.

### E2E Validation

Run:

```text
npm run test:e2e
```

or, if runtime is high during development:

```text
npm --prefix tests/e2e run e2e -- --spec cypress/e2e/orders.cy.js
```

with the E2E app running through the existing orchestration.

## Implementation Tasks

### Task 1 - Add order UI selectors

Affected:

- `OrdersIndexPage.razor`
- `OrderDetailsPage.razor`

Changes:

- add all required `data-cy` selectors to exercised elements;
- preserve visible UI.

Validation:

- static pages render selectors for list, details, tabs, actions, and flash messages.

Dependencies:

None.

### Task 2 - Add E2E order scenario seeding

Affected:

- new `E2EOrderScenarioSeeder`;
- `E2EEndpoints.cs`;
- optional request/response models.

Changes:

- seed minimal deterministic order scenarios;
- return order ids/references Cypress needs;
- guard setup through existing E2E endpoint structure.

Validation:

- after reset, each scenario endpoint creates only the data needed and returns expected ids.

Dependencies:

None.

### Task 3 - Add Cypress order setup command

Affected:

- `tests/e2e/cypress/support/commands.js`

Changes:

- add `cy.seedOrdersScenario(name)`;
- return response body to tests.

Validation:

- command can seed a scenario after `cy.resetE2E()`.

Dependencies:

Task 2.

### Task 4 - Implement listing/details/import Cypress coverage

Affected:

- new `orders.cy.js`

Changes:

- test listing and details with seeded data;
- test import outcome using deterministic setup.

Validation:

- specs assert visible row, details link, details sections, and import metrics/outcome.

Dependencies:

Tasks 1 through 3.

### Task 5 - Implement production Cypress coverage

Affected:

- `orders.cy.js`

Changes:

- test send-to-production success through UI form;
- test send-to-production failure through UI form.

Validation:

- success displays `flash-success` and production task;
- failure displays `flash-error`.

Dependencies:

Tasks 1 through 3.

### Task 6 - Implement delete and authorization Cypress coverage

Affected:

- `orders.cy.js`

Changes:

- test successful delete through UI form;
- test blocked delete through UI form;
- test `Consulta` rejection for import, send-to-production, and delete.

Validation:

- deleted order is absent from listing;
- blocked order remains visible;
- read-only requests are rejected by existing authorization behavior.

Dependencies:

Tasks 1 through 3.

### Task 7 - Validate implementation

Affected:

- repository build/test and Cypress execution.

Changes:

- run relevant .NET build/tests;
- run the order Cypress spec or full E2E command.

Validation:

- build succeeds;
- existing tests remain green or deviations are documented;
- order Cypress spec passes.

Dependencies:

Tasks 1 through 6.

## Requirement Traceability

| Requirement | Implementation |
|---|---|
| FR-001 | Tasks 1, 2, 3, and 4 |
| FR-002 | Tasks 2, 3, and 4 |
| FR-003 | Tasks 1, 2, 3, and 4 |
| FR-004 | Tasks 2, 3, and 5 |
| FR-005 | Tasks 1, 2, 3, and 5 |
| FR-006 | Tasks 1, 2, 3, and 5 |
| FR-007 | Tasks 1, 2, 3, and 6 |
| FR-008 | Tasks 1, 2, 3, and 6 |
| FR-009 | Tasks 2, 3, and 6 |
| FR-010 | Task 1 |

## Backward Compatibility

Existing application behavior and routes remain unchanged.

The only UI changes are stable test attributes.

E2E-only endpoints are unavailable outside the E2E environment.

## Migration Strategy

No database migration is required.

## Risks

### Scenario seed drift

Direct E2E seed data can drift from domain invariants.

Mitigation:

- keep scenarios small;
- use existing entity shapes and current persistence configuration;
- validate through the real UI behavior after seeding.

### Anti-forgery handling

Write actions require anti-forgery tokens.

Mitigation:

- successful mutation tests submit real forms from rendered pages.

### Import determinism

Import depends on configured order sources and product mappings.

Mitigation:

- seed known supporting catalog/mapping data before import;
- use the deterministic in-memory source rather than external providers.

### Delete eligibility

The delete service may compensate and remove related records, so scenarios must distinguish eligible and blocked states clearly.

Mitigation:

- use separate scenario names for deletable and blocked-delete cases;
- assert final listing state after the operation.

## Assumptions

- The existing E2E foundation is accepted as the base for this slice.
- Direct scenario seeding through E2E-only endpoints is acceptable because Cypress validates behavior above that setup layer.
- `Operador` is the default write-authorized actor for order workflows.
- `Consulta` direct write checks may use `cy.request` with `failOnStatusCode: false`.

## Open Technical Questions

None blocking.
