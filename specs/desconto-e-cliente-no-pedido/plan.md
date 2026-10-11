# Implementation Plan: Desconto e Cliente no Pedido

## Summary

Implementar cliente e desconto no fluxo de pedido manual, persistindo cliente como entidade propria, vinculando pedidos manualmente criados a clientes opcionais, armazenando desconto no pedido e levando o desconto para a apuracao financeira.

O plano preserva a arquitetura atual: dominio em `src/Domains`, persistencia EF Core em `src/Infra`, orquestracao em services de `src/App`, controllers MVC finos e componentes Razor estaticos em `src/UI`.

## Specification Reference

Especificacao: `specs/desconto-e-cliente-no-pedido/spec.md`

Requisitos centrais:

- FR-001 a FR-009: cliente no pedido manual, autocomplete, listagem e detalhes.
- FR-010 a FR-019: desconto no pedido e financeiro.
- FR-020 a FR-021: entidade Cliente e identificacao por Nome Normalizado.
- FR-022: recalculo financeiro apos edicao relevante.
- FR-023: pedidos importados permanecem fora do escopo.

## Current Architecture

- `ManualOrdersController` recebe criacao/edicao de pedidos manuais e delega para `ManualOrderService`.
- `ManualOrderService` cria, edita, exclui e vincula produtos em pedidos manuais.
- `Order` e `OrderItem` representam o dominio de vendas.
- `OrderRepository` mapeia `Order` para `OrderEntity` e `OrderItemEntity`.
- `OrderQueryService` monta view models para listagem, detalhes e selecao de envio.
- `SettlementCalculator` calcula receita, custos de producao/envio e margem.
- `FinancialSettlement` armazena o resultado calculado; `FinancialSettlementRepository` persiste e consulta acertos.
- Views MVC em `src/App/Views` mapeiam view models para componentes Razor em `src/UI/Components`.
- Testes atuais cobrem dominio, services, financeiro, controllers e contratos de views por leitura dos arquivos.

## Architecture Conflict

A especificacao exige recalculo automatico quando um pedido com acerto financeiro ja calculado for editado. Atualmente `ManualOrderService.UpdateAsync` chama `EnsureOrderCanChangeAsync`, que bloqueia qualquer edicao se houver envio vinculado. Na pratica, pedidos com acerto financeiro normalmente tem entrega/envio vinculado.

### Planned Resolution

Separar mudancas operacionais de mudancas comerciais:

- manter bloqueios atuais para alteracoes que exigem replanejamento operacional quando houver producao iniciada/concluida ou envio;
- permitir atualizacoes comerciais seguras em pedidos manuais com acerto financeiro, especialmente cliente e desconto;
- se uma edicao permitida alterar dados usados no financeiro, recalcular automaticamente o acerto.

Alteracoes de itens em pedidos com producao/envio devem continuar bloqueadas quando criarem inconsistencia operacional. Caso o formulario envie itens iguais aos atuais e altere apenas cliente/desconto, a edicao deve ser aceita.

## Change Surface

### Direct Impact

- Dominio de vendas:
  - `Order`
  - novos tipos de cliente/desconto
  - possivel novo repositorio de cliente
- Dominio financeiro:
  - `FinancialSettlement`
  - `SettlementCalculator`
- Persistencia:
  - `AppDbContext`
  - novas entidades/configuracoes EF
  - `OrderEntity`
  - `FinancialSettlementEntity`
  - migration EF Core
- Aplicacao:
  - `ManualOrderInputModel`
  - `ManualOrderService`
  - `OrderQueryService`
  - `FinanceAppService`
  - view models de pedido e financeiro
- UI:
  - `ManualOrderCreatePage.razor`
  - `OrdersIndexPage.razor`
  - `OrderDetailsPage.razor`
  - `FinanceIndexPage.razor`
  - `FinancialSettlementDetailsPage.razor`
  - views MVC correspondentes

### Indirect Impact

- Importacao Shopee e normalizacao de pedidos externos devem continuar criando pedidos sem cliente/desconto.
- Producao e envio devem continuar usando pedidos existentes sem exigir cliente/desconto.
- Cache de pedidos e financeiro deve ser invalidado quando cliente/desconto ou acerto forem alterados.
- Auditoria EF deve registrar novas entidades/campos sem redacao especial.

