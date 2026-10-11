# Implementation Plan: Melhorias na tela de Apuracao financeira

## Summary

Implementar as melhorias financeiras dentro do fluxo MVC/Razor existente, mantendo a especificacao como fonte de verdade.

A listagem de Apuracao financeira sera ajustada na camada de apresentacao para exibir o pedido abreviado e a data de calculo sem horario, preservando o `orderId` completo nos links e consultas. O detalhe financeiro passara a carregar os itens do pedido associado pelo `FinanceAppService` e a renderiza-los no componente `FinancialSettlementDetailsPage` como dados somente leitura.

## Specification Reference

Especificacao:

- `specs/melhorias-tela-apuracao-financeira/spec.md`

Requisitos principais:

- `FR-001` a `FR-004`: formatacao da listagem financeira.
- `FR-005` a `FR-009`: melhoria do detalhe financeiro e exibicao dos itens do pedido.
- `FR-010`: preservacao dos valores financeiros.
- `FR-011`: responsividade.

## Current Architecture

A area financeira segue o padrao MVC com componentes Razor estaticos compartilhados:

```text
GET /Finance
        |
FinanceController.Index(from, to)
        |
FinanceAppService.GetIndexAsync(from, to)
        |
FinancialSettlementIndexViewModel
        |
Views/Finance/Index.cshtml
        |
UI/Components/Finance/FinanceIndexPage.razor
```

```text
GET /Finance/Details?orderId={id}
        |
FinanceController.Details(orderId)
        |
FinanceAppService.GetByOrderIdAsync(orderId)
        |
FinancialSettlementDetailViewModel
        |
Views/Finance/Details.cshtml
        |
UI/Components/Finance/FinancialSettlementDetailsPage.razor
```

Pedidos e itens ja sao carregados por `OrderQueryService.GetDetailAsync` para a tela de pedido, usando `IOrderRepository.GetByIdAsync` e `OrderItemViewModel`. O `FinanceAppService` ja possui `IOrderRepository` injetado, entao o detalhe financeiro pode reaproveitar a mesma fonte de dados sem criar nova dependencia.

## Change Surface

## Direct Impact

- `src/App/Services/FinanceAppService.cs`
- `src/App/Models/FinancialSettlementDetailViewModel.cs`
- `src/App/Views/Finance/Index.cshtml`
- `src/App/Views/Finance/Details.cshtml`
- `src/UI/Components/Finance/FinanceIndexPage.razor`
- `src/UI/Components/Finance/FinancialSettlementDetailsPage.razor`
- `src/App/wwwroot/css/site.css`
- `tests/Craftsman.Tests/Finance/FinanceAppServiceTests.cs`
- `tests/Craftsman.Tests/Application/SharedInterfaceViewTests.cs`

## Indirect Impact

- `FinanceController.Details` must preserve the current `NotFound` behavior when no settlement exists.
- `IOrderRepository.GetByIdAsync` remains the source for order items.
- Existing financial summary/KPI behavior must remain unchanged.
- Existing authorization policy on `FinanceController` remains unchanged.

## Implementation Strategy

Keep all financial calculation behavior unchanged.

For the index page, perform display-only formatting in `Views/Finance/Index.cshtml` before passing rows to `FinanceIndexPage`. Add a separate display value for the shortened order id while preserving the full `Guid` for the details URL.

For the detail page, extend `FinancialSettlementDetailViewModel` with an item collection using the existing `OrderItemViewModel`. Update `FinanceAppService.GetByOrderIdAsync` and `GenerateAsync` detail mapping to load the order associated with the settlement and map its items. If the order cannot be loaded, return the settlement with an empty item collection so the component can render a clear empty state and avoid showing incorrect item data.

Update `FinancialSettlementDetailsPage` from the current simple definition list into a structured read-only layout using existing page patterns such as `page-heading`, `metric-row`, `metric-card`, `table-panel`, `table-responsive`, and `empty-state`.

## Data Flow

### Index

```text
FinancialSettlementListItemViewModel.OrderId
        |
Views/Finance/Index.cshtml
        |
SettlementRow(OrderId, OrderCode, formatted values)
        |
FinanceIndexPage
        |
Display OrderCode, link with OrderId
```

### Details

```text
FinanceController.Details(orderId)
        |
FinanceAppService.GetByOrderIdAsync(orderId)
        |
IFinancialSettlementRepository.GetByOrderIdAsync(orderId)
        |
IOrderRepository.GetByIdAsync(settlement.OrderId)
        |
FinancialSettlementDetailViewModel(..., Items)
        |
Views/Finance/Details.cshtml maps item rows and formatted values
        |
FinancialSettlementDetailsPage
```

## Existing Components to Reuse

### `IOrderRepository`

Reuse `GetByIdAsync` to load the order for the financial settlement. No new repository method is required for this feature.

### `OrderItemViewModel`

Reuse the existing application model that already represents order items with external id, description, quantity, unit price, currency, and optional product link.

