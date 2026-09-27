# Exclusão Segura de Pedido e Referências

## Resumo

A feature já está especificada em specs/order-edit-delete/spec.md e parcialmente/majoritariamente implementada. O fluxo atual em src/App/Services/ManualOrderService.cs já faz o essencial para exclusão segura:

Remove produção iniciada/concluída;
remove envio vinculado;
reverte consumo de estoque com movimento compensatório de entrada;
remove tarefas de produção planejadas;
exclui o pedido e seus itens.

A suíte atual passa: 203 testes aprovados com dotnet test Craftsman.slnx --no-restore.

## O Que É Necessário

Manter a exclusão centralizada no serviço de aplicação, não direto no repositório/controlador, porque a consistência depende de regras de produção, estoque, envio e financeiro juntas.

Antes de excluir:
carregar o pedido;
verificar tarefas de produção do pedido;
verificar envios vinculados;
reverter movimentos Outbound ligados às tarefas planejadas criando movimentos Inbound compensatórios;
remover/cancelar tarefas planejadas;
excluir o pedido.

Preservar rastreabilidade do estoque: não editar nem apagar movimentos históricos de estoque.

Garantir transação única via UnitOfWork.SaveChangesAsync, para não deixar reversão sem exclusão ou exclusão sem reversão.

Revisar financeiro, pois dve ser excluido junto

## Principais Riscos

Estoque duplicadamente revertido: se a operação falhar depois de criar movimentos compensatórios e for repetida fora de uma transação real, pode devolver estoque duas vezes. A mitigação é transação única e, idealmente, checagem/idempotência por BusinessReference.

Concorrência: outro usuário/processo pode iniciar produção ou criar envio entre validação e exclusão. Mitigação ideal: transação + constraints/concorrência otimista ou revalidação próxima do SaveChanges.

Cache/listagens: repositórios limpam caches de pedidos, produção e estoque; isso deve continuar obrigatório para não exibir saldos/listagens antigas.

## Testes Recomendados

Excluir pedido planejado restaura saldo e remove pedido/tarefas.
Excluir pedido sem produção remove apenas pedido/itens.
Falha durante exclusão não deixa estoque revertido parcialmente.
Repetir tentativa após falha não duplica reversão de estoque.
Listagens de pedidos, produção e saldos refletem o estado final.

## Assumptions

Exclusão física do pedido é aceitável para pedidos ainda não operados; caso auditoria seja prioridade, trocar para cancelamento/soft delete.
Produção apenas planejada pode ser revertida automaticamente.
Pedidos com envio ou acerto financeiro não devem ser excluídos pela aplicação.
Movimentos de estoque devem permanecer append-only, com compensações em vez de edição histórica.