## Implementation Strategy

Adicionar os novos dados ao dominio de forma compatível:

- criar `Customer` no dominio de Sales com Nome, Nome Normalizado, Telefone e Email;
- criar um repositorio de cliente para buscar por Nome Normalizado e listar opcoes para autocomplete;
- adicionar ao `Order` uma referencia opcional a cliente e um desconto opcional;
- representar desconto no pedido com descricao, valor monetario e metadados suficientes para input percentual quando necessario;
- persistir o valor monetario do desconto no pedido para que o financeiro seja deterministico.

No financeiro:

- adicionar `DiscountAmount` a `FinancialSettlement`;
- alterar `TotalCostAmount` para somar producao, envio e desconto;
- manter `RevenueAmount` como receita bruta;
- recalcular margem como `RevenueAmount - TotalCostAmount`;
- persistir `discount_amount` em `financial_settlements` com default zero para compatibilidade.

Na UI:

- estender a tela de pedido manual com blocos Cliente e Desconto;
- usar `datalist`/autocomplete HTML com clientes cadastrados para evitar nova dependencia;
- exibir alerta claro quando desconto for maior que a receita estimada;
- exibir alerta de recalculo quando editar pedido com acerto financeiro;
- trocar a coluna Pedido Externo por Cliente na listagem de pedidos;
- adicionar coluna de desconto em pedidos e financeiro.

## Data Flow

### Criacao de pedido manual

`POST /ManualOrders/Create`

`ManualOrdersController.Create`

`ManualOrderService.CreateAsync`

`CustomerRepository.GetByNormalizedNameAsync` ou `AddAsync`

`OrderRepository.AddAsync`

`UnitOfWork.SaveChangesAsync`

Detalhes do pedido.

### Edicao comercial com acerto

`POST /ManualOrders/Edit/{id}`

`ManualOrderService.UpdateAsync`

validacao de alteracoes operacionais permitidas

`OrderRepository.UpdateAsync`

`FinancialSettlementRepository.GetByOrderIdAsync`

`SettlementCalculator.CalculateAsync`

`FinancialSettlementRepository.ReplaceForOrderAsync`

`UnitOfWork.SaveChangesAsync`

Detalhes do pedido com financeiro atualizado.

### Apuracao financeira

`SettlementCalculator.CalculateAsync(orderId)`

Pedido + tarefas + movimentos + envios

receita bruta dos itens

desconto do pedido

`FinancialSettlement` com `DiscountAmount`.

## Existing Components to Reuse

- `ManualOrderService` como orquestrador do pedido manual.
- `OrderQueryService` para montar listagem e detalhes de pedidos.
- `FinanceAppService` para listagem, resumo, detalhe e geracao manual de acertos.
- `SettlementCalculator` para centralizar calculo financeiro.
- `FinancialSettlementRepository.DeleteByOrderAsync` como base para substituicao/recalculo de acerto.
- `IApplicationCache.RemoveByPrefix("finance:")` e `RemoveByPrefix("sales:orders:")` para invalidacao de cache.
- Componentes Razor estaticos existentes, estendidos sem trocar o padrao de renderizacao.

## New Components

### `Customer`

Entidade de dominio de Sales para Nome, Nome Normalizado, Telefone e Email.

### `CustomerNormalizer`

Helper simples para gerar Nome Normalizado sem espacos, sem caracteres nao imprimiveis e em maiusculo.

### `ICustomerRepository` e `CustomerRepository`

Repositorio para buscar cliente por Nome Normalizado, listar clientes para autocomplete, adicionar e atualizar dados quando necessario.

### `OrderDiscount`

Value object ou tipo de dominio para desconto do pedido, contendo descricao e valor monetario. O percentual informado no formulario e usado para calcular esse valor antes da persistencia.

### `CustomerEntity` e `CustomerConfiguration`

Entidade e configuracao EF para tabela `customers`.

## Files to Modify

### Domain

- `src/Domains/Sales/Entities/Order.cs`
  - adicionar `CustomerId`, dados de desconto e metodos de alteracao.
