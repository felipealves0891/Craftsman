<!-- spectro-managed: true -->
# Tarefas: Corrigir validação e consumo de estoque ao enviar pedido para produção

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Refatorar a preparação do planejamento para validar produtos, fichas técnicas e requisitos agregados por matéria-prima antes de adicionar qualquer movimento ou tarefa; manter o cálculo de agenda e o comportamento de sucesso existente. | `FR-001`, `FR-002`, `FR-003`, `FR-005`, `CON-003`, `CON-005` | `AC-001`, `AC-002`, `AC-005`, `AC-006` | — | completed |
| `TASK-002` | Tornar o envio e o consumo atomicamente consistentes usando a unidade transacional existente, removendo efeitos rastreados de falhas e aplicando revalidação/controle de concorrência compatível com PostgreSQL antes da confirmação. | `FR-004`, `FR-006`, `NFR-001`, `NFR-002`, `CON-001`, `CON-002`, `CON-003`, `CON-004` | `AC-004`, `AC-007`, `AC-008`, `AC-010` | `TASK-001` | completed |
| `TASK-003` | Ajustar logs e auditoria do envio para registrar pedido, etapa, matéria-prima, quantidades calculadas, resultado e correlação de forma segura, sem persistir efeitos de uma tentativa rejeitada como sucesso. | `FR-007`, `NFR-003`, `NFR-004` | `AC-003`, `AC-009` | `TASK-002` | completed |
| `TASK-004` | Adicionar testes unitários de inventário e produção para agregação, quantidade exata, insuficiência, falha em item posterior, ausência de consumo parcial e sucesso com rastreabilidade. | `FR-001` a `FR-005`, `CON-003`, `CON-005` | `AC-001`, `AC-002`, `AC-004`, `AC-005`, `AC-006` | `TASK-001` | completed |
| `TASK-005` | Adicionar testes de aplicação/persistência para o envio, auditoria, reenvio após rejeição, rollback e preservação das movimentações históricas; incluir cenário PostgreSQL para concorrência se houver infraestrutura disponível. | `FR-004` a `FR-007`, `NFR-001` a `NFR-004`, `CON-002` | `AC-003`, `AC-007`, `AC-008`, `AC-009`, `AC-010` | `TASK-002`, `TASK-003` | completed |
| `TASK-006` | Executar revisão de escopo, segurança e rastreabilidade e rodar os gates obrigatórios configurados, registrando limitações de concorrência caso o ambiente de integração não esteja disponível. | Todos | Todos | `TASK-004`, `TASK-005` | completed |

## Regras de execução

- Não implementar compensação automática das duas saídas históricas nesta feature.
- Não alterar autorização, antiforgery, ficha técnica, deduplicação, fórmula financeira ou esquema do banco sem nova decisão de escopo.
- Não apagar ou editar movimentações históricas; qualquer correção posterior deve ser auditável.
- Não publicar ou registrar sucesso antes da confirmação transacional dos efeitos.
- Se a concorrência real não puder ser validada contra PostgreSQL, registrar a limitação em vez de declarar o requisito comprovado.
- Manter alterações focadas em `src/App`, `src/Domains`, `src/Infra` e testes relacionados ao fluxo.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004`, `TASK-005`, `TASK-006`, `TASK-REV-001`, `TASK-REV-002`
- Bloqueadas: nenhuma
- Evidência: build, testes e segurança passaram no runner Spectro; 368 testes passaram. Cobertura com `-AllowDirtyTree`: 86,74% linhas, 71,05% branches, 97,56% linhas novas e 91,18% branches novos. O resultado oficial do runner ficou `failed` somente porque o script exige working tree sem alterações tracked não commitadas.


## TASK-REV-001 - Remediate FINDING-001

- **Status:** completed
- **Finding:** FINDING-001
- **Severity:** Medium
- **Return stage:** implement
- **Description:** Tratar conflitos de serialização/concorrência no envio para produção com resposta segura, sem erro técnico não tratado, preservando rollback e permitindo nova tentativa.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.


## TASK-REV-002 - Remediate FINDING-002

- **Status:** completed
- **Finding:** FINDING-002
- **Severity:** Medium
- **Return stage:** implement
- **Description:** Publicar eventos de planejamento somente após confirmação de SaveChanges e commit, ou adotar mecanismo equivalente, evitando efeitos externos quando a persistência falhar.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.

