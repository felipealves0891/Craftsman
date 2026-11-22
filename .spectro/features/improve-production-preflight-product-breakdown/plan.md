<!-- spectro-managed: true -->
# Plano: Exibir quebra por produto e estoque dos materiais no envio para produção

## Contexto e objetivo técnico

- O modal de preflight em `src/UI/Components/Orders/OrderDetailsPage.razor` recebe atualmente uma coleção plana de materiais.
- `ProductionPlanPreparationService` já produz `PreparedProductionItem` por item/produto e também requisitos agregados para validação.
- `ProductionPreflightService` consulta `InventoryService.GetAvailabilityAsync` apenas com os requisitos agregados e perde a relação produto → material ao montar a resposta da UI.
- O objetivo técnico é transportar essa relação até a UI, mantendo a disponibilidade global calculada uma única vez, e renderizar uma lista expansível por produto mais um resumo consolidado por matéria-prima.

## Solução proposta

1. Estender o contrato de resultado do preflight para carregar dois níveis de informação:
   - grupos de produto, com nome, quantidade do item e materiais da ficha técnica com a necessidade específica daquele produto;
   - materiais consolidados do pedido, com necessidade total, disponibilidade global, unidade, configuração, disponibilidade e quantidade faltante.
2. Calcular a disponibilidade somente sobre os requisitos agregados já usados pelo planejamento. Os materiais dos grupos serão enriquecidos por um índice `RawMaterialId → MaterialAvailability`, sem nova consulta ou recálculo por produto.
3. Mapear o contrato de domínio para modelos de apresentação no `OrderQueryService`/`ProductionPreflightViewModel`, preservando mensagens de falha e `CanConfirm`.
4. Alterar `OrderDetailsPage.razor` para renderizar grupos expansíveis (`details/summary` ou equivalente compatível com o estilo existente), sublistas de materiais e uma seção consolidada global. A confirmação continuará sendo única e condicionada a `CanConfirm`.
5. Cobrir o contrato com testes de domínio/aplicação e renderização do componente, incluindo material compartilhado entre produtos, insuficiência, configuração inválida, estado vazio e preservação do POST protegido.

## Arquitetura e contratos

- Domínio de produção:
  - `ProductionPlanPreparationService.PreparedProductionItem` é a fonte da relação produto, quantidade do pedido e requisitos específicos.
  - `ProductionPreflightService` deve combinar `PreparedProductionItem.Requirements` com o resultado agregado de `InventoryService.GetAvailabilityAsync`.
  - `ProductionPreflightResult.CanConfirm` e `FailureReason` permanecem a fonte da decisão de habilitação; a UI não recalcula estoque.
- Modelos de aplicação:
  - substituir/expandir o modelo plano `ProductionPreflightViewModel` por modelos de grupos de produto, materiais detalhados e materiais consolidados, mantendo campos de estado necessários à apresentação.
  - `OrderQueryService.BuildProductionPreflightAsync` fará apenas a projeção para a UI; não deve realizar nova leitura de estoque nem alterar regra de negócio.
- UI:
  - `src/App/Views/Orders/Details.cshtml` continuará adaptando o modelo da página para o componente.
  - `src/UI/Components/Orders/OrderDetailsPage.razor` exibirá o nome do produto, quantidade, materiais por produto e resumo global.
  - o formulário continuará apontando para `/Orders/SendToProduction/{OrderId}` com `RequestVerificationToken` e botão desabilitado quando `CanConfirm` for falso.
- Persistência e endpoint:
  - nenhuma migration, tabela, movimento, transação ou alteração de endpoint é prevista.
  - `OrdersController.SendToProduction` não deve ser alterado funcionalmente; sua autorização, antiforgery e decisão efetiva permanecem intactas.
