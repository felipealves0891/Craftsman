<!-- spectro-managed: true -->
# Plano: Corrigir o custo das saídas manuais de estoque

## Contexto e objetivo técnico

- O formulário e o modelo de entrada já recebem `UnitCostAmount`, e a entidade `StockMovement` já persiste esse valor e calcula `TotalCostAmount` como `Quantity × UnitCostAmount`.
- O defeito está em `InventoryAppService.RegisterMovementAsync`, que atualmente atribui custo apenas a entradas e força saídas manuais a custo unitário zero.
- O objetivo técnico é preservar o custo informado para saídas manuais, mantendo `Quantity` como magnitude positiva no domínio, `SignedQuantity` negativo para o efeito no saldo/listagem e `TotalCostAmount` não negativo.
- O relatório financeiro não será alterado: saídas manuais continuam sem `BusinessReference` de tarefa de produção e fora do cálculo de acerto financeiro.

## Solução proposta

- Ajustar a seleção do custo em `InventoryAppService.RegisterMovementAsync` para preservar `input.UnitCostAmount` em saídas manuais, mantendo o comportamento vigente de entradas e ajustes.
- Reutilizar o construtor e as propriedades existentes de `StockMovement`; não criar novo cálculo, value object, serviço ou dependência.
- Manter a quantidade efetiva positiva exigida pelo fluxo de saída. A representação negativa continuará sendo responsabilidade de `SignedQuantity`, usada no saldo e na view model de listagem.
- Manter o custo total derivado pelo domínio a partir da quantidade efetiva, evitando multiplicá-lo pela quantidade assinada negativa.
- Não criar migration nem alterar schema: `unit_cost_amount` já existe com precisão `18,2` e a conversão de persistência já transporta o valor.

## Arquitetura e contratos

- `src/App/Models/ManualInventoryModels.cs`: contrato de entrada já valida custo não negativo com `[Range(0, double.MaxValue)]`; permanece sem mudança funcional.
- `src/App/Services/InventoryAppService.cs`: ponto de aplicação a modificar; valida material, quantidade, motivo e saldo antes de criar o movimento.
- `src/Domains/Inventory/Entities/StockMovement.cs`: contrato de domínio a preservar; `Quantity` é magnitude, `SignedQuantity` aplica o sinal por tipo e `TotalCostAmount` usa `Quantity`.
- `src/Infra/Repositories/StockMovementRepository.cs`: persistência e leitura a validar; `ToModel` normaliza a quantidade de entrada/saída com `Math.Abs` e mantém `UnitCostAmount`.
- `src/App/Views/Inventory/Movements.cshtml`: apresentação já exibe quantidade assinada, custo unitário e custo total; não requer alteração esperada.
- `src/Domains/Finance/Services/SettlementCalculator.cs` e `SettlementCostCalculator.cs`: permanecem sem alteração; somente movimentos vinculados a tarefas de produção são considerados no custo de material.
- Fluxo de dados: formulário → `StockMovementInputModel` → validações do `InventoryAppService` → `StockMovement` com custo informado → repositório/EF → listagem com `SignedQuantity` e `TotalCostAmount`.
- Erros e limites: preservar rejeição de quantidade inválida, motivo ausente, material inexistente, saldo negativo e custo unitário negativo. Não introduzir fallback silencioso para custo de saída.
- Concorrência: nenhuma mudança de locking ou transação está prevista; o fluxo atual e seu `UnitOfWork` permanecem responsáveis pela persistência.
- Observabilidade: não há necessidade de novo log; não registrar custo ou dados adicionais além do comportamento existente.

## Alternativas consideradas

- Calcular custo médio ou último custo das entradas: rejeitado, pois a decisão registrada determina que o usuário informa o custo e não foi aprovado um novo método de custeio.
- Tornar `TotalCostAmount` negativo para saídas: rejeitado, pois o sinal da saída representa saldo/quantidade assinada e não deve inverter o valor monetário do custo.
- Alterar o relatório financeiro para incluir saídas manuais: rejeitado nesta feature, pois não existe vínculo aprovado com pedido ou acerto.
- Corrigir registros históricos com custo zero: rejeitado por enquanto, pois não há fonte confiável nem procedimento autorizado de recomposição.
- Alterar schema ou criar camada de custeio: rejeitado; o modelo atual já comporta o valor necessário.

## Riscos e compatibilidade

- Risco: uma alteração inadvertida para usar `SignedQuantity` no cálculo poderia produzir custo negativo. Mitigação: testes explícitos de quantidade assinada negativa e custo total positivo.
- Risco: mudar o comportamento de ajustes ou entradas. Mitigação: preservar a seleção de custo por tipo e cobrir regressão dos fluxos existentes.
- Risco: expectativa de correção de históricos antigos. Mitigação: testar que nenhum processo de migração/atualização histórica é introduzido e documentar a limitação.
- Compatibilidade de persistência: preservada; nenhuma migration ou alteração de contrato externo é necessária.
- Segurança: a operação continua protegida pelo fluxo existente de autorização de escrita, antiforgery e validação server-side; não há nova entrada ou superfície de confiança.

## Estratégia de testes

- Teste de aplicação: registrar saída manual com saldo suficiente, quantidade e custo; verificar `UnitCostAmount`, `TotalCostAmount`, motivo, persistência e saldo reduzido.
- Teste de apresentação/aplicação: listar a movimentação e verificar quantidade assinada negativa, custo unitário informado e custo total não negativo.
- Testes negativos: custo negativo rejeitado pela validação/modelo; quantidade zero/negativa, motivo ausente e saldo insuficiente continuam rejeitados.
- Regressão: entradas continuam preservando custo; ajustes continuam sem alterar sua regra vigente; material inexistente continua rejeitado.
- Financeiro: adicionar ou adaptar cenário que demonstre que uma saída manual sem referência de tarefa não altera o custo material do acerto financeiro atual.
- Verificação final: `git diff --check`, build com avisos como erros, testes aplicáveis, gate de segurança de dependências e cobertura conforme `.spectro/config.yaml`.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001`, `CON-004` | `AC-001` | `TASK-001`, `TASK-002` |
| `FR-002`, `NFR-001`, `CON-004` | `AC-002` | `TASK-001`, `TASK-002` |
| `FR-003`, `NFR-002` | `AC-002`, `AC-007` | `TASK-002`, `TASK-003` |
| `FR-004`, `CON-005` | `AC-005`, `AC-006` | `TASK-003` |
| `FR-005`, `CON-001` | `AC-003` | `TASK-002` |
| `NFR-003` | `AC-001` a `AC-007` | `TASK-002`, `TASK-003`, `TASK-004` |
| `CON-002` | `AC-007` | `TASK-003` |
| `CON-003` | Todos | `TASK-001`, `TASK-004` |
