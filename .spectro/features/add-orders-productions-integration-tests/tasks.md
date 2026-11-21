<!-- spectro-managed: true -->
# Tarefas: Testes de integração dos controllers de pedidos e produção

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Inspecionar e preparar o harness HTTP de testes, incluindo dependências de teste estritamente necessárias, substituição do `AppDbContext` por InMemory, isolamento por cenário e estratégia para o `MigrateAsync` do startup | `FR-007`, `NFR-001`, `NFR-002`, `NFR-004` | `AC-011` | — | completed |
| `TASK-002` | Criar autenticação/autorização de teste, antiforgery e fixture de tempo/dados sem credenciais ou PII reais | `FR-005`, `NFR-002`, `NFR-004` | `AC-008`, `AC-009`, `AC-011` | `TASK-001` | completed |
| `TASK-003` | Implementar testes HTTP das consultas de `OrdersController` e `ProductionController`, incluindo views, partials, JSON, filtros, datas, `404` e cache sem armazenamento | `FR-001`, `NFR-001` | `AC-001`, `AC-011` | `TASK-001`, `TASK-002` | completed |
| `TASK-004` | Implementar testes HTTP de `SendToProduction`, `RevertProduction` e `Delete`, incluindo efeitos em pedido/tarefas/estoque/auditoria, falhas de pré-condição, `TempData` e idempotência | `FR-002`, `FR-004`, `FR-005`, `FR-006` | `AC-002`, `AC-003`, `AC-004`, `AC-006`, `AC-007`, `AC-008`, `AC-009`, `AC-010` | `TASK-001`, `TASK-002` | completed |
| `TASK-005` | Implementar testes HTTP de `ProductionController.Advance` para `start`, `complete`, `cancel`, estados inválidos, tarefa inexistente, transição do pedido e auditoria | `FR-003`, `FR-004`, `FR-005`, `FR-006` | `AC-005`, `AC-006`, `AC-008`, `AC-009`, `AC-010` | `TASK-001`, `TASK-002` | completed |
| `TASK-006` | Implementar cenários internos de concorrência/rollback suportados pelo harness, executar gates e registrar limitações do InMemory e evidências reproduzíveis | `FR-006`, `FR-007`, `NFR-003`, `NFR-004` | `AC-011`, `AC-012`, `AC-013`, `AC-014` | `TASK-004`, `TASK-005` | completed |

## Detalhamento das tarefas

### TASK-001 — Harness HTTP e persistência de teste

- **Componentes afetados:** `tests/Craftsman.Tests/Craftsman.Tests.csproj`, novo harness/fixture em `tests/Craftsman.Tests/Integration`, `src/App/Program.cs` somente se for indispensável um seam de inicialização de teste.
- **Conclusão:** host HTTP sobe com DI real, banco InMemory isolado e sem migração relacional incompatível; recursos são descartados ao fim do cenário.
- **Testes esperados:** smoke test de rota protegida e verificação de isolamento entre duas instâncias/cenários.
- **Risco:** `Program` executa `MigrateAsync`; qualquer ajuste deve ser mínimo, restrito ao ambiente de teste e revisado antes de alterar produção.

### TASK-002 — Identidade, antiforgery, tempo e dados

- **Componentes afetados:** helpers/fixtures de teste, pipeline de autenticação e geração de dados.
- **Conclusão:** usuários anônimo, `Consulta` e `Operador` podem ser exercitados; POSTs conseguem obter/enviar token antiforgery; relógio e dados são determinísticos.
- **Testes esperados:** requests anônimos, role sem `Write`, role com `Write`, token ausente/inválido e fixture com dados mínimos válidos.
- **Risco:** modificar a autenticação real para facilitar o teste pode mascarar falhas; preferir handler/serviço de teste substituído no host.

### TASK-003 — Consultas HTTP

- **Componentes afetados:** `OrdersController`, `ProductionController`, `OrderQueryService`, `ProductionScheduleService`, views/partials e fixture InMemory.
- **Conclusão:** `Index`, `Details`, `CurrentMonth` e `Calendar` são exercitados pelo pipeline HTTP, com status, modelo/conteúdo, datas, filtros, `404` e cabeçalhos relevantes verificados.
- **Testes esperados:** pedido existente/inexistente, tarefa filtrada, data fixa, resposta JSON, partial `_Calendar` e `NoStore`.
- **Risco:** asserts de HTML excessivamente frágeis; validar contrato e dados relevantes, não markup incidental.

