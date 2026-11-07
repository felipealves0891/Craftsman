<!-- spectro-managed: true -->
# Tarefas: Reverter envio do pedido para produção

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Definir e implementar as regras de elegibilidade e a transição de pedido de `ReadyForProduction` para `Normalized`, bloqueando pedidos/tarefas com `Completed` ou estados de envio incompatíveis. | `FR-002`, `FR-004`, `CON-003`, `CON-005` | `AC-002`, `AC-004` | — | completed |
| `TASK-002` | Implementar o caso de uso de reversão com compensação de cada movimento `Outbound`, referência idempotente, exclusão das tarefas elegíveis e preservação do histórico original. | `FR-002`, `FR-003`, `FR-005`, `CON-002` | `AC-002`, `AC-003`, `AC-005` | `TASK-001` | completed |
| `TASK-003` | Integrar a operação à unidade transacional e à auditoria, preservando correlação, atomicidade e tratamento de conflitos concorrentes com avanço da produção. | `FR-005`, `FR-006`, `NFR-001`, `NFR-002`, `NFR-004` | `AC-005`, `AC-006`, `AC-007` | `TASK-001`, `TASK-002` | completed |
| `TASK-004` | Adicionar a ação POST autorizada no detalhe do pedido, confirmação de usuário, antiforgery, mensagens seguras e atualização das condições visuais; refletir a exclusão na agenda. | `FR-001`, `FR-007`, `CON-004` | `AC-001`, `AC-008`, `AC-009` | `TASK-001`, `TASK-002`, `TASK-003` | completed |
| `TASK-005` | Criar testes de domínio, serviço, persistência e concorrência para sucesso, rejeição por `Completed`, idempotência, compensação, auditoria e replanejamento. | `FR-002` a `FR-007`, `NFR-001`, `NFR-002`, `NFR-004` | `AC-002` a `AC-008` | `TASK-001`, `TASK-002`, `TASK-003` | completed |
| `TASK-006` | Criar testes de controller/UI e executar a regressão dos fluxos de envio, edição, cancelamento/avanço, agenda, estoque e financeiro, além dos gates configurados. | `FR-001`, `FR-007`, `NFR-003`, `CON-001` | `AC-001`, `AC-008`, `AC-009` | `TASK-004`, `TASK-005` | completed |

## Regras de execução

- Não alterar o fluxo de exclusão do pedido para implementar a reversão.
- Não apagar nem modificar movimentações de estoque existentes; somente criar entradas compensatórias rastreáveis.
- O servidor deve validar elegibilidade mesmo que a interface não exiba a ação.
- Tarefas `Planned`, `InProduction` e `Cancelled` são elegíveis; qualquer tarefa `Completed` bloqueia a operação inteira, sem efeitos parciais.
- O resultado aprovado deve deixar o pedido em `Normalized`, sem tarefas relacionadas e apto a chamar novamente o planejamento.
- Não criar migration sem evidência de necessidade após a implementação.
- Preservar mensagens sem detalhes técnicos e os limites de segurança/auditoria da constituição.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004`, `TASK-005`, `TASK-006`, `TASK-REV-001`
- Bloqueadas: —

## TASK-REV-001 - Remediate FINDING-001

- **Status:** completed
- **Finding:** FINDING-001
- **Severity:** Medium
- **Return stage:** implement
- **Description:** Validar a existência de remessas no servidor antes de reverter o planejamento de produção, rejeitando a operação quando houver qualquer remessa relacionada ao pedido e cobrindo o endpoint com teste de regressão.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.

