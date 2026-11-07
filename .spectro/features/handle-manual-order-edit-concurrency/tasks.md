<!-- spectro-managed: true -->
# Tarefas: Tratar concorrência ao editar pedido manual

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Ajustar o contrato de `LogData` para carregar a correlação da requisição, aceitar apenas um resumo seguro da edição e preservar o estado terminal de sucesso/erro durante o descarte. | `FR-003`, `NFR-001`, `NFR-003`, `CON-002` | `AC-003`, `AC-004` | — | completed |
| `TASK-002` | Instrumentar `ManualOrderService.UpdateAsync` com etapas mínimas e correlacionadas para leituras, validações, fronteiras de `SaveChangesAsync`, recálculo financeiro e planejamento, registrando falhas sem payload completo. | `FR-004`, `FR-005`, `NFR-003`, `CON-003` | `AC-003`, `AC-004` | `TASK-001` | completed |
| `TASK-003` | Atualizar o POST `ManualOrdersController.Edit` para criar o log seguro, capturar especificamente `DbUpdateConcurrencyException`, retornar a view `Create` com o modelo enviado e mensagem orientativa, e preservar o tratamento das demais exceções. | `FR-001`, `FR-002`, `FR-003`, `FR-006`, `NFR-002` | `AC-001`, `AC-002`, `AC-006` | `TASK-001`, `TASK-002` | completed |
| `TASK-004` | Verificar e ajustar o encerramento do fluxo de atualização para que sucesso, publicação/replanejamento e recálculo só sejam registrados/executados após gravação confirmada e que nenhuma etapa posterior seja iniciada após a exceção. | `FR-006`, `CON-004` | `AC-001`, `AC-005` | `TASK-002`, `TASK-003` | completed |
| `TASK-005` | Adicionar testes de controller, serviço, `LogData` e auditoria correlacionada cobrindo conflito, mensagem segura, caminho percorrido, ausência de efeitos posteriores, sucesso e regressões existentes. | `FR-001`–`FR-006`, `NFR-001`–`NFR-003`, `CON-001`–`CON-004` | `AC-001`–`AC-006` | `TASK-001`–`TASK-004` | completed |
| `TASK-006` | Executar os gates configurados e registrar evidências de build, testes, segurança e cobertura da mudança. | Todos | Todos | `TASK-005` | completed |

## Regras de execução

- Não adicionar migration, coluna de versão, dependência, retry, merge ou sobrescrita automática.
- Não registrar o `ManualOrderInputModel` completo, contatos de cliente, valores antes/depois, credenciais, tokens ou segredos.
- Reutilizar `HttpContext.TraceIdentifier`/`CurrentUserInfo.CorrelationId` como correlação; não criar um identificador paralelo.
- Limitar a instrumentação ao fluxo de edição manual e às entidades/efeitos relacionados à tentativa.
- Manter o tratamento existente de validação, `ArgumentException`, `ArgumentOutOfRangeException` e `InvalidOperationException`.
- Qualquer alteração de escopo ou necessidade de transação nova deve ser apresentada para aprovação antes da implementação.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004`, `TASK-005`
- Bloqueadas: nenhuma

## Evidências de implementação

- `TASK-001`: `dotnet test tests/Craftsman.Tests/Craftsman.Tests.csproj --no-restore -v minimal -m:1 -p:UseSharedCompilation=false --filter FullyQualifiedName~Craftsman.Tests.Domain.LogDataTests` — 3 aprovados.
- Build: `dotnet build tests/Craftsman.Tests/Craftsman.Tests.csproj --no-restore --warnaserror -v minimal -m:1 -p:UseSharedCompilation=false` — 0 avisos, 0 erros.
- `TASK-002`: `dotnet test tests/Craftsman.Tests/Craftsman.Tests.csproj --no-restore --no-build -v minimal -m:1 -p:UseSharedCompilation=false --filter FullyQualifiedName~Craftsman.Tests.Application.ManualOperationsTests` — 25 aprovados.
- `TASK-003`: `dotnet test tests/Craftsman.Tests/Craftsman.Tests.csproj --no-restore --no-build -v minimal -m:1 -p:UseSharedCompilation=false --filter FullyQualifiedName~Craftsman.Tests.Application.ManualOrdersControllerCoverageTests` — 1 aprovado.
- `TASK-004`/`TASK-005`: controller de concorrência, fluxo de serviço, `LogData` e regressões — 30 aprovados no conjunto focado; a exceção interrompe o fluxo antes do planejamento e a mensagem não expõe detalhes técnicos.
- Gate build: `dotnet build --no-restore --warnaserror -v minimal` — PASS, 0 avisos e 0 erros.
- Gate test: `dotnet test Craftsman.slnx --no-restore --no-build -v minimal -m:1 -p:UseSharedCompilation=false` — PASS, 334 aprovados.
- Gate coverage: `coverage-gate.ps1 -Project "." -BaseBranch "develop" -MinLineCoverage 75 -MinBranchCoverage 55 -MinNewLineCoverage 90 -MinNewBranchCoverage 85 -AllowDirtyTree` — PASS (81,88% linhas; 55,86% branches; 99,30% linhas novas; 91,43% branches novos). A execução sem `-AllowDirtyTree` é recusada pelo próprio gate por alterações locais não commitadas.
- Gate security: `dotnet restore` e `dotnet list package --vulnerable` — PASS com acesso ao feed NuGet; nenhum pacote vulnerável nos projetos `Craftsman`, `Craftsman.Domains`, `Craftsman.Infra`, `Craftsman.UI` e `Craftsman.Tests`.