### Existing UI CSS patterns

Reuse `page-heading`, `metric-row`, `metric-card`, `table-panel`, `table-responsive`, and `empty-state` where suitable. Add finance-specific styles only for gaps that cannot be covered cleanly by existing classes.

### `EnumPresentation`

Continue using `EnumPresentation.Label` in `Views/Finance/Details.cshtml` and `Views/Finance/Index.cshtml` for financial status labels.

## New Components

No new service abstraction is required.

The feature can be implemented by extending existing view models and components. If useful for the component boundary, add nested row records inside the Razor components, following the existing `SettlementRow`, `KpiCard`, and `OrderItemRow` pattern.

## Files to Modify

### `src/App/Models/FinancialSettlementDetailViewModel.cs`

Add an `IReadOnlyCollection<OrderItemViewModel>` property for the order items shown in the detail page.

### `src/App/Services/FinanceAppService.cs`

Extend detail mapping to load the order by `settlement.OrderId` and include the mapped order items.

Keep list/index settlement mapping and summary calculation unchanged.

### `src/App/Views/Finance/Index.cshtml`

Format:

- `OrderCode`: first 8 characters of `OrderId.ToString()`;
- `CalculatedAt`: date only, using the existing display culture/pattern for Brazilian Portuguese.

Preserve the full `OrderId` when constructing `FinanceIndexPage.SettlementRow`.

### `src/UI/Components/Finance/FinanceIndexPage.razor`

Extend `SettlementRow` to include a display-only order code while keeping the full `OrderId`.

Render the abbreviated order code in the `Pedido` column and continue generating the details URL from the full `OrderId`.

### `src/App/Views/Finance/Details.cshtml`

Map `Model.Items` to a component row collection with formatted values suitable for static rendering.

Continue passing financial values and status to the component.

### `src/UI/Components/Finance/FinancialSettlementDetailsPage.razor`

Replace the simple definition list with a more structured read-only detail layout.

Add an order-items section with a table and an empty state when no items are available.

### `src/App/wwwroot/css/site.css`

Add small finance-detail styles only if existing reusable classes are insufficient for a clean layout.

### `tests/Craftsman.Tests/Finance/FinanceAppServiceTests.cs`

Add service-level tests for loading order items into the financial detail view model and for preserving settlement values.

### `tests/Craftsman.Tests/Application/SharedInterfaceViewTests.cs`

Add or update view/component tests to verify:

- the financial index maps an abbreviated order display value;
- the details view passes item rows to `FinancialSettlementDetailsPage`;
- the detail component contains an order-items table and empty state.

## Files to Create

None expected.

## Files to Remove

None.

## Persistence Changes

No schema change is required.

Financial settlements already store `OrderId`, and orders already contain items. The implementation should only read existing persisted data.

No migration is required.

## API Contract Changes

No route changes are required.

Existing route:

```text
GET /Finance/Details?orderId={guid}
```

remains unchanged.

The full `orderId` continues to be used for navigation and lookup. The abbreviated pedido value is display-only.

## UI Changes

### Apuracao financeira

- The `Pedido` column shows the first 8 characters of the order id.
- The `Detalhes` link continues using the full `orderId`.
- The `Calculado em` column shows only the date.
- Existing KPI cards, filters, status, values, and empty-state behavior remain unchanged.

### Acerto financeiro detail

- The page uses a clearer layout for financial values.
- The page includes a read-only items section.
- Item rows show at least external item id, description, quantity, and unit price.
- If no items are available, the page shows a clear empty state.

## Configuration Changes

None.

## Dependencies

No new NuGet packages are required.

## Security Considerations

- Keep the existing `[Authorize(Policy = ApplicationPolicies.Read)]` protection on `FinanceController`.
- Continue treating `orderId` as untrusted route/query input.
- Render item descriptions through Razor normal text rendering so HTML/script is encoded.
- Do not add a new public endpoint for item lookup.
- Do not expose order items outside the existing financial read permission.

## Performance Considerations

No material performance impact is expected.

The detail page adds one order lookup by id. The index page does not add database queries.

Avoid loading all orders to find detail items; use `IOrderRepository.GetByIdAsync(settlement.OrderId)`.

## Observability

No additional telemetry is required.

Existing request tracing and exception handling cover the updated read flow.

## Testing Strategy

### Unit/Application Service Tests

Extend `FinanceAppServiceTests` to verify:

- `GetByOrderIdAsync` includes order items when the associated order exists;
- item data includes external id, description, quantity, unit price, currency, and product id when present;
- existing financial amounts remain unchanged.

### View/Component Structure Tests

Extend `SharedInterfaceViewTests` to verify:

- `Views/Finance/Index.cshtml` formats date without time and creates a shortened order display value;
- `FinanceIndexPage.SettlementRow` preserves full `OrderId` and includes a display value for the pedido column;
- `Views/Finance/Details.cshtml` maps `Model.Items` and passes item rows to the component;
- `FinancialSettlementDetailsPage.razor` renders an items table and an empty state.