- `src/Domains/Sales/Entities/OrderItem.cs`
  - sem mudanca esperada, exceto se comparacao de itens exigir helper.
- `src/Domains/Finance/Entities/FinancialSettlement.cs`
  - adicionar `DiscountAmount` e atualizar `TotalCostAmount`/`MarginAmount`.
- `src/Domains/Finance/Services/SettlementCalculator.cs`
  - incluir desconto do pedido no acerto.
- `src/Domains/Finance/Repositories/IFinancialSettlementRepository.cs`
  - adicionar metodo de substituicao ou atualizacao por pedido para recalculo.

### Infrastructure

- `src/Infra/Persistence/AppDbContext.cs`
  - adicionar `DbSet<CustomerEntity>`.
- `src/Infra/Persistence/Entities/OrderEntity.cs`
  - adicionar `CustomerId`, dados de desconto e possivel navegacao `Customer`.
- `src/Infra/Persistence/Entities/FinancialSettlementEntity.cs`
  - adicionar `DiscountAmount`.
- `src/Infra/Persistence/Configurations/OrderConfiguration.cs`
  - mapear FK opcional para cliente e colunas de desconto.
- `src/Infra/Persistence/Configurations/FinancialSettlementConfiguration.cs`
  - mapear `discount_amount`.
- `src/Infra/Repositories/OrderRepository.cs`
  - mapear novos campos.
- `src/Infra/Repositories/FinancialSettlementRepository.cs`
  - mapear `DiscountAmount` e implementar substituicao/recalculo.
- `src/Infra/Services/ServiceCollectionExtensions.cs`
  - registrar repositorio de cliente.

### Application

- `src/App/Models/ManualOrderInputModel.cs`
  - adicionar campos de cliente, desconto e indicador percentual.
- `src/App/Models/OrderListItemViewModel.cs`
  - adicionar cliente e indicador/valor de desconto.
- `src/App/Models/OrderDetailViewModel.cs`
  - adicionar cliente e desconto.
- `src/App/Models/FinancialSettlementListItemViewModel.cs`
  - adicionar `DiscountAmount`.
- `src/App/Models/FinancialSettlementDetailViewModel.cs`
  - adicionar `DiscountAmount`.
- `src/App/Models/FinancialSettlementSummaryViewModel.cs`
  - nao precisa de campo novo se `TotalCostAmount` ja incluir desconto.
- `src/App/Services/ManualOrderService.cs`
  - resolver/criar cliente, calcular desconto, validar alerta/descricao, recalcular acerto quando necessario.
- `src/App/Services/OrderQueryService.cs`
  - expor cliente e desconto para listagem/detalhe.
- `src/App/Services/FinanceAppService.cs`
  - propagar desconto para listagem/detalhe/resumo.
- `src/App/Controllers/ManualOrdersController.cs`
  - popular clientes para autocomplete.

### UI

- `src/App/Views/ManualOrders/Create.cshtml`
  - mapear campos novos para o componente.
- `src/App/Views/Orders/Index.cshtml`
  - trocar pedido externo por cliente e passar indicador de desconto.
- `src/App/Views/Orders/Details.cshtml`
  - passar dados de cliente/desconto.
- `src/App/Views/Finance/Index.cshtml`
  - passar valor de desconto.
- `src/App/Views/Finance/Details.cshtml`
  - passar valor de desconto.
- `src/UI/Components/ManualOrders/ManualOrderCreatePage.razor`
  - adicionar blocos Cliente e Desconto, autocomplete e alertas.
- `src/UI/Components/Orders/OrdersIndexPage.razor`
  - renderizar Cliente e Desconto.
- `src/UI/Components/Orders/OrderDetailsPage.razor`
  - renderizar card de cliente e desconto.
- `src/UI/Components/Finance/FinanceIndexPage.razor`
  - adicionar coluna Desconto.
- `src/UI/Components/Finance/FinancialSettlementDetailsPage.razor`
  - adicionar item de desconto.
- `src/App/wwwroot/css/site.css`
  - ajustes pontuais para os novos blocos, preservando estilo existente.

## Files to Create

