<!-- spectro-managed: true -->
# Esclarecimentos: Corrigir o custo das saídas manuais de estoque

## DEC-001 — Origem e sinal do custo da saída manual

- Severidade: Major
- Pergunta: Para uma saída manual, o custo unitário deve ser informado pelo usuário ou derivado do estoque? Como tratar o sinal negativo da quantidade?
- Decisão: O custo unitário é preenchido pelo usuário e deve permanecer não negativo. A saída pode ser representada com quantidade assinada negativa, como `-3`, mas esse sinal serve para saldo/exibição da movimentação e não deve transformar o custo total em negativo. O custo total usa a quantidade efetiva da movimentação multiplicada pelo custo unitário.
- Racional: O formulário já solicita `UnitCostAmount`, e o domínio mantém `Quantity` como magnitude da saída enquanto `SignedQuantity` representa o efeito no saldo. Preservar essa separação evita custo negativo por efeito de apresentação.
- Requisitos afetados: `FR-001`, `FR-002`, `FR-003`, `CON-004`, `AC-001`, `AC-002`.
- Data: 2026-11-15

## DEC-002 — Participação no relatório financeiro

- Severidade: Blocking
- Pergunta: Qual relatório financeiro deve refletir a saída manual e qual vínculo deve associá-la a um pedido, período ou agrupamento?
- Decisão: Nenhum relatório financeiro será alterado nesta feature. Saídas manuais não serão incluídas no acerto financeiro atual, pois não existe vínculo manual aprovado com pedido ou acerto.
- Racional: O processo financeiro atual calcula custos de material a partir de movimentos vinculados a tarefas de produção. Incluir uma saída sem vínculo definido poderia atribuir custos ao pedido errado ou duplicar custos.
- Requisitos afetados: `FR-004`, `CON-005`, `AC-005`, `AC-006`.
- Data: 2026-11-15

## DEC-003 — Registros históricos com custo zerado

- Severidade: Major
- Pergunta: Movimentos históricos de saída manual com custo zerado devem ser recompostos?
- Decisão: Permanecem inalterados por enquanto. Não haverá migração, edição automática ou recomposição inventada nesta feature.
- Racional: Não foi definido um procedimento autorizado nem uma fonte confiável para recuperar o custo histórico.
- Requisitos afetados: Escopo excluído, `CON-002`, `AC-007`.
- Data: 2026-11-15

## Estado da clarificação

- Perguntas bloqueantes remanescentes: nenhuma.
- Perguntas major remanescentes: nenhuma.
- Perguntas minor remanescentes: nenhuma.
- Status: pronta para planejamento.
