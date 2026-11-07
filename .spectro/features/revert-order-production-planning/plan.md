<!-- spectro-managed: true -->
# Plano: Reverter envio do pedido para produção

## Contexto e objetivo técnico

- O envio para produção é realizado por `OrdersController.SendToProduction`, que delega o planejamento a `OrderProductionPlanningService`/`ProductionPlanner`.
- O planejamento cria `ProductionTask`, registra saídas de estoque com `BusinessReference` igual ao identificador da tarefa e muda o pedido de `Normalized` para `ReadyForProduction`.
- `ManualOrderService` já possui uma compensação semelhante durante a edição do pedido, mas ela é privada, remove tarefas somente no fluxo de edição e não representa uma operação de negócio independente.
- O objetivo técnico é disponibilizar a reversão no detalhe do pedido, compensar todos os consumos das tarefas não concluídas, excluir essas tarefas e deixar o pedido em `Normalized`, pronto para novo planejamento.

## Solução proposta

- Introduzir um caso de uso de aplicação/domínio dedicado à reversão do planejamento, reutilizando `IOrderRepository`, `IProductionTaskRepository`, `IStockMovementRepository` e `IUnitOfWork`.
- Ler o pedido e todas as tarefas relacionadas antes de alterar qualquer registro. Se alguma tarefa estiver `Completed`, rejeitar a operação sem compensação, exclusão ou mudança de estado.
- Para cada tarefa elegível, localizar as movimentações `Outbound` pelo `BusinessReference` da tarefa e criar uma movimentação `Inbound` compensatória por movimento original, preservando quantidade e custo unitário.
- Usar uma referência determinística para a compensação, baseada no movimento original, para tornar a operação idempotente. A existência da compensação deve ser verificada antes da inclusão.
- Excluir as tarefas elegíveis e retornar o pedido para `Normalized` por uma transição de domínio explícita, mantendo o pedido apto a uma nova chamada de planejamento.
- Persistir compensações, exclusões, alteração do pedido e auditoria da operação dentro da mesma unidade transacional. A operação deve ser preparada em memória e confirmada por uma única gravação, com mecanismo transacional quando necessário para incluir a auditoria de negócio.
- Expor uma ação POST no detalhe do pedido, com política `Write` e antiforgery, mensagem segura para sucesso/rejeição e atualização natural da agenda após a exclusão das tarefas.

## Arquitetura e contratos

- `src/Domains/Production/Services`: adicionar o contrato/comportamento de reversão junto às regras de planejamento, sem dependência de EF Core.
- `src/Domains/Sales/Entities/Order.cs`: adicionar ou ajustar uma transição de domínio que permita `ReadyForProduction` retornar a `Normalized`, sem permitir transições inválidas de `InProduction`, `Shipped`, `Delivered` ou `Cancelled`.
- `src/App/Services`: orquestrar leitura, validação, compensação, exclusão, persistência e resultado da operação; evitar duplicar a lógica privada existente de `ManualOrderService`.
- `src/Domains/Production/Repositories/IProductionTaskRepository.cs`: reutilizar `ListByOrderAsync` e `DeleteAsync`; não introduzir exclusão genérica do pedido como substituto da reversão.
- `src/Domains/Inventory/Repositories/IStockMovementRepository.cs`: reutilizar `ListByBusinessReferenceAsync` e `ExistsByBusinessReferenceAsync` para localizar consumos e garantir idempotência.
- `src/Domains/SharedKernel/Repositories/IUnitOfWork.cs` e `src/Infra/Repositories/UnitOfWork.cs`: preservar a fronteira atual e, se necessário para a auditoria na mesma confirmação, ampliar o contrato com uma operação transacional mínima sem expor EF ao domínio.
- `src/Infra/Security/AuditAction.cs` e `IAuditService`: adicionar uma ação específica de reversão e permitir que o registro de operação participe da mesma confirmação, mantendo `CorrelationId`, usuário e caminho da requisição.
- `src/App/Controllers/OrdersController.cs` e `src/App/Views/Orders/Details.cshtml`: adicionar POST protegido e disponibilizar a ação somente quando o pedido for elegível; o servidor deve continuar sendo a autoridade final.
- `src/UI/Components/Orders/OrderDetailsPage.razor` e modelos de detalhe: apresentar confirmação, resultado e estado da ação sem expor detalhes técnicos.

## Fluxo de dados, erros e concorrência

1. O POST recebe somente o identificador do pedido e o token antiforgery.
2. O serviço carrega o pedido e suas tarefas no mesmo contexto de persistência.
3. O serviço valida a existência do pedido, a ausência de tarefas `Completed`, a ausência de estados de envio incompatíveis e a possibilidade de retornar a `Normalized`.
4. O serviço prepara compensações idempotentes para todos os movimentos de saída vinculados às tarefas.
5. O serviço exclui as tarefas, aplica a transição de pedido e registra a auditoria de negócio.
6. A unidade de trabalho confirma tudo atomicamente; em conflito, nenhuma reversão é apresentada como concluída.
7. A controller redireciona ao detalhe com mensagem segura e a próxima consulta deixa de exibir as tarefas excluídas.

