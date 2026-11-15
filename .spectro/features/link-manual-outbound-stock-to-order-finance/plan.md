<!-- spectro-managed: true -->
# Plano: Vincular saída manual de estoque a pedido no financeiro

## Contexto e objetivo técnico

- O fluxo de movimentações manuais usa `StockMovementInputModel`, `InventoryAppService`, `StockMovement` e `StockMovementRepository`. A entidade de domínio já preserva custo unitário e custo total, mas a movimentação só possui `BusinessReference`, atualmente usado para relacionar consumo a `ProductionTask`.
- O cálculo financeiro usa `SettlementCalculator` e `SettlementCostCalculator`: ele localiza movimentos cujo `BusinessReference` corresponde a tarefas de produção do pedido. Saídas manuais sem essa referência ficam fora do custo do pedido.
- A especificação aprovada exige vínculo opcional somente para saídas manuais, selecionando apenas pedidos `InProduction` sem `FinancialSettlement` persistido. O custo vinculado será somado ao custo de material somente em novas apurações; apurações existentes não serão recalculadas.

## Solução proposta

- Adicionar uma referência nullable de pedido à movimentação de estoque, separada de `BusinessReference`, para distinguir vínculo financeiro com pedido de referência operacional de tarefa de produção.
- Propagar essa referência pelo domínio, entidade de persistência, configuração EF, repositório e modelos de entrada/apresentação.
- Estender o fluxo de inventário para carregar opções de pedidos em produção sem apuração persistida, validar novamente o pedido no servidor no momento do POST e rejeitar estado inválido sem persistência parcial.
- Aplicar a referência somente quando o tipo for `Outbound`; entradas e ajustes continuam sem pedido associado.
- Alterar o cálculo do custo material para somar, uma única vez, os movimentos de saída manual vinculados diretamente ao pedido além dos movimentos ligados às tarefas de produção.
- Atualizar a invalidação de cache necessária após a criação da movimentação, preservando o comportamento de cache financeiro para apurações já persistidas.
- Exibir o pedido associado na listagem/histórico de movimentos, mantendo movimentos sem vínculo visíveis.

## Arquitetura e contratos

- `src/Domains/Inventory/Entities/StockMovement.cs`: adicionar `OrderId` nullable e um parâmetro opcional no construtor; preservar `BusinessReference` para tarefas de produção e manter as invariantes atuais de quantidade, motivo e custo.
- `src/Infra/Persistence/Entities/StockMovementEntity.cs` e `StockMovementConfiguration.cs`: adicionar coluna nullable `order_id`, índice e relacionamento opcional com `OrderEntity` usando delete restrito, mantendo o histórico de movimentos quando um pedido não puder ser removido.
- Migration: criar pela CLI `dotnet ef ... --project src/Infra/Craftsman.Infra.csproj`, conforme a constituição; não editar migration manualmente.
- `src/Infra/Repositories/StockMovementRepository.cs`: transportar `OrderId` nos mapeamentos `ToEntity`/`ToModel`; não alterar a semântica de `BusinessReference`, saldo, custo total ou ordenação histórica.
- `src/App/Models/ManualInventoryModels.cs`: adicionar `OrderId` nullable ao input e ao view model; a validação de obrigatoriedade deve ser condicional ao tipo e reforçada no serviço.
- `src/App/Services/InventoryAppService.cs`: receber o repositório de pedidos e o repositório de apurações; disponibilizar opções filtradas por `OrderStatus.InProduction` e ausência de apuração; validar a mesma regra no registro e limpar/ignorar pedido para tipos diferentes de saída.
- `src/App/Controllers/InventoryController.cs` e `src/App/Views/Inventory/Movements.cshtml`: carregar opções no GET e no retorno de validação do POST; apresentar seletor opcional somente para saída e mostrar a identificação do pedido na tabela.
- `src/Domains/Finance/Services/SettlementCostCalculator.cs`: receber o `orderId` ou separar explicitamente os movimentos por referência de tarefa e por `OrderId`; somar apenas saídas vinculadas diretamente ao pedido e evitar qualquer dupla contagem.
- `src/Domains/Finance/Services/SettlementCalculator.cs`: passar o pedido ao cálculo de custo material sem alterar receita, desconto, frete, mão de obra ou a regra de entrega.
- Fluxo de dados: formulário → `StockMovementInputModel` → opções e validações server-side → `StockMovement(OrderId)` → EF/repositório → histórico; na apuração, pedido + tarefas + movimentos → custo de material → `FinancialSettlement` novo.
- Erros: pedido inexistente, não `InProduction`, ou com settlement existente devem retornar validação orientativa; material inexistente, saldo insuficiente, quantidade, motivo e custo continuam com as rejeições atuais. Nenhuma exceção técnica deve ser exposta.
- Persistência: a criação do movimento e suas alterações de auditoria devem usar o `UnitOfWork` atual. A migration deve permitir `NULL` para preservar movimentos históricos e não requer backfill.
- Concorrência: a elegibilidade exibida pode ficar desatualizada; o POST deve reconsultar pedido e settlement antes de gravar. A criação não altera settlement existente, portanto não deve iniciar recálculo retroativo.
- Observabilidade e segurança: manter autorização de escrita, antiforgery, cancelamento e auditoria automática do `AppDbContext`; não registrar payloads ou dados sensíveis adicionais.

