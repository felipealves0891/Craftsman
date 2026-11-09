<!-- spectro-managed: true -->
# Plano: Adicionar valor das horas ao custo financeiro

## Contexto e objetivo técnico

- O `SettlementCalculator` atualmente soma apenas os custos das movimentações de estoque vinculadas às `ProductionTask` e cria um `FinancialSettlement` com esse custo de produção.
- As tarefas já persistem `ProductionDurationHours` e `ProductId`; o produto já persiste `HourlyRate` com validação de valor não negativo.
- O `ProductionPlanner` grava em cada tarefa a duração total da produção (`ProductionDurationHours` já multiplicada pela quantidade do item). O cálculo financeiro deve usar esse valor diretamente e não multiplicá-lo novamente por `Quantity`.
- O resultado do custo de horas deve ser preservado no acerto financeiro, para que alterações posteriores no cadastro do produto não reescrevam a composição histórica.

## Solução proposta

- Estender o contrato de `FinancialSettlement` com um valor persistido de custo de horas (`LaborCostAmount`), mantendo `ProductionCostAmount` como o custo total de produção usado no acerto.
- No cálculo, carregar os produtos referenciados pelas tarefas do pedido, obter de cada produto o `HourlyRate` vigente no momento do cálculo e somar `ProductionDurationHours * HourlyRate` por tarefa.
- Somar o custo de horas ao custo de materiais já obtido pelas movimentações de estoque. O custo de horas será informado separadamente ao acerto e também refletido em `ProductionCostAmount`.
- Persistir `LaborCostAmount` em `financial_settlements` com precisão `numeric(18,2)`, incluindo migration compatível com dados existentes e valor padrão zero para acertos anteriores.
- Expor o custo de horas no detalhe financeiro para tornar a composição rastreável; a listagem e os KPIs continuarão usando os totais já derivados do acerto.
- Manter o fluxo atual de elegibilidade, deduplicação e seleção de tarefas: `FinanceAppService` e `DeliveryConfirmedSettlementHandler` continuam impedindo novo acerto quando já existe um para o pedido.

## Arquitetura e contratos

- Domínio financeiro: `FinancialSettlement` recebe o custo de horas, valida que ele não é negativo e mantém `TotalCostAmount` e `MarginAmount` derivados dos valores persistidos.
- Cálculo financeiro: `SettlementCalculator` passa a depender de `IProductRepository`. Para cada tarefa selecionada pelo fluxo atual, resolve o produto pelo `ProductId` e calcula o custo usando a duração já registrada na tarefa. Produto ausente deve interromper o cálculo com erro identificável, sem criar acerto parcial.
- Precisão: cada produto mantém `HourlyRate` com duas casas na persistência; os valores monetários seguem a precisão `18,2` já usada no financeiro. O plano não introduz arredondamento intermediário além do comportamento decimal existente.
- Persistência: adicionar `LaborCostAmount` à entidade `FinancialSettlementEntity`, ao mapeamento/configuração, ao repositório de conversão e ao snapshot/migration do EF Core. Acertos existentes recebem zero.
- Aplicação/UI: ampliar os view models, a composição da página de detalhes e o componente `FinancialSettlementDetailsPage` para exibir o custo de horas; o índice financeiro e seus resumos continuarão usando `TotalCostAmount` e `MarginAmount` persistidos.
- Observabilidade: preservar os logs existentes do handler e do serviço. Falhas de produto ausente ou valor inválido devem identificar a operação e o produto, sem registrar segredos ou payloads sensíveis.
- Concorrência: manter a proteção atual por consulta de acerto existente e índice único por `OrderId`; não adicionar retry, lock ou nova política de concorrência.

## Alternativas consideradas