- Rejeições de regra devem informar que a reversão não é permitida porque há produção concluída ou estado incompatível.
- `DbUpdateConcurrencyException` ou conflito equivalente deve ser tratado como falha segura, sem sobrescrever o avanço concorrente.
- Repetição após sucesso deve ser no-op consistente quando o pedido já estiver `Normalized` sem tarefas; não deve recriar compensações.
- Movimentos originais nunca são alterados ou removidos.

## Persistência, observabilidade e segurança

- Não é prevista migration: o esquema atual já possui status, tarefas, movimentos com `BusinessReference` e auditoria.
- A referência compensatória deve ser estável e limitada ao tamanho configurado para `BusinessReference`.
- Auditoria automática de entidades deve continuar registrando exclusões, inclusões de compensação e alteração do pedido; a ação específica deve registrar resultado, pedido e resumo seguro dos efeitos.
- O `CorrelationId` existente deve acompanhar a tentativa. Não registrar corpo da requisição, tokens, SQL, stack trace ou dados sensíveis desnecessários.
- A autorização deve seguir `ApplicationPolicies.Write` e a confirmação antiforgery já adotada pelas ações POST.

## Alternativas consideradas

- Reutilizar diretamente `ManualOrderService.UpdateAsync`: rejeitado porque acopla a reversão a uma edição e não fornece uma ação explícita, auditoria própria nem tratamento claro de repetição.
- Cancelar tarefas em vez de excluí-las: rejeitado pela decisão registrada; manteria tarefas antigas e impediria o replanejamento conforme a regra atual de planejamento.
- Excluir o pedido: rejeitado porque é destrutivo e já possui fluxo próprio de compensação/exclusão, diferente de desfazer somente o planejamento.
- Reverter alterando ou apagando movimentos originais: rejeitado pela constituição; o estoque exige trilha de movimentos auditáveis.

## Riscos e compatibilidade

- Compensar uma tarefa `InProduction` pode devolver material que já foi consumido fisicamente. Mitigação: registrar explicitamente essa decisão, exigir confirmação no UI e auditar o efeito; tarefas `Completed` continuam bloqueando.
- Uma auditoria gravada fora da mesma confirmação pode divergir do estado operacional. Mitigação: preparar o registro de operação na mesma unidade transacional ou fazer a mudança falhar quando a auditoria não puder ser confirmada.
- Replanejamento imediato pode criar novas tarefas com novos identificadores. Mitigação: usar referências compensatórias baseadas nos movimentos originais, não no estado atual do pedido.
- A exclusão pode afetar consultas em cache. Mitigação: invalidar caches existentes relacionados a pedido, produção e estoque, se aplicável, e validar consultas subsequentes.

## Estratégia de testes

- Domínio: transição `ReadyForProduction` para `Normalized`, rejeição de `Completed` e rejeição de estados de pedido incompatíveis.
- Serviço: compensação de todos os consumos, preservação dos originais, exclusão de tarefas, idempotência e pedido replanejável.
- Controller/UI: autorização, antiforgery, confirmação, mensagens seguras, sucesso e rejeições.
- Persistência: confirmação atômica, auditoria/correlação, conflito concorrente com `start`/`complete` e ausência de alterações parciais.
- Regressão: envio original para produção, edição de pedido, cancelamento/avanço de tarefas, agenda e cálculo financeiro baseado em movimentos.
- Gates: executar build, testes, segurança de pacotes e cobertura conforme `.spectro/config.yaml` após a implementação.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001`, `NFR-003` | `AC-001`, `AC-009` | `TASK-004`, `TASK-006` |
| `FR-002`, `CON-002`, `CON-003`, `CON-005` | `AC-002`, `AC-004` | `TASK-001`, `TASK-002`, `TASK-003` |
| `FR-003` | `AC-003` | `TASK-002`, `TASK-005` |
| `FR-004` | `AC-004` | `TASK-001`, `TASK-005` |
| `FR-005`, `NFR-001` | `AC-005` | `TASK-002`, `TASK-003`, `TASK-005` |
| `FR-006`, `NFR-004` | `AC-007` | `TASK-003`, `TASK-005` |
| `FR-007` | `AC-008` | `TASK-004`, `TASK-005` |
| `NFR-002` | `AC-006` | `TASK-003`, `TASK-005` |
| `CON-001`, `CON-004` | `AC-003`, `AC-008` | `TASK-001`, `TASK-004` |
