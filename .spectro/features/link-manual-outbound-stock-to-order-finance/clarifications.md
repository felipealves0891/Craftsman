<!-- spectro-managed: true -->
# Clarificações: Vincular saída manual de estoque a pedido no financeiro

## Decisões

- `DEC-001` — Classificação financeira da saída vinculada
  - Pergunta: Como o custo da saída manual vinculada deve aparecer no financeiro?
  - Decisão: Somar ao custo de material do pedido.
  - Racional: Mantém o custo da movimentação dentro da parcela de material já existente no relatório, sem criar uma categoria financeira nova.
  - Requisitos afetados: `FR-004`, `FR-005`, `NFR-001`, `CON-005`, `AC-004`, `AC-005`.
  - Data: 2026-11-15.

- `DEC-002` — Pedidos elegíveis para seleção
  - Pergunta: Quais pedidos podem ser selecionados para a saída manual?
  - Decisão: Somente pedidos em produção e sem apuração financeira persistida.
  - Racional: Restringe o vínculo ao estágio operacional informado e evita alterar pedidos que já possuem resultado financeiro persistido.
  - Requisitos afetados: `FR-009`, `FR-006`, `FR-008`, `NFR-002`, `AC-001`, `AC-002`, `AC-007`.
  - Data: 2026-11-15.

- `DEC-003` — Alteração posterior do vínculo
  - Pergunta: O pedido associado poderá ser corrigido após a criação da movimentação?
  - Decisão: Não. O vínculo será informado somente na criação.
  - Racional: Preserva o histórico auditável e evita uma operação de edição sem trilha de correção definida.
  - Requisitos afetados: `CON-002`, `FR-007`, `FR-008`.
  - Data: 2026-11-15.

- `DEC-004` — Pedidos com apuração existente
  - Pergunta: O que fazer quando o pedido já tiver uma apuração financeira persistida?
  - Decisão: O pedido não aparecerá nas opções de seleção.
  - Racional: Evita criar uma movimentação que exigiria decidir como invalidar ou recalcular um resultado financeiro já persistido.
  - Requisitos afetados: `FR-009`, `FR-008`, `NFR-004`, `AC-001`, `AC-007`.
  - Data: 2026-11-15.

- `DEC-005` — Atualização de apurações persistidas
  - Pergunta: O relatório deve revisar ou atualizar apurações financeiras já persistidas?
  - Decisão: Não. A saída vinculada será considerada somente em novas apurações; registros persistidos permanecem inalterados.
  - Racional: Mantém a imutabilidade prática do resultado histórico e limita o escopo da feature.
  - Requisitos afetados: `FR-004`, `FR-008`, `CON-005`, `AC-004`, `AC-007`.
  - Data: 2026-11-15.

## Estado

- Não permanecem ambiguidades bloqueantes identificadas nesta clarificação.
- A especificação está pronta para planejamento.
