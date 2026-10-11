# Feature: Melhorias na tela de Apuracao financeira

## Summary

Melhorar a apresentacao da listagem de Apuracao financeira e da tela de detalhe do Acerto financeiro.

A listagem deve exibir identificadores de pedido de forma mais curta e datas de calculo sem horario. A tela de detalhe deve apresentar melhor os dados do acerto financeiro e incluir os itens do pedido relacionado.

## Context

A area financeira possui uma pagina de Apuracao financeira que lista acertos financeiros por pedido e permite acessar os detalhes de um acerto pela rota `Finance/Details?orderId=`.

Na listagem atual, o campo `Pedido` exibe o identificador completo do pedido, como `7f4b1817-21cf-4f3e-a2c3-4e936db7299b`. Esse formato ocupa muito espaco na tabela e dificulta a leitura rapida.

O campo `Calculado em` exibe data e horario. Para o uso esperado da listagem, apenas a data do calculo e necessaria.

A tela de detalhe do Acerto financeiro apresenta os valores do acerto, mas ainda nao exibe os itens do pedido que originaram a receita e os custos apresentados.

## Problem

Usuarios que analisam acertos financeiros precisam ler rapidamente a listagem e conferir o contexto do pedido no detalhe. A exibicao completa do identificador do pedido e o horario no campo de calculo deixam a tabela mais poluida, enquanto a ausencia dos itens do pedido no detalhe obriga o usuario a navegar para outra tela para validar o conteudo financeiro.

## Goals

- Reduzir a largura visual do campo `Pedido` na listagem de Apuracao financeira.
- Exibir somente a data no campo `Calculado em` da listagem de Apuracao financeira.
- Melhorar a organizacao visual da tela `Finance/Details?orderId=`.
- Exibir os itens do pedido relacionado na tela de detalhe do Acerto financeiro.
- Preservar as regras de calculo financeiro existentes.
- Preservar as rotas e permissoes existentes da area financeira.

## Non-Goals

Esta feature nao inclui:

- alterar a forma de calculo de receita, custos, margem ou status do acerto financeiro;
- alterar a geracao de acertos financeiros;
- alterar filtros da Apuracao financeira;
- adicionar novos KPIs financeiros;
- adicionar edicao de itens do pedido pela tela financeira;
- alterar a tela de detalhes do pedido fora do fluxo financeiro;
- mudar regras de autorizacao da area financeira.

## Actors

### Usuario autorizado

Consulta a Apuracao financeira, acessa os detalhes de um acerto e confere os itens do pedido relacionado.

### Sistema

Carrega os acertos financeiros, formata os dados para exibicao e disponibiliza os itens do pedido relacionado no detalhe financeiro.

## Preconditions

- O usuario esta autorizado a acessar a area financeira.
- Existem zero ou mais acertos financeiros calculados.
- Um acerto financeiro de detalhe esta associado a um pedido existente.

## Functional Requirements

### FR-001 - Pedido abreviado na listagem

Na listagem de Apuracao financeira, o campo `Pedido` deve exibir apenas os 8 primeiros caracteres do identificador do pedido.

Exemplo:

```text
7f4b1817-21cf-4f3e-a2c3-4e936db7299b -> 7f4b1817
```

### FR-002 - Preservar identificador completo para navegacao

A abreviacao visual do pedido nao deve alterar o identificador usado para navegar para `Finance/Details?orderId=`.

### FR-003 - Data de calculo sem horario na listagem

Na listagem de Apuracao financeira, o campo `Calculado em` deve exibir apenas a data, sem horario.

### FR-004 - Formato de data da listagem

A data exibida em `Calculado em` deve seguir o padrao de data ja usado pela aplicacao para exibicao ao usuario em portugues brasileiro.

### FR-005 - Melhorar apresentacao do detalhe financeiro

A tela `Finance/Details?orderId=` deve organizar os dados do acerto financeiro de forma mais legivel do que uma lista simples de definicoes, facilitando a leitura de pedido, receita, custos, margem, status e data de calculo.

### FR-006 - Itens do pedido no detalhe financeiro

A tela `Finance/Details?orderId=` deve exibir os itens do pedido associado ao acerto financeiro.