### Build/Test Validation

Run:

```text
dotnet test
```

or at minimum:

```text
dotnet test tests/Craftsman.Tests/Craftsman.Tests.csproj
```

## Implementation Tasks

### Task 1 - Extend the financial detail model

Affected:

- `FinancialSettlementDetailViewModel`
- `FinanceAppService`
- `FinanceAppServiceTests`

Changes:

- Add item collection to the financial detail view model.
- Load the associated order in the financial detail flow.
- Map order items using the same fields as `OrderItemViewModel`.
- Return an empty collection when the order cannot be loaded, preserving settlement details.

Validation:

- Detail for a settlement with an order includes item rows.
- Financial values remain unchanged.
- Existing `NotFound` behavior for missing settlements is unchanged.

Dependencies:

None.

### Task 2 - Update financial index presentation mapping

Affected:

- `Views/Finance/Index.cshtml`
- `FinanceIndexPage.razor`
- `SharedInterfaceViewTests`

Changes:

- Add a display order code based on the first 8 characters of `OrderId.ToString()`.
- Render the display code in the `Pedido` column.
- Keep the details URL based on the full `OrderId`.
- Change `CalculatedAt` formatting from date/time to date-only.

Validation:

- List row displays `7f4b1817` for `7f4b1817-21cf-4f3e-a2c3-4e936db7299b`.
- Details link still contains the full GUID.
- `Calculado em` does not contain hour or minute.

Dependencies:

None.

### Task 3 - Update financial details UI

Affected:

- `Views/Finance/Details.cshtml`
- `FinancialSettlementDetailsPage.razor`
- `site.css`
- `SharedInterfaceViewTests`

Changes:

- Map financial detail items to component rows.
- Refactor the detail component into a structured read-only page.
- Add the items table.
- Add empty state for missing items.
- Reuse existing classes before adding finance-specific CSS.

Validation:

- Detail page renders financial values and status.
- Detail page renders item external id, description, quantity, and unit price.
- Detail page renders a clear empty state when item collection is empty.
- Layout remains responsive with existing table responsive wrappers.

Dependencies:

Task 1.

### Task 4 - Run verification

Affected:

- Test suite
- Build output

Changes:

- Run relevant automated tests.
- Fix any regressions introduced by updated view model constructor signatures or component parameters.

Validation:

- Relevant tests pass.
- Project compiles.

Dependencies:

Tasks 1, 2, and 3.

## Requirement Traceability

| Requirement | Implementation |
|---|---|
| FR-001 | Task 2 |
| FR-002 | Task 2 |
| FR-003 | Task 2 |
| FR-004 | Task 2 |
| FR-005 | Task 3 |
| FR-006 | Tasks 1 and 3 |
| FR-007 | Tasks 1 and 3 |
| FR-008 | Task 3 |
| FR-009 | Task 1 and Task 3 |
| FR-010 | Task 1 and Task 4 |
| FR-011 | Task 3 |
| BR-001 | Task 2 |
| BR-002 | Task 2 |
| BR-003 | Task 1 |
| BR-004 | Task 3 |

## Backward Compatibility

- Existing `/Finance` and `/Finance/Details?orderId=` routes remain unchanged.
- Existing links continue passing full GUID values.
- Existing filters and KPI behavior on `/Finance` remain unchanged.
- No persisted data changes are introduced.
- No authorization policy changes are introduced.

## Migration Strategy

No database migration is required.

The change can be deployed as application code only.

## Rollback Considerations

Rollback is application-level only because the feature introduces no schema, configuration, integration, or data migration changes.

## Implementation Risks

### View model constructor breakage

Adding items to `FinancialSettlementDetailViewModel` will require updating all constructor call sites.

Mitigation:

Update `FinanceAppService.ToDetail`, tests, and any direct instantiations found by compiler errors.

### Ambiguous abbreviated ids

Two GUIDs can theoretically share the same first 8 characters.

Mitigation:

Keep the full GUID in the details URL and consider adding accessible full-id metadata if the specification later resolves that open question.

### Missing associated order

The financial settlement may exist while the order cannot be loaded.

Mitigation:

Render settlement details and an empty item state rather than showing incorrect item data.

## Assumptions

- The approved specification is `specs/melhorias-tela-apuracao-financeira/spec.md`.
- Date-only formatting for the index can use the existing local time conversion followed by a date-only format.
- The detail page may keep showing date/time for `Calculado em` until the specification resolves whether detail should also hide time.
- The existing `OrderItemViewModel` is acceptable for finance detail item data.
- The current financial read authorization is sufficient for displaying order items inside the financial detail page.

## Open Technical Questions

None blocking.

Product-level open questions remain in the specification, but the plan avoids depending on them:

- no tooltip/full-id reveal is required for implementation because full `orderId` is preserved in navigation;
- no subtotal per item is planned because it is not required by acceptance criteria;
- detail date/time behavior is left unchanged unless the specification is updated.