- Usar somente `ProductionCostAmount` sem campo separado de custo de horas: rejeitado porque não preserva uma composição financeira explícita nem permite rastrear o valor calculado no acerto.
- Multiplicar `ProductionDurationHours` por `ProductionTask.Quantity`: rejeitado porque o planner já grava a duração total para a quantidade planejada; fazê-lo duplicaria o custo.
- Usar `HourlyRate` global em `appsettings`: rejeitado pela decisão `DEC-001`; o produto é a única fonte do valor-hora.
- Calcular horas por diferença entre `StartedAt` e `CompletedAt`: rejeitado porque a especificação exige as horas já registradas na tarefa e não altera o apontamento de produção.
- Alterar o fluxo de seleção por status das tarefas: rejeitado pela `DEC-004`; o cálculo deve preservar a seleção vigente do financeiro.

## Riscos e compatibilidade

- A inclusão de coluna exige migration; o valor padrão zero mantém leitura de acertos históricos e evita perda de dados.
- Produtos removidos ou ausentes para uma tarefa indicam inconsistência entre produção e catálogo. O cálculo deve falhar de forma explícita antes de persistir o acerto, sem mascarar a origem do problema.
- A alteração do `HourlyRate` afetará apenas novos cálculos; acertos já persistidos conservarão `LaborCostAmount` e os totais calculados.
- O cache de produtos pode servir a leitura do catálogo, mas a implementação deve preservar cancelamento e não compartilhar operações concorrentes sobre o mesmo `DbContext`.
- O repositório financeiro e as telas existentes têm múltiplos construtores/records usados em testes; as mudanças devem atualizar todos os usos para evitar regressões de compilação e de renderização.
- A constituição menciona .NET 8, enquanto os projetos atuais declaram `net10.0`; o plano preserva o framework efetivamente usado pelos projetos e não propõe alteração de versão.

## Estratégia de testes

- Domínio: validar custo de horas não negativo, inclusão no custo total/margem e persistência do valor no objeto de acerto.
- Cálculo financeiro: cobrir produto com `HourlyRate`, múltiplas tarefas/produtos, duração já escalada pela quantidade, ausência de tarefas, custo de materiais + horas, produto ausente e pedido não entregue.
- Persistência: verificar round-trip de `LaborCostAmount`, valor zero para registros/objetos legados e migration/configuração com precisão `18,2`.
- Aplicação/UI: verificar que o detalhe recebe e renderiza o custo de horas e que índice, resumo, custo total e margem continuam corretos.
- Regressão: manter testes de deduplicação no `FinanceAppService` e no handler de entrega; garantir que o segundo cálculo não substitui o acerto existente.
- Gates finais: executar os comandos obrigatórios de build, testes, segurança e cobertura definidos em `.spectro/config.yaml`, com `--warnaserror` no build e cobertura dos critérios de aceitação.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001` | `AC-001`, `AC-009` | `TASK-001`, `TASK-002`, `TASK-005` |
| `FR-002` | `AC-002`, `AC-005` | `TASK-002`, `TASK-005` |
| `FR-003` | `AC-003`, `AC-006` | `TASK-001`, `TASK-002`, `TASK-003`, `TASK-005` |
| `FR-004` | `AC-004` | `TASK-001`, `TASK-003`, `TASK-004`, `TASK-005` |
| `FR-005` | `AC-007`, `AC-008` | `TASK-002`, `TASK-004`, `TASK-005` |
| `NFR-001` | `AC-002`, `AC-004`, `AC-009` | `TASK-001`, `TASK-003`, `TASK-005` |
| `NFR-002` | `AC-003`, `AC-006` | `TASK-001`, `TASK-002`, `TASK-004`, `TASK-005` |
| `NFR-003` | `AC-009` | `TASK-002`, `TASK-005` |
| `CON-001` | `AC-002` | `TASK-002`, `TASK-005` |
| `CON-002` | `AC-002`, `AC-005` | `TASK-002`, `TASK-005` |
| `CON-003` | `AC-001`, `AC-009` | `TASK-001`, `TASK-002`, `TASK-005` |
| `CON-004` | `AC-003`, `AC-006` | `TASK-003`, `TASK-005` |

