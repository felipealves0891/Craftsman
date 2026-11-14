<!-- spectro-managed: true -->
# Clarificações: Corrigir a inclusão de custo de mão de obra

## Decisões

- `DEC-001` — Operação reversível dos custos financeiros
  - Pergunta: Como o `Up` e o `Down` da migration devem alterar os valores financeiros que incorporaram mão de obra?
  - Decisão: O `Up` deve subtrair o valor persistido de mão de obra do custo de produção e do custo total e somá-lo à margem. O `Down` deve fazer a operação inversa, somando o mesmo valor ao custo de produção e ao custo total e subtraindo-o da margem.
  - Racional: A reversão precisa ser aritmética e rastreável, sem recalcular o valor-hora nem depender do serviço em background. Para que o `Down` seja possível, o valor persistido de mão de obra deve ser preservado pelo `Up` como fonte da operação inversa.
  - Requisitos afetados: `FR-003`, `FR-004`, `FR-008`, `CON-002`, `AC-002`, `AC-003`, `AC-009`.
  - Data: 2026-11-14.

- `DEC-002` — Colunas auxiliares
  - Pergunta: As colunas `labor_cost_amount` e `labor_cost_reconciled` devem ser removidas ou permanecer no esquema?
  - Decisão: Manter `labor_cost_amount` e remover `labor_cost_reconciled`. O valor persistido de mão de obra deve permanecer disponível como dado-base para a operação inversa do `Down`, sem ser usado pelo `Up` para compor os custos financeiros.
  - Racional: Preserva o valor necessário ao rollback aritmético, enquanto elimina o marcador de reconciliação que deixa de ser necessário após o recálculo global.
  - Requisitos afetados: `FR-003`, `CON-002`, `AC-002`, `AC-004`, `AC-009`.
  - Data: 2026-11-14.

- `DEC-003` — População e origem dos recálculos
  - Pergunta: Quais acertos devem ser recalculados e os valores devem ser ajustados sobre os derivados existentes ou reconstruídos dos dados-base?
  - Decisão: Recalcular todos os acertos financeiros persistidos. As colunas de custo de produção, custo total e margem devem ser reconstruídas a partir dos demais dados persistidos; o `Up` exclui mão de obra da composição e o `Down` a inclui usando o valor preservado.
  - Racional: Evita acumulação, dupla subtração ou dependência de valores derivados possivelmente inconsistentes.
  - Requisitos afetados: `FR-002`, `FR-003`, `FR-004`, `FR-007`, `FR-008`, `AC-002`, `AC-005`, `AC-010`.
  - Data: 2026-11-14.

- `DEC-004` — Exclusão do cálculo
  - Pergunta: O valor histórico de mão de obra deve continuar armazenado mesmo depois de deixar de compor os custos financeiros?
  - Decisão: O valor deve ser preservado em `labor_cost_amount` como dado histórico, mas excluído do cálculo do `Up`; o `Down` deve reutilizá-lo para recompor os valores.
  - Racional: Sem uma fonte persistida, o `Down` não consegue executar a operação inversa de forma determinística.
  - Requisitos afetados: `FR-003`, `FR-008`, `CON-002`, `AC-002`, `AC-009`.
  - Data: 2026-11-14.

- `DEC-005` — Coluna de reconciliação
  - Pergunta: Quais colunas relacionadas à reconciliação devem permanecer após o reparo?
  - Decisão: Remover `labor_cost_reconciled` e manter `labor_cost_amount`.
  - Racional: O marcador de reconciliação deixa de ser necessário quando todos os acertos são recalculados pela migration; o valor monetário histórico ainda é necessário para o `Down`.
  - Requisitos afetados: `CON-004`, `AC-002`, `AC-004`, `AC-009`.
  - Data: 2026-11-14.

## Estado

- A ambiguidade sobre a política aritmética do `Up`/`Down` foi resolvida.
- Não permanecem ambiguidades bloqueantes ou maiores nesta clarificação.
- A especificação está pronta para planejamento.