- `src/Domains/Sales/Entities/Customer.cs`
- `src/Domains/Sales/ObjectValues/OrderDiscount.cs`
- `src/Domains/Sales/Services/CustomerNormalizer.cs`
- `src/Domains/Sales/Repositories/ICustomerRepository.cs`
- `src/Infra/Persistence/Entities/CustomerEntity.cs`
- `src/Infra/Persistence/Configurations/CustomerConfiguration.cs`
- `src/Infra/Repositories/CustomerRepository.cs`
- nova migration EF Core, por exemplo `AddCustomersAndOrderDiscounts`.

## Files to Remove

None.

## Persistence Changes

### New table: `customers`

Columns:

- `id` uuid primary key;
- `name` varchar, required;
- `normalized_name` varchar, required, unique;
- `phone` varchar, nullable;
- `email` varchar, nullable;
- `created_at` timestamp with time zone, required.

### `orders`

Add nullable columns:

- `customer_id` uuid null;
- `discount_description` varchar null;
- `discount_amount` numeric(18,2) null/default zero.

Add index on `customer_id`. Configure FK to `customers` with restrict/no action delete behavior.

### `financial_settlements`

Add:

- `discount_amount` numeric(18,2) not null default 0.

Existing rows should receive zero by migration default.

## API Contract Changes

Existing routes remain:

- `GET /ManualOrders/Create`
- `POST /ManualOrders/Create`
- `GET /ManualOrders/Edit/{id}`
- `POST /ManualOrders/Edit/{id}`
- `GET /Orders`
- `GET /Orders/Details/{id}`
- `GET /Finance`
- `GET /Finance/Details?orderId=...`

Form posts for manual orders gain optional fields:

- customer name/phone/email;
- discount description;
- discount value;
- discount percent flag.

Existing calls without these fields remain valid.

## UI Changes

### Novo Pedido / Editar Pedido

- Adicionar bloco Cliente com Nome, Telefone e Email.
- Nome usa autocomplete com clientes cadastrados.
- Adicionar bloco Desconto com descricao, valor e opcao percentual.
- Atualizar resumo lateral para mostrar subtotal, desconto estimado e total apos desconto, se coerente com layout.
- Mostrar alerta claro quando desconto estimado exceder receita bruta.
- Em edicao com acerto financeiro existente, mostrar alerta de que o acerto sera recalculado automaticamente.

### Pedidos

- Substituir coluna Pedido Externo por Cliente.
- Adicionar coluna Desconto indicando Sim/Nao ou valor, conforme melhor encaixe visual.
- Manter a origem visivel para compatibilidade operacional.

### Detalhes do Pedido

- Exibir card de cliente quando existir.
- Exibir desconto quando existir; pedidos sem desconto devem mostrar ausencia de forma clara.

### Financeiro

- Adicionar coluna Desconto na listagem da Apuracao financeira.
- Manter Total de Custos como custo consolidado ja incluindo desconto.
- Exibir desconto no detalhe do Acerto financeiro.

## Configuration Changes

None.

## Dependencies

No new NuGet package is required.

## Security Considerations

- Dados de cliente e desconto sao entrada do usuario; Razor deve continuar usando encoding padrao.
- Normalizacao de nome deve evitar caracteres nao imprimiveis antes de comparar/persistir.
- A constraint unica de Nome Normalizado deve ser tratada no service/repository para evitar duplicidade em concorrencia.
- Autocomplete deve listar apenas dados de clientes ja disponiveis para usuarios autorizados a criar pedidos manuais.
- Politicas atuais `ApplicationPolicies.Read` e `ApplicationPolicies.Write` devem ser preservadas.

## Performance Considerations

- Listagem de clientes para autocomplete deve ser simples e ordenada; para o volume atual, carregar todos os clientes cadastrados e suficiente.
- Consultas de pedido podem incluir cliente via join/navegacao sem N+1.
- `SettlementCalculator` deve continuar evitando calculos client-side desnecessarios alem do padrao atual.
- Indices em `customers.normalized_name`, `orders.customer_id`, `financial_settlements.order_id` preservam consultas esperadas.

## Observability

No telemetry new is required.

Auditoria EF existente registrara criacao/alteracao de clientes, pedidos e acertos.

## Testing Strategy

### Unit Tests