### FR-007 - Informacoes dos itens do pedido

Para cada item do pedido exibido no detalhe financeiro, o sistema deve apresentar, no minimo:

- identificador externo do item, quando existir;
- descricao;
- quantidade;
- valor unitario.

### FR-008 - Estado sem itens no detalhe financeiro

Se o pedido associado nao possuir itens disponiveis para exibicao, a tela de detalhe financeiro deve apresentar um estado vazio claro, sem falhar.

### FR-009 - Acerto sem pedido associado encontrado

Se o acerto financeiro existir, mas o pedido associado nao puder ser carregado, a tela de detalhe financeiro nao deve exibir dados incorretos de itens e deve seguir o comportamento de erro ou estado vazio definido pela aplicacao.

### FR-010 - Valores financeiros preservados

As melhorias de apresentacao nao devem alterar os valores de receita, custo de producao, custo de envio, custo total, margem, status ou data do acerto financeiro.

### FR-011 - Responsividade

As melhorias visuais da listagem e do detalhe financeiro devem funcionar em desktop e mobile sem quebrar o layout ou ocultar informacoes essenciais.

## Business Rules

### BR-001 - Abreviacao apenas visual

O pedido abreviado na listagem e apenas uma representacao visual. O identificador completo continua sendo a referencia do pedido para rotas, consultas e regras de negocio.

### BR-002 - Data de calculo

O campo `Calculado em` continua representando a data de calculo do acerto financeiro. A mudanca e apenas na granularidade exibida ao usuario na listagem.

### BR-003 - Itens pertencem ao pedido do acerto

Os itens exibidos na tela de detalhe financeiro devem pertencer ao mesmo pedido identificado pelo `orderId` do acerto financeiro.

### BR-004 - Detalhe financeiro nao edita pedido

A exibicao de itens do pedido no detalhe financeiro e somente leitura.

## User Flow

1. Usuario acessa a pagina de Apuracao financeira.
2. Sistema exibe a listagem de acertos financeiros.
3. Para cada acerto, sistema mostra o pedido com 8 caracteres e a data de calculo sem horario.
4. Usuario seleciona a acao `Detalhes` de um acerto.
5. Sistema abre `Finance/Details?orderId=`.
6. Sistema exibe os dados financeiros do acerto de forma organizada.
7. Sistema exibe os itens do pedido associado.

## Scenarios

### Scenario: Listagem exibe pedido abreviado

Given existe um acerto financeiro do pedido `7f4b1817-21cf-4f3e-a2c3-4e936db7299b`
When o usuario acessa a Apuracao financeira
Then o campo `Pedido` deve exibir `7f4b1817`
And o link de detalhes deve continuar apontando para o `orderId` completo.

### Scenario: Listagem exibe apenas data de calculo

Given existe um acerto financeiro calculado em uma data e horario especificos
When o usuario acessa a Apuracao financeira
Then o campo `Calculado em` deve exibir somente a data
And nao deve exibir hora ou minuto.

### Scenario: Detalhe financeiro exibe itens do pedido

Given existe um acerto financeiro para um pedido com itens
When o usuario acessa `Finance/Details?orderId=` para esse pedido
Then a tela deve exibir os dados do acerto financeiro
And deve listar os itens do pedido associado.

### Scenario: Pedido sem itens disponiveis

Given existe um acerto financeiro para um pedido sem itens disponiveis para exibicao
When o usuario acessa o detalhe financeiro
Then a tela deve exibir os dados do acerto financeiro
And deve exibir um estado vazio para os itens do pedido.

### Scenario: Acerto financeiro inexistente

Given nao existe acerto financeiro para o `orderId` informado
When o usuario acessa `Finance/Details?orderId=`
Then o comportamento de nao encontrado existente deve ser preservado.

## Invariants

- A listagem financeira deve continuar respeitando os filtros existentes.
- A abreviacao do pedido nao deve alterar dados persistidos.
- A abreviacao do pedido nao deve alterar o valor do parametro `orderId`.
- A tela de detalhe financeiro deve permanecer somente leitura.
- Os itens do pedido nao devem modificar o resultado do acerto financeiro.
- As permissoes da area financeira devem permanecer as mesmas.

