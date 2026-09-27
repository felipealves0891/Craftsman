# Implementation Plan: Exclusao Completa de Pedido e Referencias

## Summary

Substituir a exclusao restrita atual por um processo de exclusao completo e idempotente. O fluxo continuara entrando por `OrdersController.Delete`, mas delegara para `ManualOrderService.DeleteAsync` com um identificador de processo. O servico registrara uma auditoria persistente, compensara consumos de estoque por movimentos inbound append-only e removera referencias operacionais diretas antes de remover o pedido.

## Specification Reference

`specs/exclusao-completa-pedidos/spec.md`

## Current Architecture

- `OrdersController` expõe a acao POST de exclusao com `ApplicationPolicies.Write` e anti-forgery.
- `ManualOrderService.DeleteAsync` atualmente bloqueia producao iniciada e envios por meio de `EnsureOrderCanChangeAsync`.
- `OrderRepository.DeleteAsync` remove o pedido; os itens sao removidos por cascade.
- `ProductionTaskRepository` ja lista e remove tarefas por id.
- `ShipmentRepository` lista envios, mas nao possui remocao.
- `FinancialSettlementRepository` consulta por pedido, mas nao possui remocao.
- `StockMovementRepository` lista movimentos por `BusinessReference`; consumo de producao usa o id da tarefa como referencia.
- `AppDbContext` cria audit logs genericos, mas esses logs nao contem o resumo exigido nem dao suporte suficiente a retomada/idempotencia por processo.

## Change Surface

- `src/App/Services/ManualOrderService.cs`
- `src/App/Controllers/OrdersController.cs`
- `src/UI/Components/Orders/OrderDetailsPage.razor`
- `src/App/Views/Orders/Details.cshtml`
- `src/Domain` interfaces de repositório afetadas
- `src/Infra` repositórios EF e configuração de persistência
- `src/Infra/Persistence/Migrations`
- `tests/Craftsman.Tests/Application/ManualOperationsTests.cs`

## Implementation Strategy

Criar um registro persistente de processo de exclusao de pedido, identificado por `processId`, antes de qualquer remocao operacional. O processo armazenara o snapshot minimo exigido do pedido e contadores de referencias encontradas.

O controller recebera `processId` opcional do formulario. A view gerara um novo `Guid` escondido ao renderizar o botao, permitindo que uma repeticao do mesmo POST reutilize o mesmo processo.

O servico passara a:

- carregar o pedido, tarefas, envios e financeiro;
- criar ou reutilizar a auditoria do processo;
- criar movimentos inbound compensatorios para cada movimento outbound de tarefas do pedido quando ainda nao existir compensacao para aquele par processo/movimento;
- remover financeiro, envios e tarefas associados ao pedido;
- remover o pedido.

Se o pedido ja tiver sido removido em uma retentativa com o mesmo `processId`, o servico deve tratar como sucesso, desde que o processo de exclusao exista. Se o pedido nao existir e nao houver processo correspondente, deve retornar erro de pedido nao encontrado.

## Data Flow

`POST /Orders/Delete/{id}` com `processId`

`OrdersController.Delete`

`ManualOrderService.DeleteAsync(orderId, processId)`

`OrderDeletionProcessRepository` + repositórios operacionais + `StockMovementRepository`

`UnitOfWork.SaveChangesAsync`

redirect para `Orders/Index`.

## Existing Components to Reuse

- `ApplicationPolicies.Write` e `[ValidateAntiForgeryToken]`.
- `ManualOrderService` como orquestrador do fluxo de pedidos manuais.
- `StockMovementRepository.ListByBusinessReferenceAsync` para localizar consumos por tarefa.
- `StockMovement` para registrar compensacoes append-only.
- `UnitOfWork` para persistir a unidade de trabalho.

## New Components

### `OrderDeletionProcess`

Modelo de dominio/application simples para representar a auditoria idempotente da exclusao.

### `IOrderDeletionProcessRepository` e `OrderDeletionProcessRepository`

Necessarios para criar/reusar o processo por id e consultar retomadas apos falha.

### `OrderDeletionProcessEntity` e configuração EF

Persistem o snapshot minimo exigido pela especificacao.

## Files to Modify

