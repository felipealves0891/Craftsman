<!-- spectro-managed: true -->
# Clarificações: Testes de integração dos controllers de pedidos e produção

## Decisões resolvidas

### CL-001 — Conjunto inicial de endpoints

- **Pergunta:** quais endpoints entram no primeiro incremento?
- **Decisão:** incluir `OrdersController.Index`, `OrdersController.Details`, `OrdersController.SendToProduction`, `OrdersController.RevertProduction`, `OrdersController.Delete`, `ProductionController.Index`, `ProductionController.CurrentMonth`, `ProductionController.Calendar` e `ProductionController.Advance`.
- **Racional:** o conjunto foi confirmado pela inspeção do código, da documentação e do agente `integration-tests`; o fluxo `Import` foi explicitamente removido pelo usuário.
- **Requisitos afetados:** `FR-001`, `FR-002`, `FR-003`, `AC-001` a `AC-010`.
- **Data:** 2026-11-21

### CL-002 — Exclusão do fluxo de importação

- **Pergunta:** o fluxo `Import` deve ser coberto nesta feature?
- **Decisão:** não. O fluxo foi removido do escopo, requisitos e critérios de aceite.
- **Racional:** decisão explícita do usuário.
- **Requisitos afetados:** escopo incluído, `FR-002` e `AC-002`.
- **Data:** 2026-11-21

## Decisões adicionais resolvidas

### CL-003 — Banco para os testes de integração

- **Pergunta:** qual estratégia deve ser usada para a persistência relacional?
- **Decisão:** usar Entity Framework Core InMemory.
- **Racional:** reduz a dependência operacional dos testes e atende ao escopo aprovado, que não exige validação específica do provedor PostgreSQL.
- **Requisitos afetados:** `NFR-001`, `NFR-002`, `CON-004`, `AC-001` e `AC-011`.
- **Data:** 2026-11-21

### CL-004 — Concorrência e rollback transacional

- **Pergunta:** cenários de concorrência real e rollback após falha parcial entram nesta feature?
- **Decisão:** incluir cenários de concorrência e rollback somente no comportamento da aplicação, sem expandir para componentes ou serviços externos.
- **Racional:** cobre os riscos relevantes dos fluxos dos controllers sem ampliar a fronteira da feature para infraestrutura externa.
- **Requisitos afetados:** escopo excluído, `FR-004`, `FR-006`, `AC-006`, `AC-007` e `AC-010`.
- **Data:** 2026-11-21
