# Feature: Desconto e Cliente no Pedido

## Summary

Adicionar informacoes de desconto e cliente ao fluxo do pedido manual, permitindo que pedidos tenham desconto vinculado e dados de cliente exibidos nas telas de pedidos e financeiro.

A feature melhora a rastreabilidade comercial do pedido e ajusta a apuracao financeira para considerar descontos no custo total e na margem.

## Context

Atualmente o pedido possui origem, referencia externa/manual, data de envio e itens. A tela de novo pedido manual permite cadastrar itens e calcular um total estimado a partir de quantidade e valor unitario.

A listagem de pedidos exibe origem, pedido externo, data de envio, status, quantidade de itens e data de criacao. Os detalhes do pedido exibem informacoes gerais, itens, producao e envios.

A apuracao financeira calcula receita, custo de producao, custo de envio, custo total e margem. A margem atual considera receita menos custo total, sem desconto.

## Problem

Pedidos manuais nao registram cliente nem desconto, dificultando identificar para quem o pedido foi feito e impedindo que descontos concedidos sejam refletidos na apuracao financeira.

## Goals

- Permitir registrar cliente em pedidos manuais.
- Permitir registrar desconto em pedidos manuais.
- Exibir cliente e desconto nas telas de pedidos.
- Exibir desconto nas telas de financeiro.
- Considerar desconto no custo total e na margem financeira.
- Preservar compatibilidade com pedidos existentes sem cliente e sem desconto.
- Validar cliente e desconto no front-end e no back-end com mensagens claras.

## Non-Goals

Esta feature nao inclui:

- cadastro completo de clientes em uma tela dedicada;
- gestao de historico de alteracoes de cliente;
- cupom promocional;
- aprovacao de desconto;
- desconto por item;
- multiplos descontos por pedido;
- permitir cliente/desconto em pedidos importados nesta entrega.

## Actors

### Usuario autorizado

Cria pedidos manuais, consulta pedidos e consulta apuracao financeira.

### Sistema

Persiste os dados do pedido e calcula a apuracao financeira considerando desconto quando existente.

## Preconditions

- O usuario esta autorizado a criar pedidos manuais.
- O usuario esta autorizado a consultar pedidos e financeiro conforme as regras atuais.
- O pedido manual contem ao menos um item valido.

## Functional Requirements

### FR-001 - Cliente no pedido manual

A tela de Novo Pedido deve permitir informar cliente para pedidos manuais.

### FR-002 - Campos do cliente

O cliente do pedido deve conter Nome, Telefone e Email.

### FR-003 - Nome obrigatorio quando cliente for informado

Quando os dados de cliente forem preenchidos no pedido manual, Nome deve ser obrigatorio.

### FR-004 - Telefone e email opcionais

Telefone e Email devem ser opcionais.

### FR-005 - Cliente opcional na persistencia

Pedidos existentes e pedidos sem cliente devem continuar validos. A referencia de cliente no pedido deve ser opcional no banco de dados.

### FR-006 - Validacao de cliente no front-end e back-end

A aplicacao deve validar os dados de cliente no front-end e no back-end, apresentando erros claros quando a entrada for invalida.

### FR-007 - Autocomplete de clientes cadastrados

A tela de Novo Pedido deve oferecer autocomplete para clientes ja cadastrados.

### FR-008 - Cliente na listagem de pedidos

Na pagina de Pedidos, a coluna Pedido Externo deve ser removida e substituida por Cliente.

### FR-009 - Cliente nos detalhes do pedido

A pagina de Detalhes do pedido deve exibir um card com os dados do cliente quando o pedido possuir cliente.

### FR-010 - Desconto no pedido manual

A tela de Novo Pedido deve permitir informar desconto vinculado ao pedido.

### FR-011 - Campos do desconto

O desconto do pedido deve conter descricao e valor.

### FR-012 - Desconto percentual

O desconto deve oferecer uma opcao percentual, na qual o valor monetario do desconto e calculado ao salvar o pedido.

### FR-013 - Desconto na listagem de pedidos

Na pagina de Pedidos, a aplicacao deve adicionar uma coluna informando se o pedido possui desconto.

### FR-014 - Desconto nos detalhes do pedido

A pagina de Detalhes do pedido deve exibir o desconto do pedido quando existente.

### FR-015 - Desconto na apuracao financeira

Na pagina de Apuracao financeira, a listagem deve exibir uma coluna Desconto com o valor do desconto considerado no acerto.

### FR-016 - Desconto no custo total

O card Total de Custos da Apuracao financeira deve incluir descontos no total consolidado de custos.

### FR-017 - Margem financeira com desconto

A margem financeira deve ser calculada como Receita - (Custo + Desconto).

### FR-018 - Desconto no detalhe do acerto financeiro

A pagina de Acerto financeiro deve exibir um item de desconto.

### FR-019 - Compatibilidade financeira

Acertos de pedidos sem desconto devem continuar com desconto igual a zero.

### FR-020 - Cadastro automatico de cliente

