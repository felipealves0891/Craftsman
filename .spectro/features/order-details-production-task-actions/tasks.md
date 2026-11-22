<!-- spectro-managed: true -->
# Tarefas: Ações de tarefas na produção do detalhe do pedido

| ID | Tarefa | Requisitos | Critérios de aceite | Dependências | Componentes afetados | Conclusão verificável, testes esperados e risco | Status |
|---|---|---|---|---|---|---|---|
| `TASK-001` | Mapear as capacidades de ação nas linhas de tarefa do detalhe a partir do status existente, sem criar transições paralelas, preservando o filtro de tarefas pelo pedido. | `FR-003`, `FR-007`, `NFR-004`, `CON-002`, `CON-004` | `AC-003`, `AC-004`, `AC-005`, `AC-006`, `AC-013` | Nenhuma | `OrderQueryService`, `OrderDetailViewModel`, `OrderDetailsPage.razor`, modelos de UI, testes de consulta/renderização | Cada linha contém o identificador e capacidades coerentes com o status; pedido sem tarefas não renderiza ações. Testes direcionados de view model e renderização passaram. Risco: duplicar regra do calendário; capacidades foram reutilizadas do modelo existente. | completed |
| `TASK-002` | Adicionar a coluna de ações à tabela do detalhe com controles equivalentes aos do calendário, incluindo nomes acessíveis, estados desabilitados e confirmação explícita para `Cancelar`. | `FR-001`, `FR-002`, `FR-003`, `FR-008`, `NFR-002`, `CON-005` | `AC-001`, `AC-002`, `AC-003`, `AC-004`, `AC-005`, `AC-014` | `TASK-001` | `src/UI/Components/Orders/OrderDetailsPage.razor`, estilos/scripts somente se necessários, testes de interface | A tabela apresenta os três controles por tarefa, associa o `id` correto, desabilita ações incompatíveis e confirma cancelamento habilitado. Testes de renderização passaram cobrindo rótulos e estados. Risco residual: confirmação é interface; validação do servidor permanece ativa. | completed |
| `TASK-003` | Adaptar o avanço de produção para aceitar retorno contextual seguro ao detalhe, manter a autorização/antiforgery/auditoria e preservar o destino atual da agenda quando não houver contexto. | `FR-004`, `FR-005`, `FR-006`, `NFR-001`, `NFR-003`, `NFR-004`, `CON-001`, `CON-002`, `CON-003` | `AC-006`, `AC-007`, `AC-008`, `AC-009`, `AC-010`, `AC-011`, `AC-012` | `TASK-001` | `ProductionController`, `ProductionScheduleService`, contratos de POST/retorno, testes de controller/integração | A ação usa o fluxo existente; sucesso e erro contextual retornam ao mesmo pedido; chamadas sem contexto continuam compatíveis; contexto incompatível volta à agenda sem redirecionamento arbitrário. Testes unitários e HTTP direcionados passaram. | completed |
| `TASK-004` | Executar testes direcionados, revisão de segurança e gates do projeto, registrando evidências e verificando a matriz de aceitação completa. | Todos os requisitos aplicáveis | Todos os critérios aplicáveis | `TASK-001`, `TASK-002`, `TASK-003` | `tests/Craftsman.Tests`, solução, `.spectro/features/order-details-production-task-actions/gates.json` e evidências | Build, testes, segurança de pacotes e cobertura passaram conforme configuração. A validação automatizada cobriu renderização, controller e HTTP; não há harness de navegador configurado para aceitação visual manual. | completed |

## Regras de execução

- Não alterar as transições, invariantes, duração ou persistência das tarefas de produção.
- Não criar endpoint paralelo nem aceitar URL arbitrária para retorno.
- O contexto de retorno deve ser opcional e compatível com a agenda existente.
- A confirmação de cancelamento é adicional à validação do domínio; botão desabilitado não substitui autorização ou antiforgery.
- Não adicionar dependências externas ou migration para esta feature.
- Qualquer necessidade de alterar regras de negócio, autorização ou escopo deve ser registrada e aprovada antes da implementação.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`.
- Pendentes: nenhuma.

## Evidências desta execução

- Build: `dotnet build Craftsman.slnx --no-restore --warnaserror -v minimal -m:1 -p:UseSharedCompilation=false` — aprovado, 0 avisos e 0 erros.
- Testes direcionados: 32 aprovados, 0 falhas.
- Suíte completa: `dotnet test Craftsman.slnx --no-restore --no-build -m:1 -v minimal` — 431 aprovados, 0 falhas.
- Verificação de diff: `git diff --check` — aprovada.
- Gates Spectro finais: build, testes, segurança de pacotes e cobertura — aprovados na execução `gate-20261122T185911Z`.
- Cobertura final: linhas 88,68%, branches 73,88%, linhas novas 100,00% e branches novos acima do mínimo configurado.