- `ManualOrderService.cs`: implementar processo completo, remover bloqueio de envio/producao iniciada para exclusao e preservar bloqueio apenas para edicao.
- `OrdersController.cs`: receber `processId`, encaminhar ao servico e tratar erros esperados sem expor detalhes tecnicos.
- `OrderDetailsPage.razor` e `Details.cshtml`: incluir campo oculto `processId` e permitir exclusao mesmo quando houver producao/envio.
- Interfaces/repositórios de producao, envio e financeiro: adicionar remocao por pedido quando necessario.
- `StockMovementRepository`: adicionar consulta por prefixo ou existencia de referencia para idempotencia.
- `ApplicationServiceCollectionExtensions`/infra DI: registrar novo repositório, se necessario.
- Testes de aplicacao: cobrir exclusao completa, compensacao, auditoria e idempotencia.

## Files to Create

- Modelo e interface de processo de exclusao.
- Entidade/configuração EF do processo.
- Migration EF para a tabela `order_deletion_processes`.

## Persistence Changes

Adicionar tabela de processos de exclusao de pedido com:

- `id` como `Guid` do processo;
- usuario solicitante e data/hora;
- `order_id`, origem, referencia externa, status, data de envio;
- quantidade/resumo de itens;
- quantidade de tarefas e envios removidos;
- indicacao de financeiro existente/removido;
- data de conclusao opcional.

Nao alterar nem remover registros historicos de estoque.

## API Contract Changes

`POST /Orders/Delete/{id}` passara a aceitar campo de formulario opcional `processId`. Chamadas sem esse campo continuam validas, gerando um novo processo no servidor.

## UI Changes

O botao de exclusao nos detalhes do pedido deve permanecer protegido por anti-forgery e enviar o `processId` oculto. A exclusao deve ficar disponivel para pedidos com producao ou envio, pois a feature exige remocao dessas referencias.

## Configuration Changes

Nenhuma.

## Dependencies

Nenhum novo pacote.

## Security Considerations

- Manter `ApplicationPolicies.Write` na action de exclusao.
- Manter anti-forgery.
- Registrar o usuario autenticado no processo de exclusao usando o contexto de usuario existente.
- Nao expor detalhes tecnicos inesperados ao usuario; erros esperados podem mostrar mensagem controlada.

## Performance Considerations

As buscas por referencias serao filtradas por `OrderId` ou `BusinessReference`. A compensacao consulta movimentos por tarefa, consistente com o desenho atual de estoque.

## Testing Strategy

- Servico remove pedido, itens, producao, envio e financeiro.
- Servico preserva outbound historico e cria inbound compensatorio.
- Retentativa com mesmo `processId` nao duplica compensacao nem auditoria.
- Pedido inexistente sem processo retorna erro esperado.
- View/controller mantem anti-forgery, permissao de escrita e envia `processId`.

## Implementation Tasks

### Task 1 - Persistir processo de exclusao

Criar entidade, configuracao, modelo e repositorio para auditoria/idempotencia.

### Task 2 - Completar remocoes operacionais

Adicionar metodos de remocao por pedido em producao, envio e financeiro, preservando caches.

### Task 3 - Implementar orquestracao de exclusao

Atualizar `ManualOrderService.DeleteAsync` para criar/reusar auditoria, compensar movimentos e remover referencias.

### Task 4 - Atualizar UI/controller

Aceitar/enviar `processId`, permitir exclusao para pedidos com referencias e preservar seguranca.

### Task 5 - Adicionar testes e validar

Adicionar testes comportamentais e executar build/testes relevantes.

## Requirement Traceability

| Requirement | Implementation |
|---|---|
| FR-001 | Tasks 2, 3 |
| FR-002 | Tasks 2, 3 |
| FR-003 | Tasks 2, 3 |
| FR-004 | Tasks 2, 3 |
| FR-005 | Task 3 |
| FR-006 | Task 3 |
| FR-007 | Task 3 |
| FR-008 | Task 1, 3 |
| FR-009 | Task 1, 3, 5 |
| FR-010 | Task 1, 3 |

## Backward Compatibility

O endpoint de exclusao existente permanece o mesmo. O novo `processId` e opcional. Consultas, cadastros independentes e historico de estoque permanecem compativeis.

## Migration Strategy

Adicionar migration EF com a nova tabela antes de usar o processo em producao. A tabela nao requer backfill.

## Risks

- Falhas entre compensacao e remocao podem causar duplicidade se a idempotencia por `BusinessReference` nao for verificada.
- Remover referencias fora da ordem pode violar FKs quando o banco real tiver restricoes nao modeladas nos testes in-memory.

## Assumptions

- Movimentos outbound de producao continuam referenciando `ProductionTask.Id.ToString()` em `BusinessReference`.
- Ha no maximo um acerto financeiro por pedido, coerente com o indice unico existente.
- O audit log generico continua existindo, mas nao substitui o processo de exclusao exigido pela feature.

## Open Technical Questions

Nenhuma questao tecnica bloqueante.
