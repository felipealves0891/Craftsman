<!-- spectro-managed: true -->
# Tarefas: Reverter movimentação de estoque

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Definir o contrato mínimo de repositório e a transformação de uma movimentação original em compensatória, incluindo saída/entrada, ajuste com quantidade oposta, motivo literal e referência determinística. | `FR-005`, `FR-006`, `CON-001`, `CON-002`, `CON-005` | `AC-005`, `AC-006`, `AC-007` | — | completed |
| `TASK-002` | Implementar no serviço de aplicação o caso de uso de reversão, com validação de pedido/tarefas, exceção de exclusão, saldo, idempotência e unidade de trabalho atômica. | `FR-002`, `FR-003`, `FR-004`, `FR-005`, `FR-006`, `FR-007`, `FR-008`, `NFR-001`, `NFR-002`, `CON-003`, `CON-004` | `AC-002`, `AC-003`, `AC-004`, `AC-005`, `AC-006`, `AC-007`, `AC-008`, `AC-009` | `TASK-001` | completed |
| `TASK-003` | Expor o POST autorizado e protegido por antiforgery e adicionar a ação/confirmação na tela de movimentos, mantendo mensagens seguras e o padrão visual existente. | `FR-001`, `FR-009`, `NFR-003` | `AC-001`, `AC-010` | `TASK-002` | completed |
| `TASK-004` | Implementar no repositório EF Core a consulta por ID, exclusão excepcional, persistência da compensação, consulta de referência e invalidação de cache; adicionar migration somente se uma garantia de unicidade for necessária. | `FR-004`, `FR-005`, `FR-006`, `FR-007`, `FR-008`, `NFR-001`, `NFR-002`, `NFR-004` | `AC-004`, `AC-005`, `AC-006`, `AC-007`, `AC-008`, `AC-009` | `TASK-001`, `TASK-002` | completed |
| `TASK-005` | Criar testes unitários, de aplicação, controller, persistência e regressão para todos os critérios de aceite, incluindo idempotência e autorização. | `FR-001`, `FR-002`, `FR-003`, `FR-004`, `FR-005`, `FR-006`, `FR-007`, `FR-008`, `FR-009`, `NFR-001`, `NFR-002`, `NFR-003`, `NFR-004`, `CON-001`, `CON-002`, `CON-003`, `CON-004`, `CON-005` | `AC-001`, `AC-002`, `AC-003`, `AC-004`, `AC-005`, `AC-006`, `AC-007`, `AC-008`, `AC-009`, `AC-010` | `TASK-001` a `TASK-004` | completed |
| `TASK-006` | Executar build, testes, segurança de dependências, cobertura e revisão final de segurança; registrar evidências nos gates Spectro. | `NFR-001` a `NFR-004`, `CON-001` a `CON-005` | Todos | `TASK-005` | completed |

## Regras de execução

- Não editar ou apagar movimentações originais no caminho compensatório.
- A exclusão física é permitida somente para saída com `BusinessReference` nula/vazia e motivo exatamente `Production consumption`.
- Usar `ProductionTaskStatus.Completed` como bloqueio de pedido concluído e `OrderStatus.Delivered` como bloqueio de entrega.
- Reutilizar `ApplicationPolicies.Write`; não criar permissão nova nesta feature.
- Não introduzir dependência externa, novo modelo de estoque ou migração sem evidência técnica.
- A concorrência real contra PostgreSQL não faz parte desta entrega; não registrar testes InMemory ou sequenciais como prova de comportamento concorrente em produção.
- Alterações de requisitos e rastreabilidade devem listar IDs individualmente, sem intervalos não interpretados pelo validador atual.
- Preservar cancelamento, logs estruturados e mensagens sem dados sensíveis.
- Executar tarefas na ordem indicada, mantendo cada incremento compilável e testável.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004`, `TASK-005`, `TASK-006`
- Bloqueadas: nenhuma

