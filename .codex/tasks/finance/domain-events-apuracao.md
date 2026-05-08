# Domain Events e Apuracao Financeira Manual

- [x] Domain Events com confirmacao de execucao e tratativa de erro.
- [x] Data de Envio do Pedido sempre futura em UTC.
- [x] DeliveryConfirmedSettlementHandler invocado por evento de entrega.
- [x] Geracao manual de apuracao financeira pelo Financeiro.

## Criterios de aceitacao

- Cada handler de Domain Event registra sucesso ou falha.
- Falhas de handler ficam disponiveis para reprocessamento.
- Pedido rejeita Data de Envio igual ou anterior a data UTC atual.
- Entrega confirmada aciona apuracao financeira automaticamente.
- Financeiro permite gerar apuracao manual para entregas sem apuracao.
