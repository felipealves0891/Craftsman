# Envio para Produção manualmente

## Objetivo
O usuario deve poder enviar pedido para produção em manualmente, caso o fluxo automatico de erro

## Tarefas

### Envio para produção manualmente
- [x] Adicionar botão para enviar pedido para produção na tela de detalhes de pedido
- [x] Criar rota para enviar pedido para produção
- [x] Reusar o serviço existente
- [x] Na agenda adicione o nome do cliente e a descrição em vez do ID do pedido
- [x] Troque os textos dos botões de Iniciar, concluir e cancelar por icones, e os texto como tooltip
- [x] O calendario deve ter scroll vertival para não ficar tão grande
- [x] Bloquear botão de concluir quando estiver em um status diferente de Em Produção
- [x] Bloquear botão de cancelar quando estiver em um status diferente de Em Produção
- [x] Bloquear botão de iniciar quando estiver em um status diferente de Planejado

Critérios de aceitação:
- O Usuario deve poder enviar um pedido para produção manualmente.
- O usuario deve identificar o pedido de forma simples
- Os botões da tarefa devem caber em uma linha
- A tela não pode ficar extremamente grande verticalmente