- `CustomerNormalizer` remove espacos/caracteres nao imprimiveis e aplica maiusculas.
- `Customer` rejeita nome vazio e expoe Nome Normalizado.
- `Order` aceita cliente opcional e desconto opcional.
- `Order` rejeita desconto com descricao vazia quando valor maior que zero.
- `FinancialSettlement` soma desconto em custo total e margem.

### Service/Integration Tests

- `ManualOrderService` cria cliente novo ao salvar pedido manual.
- `ManualOrderService` reutiliza cliente existente por Nome Normalizado.
- `ManualOrderService` permite pedido manual sem cliente/desconto.
- `ManualOrderService` calcula desconto percentual sobre total bruto dos itens.
- `ManualOrderService` aceita desconto maior que receita e deixa dados disponiveis para alerta.
- `ManualOrderService.UpdateAsync` recalcula acerto quando desconto muda em pedido com acerto.
- `SettlementCalculator` inclui desconto no acerto.
- `FinancialSettlementRepository` persiste e carrega `DiscountAmount`.
- `OrderRepository` persiste e carrega cliente/desconto.

### View/Contract Tests

- `ManualOrderCreatePage` renderiza blocos de cliente/desconto, autocomplete e alertas.
- `OrdersIndexPage` nao renderiza coluna Pedido Externo e renderiza Cliente/Desconto.
- `OrderDetailsPage` renderiza card de cliente e desconto.
- `FinanceIndexPage` renderiza coluna Desconto.
- `FinancialSettlementDetailsPage` renderiza item de desconto.
- Views MVC passam novos parametros aos componentes.

### Build/Test Validation

- `dotnet build`
- `dotnet test`

## Implementation Tasks

### Task 1 - Add customer domain and persistence

Affected:

- Sales domain
- EF entities/configurations
- repositories
- DI

Changes:

- create `Customer`, `CustomerNormalizer`, `ICustomerRepository`, `CustomerRepository`;
- add `CustomerEntity`, configuration, DbSet and DI registration;
- create migration with `customers` table and unique normalized name.

Validation:

- customer normalization tests;
- repository persistence tests.

Dependencies:

None.

### Task 2 - Extend order with customer and discount

Affected:

- `Order`
- `OrderEntity`
- `OrderConfiguration`
- `OrderRepository`
- manual order input model

Changes:

- add optional customer reference to order;
- add optional discount data to order;
- update repository mappings;
- update migration with nullable order columns;
- preserve imports and old orders with null customer and zero/no discount.

Validation:

- order domain tests;
- repository round-trip tests;
- old order creation tests continue passing.

Dependencies:

Task 1.

### Task 3 - Update manual order create/edit behavior

Affected:

- `ManualOrderService`
- `ManualOrdersController`
- `ManualOrderInputModel`

Changes:

- populate existing customers for autocomplete;
- resolve/create customer by Nome Normalizado on save;
- calculate percentage discount from gross item total;
- require discount description when discount amount is greater than zero;
- allow commercial-only updates on orders with settlement where operational data remains unchanged;
- trigger financial recalculation after relevant update when settlement exists.

Validation:

- service tests for create without customer/discount;
- create with new customer;
- reuse existing customer;
- percentage discount;
- discount greater than revenue;
- recalc settlement after edit.

Dependencies:

Tasks 1 and 2.

### Task 4 - Extend financial settlement calculation and persistence

Affected:

- `FinancialSettlement`
- `SettlementCalculator`
- `FinancialSettlementRepository`
- `FinanceAppService`
- financial view models

Changes:

- add `DiscountAmount`;
- include discount in total cost and margin;
- persist/load discount;
- add replace/update operation for recalculation;
- invalidate finance cache on replacement;
- expose discount in list/detail view models.

Validation:

- settlement calculator tests with and without discount;
- finance summary test where total costs include discount;
- detail/list tests expose discount.

Dependencies:

Task 2.

### Task 5 - Update order query models and screens

Affected:

- `OrderQueryService`
- order view models
- order MVC views
- order Razor components

Changes:

- list customer name and discount indicator/value;
- replace Pedido Externo column with Cliente;
- add discount column;
- details card for customer;
- details discount display.

Validation:

- view/contract tests;
- query service tests for missing customer/discount compatibility.

Dependencies:

Tasks 1 and 2.

### Task 6 - Update manual order UI

Affected:

- `ManualOrderCreatePage.razor`
- `Create.cshtml`
- `site.css`

Changes:

- add Cliente block;
- add Desconto block;
- add datalist/autocomplete;
- update client-side summary to compute gross total, discount estimate and warning;
- show server validation messages through existing summary pattern;
- show recalc warning in edit when applicable.

Validation:

- view/contract tests;
- existing manual order form tests updated.

Dependencies:

Task 3.

### Task 7 - Update finance UI

Affected:

- finance MVC views
- finance Razor components

Changes:

- add Desconto column in Apuracao financeira;
- add discount item in Acerto financeiro details;
- ensure Total de Custos and Margem display values from updated model.

Validation:

- view/contract tests for finance index/details.

Dependencies:

Task 4.

### Task 8 - Migration, build and full validation

Affected:

- EF migration and model snapshot
- test suite

Changes:

- generate migration through existing EF Core workflow;
- review generated migration and snapshot;
- run build and tests.

Validation:

- `dotnet build`
- `dotnet test`

Dependencies:

Tasks 1-7.

## Requirement Traceability

| Requirement | Implementation |
|---|---|
| FR-001, FR-002, FR-003, FR-004, FR-006 | Tasks 3 and 6 |
| FR-005 | Tasks 1, 2 and 8 |
| FR-007 | Tasks 1, 3 and 6 |
| FR-008, FR-009 | Task 5 |
| FR-010, FR-011, FR-012 | Tasks 2, 3 and 6 |
| FR-013, FR-014 | Task 5 |
| FR-015, FR-016, FR-017, FR-018, FR-019 | Tasks 4 and 7 |
| FR-020, FR-021 | Task 1 |
| FR-022 | Tasks 3 and 4 |
| FR-023 | Tasks 2 and 3 |
| BR-001, BR-002, BR-003, BR-005, BR-006, BR-007 | Tasks 2, 3, 4 and 6 |
| BR-004 | Tasks 2, 4 and 8 |
| BR-008 | Task 1 |

## Backward Compatibility

- Existing orders remain valid because customer and discount fields are nullable/zero.
- Existing financial settlements remain valid because `discount_amount` defaults to zero.
- Existing routes and controller actions remain unchanged.
- Existing import flow can keep constructing orders without customer/discount.
- Existing finance cache invalidation prefix remains usable.

## Migration Strategy

1. Create `customers` table.
2. Add nullable customer and discount columns to `orders`.
3. Add non-null `discount_amount` to `financial_settlements` with default zero.
4. Add indexes and FK.
5. Deploy application code that handles null customer and zero discount.

No backfill of old orders is required.

## Rollback Considerations

Rollback removes new customer and discount columns/tables, losing data entered after deployment. No existing pre-feature data requires transformation.

## Risks

### Editing settled orders

The existing application blocks editing orders with shipments. Allowing all edits could corrupt production/shipping history.

Mitigation:

Allow only commercial-safe edits when an acerto exists and operational data remains unchanged. Recalculate the financial settlement after those edits.

### Customer uniqueness race

Two concurrent requests can try to create the same normalized customer.

Mitigation:

Use a unique index and handle duplicate persistence by re-querying the existing customer.

### Financial cache staleness

Recalculation must invalidate finance summaries and lists.

Mitigation:

Use repository methods that remove `finance:` cache prefix for add/replace/delete.

### UI warning semantics

Discount greater than revenue must be accepted with a clear alert, not blocked.

Mitigation:

Implement front-end warning and server-side non-blocking warning state where the page is re-rendered; do not add a hard validation error for this case.

## Assumptions

- "Clientes cadastrados" refers to the new `customers` table populated during manual order saves.
- Nome Normalizado uniqueness is sufficient even if phone/email differ.
- Desconto stores the calculated monetary amount; the percentage flag does not need to be preserved as financial source of truth unless implementation keeps it for edit UX.
- Pedidos importados are identified by source not being the manual source currently used by manual creation; they will not be offered customer/discount editing in this feature.

## Open Technical Questions

None blocking.