## Data

### Input

Listagem financeira:

- filtros existentes de periodo, quando informados.

Detalhe financeiro:

- `orderId`: identificador completo do pedido.

### Output

Listagem financeira:

- pedido abreviado com 8 caracteres;
- receita;
- custos;
- margem;
- status;
- data de calculo sem horario;
- acao de detalhes usando o identificador completo.

Detalhe financeiro:

- identificador do pedido;
- receita;
- custo de producao;
- custo de envio;
- custo total;
- margem;
- status;
- data de calculo;
- itens do pedido associado.

## Validation

- `orderId` vazio ou invalido deve continuar seguindo o comportamento atual do ASP.NET MVC para a rota.
- `orderId` valido sem acerto financeiro deve continuar retornando nao encontrado.
- A existencia de itens do pedido nao e obrigatoria para exibir o acerto financeiro.

## Error Handling

Se os dados financeiros nao puderem ser carregados:

- o comportamento existente da tela financeira deve ser preservado;
- detalhes tecnicos da falha nao devem ser expostos ao usuario.

Se os itens do pedido nao puderem ser carregados:

- a tela nao deve exibir itens incorretos;
- a implementacao deve evitar apresentar totais ou dados parciais como se fossem completos;
- o comportamento deve seguir o padrao existente da aplicacao para falhas de leitura.

## Authorization

As melhorias devem usar a mesma autorizacao da area financeira.

Um usuario sem permissao de leitura financeira nao deve conseguir visualizar a listagem, o detalhe financeiro ou os itens do pedido por meio da tela financeira.

## Security

- O `orderId` recebido pela tela de detalhe deve ser tratado como entrada nao confiavel.
- A exibicao de itens do pedido nao deve expor dados para usuarios sem acesso financeiro.
- A feature nao deve criar endpoints publicos sem autorizacao.
- Textos de descricao dos itens devem ser renderizados de forma segura contra injecao de HTML ou script.

## Compatibility

- A rota `Finance/Details?orderId=` deve continuar funcionando.
- O link `Detalhes` da listagem deve continuar usando o identificador completo do pedido.
- A listagem financeira deve continuar funcionando com e sem filtros de periodo.
- O comportamento de `NotFound` para acerto inexistente deve ser preservado.
- Os modelos, consultas e telas existentes devem continuar compativeis com acertos financeiros ja persistidos.

## Edge Cases

- Lista de apuracao sem acertos financeiros.
- Pedido com identificador completo valido.
- Pedido com muitos itens.
- Pedido sem itens.
- Item sem identificador externo.
- Item com descricao longa.
- Valores unitarios iguais a zero.
- Acerto financeiro existente para pedido que nao pode ser carregado.
- Acesso mobile a listagem e ao detalhe.

## Dependencies

Esta feature depende de:

- pagina existente de Apuracao financeira;
- tela existente de detalhe do Acerto financeiro;
- consulta existente de acertos financeiros;
- consulta existente de pedidos e seus itens;
- regras atuais de autorizacao da area financeira.

## Constraints

- A abreviacao do pedido deve usar os 8 primeiros caracteres do identificador textual.
- A listagem deve ocultar o horario do campo `Calculado em`.
- Os itens do pedido no detalhe financeiro devem ser somente leitura.
- Nenhuma regra de calculo financeiro deve ser alterada.
- Nenhuma nova integracao externa e esperada.

## Architecture Impact

A feature afeta conceitualmente:

- formatacao dos dados exibidos na listagem financeira;
- dados carregados para a tela de detalhe financeiro;
- apresentacao visual do detalhe financeiro;
- testes de componentes ou views financeiras.

Nao ha expectativa de alteracao na persistencia dos acertos financeiros.

## Technical Decisions

### TD-001 - Abreviacao visual do pedido

O pedido sera abreviado apenas na camada de apresentacao da listagem financeira.

Reason:

A regra solicitada e de exibicao, e o identificador completo ainda e necessario para navegacao e consulta.

### TD-002 - Itens exibidos no detalhe financeiro

Os itens do pedido serao exibidos na tela de detalhe financeiro como dados somente leitura.

Reason:

O objetivo e permitir conferencia do contexto do acerto financeiro sem transformar a tela financeira em tela de edicao do pedido.

## Alternatives Considered

### Substituir completamente o identificador do pedido no sistema

Rejeitado porque a solicitacao e apenas visual e o identificador completo continua necessario para rotas, consultas e integridade de dados.

### Exibir itens do pedido apenas como link para a tela do pedido

Nao selecionado porque a solicitacao pede adicionar os itens do pedido diretamente no Acerto financeiro.

## Acceptance Criteria

- A listagem de Apuracao financeira exibe o campo `Pedido` com apenas os 8 primeiros caracteres do identificador.
- O link de detalhes da listagem continua usando o `orderId` completo.
- A listagem de Apuracao financeira exibe `Calculado em` apenas como data, sem horario.
- A tela `Finance/Details?orderId=` apresenta os dados do acerto financeiro de forma visualmente mais organizada.
- A tela `Finance/Details?orderId=` exibe os itens do pedido associado.
- Cada item exibido informa descricao, quantidade e valor unitario.
- Pedido sem itens disponiveis exibe estado vazio claro no detalhe financeiro.
- Acerto financeiro inexistente continua retornando nao encontrado.
- As permissoes existentes da area financeira sao preservadas.
- As regras e valores de calculo financeiro nao sao alterados.
- O projeto compila e os testes relevantes passam.

## Test Considerations

A implementacao deve verificar no minimo:

### Listagem

Pedido completo e exibido de forma abreviada, mantendo o identificador completo no link de detalhes.

### Data

`Calculado em` na listagem exibe somente data, sem horario.

### Detalhe financeiro

Detalhe exibe os dados financeiros existentes e os itens do pedido relacionado.

### Estado vazio

Pedido sem itens disponiveis exibe mensagem ou estado vazio adequado.

### Autorizacao

Usuarios sem permissao financeira continuam sem acesso aos dados financeiros e aos itens exibidos por essa tela.

### Compatibilidade

Acerto inexistente continua retornando nao encontrado.

## Out of Scope

Ficam explicitamente fora do escopo:

- modificar regras de calculo de acerto financeiro;
- adicionar edicao de pedido na tela financeira;
- adicionar exportacao de dados financeiros;
- criar novos filtros ou ordenacao da listagem;
- alterar permissoes da area financeira;
- alterar a tela principal de detalhes do pedido.

## Open Questions

- A data do detalhe financeiro tambem deve ocultar o horario, ou a mudanca de data sem horario vale somente para a listagem de Apuracao financeira?
- O pedido abreviado deve ter tooltip, titulo acessivel ou algum outro mecanismo para revelar o identificador completo na listagem?
- Alem de descricao, quantidade e valor unitario, a tela de detalhe financeiro deve exibir subtotal por item?
- Se o pedido associado ao acerto financeiro nao for encontrado, a tela deve retornar erro, `NotFound`, ou exibir o acerto com estado vazio de itens?

## Assumptions

- A solicitacao de remover horario em `Calculado em` se aplica explicitamente a listagem de Apuracao financeira.
- A tela de detalhe financeiro pode continuar exibindo o identificador completo do pedido, salvo decisao posterior.
- Os itens do pedido podem ser obtidos da mesma fonte usada pela tela de detalhes do pedido.
- A melhoria de apresentacao do detalhe financeiro pode reutilizar padroes visuais ja existentes na aplicacao.

## Risks

### Ambiguidade do identificador abreviado

Dois pedidos podem compartilhar os mesmos 8 primeiros caracteres, ainda que isso seja improvavel para GUIDs.

Mitigation:

Manter o identificador completo na navegacao e considerar mecanismo visual/acessivel para revelar o identificador completo se necessario.

### Falta de pedido associado

Se dados historicos tiverem acerto financeiro sem pedido carregavel, a exibicao de itens pode falhar.

Mitigation:

Definir tratamento explicito para pedido ausente antes da implementacao ou preservar o detalhe financeiro sem exibir itens incorretos.

## Implementation Notes

A implementacao deve reaproveitar, quando possivel, os modelos e consultas ja usados para exibir itens na tela de detalhes do pedido, mantendo a tela financeira como somente leitura.