Cliente deve ser uma entidade propria. Ao salvar um pedido manual com cliente, a aplicacao deve vincular o pedido a um cliente existente ou criar um novo cliente caso ele ainda nao exista.

### FR-021 - Identificacao de cliente por nome normalizado

A identificacao de cliente existente deve usar um campo Nome Normalizado derivado do nome do cliente, sem espacos, sem caracteres nao imprimiveis e em maiusculo. Nome Normalizado deve ser unico na persistencia.

### FR-022 - Recalculo financeiro apos edicao

Quando um pedido com acerto financeiro ja calculado for editado e a edicao alterar dados que afetam o acerto, a aplicacao deve recalcular automaticamente o acerto financeiro.

### FR-023 - Restricao inicial para pedidos importados

Cliente e desconto nao devem ser adicionados a pedidos importados nesta feature.

## User Flow

1. Usuario abre Novo Pedido.
2. Sistema exibe os blocos Dados do pedido, Cliente, Itens do pedido e Desconto.
3. Usuario informa os dados obrigatorios do pedido.
4. Usuario informa cliente.
5. Sistema sugere clientes ja cadastrados no autocomplete.
6. Usuario informa desconto, se aplicavel.
7. Usuario salva o pedido.
8. Sistema valida os dados no back-end.
9. Sistema calcula o valor monetario do desconto percentual, quando aplicavel.
10. Sistema persiste o pedido com cliente e desconto.
11. Usuario consulta Pedidos, Detalhes do pedido e Financeiro.
12. Sistema exibe cliente e desconto conforme as telas afetadas.

## Scenarios

### Scenario: Pedido manual com cliente e desconto monetario

Given o usuario esta criando um pedido manual
And informa Nome do cliente
And informa um desconto monetario valido
When salva o pedido
Then o pedido deve ser criado
And a listagem de pedidos deve exibir o cliente
And a listagem deve indicar que o pedido possui desconto
And os detalhes do pedido devem exibir cliente e desconto

### Scenario: Pedido manual com desconto percentual

Given o usuario esta criando um pedido manual com itens validos
And informa desconto percentual valido
When salva o pedido
Then o sistema deve calcular o valor monetario do desconto sobre o total bruto dos itens
And persistir o valor calculado vinculado ao pedido

### Scenario: Pedido sem cliente

Given existe um pedido sem cliente
When o usuario consulta pedidos ou detalhes
Then a aplicacao deve continuar funcionando
And deve exibir uma representacao clara de cliente ausente.

### Scenario: Apuracao financeira com desconto

Given existe pedido entregue com receita, custo e desconto
When o acerto financeiro e calculado
Then o desconto deve ser exibido no financeiro
And o custo total deve incluir o desconto
And a margem deve ser Receita - (Custo + Desconto).

### Scenario: Cliente existente pelo nome normalizado

Given existe cliente cadastrado com Nome Normalizado igual ao nome informado no pedido
When o usuario salva um pedido manual com esse cliente
Then o pedido deve ser vinculado ao cliente existente
And a aplicacao nao deve criar cliente duplicado.

### Scenario: Edicao de pedido com acerto financeiro

Given existe pedido manual com acerto financeiro calculado
When o usuario edita dados que afetam o acerto financeiro
Then o sistema deve recalcular automaticamente o acerto financeiro.

## Business Rules

### BR-001 - Desconto unico por pedido

Cada pedido deve possuir no maximo um desconto.

### BR-002 - Desconto sem valor e equivalente a ausencia de desconto

Pedidos sem desconto informado devem ser tratados como desconto zero nas telas e nos calculos financeiros.

### BR-003 - Desconto nao altera receita bruta

O desconto deve afetar custo total e margem financeira, mas nao deve alterar a receita bruta do pedido.

### BR-004 - Dados antigos permanecem validos

Pedidos existentes sem cliente e sem desconto devem permanecer consultaveis e aptos aos fluxos atuais.

### BR-005 - Percentual sobre total bruto dos itens

Quando o desconto for percentual, o valor monetario do desconto deve ser calculado sobre o total bruto dos itens do pedido.

### BR-006 - Desconto maior que receita

A aplicacao deve permitir desconto maior que a receita do pedido e margem negativa, desde que apresente um alerta claro ao usuario.

### BR-007 - Descricao obrigatoria para desconto

Quando houver valor de desconto maior que zero, a descricao do desconto deve ser obrigatoria.

### BR-008 - Cliente existente por nome normalizado

Dois clientes com o mesmo Nome Normalizado representam o mesmo cliente para fins de autocomplete e vinculo de pedido.

## Invariants

- Receita nao deve ser recalculada por causa do desconto.
- Pedidos existentes sem cliente devem continuar validos.
- Pedidos existentes sem desconto devem continuar validos.
- As regras atuais de autorizacao devem continuar aplicadas nas telas afetadas.
- Entradas de usuario devem ser tratadas como nao confiaveis no servidor.
- Nome Normalizado de cliente deve permanecer unico.

## Data

### Cliente

- Nome: texto, obrigatorio quando cliente for informado.
- Nome Normalizado: texto derivado do Nome, unico, sem espacos, sem caracteres nao imprimiveis e em maiusculo.
- Telefone: texto, opcional.
- Email: texto, opcional.