### TASK-004 — Operações de pedidos

- **Componentes afetados:** `OrdersController`, `OrderProductionPlanningService`, `ManualOrderService`, repositórios de pedidos/tarefas/estoque/remessas, auditoria e `TempData`.
- **Conclusão:** sucesso e rejeições de `SendToProduction`, `RevertProduction` e `Delete` verificam redirect, estado e efeitos persistidos, mensagens seguras e idempotência documentada.
- **Testes esperados:** estoque suficiente/insuficiente, produto/BOM inválidos, remessa ou tarefa concluída, reversão repetida, exclusão e ausência de duplicação.
- **Risco:** o fluxo envolve muitos agregados; verificar invariantes após cada falha para não aceitar efeitos parciais.

### TASK-005 — Avanço de produção

- **Componentes afetados:** `ProductionController`, `ProductionScheduleService`, repositórios de tarefas/pedidos, auditoria e `TempData`.
- **Conclusão:** `start`, `complete` e `cancel` validam estados permitidos, transição do pedido, redirect, auditoria e erros sem alteração indevida.
- **Testes esperados:** cada transição válida, tarefa inexistente, ação desconhecida e transição inválida, com usuário autorizado e não autorizado.
- **Risco:** completar/cancelar pode ter regras diferentes por estado; não generalizar além do comportamento observado.

### TASK-006 — Concorrência, rollback e gates

- **Componentes afetados:** harness, serviços reais e seams de falha/conflito no host de teste; configuração `.spectro/config.yaml` e evidências Spectro.
- **Conclusão:** cenários internos suportados são reproduzíveis; efeitos duplicados/parciais são verificados; limitações do InMemory são registradas; gates são executados com resultados reais.
- **Testes esperados:** duas operações controladas sobre o mesmo recurso, falha após etapa intermediária, preservação/rollback observável e execução dos comandos de build, teste, segurança e cobertura.
- **Risco:** InMemory não oferece semântica de transação/lock relacional; um teste que dependa disso deve ser marcado como não comprovável nesta feature, não falsamente aprovado.

## Regras de execução

- Não implementar `Import`.
- Não alterar regras de negócio, contratos públicos ou adicionar infraestrutura externa.
- Preferir fixtures e helpers compartilhados somente quando reduzirem duplicação real; evitar abstrações genéricas sem semântica de teste.
- Cada teste deve criar dados isolados e limpar/liberar host, `HttpClient`, banco e serviços ao finalizar.
- Os testes devem observar comportamento através de HTTP; acesso direto ao `DbContext` fica restrito à preparação e à verificação persistida.
- Se a limitação do InMemory impedir comprovar `AC-012` ou `AC-013`, registrar o caso como limitação/risco e não mascará-lo com um teste artificial.
- Usar o agente `integration-tests` na implementação das tarefas de código de teste, conforme `CON-002`.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004`, `TASK-005`, `TASK-006`, `TASK-REV-001`, `TASK-REV-003`, `TASK-REV-004`
- Bloqueadas: —

## Evidências de implementação

- Harness HTTP real com `WebApplicationFactory<Program>` e `ConfigureTestServices` em `tests/Craftsman.Tests/Integration/Http`.
- `AppDbContext` substituído por EF Core InMemory com nome único por factory/cenário; contexts e factories são descartados pelos testes.
- O startup mantém `MigrateAsync` fora do ambiente normal e o ignora somente quando `ASPNETCORE_ENVIRONMENT=Testing`.
- Smoke tests cobrem redirecionamento HTTP de rota protegida e isolamento entre duas instâncias do harness.
- Verificações: `dotnet restore tests/Craftsman.Tests/Craftsman.Tests.csproj` (êxito); `dotnet build tests/Craftsman.Tests/Craftsman.Tests.csproj --no-restore --warnaserror -v minimal -m:1 -p:UseSharedCompilation=false` (êxito, 0 warnings); `dotnet test tests/Craftsman.Tests/Craftsman.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~Craftsman.Tests.Integration.Http.TestWebApplicationFactoryTests" -v minimal -m:1` (2 aprovados).
- TASK-002/TASK-003: handler de autenticação por role, `FixedTimeProvider`, dados determinísticos, helper antiforgery, consultas HTTP de Orders/Production, `404`, filtros, JSON, partial e `NoStore`.
- Teste focado final: `dotnet test tests/Craftsman.Tests/Craftsman.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~OrdersAndProductionQueryTests|FullyQualifiedName~TestWebApplicationFactoryTests" -v minimal` (8 aprovados).
- TASK-004/TASK-005: testes HTTP de planejamento rejeitado, reversão, exclusão, avanço `start`/`complete`/`cancel`, autorização e antiforgery, com verificação de efeitos persistidos.
- TASK-006: `dotnet build Craftsman.slnx --no-restore --warnaserror` passou; `dotnet test Craftsman.slnx --no-restore` passou com 397 testes; gate de segurança de pacotes passou; cobertura com `-AllowDirtyTree` passou (88,15% linhas, 73,26% branches, 97,23% linhas novas, 92,70% branches novos). O gate de cobertura padrão não pôde medir a árvore suja sem `-AllowDirtyTree`.
- Limitações: o EF Core InMemory não comprova locks, isolamento, concorrência distribuída, rollback transacional relacional ou falha entre etapas; nenhum teste artificial foi adicionado para alegar `AC-012`/`AC-013`. A idempotência observada nesta suíte limita-se à repetição HTTP de reversão sem tarefas remanescentes.