## Alternativas consideradas

- Reutilizar `BusinessReference` com `OrderId.ToString()`: rejeitado, pois mistura referência de pedido com referência de tarefa de produção e torna o cálculo suscetível a associação ambígua ou duplicidade.
- Criar uma tabela de associação separada: rejeitado para este escopo; uma relação opcional um-para-um por movimentação é suficiente e a coluna nullable preserva o histórico sem nova camada.
- Derivar o pedido a partir do material, descrição ou texto livre: rejeitado; a especificação exige associação explícita e inequívoca pelo usuário.
- Atualizar settlements existentes ao criar uma saída: rejeitado pela decisão de clarificação; somente novas apurações consideram o vínculo.
- Permitir todos os estados de pedido: rejeitado; a seleção fica restrita a `InProduction` e sem apuração persistida.

## Riscos e compatibilidade

- Risco: migration ou relacionamento com delete cascade pode apagar histórico de estoque junto com pedido. Mitigação: FK nullable com `DeleteBehavior.Restrict` e teste de persistência.
- Risco: custo ser contado duas vezes quando o mesmo movimento possuir `BusinessReference` de tarefa e `OrderId`. Mitigação: fluxo manual define apenas `OrderId`; o cálculo usa conjuntos distintos e teste explícito de não duplicidade.
- Risco: duas requisições concorrentes podem registrar saídas vinculadas válidas para o mesmo pedido antes de uma apuração. Mitigação: revalidar no servidor e manter cada movimento auditável; não introduzir bloqueio sem requisito de exclusividade.
- Risco: cache de opções de pedidos ou saldo ficar desatualizado. Mitigação: invalidar prefixos existentes após gravação e não cachear uma nova regra sem necessidade.
- Risco: histórico antigo não possuir `OrderId`. Mitigação: coluna nullable, migration sem backfill e listagem preservando `null`.
- Compatibilidade: movimentos atuais, referências de tarefas, entradas, ajustes, saldos, custos e settlements já persistidos devem manter o comportamento atual.
- Aprovação: a regra financeira já foi aprovada na clarificação; o plano não altera outros componentes de receita ou custos.

## Estratégia de testes

- Domínio: construir movimento com e sem `OrderId`, validar normalização e invariantes existentes; confirmar que `BusinessReference` e `OrderId` podem ser distinguidos e que custo total permanece `Quantity × UnitCostAmount`.
- Aplicação/inventário: listar apenas pedidos em produção sem settlement; excluir pedidos em outros estados e com settlement; registrar saída vinculada; rejeitar pedido inexistente, fora de produção ou já apurado; garantir que entrada/ajuste não criem vínculo.
- Persistência: verificar migration/configuração, round-trip de `OrderId`, FK restrita e preservação de movimentos históricos com `NULL`.
- Financeiro: verificar custo de material com produção + saída vinculada, somente saída vinculada, saída sem vínculo, ausência de duplicidade e não alteração de settlement já persistido.
- Apresentação/controlador: verificar seletor condicional, repopulação após erro e identificação do pedido no histórico; preservar autorização, antiforgery e mensagens seguras.
- Gates finais: `git diff --check`, build com `--warnaserror`, testes, vulnerabilidades de pacotes e cobertura conforme `.spectro/config.yaml`. A migration deve ser criada e validada com `dotnet ef` no projeto de Infra.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001`, `FR-006`, `FR-009`, `CON-003` | `AC-001`, `AC-002` | `TASK-002`, `TASK-003`, `TASK-005` |
| `FR-002`, `NFR-002`, `CON-002` | `AC-002`, `AC-006` | `TASK-001`, `TASK-002`, `TASK-003` |
| `FR-003` | `AC-003` | `TASK-002`, `TASK-004` |
| `FR-004`, `FR-005`, `NFR-001`, `CON-004`, `CON-005` | `AC-004`, `AC-005` | `TASK-004`, `TASK-005` |
| `FR-007` | `AC-006` | `TASK-003`, `TASK-005` |
| `FR-008`, `NFR-004` | `AC-007` | `TASK-002`, `TASK-004`, `TASK-005` |
| `NFR-003`, `NFR-005`, `CON-001` | `AC-002`, `AC-008` | `TASK-002`, `TASK-003`, `TASK-005`, `TASK-006` |
