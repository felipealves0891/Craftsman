<!-- spectro-managed: true -->
# Revisão: Adicionar valor das horas ao custo financeiro

## Escopo e evidências

- Especificação: `.spectro/features/add-hourly-labor-cost/spec.md` — hash `917b5ebdc6e6966499784968f1a7fa5b25698488f47e7f29ed57966b9dae1ab1`.
- Clarificações: `.spectro/features/add-hourly-labor-cost/clarifications.md` — hash `19af31a3b7c555a919fc26c0218b9c42f493c77853486d241003efec24d97509`.
- Plano/tarefas: `.spectro/features/add-hourly-labor-cost/plan.md` e `tasks.md` — hashes registrados no pacote de revisão.
- Gates: `.spectro/features/add-hourly-labor-cost/gates.json` — execução `gate-20261109T010643Z`, todos os 4 gates obrigatórios aprovados.
- Pacote independente: `.spectro/features/add-hourly-labor-cost/reviews/review-20261109T011500Z/`.
- Revisão do código: `5fdacfe621e4aa755f348240bb186e675fd78a7a`.
- Rastreabilidade: `traceability.json` não identificou requisitos ou critérios de aceite sem tarefa/validação.
- Continuidade das evidências: `gates.json` foi executado na revisão de código `0986f82`; o commit posterior `5fdacfe` contém somente `gates.json`/`state.json`, sem alterações de código, testes ou configuração.

## Requisitos verificados

- O cálculo usa `ProductionTask.ProductionDurationHours * Product.HourlyRate` e não multiplica novamente por quantidade.
- O custo de horas é somado ao custo material e persistido como `LaborCostAmount`, mantendo `ProductionCostAmount` como custo total de produção.
- Custo total e margem continuam derivados sem dupla contagem.
- O detalhe financeiro exibe o custo de horas separadamente.
- Produto ausente falha antes da criação do acerto.
- A migration adiciona `labor_cost_amount` com precisão `numeric(18,2)` e default zero para registros existentes.
- Deduplicação, elegibilidade de entrega, receita, desconto e custo de envio existentes foram preservados.
- A suíte registrou 347 testes aprovados; build, segurança e cobertura passaram nos gates configurados.

## Findings

| ID | Severidade | Categoria | Localização | Evidência | Impacto | Correção recomendada | Etapa de retorno | Status |
|---|---|---|---|---|---|---|---|---|
| `FINDING-001` | Medium | scope / consistency | `.spectro/config.yaml:25`; `.spectro/constitution.md:55, 64` | O conjunto revisado altera os limiares de cobertura de `81/55` para `82/56`, enquanto a constituição declara `90/75` para cobertura total. A constituição exige aprovação humana explícita para mudanças nos limiares de qualidade. | Os gates podem aprovar a feature com política configurada diferente da política declarada na constituição. A implementação financeira não é afetada, mas a aprovação final da mudança de política não está evidenciada neste pacote. | Confirmar aprovação humana explícita dos novos limiares e alinhar `.spectro/config.yaml` à constituição, ou registrar/atualizar a política constitucional antes de considerar a mudança de qualidade aprovada. | `human_review` | open |

## Riscos residuais

- O cálculo consulta a lista de produtos para resolver `HourlyRate`; o custo é adequado ao fluxo atual, mas o impacto de desempenho em catálogos muito grandes não foi medido.
- Alterações posteriores em `HourlyRate` afetam apenas novos acertos; acertos persistidos conservam o custo calculado.
- A cobertura total e a cobertura de código novo passaram conforme os limiares efetivamente configurados; a divergência entre configuração e constituição permanece o único ponto de governança.

## Decisão

- `needs_human_review`
- Justificativa: A implementação atende à especificação, ao plano e aos critérios de aceite, sem findings `Critical` ou `High`. A decisão final depende apenas da aprovação humana explícita da alteração dos limiares de qualidade identificada em `FINDING-001`.

