Hoje temos alguns furos no fluxo do pedido, por exemplo:

- **InProduction**

    O status `InProduction` existe no domínio `Order` e é atualizado via `StartProduction()`, mas não existe chamada real no fluxo da aplicação. A tela de produção altera o status da `ProductionTask`, não do `Order`, então precisamos atualiza-lo.

- **Shipiment**

    O status `Shipped` existe no domínio `Order` e é atualizado via `MarkShipped()`, mas não existe chamada real no fluxo da aplicação. O envio cria/altera `Shipment`, não o `Order`, então precisamos atualiza-lo.

- **Delivered**

    O status `Delivered` existe no domínio `Order` e é atualizado via `MarkDelivered()`, mas não existe chamada real atualizando o `Order`. Quando uma entrega é confirmada, o `Shipment` dispara `DeliveryConfirmedEvent`, usado pelo financeiro, mas o pedido não é marcado como `Delivered`.



