# Feature: Relatorio Mensal de Gastos

## Summary

Adicionar um painel de KPIs na pagina de Apuracao financeira para resumir os totais do periodo selecionado.

O painel deve ajudar usuarios autorizados a avaliar rapidamente receita, custos, margem total e margem media por pedido sem precisar somar manualmente os itens da listagem.

## Context

A pagina de Apuracao financeira ja lista os acertos financeiros dos pedidos e permite filtrar a consulta por periodo.

Os acertos financeiros possuem valores de receita, custo de producao, custo de envio, custo total, margem, status e data de calculo. A listagem de acertos financeiros ja pode ser consultada com filtros opcionais de inicio e fim.

## Problem

Usuarios que analisam o desempenho financeiro de um periodo precisam interpretar cada linha da listagem para entender os totais consolidados. Isso torna a apuracao mais lenta e aumenta o risco de conclusoes incorretas sobre receita, custos e margem.

## Goals

- Exibir KPIs consolidados para o periodo selecionado na pagina de Apuracao financeira.
- Calcular os KPIs no servidor.
- Reutilizar cache para evitar recalculo dos totais a cada requisicao.
- Invalidar os caches financeiros quando um novo acerto financeiro for incluido no calculo.
- Manter a experiencia responsiva em desktop e mobile.

## Non-Goals

Esta feature nao inclui:

- graficos financeiros;
- exportacao de relatorios;
- comparacao entre periodos;
- filtros adicionais alem do periodo ja usado pela pagina;
- alteracao das regras de calculo de acerto financeiro por pedido;
- alteracao de permissoes da area financeira.

## Actors

### Usuario autorizado

Consulta a pagina de Apuracao financeira e visualiza a lista de acertos e os KPIs do periodo selecionado.

### Sistema

Calcula, armazena em cache, recupera e invalida os totais financeiros consolidados.

## Preconditions

- O usuario esta autorizado a acessar a pagina de Apuracao financeira.
- Existem zero ou mais acertos financeiros calculados.
- O periodo selecionado e representado pelos filtros de inicio e fim aceitos pela pagina financeira.

## Functional Requirements

### FR-001 - Exibir painel de KPIs financeiros

A pagina de Apuracao financeira deve exibir um painel com KPIs consolidados para o periodo selecionado.

### FR-002 - Total de Receita

O painel deve exibir o total de receita dos acertos financeiros incluidos no periodo selecionado.

### FR-003 - Total de Custos

O painel deve exibir o total de custos dos acertos financeiros incluidos no periodo selecionado.

### FR-004 - Total de Margem

O painel deve exibir o total de margem dos acertos financeiros incluidos no periodo selecionado.

### FR-005 - Margem por Pedido

O painel deve exibir a margem media por pedido, calculada como total de margem dividido pela quantidade de pedidos incluidos no periodo selecionado.

### FR-006 - Escopo do periodo selecionado

Os KPIs devem respeitar os mesmos filtros de periodo usados pela listagem de Apuracao financeira.

### FR-007 - Calculo no servidor

Os totais dos KPIs devem ser calculados no servidor.

### FR-008 - Cache dos KPIs

O resultado consolidado dos KPIs deve ser armazenado em cache para que o calculo nao seja executado novamente a cada requisicao equivalente.

### FR-009 - Duracao do cache

O cache dos KPIs financeiros deve expirar 1 hora apos sua criacao.

### FR-010 - Invalidacao ao incluir acerto financeiro

Quando um acerto financeiro for incluido no calculo, todos os caches financeiros relacionados aos KPIs e a listagem financeira devem ser excluidos.

### FR-011 - Integracao com confirmacao de entrega

Quando o `DeliveryConfirmedSettlementHandler` criar um acerto financeiro em resposta ao `DeliveryConfirmedEvent`, a invalidacao de cache financeiro deve ocorrer para que os KPIs reflitam o novo acerto em requisicoes posteriores.

### FR-012 - Cards com titulo e icone

Cada KPI deve ser apresentado em um card com titulo e icone associados ao indicador.

### FR-013 - Comportamento mobile dos cards

