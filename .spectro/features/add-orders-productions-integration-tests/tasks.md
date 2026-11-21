<!-- spectro-managed: true -->
# Tarefas: Testes de integração dos controllers de pedidos e produção

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Manter harness HTTP, isolamento InMemory e descarte de recursos por cenário | `FR-007`, `NFR-001`, `NFR-002`, `NFR-004` | `AC-011` | — | completed |
| `TASK-002` | Manter autenticação de teste por role, antiforgery, relógio fixo e dados sem credenciais reais | `FR-005`, `NFR-002`, `NFR-004` | `AC-008`, `AC-009`, `AC-011` | `TASK-001` | completed |
| `TASK-003` | Manter testes HTTP existentes das consultas de pedidos e produção | `FR-001`, `NFR-001` | `AC-001` | `TASK-001`, `TASK-002` | completed |
| `TASK-004` | Manter testes HTTP existentes de `RevertProduction`, `Delete` e efeitos compensatórios | `FR-002`, `FR-006` | `AC-003`, `AC-004`, `AC-010` | `TASK-001`, `TASK-002` | completed |
| `TASK-005` | Manter testes HTTP existentes de `ProductionController.Advance` | `FR-003`, `FR-004`, `FR-005` | `AC-005`, `AC-006`, `AC-008`, `AC-009` | `TASK-001`, `TASK-002` | completed |
| `TASK-006` | Auditar lacunas da cobertura atual de `SendToProduction` contra a matriz de critérios | `FR-008`, `FR-009`, `CON-003` | `AC-015` a `AC-020` | `TASK-004` | completed |
| `TASK-007` | Ampliar fixtures e testes de sucesso/rejeição de `SendToProduction`, incluindo necessidade agregada e ausência de efeitos parciais | `FR-008`, `FR-009` | `AC-015`, `AC-016`, `AC-017` | `TASK-006` | completed |
| `TASK-008` | Cobrir pedido inexistente e segunda submissão, verificando duplicação de tarefas, consumo e auditoria conforme contrato observado | `FR-004`, `FR-006`, `FR-008` | `AC-006`, `AC-010`, `AC-018` | `TASK-006`, `TASK-007` | completed |
| `TASK-009` | Cobrir autenticação, policy `Write` e antiforgery especificamente no endpoint `SendToProduction` | `FR-005`, `FR-008` | `AC-008`, `AC-009`, `AC-020` | `TASK-002`, `TASK-006` | completed |
| `TASK-010` | Avaliar e, somente se demonstrável no InMemory, testar concorrência interna; caso contrário registrar formalmente a limitação | `FR-006`, `FR-008`, `FR-009` | `AC-012`, `AC-019` | `TASK-007`, `TASK-008` | completed |
| `TASK-011` | Executar gates e registrar evidências reais, limitações e ausência de dependências externas | `FR-007`, `NFR-003`, `NFR-004`, `CON-001`, `CON-002`, `CON-004` | `AC-011`, `AC-014` | `TASK-007`, `TASK-008`, `TASK-009`, `TASK-010` | pending |

## Detalhamento das tarefas

### TASK-006 — Auditoria de lacunas

- **Componentes afetados:** `OrdersProductionMutationTests`, `TestDataFixture`, `TestWebApplicationFactory` e contratos de `OrdersController`.
- **Conclusão:** matriz explícita identifica o que já cobre `AC-015`–`AC-020` e o que precisa de teste; nenhuma dependência externa é introduzida.
- **Risco:** não transformar comportamento inferido em requisito; confirmar status, redirect e auditoria no código existente.

### TASK-007 — Sucesso e rejeições sem efeitos parciais

- **Componentes afetados:** fixtures e testes em `tests/Craftsman.Tests/Integration/Http`.
- **Conclusão:** cenários cobrem sucesso, estoque insuficiente agregado, produto inativo, BOM inválida/ausente e item sem produto, verificando pedido, tarefas, movimentos `Outbound` e auditoria.
- **Testes esperados:** novos casos orientados a comportamento através de `HttpClient`; preparação/verificação via `AppDbContext` InMemory.
- **Risco:** InMemory não equivale ao PostgreSQL; limitar asserts ao contrato observável e registrar a limitação.

### TASK-008 — Repetição e inexistência