### Desconto

- Descricao: texto, obrigatorio quando houver desconto maior que zero.
- Valor: monetario calculado ou informado.
- Percentual: opcao para indicar que o valor informado representa percentual.

## Validation

- Nome do cliente deve ser obrigatorio quando qualquer dado de cliente for informado.
- Telefone e Email podem ficar vazios.
- Desconto ausente deve ser valido.
- Valor de desconto informado deve ser numerico e nao negativo.
- Percentual de desconto deve ser numerico e nao negativo.
- Descricao do desconto deve ser obrigatoria quando houver desconto maior que zero.
- Desconto maior que a receita do pedido deve ser aceito somente com alerta claro ao usuario.
- O back-end deve rejeitar entradas invalidas mesmo que a validacao do front-end seja contornada.
- Mensagens de validacao devem indicar claramente o campo com problema.

## Authorization

As telas e acoes afetadas devem preservar as politicas atuais:

- leitura para consultar pedidos e financeiro;
- escrita para criar ou alterar pedidos.

## Security

Cliente, descricao do desconto, telefone e email sao entradas do usuario e devem ser renderizados com codificacao segura.

A persistencia nao deve montar consultas por concatenacao de strings com entrada do usuario.

## Compatibility

- Pedidos ja persistidos sem cliente devem continuar carregando.
- Pedidos ja persistidos sem desconto devem continuar carregando.
- Rotas existentes de Pedidos, Novo Pedido, Detalhes do pedido e Financeiro devem continuar funcionando.
- Dados importados de fontes externas devem continuar validos mesmo sem cliente/desconto.
- Pedidos importados nao devem passar a exigir cliente ou desconto.

## Edge Cases

- Cliente ausente.
- Cliente com nome em branco e telefone/email preenchidos.
- Cliente informado com variacoes de espacos, caracteres nao imprimiveis ou caixa.
- Pedido com desconto zero.
- Pedido sem desconto.
- Desconto percentual em pedido com total zero.
- Desconto maior que a receita do pedido.
- Pedido antigo sem colunas de cliente/desconto preenchidas.
- Acerto financeiro ja calculado antes da existencia do desconto.
- Edicao de pedido com acerto financeiro ja calculado.

## Dependencies

Esta feature depende dos fluxos atuais de:

- pedido manual;
- listagem de pedidos;
- detalhes do pedido;
- calculo de acerto financeiro;
- listagem de apuracao financeira;
- detalhes de acerto financeiro;
- persistencia EF Core.

## Acceptance Criteria

- Novo Pedido exibe bloco de Cliente com Nome, Telefone e Email.
- Novo Pedido exibe bloco de Desconto com descricao, valor e opcao percentual.
- Cliente e validado no front-end e back-end.
- Desconto e validado no front-end e back-end.
- Pedido manual pode ser salvo com cliente e desconto.
- Pedido manual pode ser salvo sem cliente e sem desconto, mantendo compatibilidade.
- Cliente informado em pedido manual cria ou reutiliza entidade de cliente por Nome Normalizado.
- Listagem de Pedidos substitui Pedido Externo por Cliente.
- Listagem de Pedidos informa se o pedido possui desconto.
- Detalhes do Pedido exibem card de cliente.
- Detalhes do Pedido exibem desconto.
- Apuracao financeira exibe coluna Desconto.
- Total de Custos inclui desconto.
- Margem usa Receita - (Custo + Desconto).
- Acerto financeiro detalhado exibe desconto.
- Edicoes relevantes em pedido com acerto recalculam automaticamente o acerto financeiro.
- Pedidos importados continuam sem cliente/desconto nesta entrega.
- Build passa.
- Testes relevantes passam.

## Test Considerations

Verificar no minimo:

- criacao de pedido com cliente;
- criacao de pedido sem cliente;
- reutilizacao de cliente existente por nome normalizado;
- validacao de nome obrigatorio quando cliente parcial for informado;
- criacao de pedido com desconto monetario;
- criacao de pedido com desconto percentual;
- alerta para desconto maior que a receita;
- listagem de pedidos com cliente e indicador de desconto;
- detalhes de pedido com cliente e desconto;
- calculo financeiro com desconto;
- calculo financeiro sem desconto;
- recalculo automatico de acerto financeiro apos edicao relevante do pedido;
- compatibilidade de pedido antigo sem cliente/desconto.

## Open Questions

Nenhuma pergunta aberta bloqueante no momento.

## Risks

### Acertos financeiros existentes

Acertos ja calculados antes da feature podem nao conter desconto persistido no proprio acerto.

Mitigacao:

O planejamento deve definir a estrategia de persistencia do desconto financeiro preservando compatibilidade com acertos antigos e suportando recalculo automatico apos edicoes relevantes.

### Ambiguidade de cliente cadastrado

Nao existe, no comportamento atual observado, uma area explicita de cadastro de clientes.

Mitigacao:

Criar entidade propria de cliente e usar Nome Normalizado unico como referencia para autocomplete e criacao automatica.
