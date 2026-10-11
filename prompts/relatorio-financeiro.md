# Relatorio Mensal de Gastos

## Contexto

Hoje temos a pagina de `Apuração financeira`, onde listamos os resultados dos pedidos.

## Objetivo

Criar painel com KPIs dos totais do periodo selecionado:

- Total de Receita 
- Total de Custos
- Total de Margem
- Margem por Pedido (total de margem / quantidade de pedidos)

Os cards devem ter um titulo e um icone, onde no mobile aparece apenas o icone e caso a pessoa clique no card um tooltip apareça com o titulo do card.

## Requisitos

O calculo deve ser feito no servidor, e incluido no cache para que o calculo não sejá feito a cada requisição.

O Cache deve durar por 1 hora, e caso um pedido for incluido no calculo , todos os caches devem ser excluidos, para isso, temos um handle `DeliveryConfirmedSettlementHandler` do evento `DeliveryConfirmedEvent`, que pode fazer isso tambem.