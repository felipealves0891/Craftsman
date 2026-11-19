<!-- spectro-managed: true -->
# Clarificações: Reverter movimentação de estoque

## DEC-001 — Estado que bloqueia por pedido concluído

- Severidade: Blocking
- Pergunta: Qual estado representa “pedido concluído”?
- Decisão: Considerar não elegível qualquer movimentação associada a pedido que possua ao menos uma tarefa de produção em `ProductionTaskStatus.Completed`.
- Racional: É o estado de conclusão existente no fluxo de produção; não será criado ou presumido um novo `OrderStatus.Completed` nesta feature.
- Requisitos afetados: `FR-002`, `FR-003`, `AC-002`, `AC-003`.
- Data: 2026-11-18

## DEC-002 — Reversão de ajustes

- Severidade: Major
- Pergunta: Como reverter movimentações do tipo `Adjustment`?
- Decisão: Criar uma nova movimentação do tipo `Adjustment` com quantidade oposta à movimentação original.
- Racional: Preserva o histórico e produz o efeito inverso sem transformar o ajuste em entrada ou saída.
- Requisitos afetados: `FR-005`, `FR-006`, `FR-007`, `AC-006`.
- Data: 2026-11-18

## DEC-003 — Motivo da movimentação compensatória

- Severidade: Major
- Pergunta: Qual motivo identificar a compensação?
- Decisão: Usar o motivo literal `Stock movement reversal`.
- Racional: Fornece uma identificação estável e explícita para consultas, auditoria e detecção de repetição.
- Requisitos afetados: `FR-005`, `FR-006`, `CON-005`, `AC-005`, `AC-007`.
- Data: 2026-11-18

## DEC-004 — Autorização

- Severidade: Major
- Pergunta: A reversão exige permissão nova?
- Decisão: Reutilizar a mesma permissão já exigida para registrar movimentações de estoque.
- Racional: Mantém o modelo de autorização existente e evita ampliar escopo de permissões sem necessidade demonstrável.
- Requisitos afetados: `FR-001`, `FR-009`, `AC-001`, `AC-010`.
- Data: 2026-11-18

## DEC-005 — Pedido entregue e referência ausente

- Severidade: Minor
- Pergunta: Como interpretar entrega e ausência de referência?
- Decisão: Usar `OrderStatus.Delivered` para pedido entregue. Considerar ausência de referência quando `BusinessReference` for nula ou vazia; movimentações sem `OrderId` não têm validação de estado de pedido.
- Racional: Esses conceitos correspondem aos estados e à normalização já existentes no domínio.
- Requisitos afetados: `FR-002`, `FR-003`, `FR-004`, `AC-002`, `AC-003`, `AC-004`.
- Data: 2026-11-18