Em telas mobile, cada card deve exibir apenas o icone como identificador visual do KPI e deve permitir que o usuario veja o titulo do card por tooltip ao clicar ou tocar no card.

### FR-014 - Estado sem acertos financeiros

Quando nao houver acertos financeiros no periodo selecionado, o painel deve exibir totais zerados e a margem por pedido deve ser zero, sem erro de divisao por zero.

## Business Rules

### BR-001 - Fonte dos KPIs

Os KPIs devem ser derivados dos mesmos acertos financeiros elegiveis para a listagem do periodo selecionado.

### BR-002 - Formula de custo total

O Total de Custos deve representar a soma dos custos totais dos acertos financeiros incluidos no periodo.

### BR-003 - Formula de margem por pedido

A Margem por Pedido deve ser calculada como:

```text
Total de Margem / quantidade de pedidos incluidos no periodo
```

Se a quantidade de pedidos for zero, a Margem por Pedido deve ser zero.

### BR-004 - Cache nao altera resultado esperado

O uso de cache nao pode alterar os valores esperados dos KPIs. Dados em cache devem representar o mesmo resultado que seria obtido pelo calculo servidor-side para o mesmo periodo e mesmo conjunto de acertos.

## User Flow

1. Usuario acessa a pagina de Apuracao financeira.
2. Sistema identifica o periodo selecionado.
3. Sistema obtem os KPIs financeiros do cache quando existir entrada valida.
4. Se nao houver cache valido, sistema calcula os KPIs no servidor e armazena o resultado por 1 hora.
5. Sistema exibe os cards de KPIs e a listagem financeira do periodo.
6. Usuario altera o periodo e submete o filtro.
7. Sistema exibe KPIs e listagem correspondentes ao novo periodo.

## System Flow

1. Uma entrega e confirmada.
2. O evento `DeliveryConfirmedEvent` e tratado pelo `DeliveryConfirmedSettlementHandler`.
3. O sistema calcula e inclui o acerto financeiro do pedido quando ele ainda nao existe.
4. O sistema remove os caches financeiros relacionados aos KPIs e a listagem.
5. Requisicoes posteriores a Apuracao financeira recalculam ou recuperam dados consistentes com o novo acerto.

## Scenarios

### Scenario: Painel exibe totais do periodo

Given existem acertos financeiros dentro e fora do periodo selecionado
When o usuario acessa a Apuracao financeira com esse periodo
Then o painel deve somar apenas os acertos financeiros incluidos no periodo
And a listagem deve continuar exibindo os acertos financeiros do mesmo periodo

### Scenario: Periodo sem resultados

Given nenhum acerto financeiro existe no periodo selecionado
When o usuario acessa a Apuracao financeira com esse periodo
Then Total de Receita deve ser zero
And Total de Custos deve ser zero
And Total de Margem deve ser zero
And Margem por Pedido deve ser zero

### Scenario: Cache valido evita novo calculo

Given os KPIs de um periodo ja foram calculados e armazenados em cache
And o cache ainda nao expirou
When uma requisicao equivalente para o mesmo periodo for realizada
Then o sistema deve reutilizar o resultado em cache

### Scenario: Novo acerto invalida KPIs

Given existem KPIs financeiros armazenados em cache
When um novo acerto financeiro for incluido no calculo
Then os caches financeiros devem ser removidos
And a proxima requisicao deve refletir o novo conjunto de acertos financeiros

### Scenario: Mobile exibe tooltip de titulo

Given o usuario acessa a Apuracao financeira em uma tela mobile
When o usuario toca em um card de KPI
Then o sistema deve exibir um tooltip com o titulo do card

## Invariants

- Os KPIs devem respeitar as mesmas regras de autorizacao da pagina de Apuracao financeira.
- O calculo dos KPIs nao deve modificar acertos financeiros.
- O cache financeiro nao deve expor dados para usuarios sem acesso a area financeira.
- A listagem financeira existente deve continuar funcionando quando nenhum KPI estiver em cache.
- A invalidacao de cache deve ocorrer sempre que a inclusao de acerto financeiro puder alterar os totais.

## Data

### Input

Periodo financeiro:

- `from`: data inicial opcional.
- `to`: data final opcional.

