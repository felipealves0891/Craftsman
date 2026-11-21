<!-- spectro-managed: true -->
# Plano: Testes de integração dos controllers de pedidos e produção

## Contexto e objetivo técnico

O repositório já possui um harness HTTP baseado em `WebApplicationFactory<Program>`, autenticação de teste, antiforgery, relógio fixo e persistência EF Core InMemory isolada por factory. Também existem testes HTTP para consultas, mutações de produção, reversão, exclusão e avanço de tarefas.

Este plano consolida a cobertura existente e define o incremento pendente para reforçar `OrdersController.SendToProduction`, principalmente contra o risco documentado de consumir estoque parcialmente quando o planejamento é rejeitado. A implementação deve usar o agente `integration-tests` e permanecer autocontida, sem PostgreSQL, containers, rede, serviços externos ou novos pacotes.

## Solução proposta

1. Manter `TestWebApplicationFactory` como fronteira HTTP real, com `Testing`, `ConfigureTestServices`, autenticação por role e `AppDbContext` InMemory com nome único.
2. Reutilizar `TestDataFixture` e `HttpTestHelpers`, ampliando-os apenas para representar pedidos com múltiplos itens/necessidades agregadas e estados inválidos necessários aos critérios.
3. Exercitar `SendToProduction` por POST, obtendo o token antiforgery por GET e verificando a resposta HTTP e o estado persistido em um novo contexto.
4. Para cada rejeição, comparar pedido, tarefas, movimentos `Outbound` e auditoria; a auditoria de tentativa pode existir, mas não deve ser confundida com planejamento efetivo.
5. Cobrir repetição e pedido inexistente conforme o contrato observado. Para concorrência, executar somente o cenário que o harness InMemory consiga demonstrar; caso contrário, registrar a limitação sem simular semântica relacional.
6. Executar os gates configurados e registrar apenas evidências reais.

## Arquitetura e contratos

### Componentes existentes

- `src/App/Program.cs`: composição MVC, autenticação, autorização, antiforgery e bypass de migração somente no ambiente `Testing`.
- `src/App/Controllers/OrdersController.cs`: `SendToProduction`, `RevertProduction` e `Delete`; `SendToProduction` redireciona para detalhes e registra auditoria.
- `src/Domains/Production/Services/OrderProductionPlanningService.cs`: valida pedido/tarefas, planeja, consome estoque, atualiza status e publica efeitos persistidos.
- `src/Domains/Production/Services/ProductionPlanner.cs`: valida produto/BOM, agrega necessidades e calcula tarefas/consumo.
- `src/Domains/Inventory/Services/InventoryService.cs`: verifica disponibilidade e cria movimentos de estoque.
- `src/Infra/Services/ServiceCollectionExtensions.cs`: DI real de repositórios, serviços, auditoria e `AppDbContext`.
- `tests/Craftsman.Tests/Integration/Http/TestWebApplicationFactory.cs`: host HTTP e isolamento InMemory.
- `tests/Craftsman.Tests/Integration/Http/TestDataFixture.cs`: dados determinísticos de pedido, produto, BOM, matéria-prima, estoque e tarefa.
- `tests/Craftsman.Tests/Integration/Http/OrdersProductionMutationTests.cs`: cobertura HTTP atual de planejamento, rejeições, reversão, exclusão e avanço.

### Fluxo de dados

`HttpClient` → roteamento MVC → autenticação/autorização → antiforgery → `OrdersController.SendToProduction` → `OrderProductionPlanningService` → planner/estoque/repositórios → `AppDbContext` InMemory → redirect, auditoria e estado persistido.

Preparação e verificação podem acessar o `DbContext` diretamente; a operação sob teste deve atravessar HTTP. Não serão usados serviços externos nem doubles para substituir o fluxo normal.

### Contratos observáveis

