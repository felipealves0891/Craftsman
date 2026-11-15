<!-- spectro-managed: true -->
# Revisão: Corrigir o custo das saídas manuais de estoque

## Resumo

- Decisão: `approved`
- Revisão independente da implementação contra especificação, clarificações, plano, tarefas, código, testes, gates e constituição do projeto.
- Não foram identificados achados Critical ou High, nem lacunas de rastreabilidade bloqueantes.

## Artefatos examinados

- `.spectro/features/fix-manual-outbound-stock-cost/spec.md`
- `.spectro/features/fix-manual-outbound-stock-cost/clarifications.md`
- `.spectro/features/fix-manual-outbound-stock-cost/plan.md`
- `.spectro/features/fix-manual-outbound-stock-cost/tasks.md`
- `.spectro/features/fix-manual-outbound-stock-cost/gates.json`
- `.spectro/features/fix-manual-outbound-stock-cost/state.json`
- Pacote independente: `.spectro/features/fix-manual-outbound-stock-cost/reviews/review-20261115T151237Z/`
- `src/App/Services/InventoryAppService.cs`
- `src/Domains/Inventory/Entities/StockMovement.cs`
- `src/Infra/Repositories/StockMovementRepository.cs`
- `src/App/Views/Inventory/Movements.cshtml`
- `tests/Craftsman.Tests/Inventory/InventoryAppServiceTests.cs`
- `tests/Craftsman.Tests/Finance/SettlementCalculatorTests.cs`
- `.spectro/constitution.md` e `.spectro/config.yaml`

## Consistência de revisão e gates

- `gates.json` referencia a revisão de código `b3d4b8f82f6477c2891885c7e5d0da0715c9948a`, que corresponde ao `HEAD` examinado no início da revisão.
- A execução `gate-20261115T181048Z` passou nos quatro gates obrigatórios: build, testes, segurança de dependências e cobertura.
- O relatório foi validado por `validate_gates.mjs`.
- O helper de preparação registrou `uncommitted` porque `gates.json` e `state.json` são atualizados pelo próprio runner após a execução; não há código de produção pendente fora da revisão commitada.
- A rastreabilidade foi validada com resultado `complete`: 13 requisitos, 7 critérios de aceite e 4 tarefas sem lacunas.

## Requisitos e comportamento verificados

- `FR-001`/`AC-001`: `InventoryAppService` preserva `UnitCostAmount` para entradas e saídas; o teste registra saída com custo `2,5` sem zerá-lo.
- `FR-002`/`AC-002`: `StockMovement.TotalCostAmount` continua usando a quantidade efetiva, enquanto `SignedQuantity` representa a saída como negativa; o teste verifica `-3` unidades e custo total `7,5`.
- `FR-003`/`AC-002`: a listagem converte a saída para quantidade assinada e preserva custo unitário e total.
- `FR-004`/`AC-005`/`AC-006`: o cálculo financeiro continua filtrando movimentos por referência de tarefa de produção; o teste confirma que saída manual sem referência não altera custo, total ou margem.
- `FR-005`/`AC-003`/`AC-004`: validações de motivo, quantidade, saldo e comportamento de entradas/ajustes permanecem cobertas pela suíte existente e pelos novos testes.
- `AC-007`: o teste de histórico confirma que uma saída já persistida com custo zero continua sendo exibida sem recomposição automática.
- `NFR-001`/`NFR-002`: precisão decimal, rastreabilidade e formato de apresentação usam contratos e persistência existentes; não foi criada migration nem dependência nova.

## Segurança

- Entradas continuam validadas no servidor pelo modelo e pelo serviço; custo negativo, quantidade inválida e saldo negativo permanecem rejeitados.
- A autorização de escrita, antiforgery, persistência via `UnitOfWork` e o escopo de dados existentes não foram enfraquecidos.
- Não foram introduzidos endpoints, integrações, segredos, logs sensíveis ou novas superfícies de confiança.
- O gate de vulnerabilidades de dependências passou sem diagnóstico.

## Findings

Nenhum finding identificado.

## Riscos residuais

- Movimentos históricos gravados com custo zero permanecem sem recomposição, conforme decisão explícita da especificação.
- Saídas manuais continuam fora do acerto financeiro por não possuírem vínculo aprovado com pedido ou tarefa de produção. Uma futura inclusão exigirá nova especificação e regra de associação.
- O estado dos artefatos gerados pelo runner pode aparecer como alteração local após a execução dos gates; isso não representa alteração de produção.

## Decisão final

`approved`

Nenhuma remediação é necessária. A feature pode avançar para conclusão humana conforme o workflow Spectro.
