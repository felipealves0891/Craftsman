# Descontos e Cliente no Pedido

## Objetivo

Temos duas alterações importantes para fazer no fluxo do pedido, que pode impactar varias etapas do da aplicação.

### Desconto

Adicionar bloco de desconto na pagina de `Novo Pedido` (Pedido Manual), que deve ser vinculado ao pedido com descrição e valor do desconto, com a opção de percentual, onde o valor é calculado ao salvar o pedido.


#### Pedidos

Na pagina de `Pedidos`, devemos adicionar uma coluna informando se o pedido possui desconto. E na pagina de `Detalhes` do pedido, devemos adicionar o desconto.

#### Financeiro

Na pagina de `Apuração financeira`, devemos colocar uma coluna de `Desconto` com o valor do desconto, que deve ser adicionado ao calculo do card de `Total de Custos`. E o calculo de `Margem` deve ser (Receita - (Custo + Desconto)). 

E adicionar um item de desconto na pagina de `Acerto financeiro` (Detalhes).

### Cliente

Adicionar bloco de `Cliente` na pagina de `Novo Pedido` (Pedido Manual), que deve ser de texto aberto, com o auto-complete de clientes já dastrados. 

O `Cliente` deve ter os campos `Nome`, `Telefone` e `Email`. Mas só o nome deve ser obrigatorio, as outras informações devem ser opcionais.

Para manter compatibilidade, `Cliente` deve possuir uma validação no front-end e back-end, e ser opcional no banco de dados.

Devemos ter testes e erros claros, para facilidar validações futuras.

#### Pedidos

Na pagina de pedidos, devemos retirar o campo `Pedido Externo` e adicionar o campo `Cliente` no lugar. E na pagina de `Detalhes` do pedido, incluir um card com o cliente.

## Importante

Sempre me pergunte caso aja duvidas, unca deduza nada, e sempre siga o fluxo de desenvolvimento indocado no AGENTS.md