- Sucesso: redirect para `/Orders/Details/{id}`, pedido `ReadyForProduction`, tarefas planejadas, movimentos `Outbound` correspondentes e auditoria `SendToProduction` com `Planned = true`.
- Rejeição de planejamento: redirect conforme contrato, pedido preservado, nenhuma tarefa ou saída de produção parcial e auditoria com `Planned = false` quando a operação chegar ao serviço.
- Segurança: anônimo/role sem `Write` rejeitado antes da operação; POST sem antiforgery rejeitado sem efeitos persistentes.
- Repetição: nenhuma segunda tarefa ou consumo para o mesmo pedido; texto exato da mensagem permanece dependente do comportamento confirmado.
- Concorrência: somente resultado demonstrável pelo InMemory; locks, isolamento, serialização e deadlocks PostgreSQL permanecem fora da garantia.

## Alternativas consideradas

- **PostgreSQL real ou container:** rejeitado por `CL-005`; adicionaria dependência externa e não é necessário para os contratos desta suíte.
- **Instanciar o controller diretamente:** rejeitado porque não valida roteamento, autenticação, autorização ou antiforgery.
- **Mocks para planner/estoque no caminho normal:** rejeitado porque esconderia persistência e efeitos parciais; doubles só seriam admissíveis para uma falha interna controlada, se já houver seam apropriado.
- **Criar abstrações de produção para facilitar testes:** rejeitado por `CON-004`; primeiro devem ser esgotados o harness e as extensões de DI existentes.

## Riscos e compatibilidade

- O target efetivo observado é `net10.0`, apesar de a constituição mencionar .NET 8; o plano preserva o target atual e não propõe migração.
- InMemory não reproduz constraints, SQL, transações relacionais, locks ou isolamento; AC-012/AC-019 só serão validados no limite que o harness comprovar.
- A autorização por recurso/tenant não foi demonstrada; a suíte cobre as policies globais existentes e registra essa limitação.
- Fixtures com múltiplos itens podem ficar frágeis se assumirem detalhes incidentais do modelo; devem verificar invariantes e efeitos, não markup ou ordenação não contratual.
- O fluxo registra auditoria de tentativa mesmo quando o planejamento retorna falso; os testes devem distinguir tentativa rejeitada de efeito de negócio efetivo.

## Estratégia de testes

- **Baseline existente:** preservar os testes HTTP de consultas, harness, isolamento, autenticação, antiforgery, reversão, exclusão e avanço já implementados.
- **Sucesso de `SendToProduction`:** pedido elegível, status, quantidade de tarefas, consumo por matéria-prima, referência da tarefa e auditoria.
- **Falhas sem efeitos parciais:** estoque agregado insuficiente, produto inativo, BOM ausente/inválida, item sem produto e pedido inexistente.
- **Segurança específica:** anônimo, `Consulta`, `Operador` sem token e token inválido no endpoint de envio.
- **Repetição:** segunda submissão após planejamento confirmado, verificando contagens e referências persistidas.
- **Concorrência/rollback:** apenas cenários internos suportados; se InMemory não comprovar a propriedade, registrar a limitação e não criar teste artificial.
- **Validação:** executar build, testes, segurança de pacotes e cobertura conforme `.spectro/config.yaml`, sem alegar execução não realizada.

## Rastreabilidade

| Requisito | Critérios de aceite | Tarefa |
|---|---|---|
| `FR-001`, `NFR-001` | `AC-001` | `TASK-003` |
| `FR-002` | `AC-002` a `AC-004` | `TASK-004` |
| `FR-003` | `AC-005` | `TASK-005` |
| `FR-004` | `AC-006`, `AC-007` | `TASK-004`, `TASK-005`, `TASK-008` |
| `FR-005` | `AC-008`, `AC-009`, `AC-020` | `TASK-002`, `TASK-009` |
| `FR-006` | `AC-010`, `AC-018`, `AC-019` | `TASK-004`, `TASK-008`, `TASK-010` |
| `FR-007`, `NFR-002`, `NFR-004` | `AC-011` | `TASK-001`, `TASK-011` |
| `FR-007`, `NFR-003` | `AC-014` | `TASK-011` |
| `FR-008` | `AC-015` a `AC-020` | `TASK-007` a `TASK-010` |
| `FR-009` | `AC-016`, `AC-017`, `AC-019` | `TASK-008`, `TASK-010` |
| `CON-002`, `CON-004` | `AC-011`, `AC-014` | `TASK-007`, `TASK-011` |
