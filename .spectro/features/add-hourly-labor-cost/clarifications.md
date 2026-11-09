<!-- spectro-managed: true -->
# Clarificações: Adicionar valor das horas ao custo financeiro

## Decisões

- `DEC-001` — Fonte do valor-hora
  - Pergunta: O valor em `appsettings` deveria substituir, complementar ou ter precedência sobre `HourlyRate`?
  - Decisão: Desconsiderar o valor global em `appsettings` e utilizar exclusivamente o `HourlyRate` cadastrado no produto.
  - Racional: O produto já possui um valor-hora definido e essa é a fonte escolhida para o custeio desta funcionalidade.
  - Requisitos afetados: `FR-001`, `FR-002`, `CON-003`.
  - Data: 2026-11-08.

- `DEC-002` — Persistência do cálculo
  - Pergunta: O valor-hora deveria ser apenas consultado no momento do cálculo ou o resultado deveria ser preservado no acerto?
  - Decisão: Persistir no acerto o valor calculado do custo das horas, junto ao custo de produção resultante.
  - Racional: O acerto deve preservar o resultado financeiro calculado mesmo que o cadastro do produto seja alterado posteriormente.
  - Requisitos afetados: `FR-003`, `FR-004`, `NFR-002`, `CON-004`.
  - Data: 2026-11-08.

- `DEC-003` — Configuração global
  - Pergunta: Como tratar configuração ausente ou igual a zero em `appsettings`?
  - Decisão: A pergunta não se aplica; o cálculo não utilizará valor-hora global em `appsettings`.
  - Racional: `HourlyRate` do produto é a única fonte do valor-hora.
  - Requisitos afetados: `FR-001`, `CON-003`, `AC-001`, `AC-009`.
  - Data: 2026-11-08.

- `DEC-004` — Seleção das tarefas
  - Pergunta: O custo deveria considerar todas as tarefas, somente concluídas ou somente tarefas não canceladas?
  - Decisão: Seguir o fluxo financeiro existente para selecionar as tarefas do pedido, sem criar uma nova regra de status para esta funcionalidade.
  - Racional: A alteração deve adicionar o custo das horas ao cálculo vigente sem alterar o fluxo financeiro existente.
  - Requisitos afetados: `FR-002`, `FR-005`, `CON-002`, `AC-005`, `AC-007`, `AC-008`.
  - Data: 2026-11-08.

## Estado

- Não permanecem ambiguidades bloqueantes identificadas nesta clarificação.
- A especificação está pronta para planejamento.