- **Componentes afetados:** `OrdersProductionMutationTests` e `TestDataFixture`.
- **Conclusão:** pedido inexistente e segunda chamada não deixam tarefas/consumos duplicados; a mensagem exata só será fixada se confirmada no contrato efetivo.
- **Risco:** não presumir idempotência além da ausência de duplicação documentada.

### TASK-009 — Segurança do endpoint

- **Componentes afetados:** `TestWebApplicationFactory`, `HttpTestHelpers` e testes HTTP.
- **Conclusão:** anônimo, `Consulta`, `Operador` sem token e token inválido são rejeitados antes de efeitos de planejamento.
- **Risco:** não alterar autenticação/autorização de produção; usar somente o handler de teste já existente.

### TASK-010 — Concorrência limitada pelo provedor

- **Componentes afetados:** harness e testes, apenas se o comportamento for demonstrável sem semântica relacional externa.
- **Conclusão:** ou existe teste interno reproduzível sem alegar locks/transações PostgreSQL, ou a limitação é registrada como evidência e `AC-012`/`AC-019` ficam explicitamente não comprovados por este provedor.
- **Risco:** alto risco de falso positivo se o InMemory for usado para simular serialização.

### TASK-011 — Gates e evidências

- **Componentes afetados:** `.spectro/config.yaml`, outputs da feature e resultados dos comandos configurados.
- **Conclusão:** build, testes, segurança e cobertura são executados com comandos reais; falhas operacionais e limitações são registradas sem mascaramento.
- **Risco:** cobertura pode exigir árvore limpa conforme o script; não usar exceção não aprovada como resultado oficial.

## Regras de execução

- Não implementar `Import` nem alterar regras de negócio, contratos públicos ou infraestrutura externa.
- Usar o agente `integration-tests` para implementação de testes/harness, conforme `CON-002`.
- Não adicionar pacotes ou serviços externos; reutilizar o harness e fixtures existentes.
- Operações sob teste devem atravessar HTTP; acesso direto ao `DbContext` fica restrito a preparação e verificação.
- Se uma limitação do InMemory impedir provar concorrência ou rollback relacional, registrar a limitação em vez de criar teste artificial.
- Manter tarefas pequenas, sem refatoração não relacionada.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004`, `TASK-005`, `TASK-006`, `TASK-007`, `TASK-008`, `TASK-009`, `TASK-010`.
- Pendentes: `TASK-011`.
- Bloqueadas: —

## Evidências de implementação

- `TASK-007`: fixture ampliada para dois itens/produtos compartilhando matéria-prima; teste confirma agregação antes do consumo e ausência de saída parcial.
- `TASK-008`: testes HTTP adicionados para pedido inexistente e repetição de `SendToProduction`, sem duplicação de tarefa/consumo.
- `TASK-009`: testes HTTP adicionados para role `Consulta` e POST de operador sem antiforgery.
- Teste focado: `dotnet test tests/Craftsman.Tests/Craftsman.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~OrdersProductionMutationTests" -v minimal` — 21 aprovados.
- Suíte HTTP: `dotnet test tests/Craftsman.Tests/Craftsman.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~Craftsman.Tests.Integration.Http" -v minimal` — 23 aprovados.
- Build: `dotnet build tests/Craftsman.Tests/Craftsman.Tests.csproj --no-restore --warnaserror -v minimal -m:1 -p:UseSharedCompilation=false` — 0 warnings, 0 erros.
- `TASK-010`: não foi adicionado teste artificial de concorrência; InMemory não comprova locks, isolamento, serialização ou deadlocks PostgreSQL.

## TASK-REV-001 - Remediate FINDING-001

- **Status:** completed
- **Finding:** FINDING-001
- **Severity:** Medium
- **Return stage:** plan
- **Description:** Vincular CON-001 a uma tarefa e validação explícitas no plano/tarefas, comprovando que a implementação não altera código de produção nesta feature.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.
## TASK-REV-002 - Remediate FINDING-002

- **Status:** completed
- **Finding:** FINDING-002
- **Severity:** Medium
- **Return stage:** plan
- **Description:** Adicionar AC-013 ao mapeamento de plano/tarefas ou esclarecer formalmente sua exclusão, incluindo a validação de rollback/preservação de consistência.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.

## TASK-REV-003 - Remediate FINDING-003

- **Status:** completed
- **Finding:** FINDING-003
- **Severity:** Medium
- **Return stage:** clarify
- **Description:** Alinhar NFR-003 com o target efetivo net10.0 ou registrar decisão formal que explique a compatibilidade exigida.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.