- Formatação:
  - reutilizar `decimal` e `UnitOfMeasure` existentes; não arredondar valores no contrato.
  - o resumo consolidado deve deixar explícito que a disponibilidade é global/compartilhada, evitando que saldos repetidos nos grupos sejam somados visualmente.

## Alternativas consideradas

- Exibir somente a lista plana atual: rejeitada porque não fornece referência do material ao produto.
- Recalcular estoque individualmente dentro de cada produto: rejeitada porque pode divergir da validação agregada e duplicar consultas.
- Alterar o planejamento para consumir por produto ou criar reservas: rejeitada por estar fora do escopo e por mudar regras de estoque.
- Exibir somente o resumo consolidado: rejeitada porque perde a referência solicitada entre produto e matéria-prima.
- A solução escolhida exibe ambos os níveis, mas mantém uma única fonte de disponibilidade agregada.

## Riscos e compatibilidade

- Materiais compartilhados podem mostrar a mesma disponibilidade global em mais de um grupo; rótulos e o resumo consolidado devem deixar claro que o saldo não é aditivo por grupo.
- Pedidos com muitos produtos podem aumentar a altura do modal; grupos expansíveis devem reduzir a sobrecarga inicial e preservar legibilidade em telas estreitas.
- O contrato de preflight é usado por testes e pela página de detalhes; alterações devem atualizar todos os consumidores sem mudar o contrato HTTP de envio.
- Falhas de preparação continuam sem materiais determináveis e com confirmação bloqueada; não devem ser preenchidas com valores sintéticos.
- Não há mudança de segurança de fronteira, mas a regressão deve confirmar autorização, antiforgery e ausência de alteração no POST.

## Estratégia de testes

- Testes de domínio/aplicação:
  - um produto com material suficiente e insuficiente;
  - dois produtos com materiais distintos;
  - dois produtos compartilhando a mesma matéria-prima, verificando necessidade por produto e necessidade/disponibilidade globais;
  - material inativo ou ausente;
  - pedido sem vínculo/configuração válida, confirmando falha e `CanConfirm = false`.
- Testes de componente:
  - presença do grupo expansível, nome e quantidade do produto;
  - sublista com material, quantidade necessária, estoque, unidade e situação;
  - resumo consolidado e indicação de saldo global compartilhado;
  - botão único de confirmação, token antiforgery e bloqueio em estado inválido;
  - estado vazio/mensagem de pré-requisito e renderização com múltiplos grupos.
- Testes de integração existentes de `Orders/Details` e mutações devem permanecer verdes para confirmar que o endpoint e os efeitos de planejamento não mudaram.
- Gates previstos pelo projeto: build com warnings-as-errors, testes, vulnerabilidades de pacotes e cobertura configurada. Nenhum gate é executado nesta etapa de planejamento.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001`, `FR-002`, `FR-003` | `AC-001`, `AC-002`, `AC-003` | `TASK-001`, `TASK-002`, `TASK-003` |
| `FR-004` | `AC-004`, `AC-005` | `TASK-001`, `TASK-003`, `TASK-004` |
| `FR-005` | `AC-006`, `AC-007` | `TASK-002`, `TASK-003`, `TASK-004` |
| `FR-006`, `FR-008` | `AC-003`, `AC-011` | `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004` |
| `FR-007` | `AC-008` | `TASK-002`, `TASK-003`, `TASK-004` |
| `NFR-001` | `AC-006`, `AC-011` | `TASK-001`, `TASK-002`, `TASK-004` |
| `NFR-002`, `NFR-003` | `AC-009` | `TASK-003`, `TASK-004` |
| `NFR-004` | `AC-010` | `TASK-003`, `TASK-004` |
| `CON-001`, `CON-005` | `AC-001`–`AC-011` | `TASK-001`–`TASK-004` |
| `CON-002`, `CON-003` | `AC-006`, `AC-007`, `AC-011` | `TASK-001`, `TASK-003`, `TASK-004` |
| `CON-004` | `AC-010` | `TASK-004` |