### Output

Painel de KPIs:

- Total de Receita.
- Total de Custos.
- Total de Margem.
- Margem por Pedido.

Cada KPI deve conter:

- titulo;
- icone;
- valor formatado para exibicao.

## Validation

- A ausencia de `from` e valida.
- A ausencia de `to` e valida.
- Quando ambos os filtros estiverem ausentes, os KPIs devem considerar o mesmo escopo da listagem sem periodo.
- Datas invalidas devem seguir o comportamento existente da pagina financeira para filtros de periodo.

## Error Handling

Se os KPIs nao puderem ser carregados:

- a pagina nao deve exibir valores parciais ou enganosos como se fossem corretos;
- o erro deve seguir o comportamento existente da area financeira;
- detalhes tecnicos da falha nao devem ser expostos ao usuario.

## Authorization

O painel de KPIs deve exigir a mesma autorizacao de leitura da pagina de Apuracao financeira.

A invalidacao de cache por eventos internos nao deve criar nova superficie de acesso para usuarios nao autorizados.

## Security

- Os filtros de periodo devem continuar sendo tratados como entrada nao confiavel.
- A feature nao deve introduzir endpoints publicos para consulta de totais financeiros sem autorizacao.
- O cache nao deve armazenar informacoes sensiveis em chaves ou mensagens visiveis ao usuario.

## Performance Requirements

- Requisicoes equivalentes para o mesmo periodo devem poder reutilizar o resultado consolidado em cache durante 1 hora.
- A pagina nao deve recalcular os KPIs em toda requisicao enquanto houver cache valido.
- A invalidacao deve remover caches financeiros afetados sem depender da expiracao natural de 1 hora.

## Compatibility

- A rota existente da pagina de Apuracao financeira deve continuar funcionando.
- Os filtros de periodo existentes devem continuar funcionando.
- A listagem de acertos financeiros existente deve permanecer disponivel.
- A criacao de acertos financeiros por confirmacao de entrega deve continuar preservando a regra de nao duplicar acerto por pedido.

## Edge Cases

- Nenhum acerto financeiro existe no sistema.
- Nenhum acerto financeiro existe no periodo selecionado.
- Existe apenas um pedido no periodo selecionado.
- Existem muitos pedidos no periodo selecionado.
- `from` esta ausente.
- `to` esta ausente.
- `from` e `to` estao ausentes.
- Um novo acerto financeiro e criado enquanto havia cache valido.
- O handler recebe evento para pedido que ja possui acerto financeiro.
- O cache expira naturalmente apos 1 hora.

## Dependencies

Esta feature depende de:

- pagina existente de Apuracao financeira;
- acertos financeiros calculados;
- filtros de periodo financeiros existentes;
- mecanismo de cache da aplicacao;
- evento `DeliveryConfirmedEvent`;
- handler `DeliveryConfirmedSettlementHandler`.

## Constraints

- O calculo dos KPIs deve ocorrer no servidor.
- O cache dos KPIs deve durar 1 hora.
- A invalidacao deve ocorrer quando um acerto financeiro for incluido.
- O painel deve ser compativel com desktop e mobile.

## Architecture Impact

A feature afeta conceitualmente:

- consulta de dados financeiros consolidados;
- apresentacao da pagina de Apuracao financeira;
- estrategia de cache financeiro;
- invalidacao de cache durante criacao de acertos financeiros.

Nenhuma nova integracao externa e esperada.

## Technical Decisions

### TD-001 - KPIs server-side

Os KPIs serao calculados no servidor.

Reason:

O prompt exige calculo server-side e os totais devem refletir o conjunto completo de acertos financeiros do periodo, nao apenas dados eventualmente renderizados no cliente.

### TD-002 - Cache com expiracao de 1 hora

Os KPIs financeiros consolidados serao cacheados com expiracao de 1 hora.

Reason:

O prompt define explicitamente a duracao do cache e busca evitar recalculo em toda requisicao.

### TD-003 - Invalidacao por inclusao de acerto financeiro

A inclusao de novo acerto financeiro deve invalidar caches financeiros.

Reason:

Qualquer novo acerto pode alterar totais de receita, custos, margem e margem por pedido em um ou mais periodos.

## Alternatives Considered

### Calculo no cliente

Rejeitado porque o prompt exige calculo no servidor e a pagina nao deve depender de dados client-side para consolidar os totais.

### Cache apenas por expiracao natural

Rejeitado porque um novo acerto financeiro deve aparecer nos totais sem aguardar ate 1 hora.

## Acceptance Criteria

- A pagina de Apuracao financeira exibe cards para Total de Receita, Total de Custos, Total de Margem e Margem por Pedido.
- Os KPIs refletem o mesmo periodo aplicado na listagem financeira.
- Total de Receita soma as receitas dos acertos incluidos no periodo.
- Total de Custos soma os custos totais dos acertos incluidos no periodo.
- Total de Margem soma as margens dos acertos incluidos no periodo.
- Margem por Pedido divide a margem total pela quantidade de pedidos incluidos no periodo.
- Periodos sem acertos exibem valores zerados e nao causam divisao por zero.
- O calculo dos KPIs ocorre no servidor.
- Requisicoes equivalentes reutilizam cache valido por ate 1 hora.
- A inclusao de novo acerto financeiro invalida caches financeiros.
- Acertos financeiros criados por `DeliveryConfirmedSettlementHandler` invalidam caches financeiros.
- Em desktop, os cards exibem titulo e icone.
- Em mobile, os cards exibem apenas o icone e mostram o titulo por tooltip ao clique/toque.
- O projeto compila e os testes relevantes passam.

## Test Considerations

A implementacao deve verificar no minimo:

### Happy Path

KPIs calculados corretamente para periodo com multiplos acertos financeiros.

### Period Filtering

KPIs incluem apenas acertos financeiros dentro do periodo selecionado.

### Empty Results

Periodo sem acertos retorna totais zerados e margem por pedido igual a zero.

### Cache

KPIs equivalentes sao retornados do cache enquanto a entrada estiver valida.

### Cache Expiration

Cache de KPIs expira apos 1 hora.

### Cache Invalidation

Inclusao de acerto financeiro remove caches financeiros e permite recalculo posterior.

### Event Handler

Acerto criado via `DeliveryConfirmedSettlementHandler` invalida caches financeiros.

### UI Responsiveness

Cards exibem titulo e icone em desktop, e tooltip de titulo no mobile.

## Out of Scope

Ficam explicitamente fora do escopo:

- dashboard financeiro separado;
- novos tipos de relatorio;
- persistencia historica dos totais consolidados;
- mudanca no calculo individual de receita, custos ou margem de um pedido;
- mudanca nas permissoes da area financeira.

## Open Questions

- O periodo deve ser entendido pela data de calculo do acerto financeiro, mantendo o comportamento atual da listagem, ou por outra data de negocio do pedido?
- A formatacao dos valores deve seguir exatamente a formatacao monetaria ja usada na listagem financeira atual?
- O tooltip mobile deve aparecer tambem por foco de teclado para acessibilidade?

## Assumptions

- O periodo dos KPIs acompanha o mesmo criterio de periodo da listagem financeira existente.
- Cada acerto financeiro corresponde a um pedido para o calculo de Margem por Pedido.
- A invalidacao de todos os caches financeiros e aceitavel para garantir consistencia simples.

## Risks

### Dados temporariamente defasados

Sem invalidacao ao incluir acerto financeiro, o painel poderia exibir valores desatualizados ate a expiracao do cache.

Mitigation:

Invalidar caches financeiros sempre que um acerto financeiro for incluido.

### Ambiguidade da data do periodo

Se o periodo esperado pelo negocio for diferente da data usada pela listagem atual, os KPIs podem consolidar dados em um recorte incorreto.

Mitigation:

Confirmar a semantica do periodo antes da implementacao caso a data de calculo nao seja suficiente para o negocio.

## Implementation Notes

O prompt cita o `DeliveryConfirmedSettlementHandler` como ponto possivel para invalidar cache quando o acerto financeiro for criado por evento. A implementacao tambem deve considerar qualquer outro fluxo existente que inclua acertos financeiros, para preservar a regra de invalidacao.