- TASK-REV-001: testes HTTP adicionados para planejamento elegível, consumo de estoque e auditoria; rejeições de estoque insuficiente, produto inativo, BOM inválida, tarefa inexistente e transição inválida; ausência de efeitos parciais verificada. Suíte atual: 404 testes aprovados.
- TASK-REV-002: o runner oficial foi renovado em `gate-20261121T195938Z`; build, testes e segurança passaram, mas cobertura permanece `failed` porque o script exige árvore tracked limpa. A execução explícita com `-AllowDirtyTree` passou: 88,15% linhas, 73,42% branches, 97,23% linhas novas e 92,70% branches novos. A tarefa permanece pendente até haver estado limpo ou autorização formal para a exceção.
- TASK-REV-004: reversão HTTP após planejamento verifica compensação `Inbound`, auditoria, normalização e idempotência; exclusão após planejamento verifica remoção de tarefa e compensação de estoque. Suíte atual: 406 testes aprovados.

## TASK-REV-001 - Remediate FINDING-001

- **Status:** completed
- **Finding:** FINDING-001
- **Severity:** High
- **Return stage:** implement
- **Description:** Cobrir pelo pipeline HTTP o planejamento bem-sucedido de pedido elegível, consumo de estoque e auditoria, além das rejeições documentadas de estoque insuficiente, produto inativo/BOM inválida, tarefa inexistente e transições inválidas; verificar ausência de efeitos parciais.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.


## TASK-REV-002 - Remediate FINDING-002

- **Status:** pending
- **Finding:** FINDING-002
- **Severity:** High
- **Return stage:** gates
- **Description:** Regenerar gates.json e evidências sobre o estado final da implementação; obter cobertura no modo oficial sem falha por árvore suja ou registrar/obter a aprovação necessária para a exceção.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.
- **Diagnostic:** bloqueada operacionalmente pela política do gate de cobertura; não é uma falha de código ou de limiar de cobertura.


## TASK-REV-003 - Remediate FINDING-003

- **Status:** completed
- **Finding:** FINDING-003
- **Constraints covered:** `CON-001`, `CON-003`, `CON-004`
- **Severity:** Medium
- **Return stage:** plan
- **Description:** Corrigir a rastreabilidade dos constraints CON-001, CON-003 e CON-004 para tarefas/plano, ou documentar explicitamente por que a validação é coberta fora do mapeamento automático.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.


## TASK-REV-004 - Remediate FINDING-004

- **Status:** completed
- **Finding:** FINDING-004
- **Severity:** High
- **Return stage:** implement
- **Description:** Adicionar teste HTTP que planeje um pedido elegível, execute RevertProduction e verifique movimentos Inbound compensatórios auditáveis, ausência de duplicação na repetição e exclusão de dependências de produção; manter limitações do InMemory explícitas.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.

- **Evidence:** `OrdersProductionMutationTests` covers planning through HTTP, compensating Inbound movement and `ProductionPlanningReverted` audit, repeated reversal without duplicate compensation, and deletion of an order with its production task and stock compensation. The EF Core InMemory limitation remains explicit: relational transaction/lock semantics are not proven.

