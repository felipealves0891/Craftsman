# Implementation Plan: Relatorio Mensal de Gastos

## Summary

Implementar um painel de KPIs na pagina existente de Apuracao financeira, reutilizando o fluxo atual de consulta por periodo e o mecanismo existente de cache da aplicacao.

A implementacao deve manter o comportamento atual da listagem financeira, acrescentar um resumo consolidado server-side, cachear esse resumo por 1 hora e garantir invalidacao quando novos acertos financeiros forem incluidos.

## Specification Reference

Este plano implementa:

`specs/relatorio-financeiro/spec.md`

## Current Architecture

A area financeira segue o fluxo MVC com componente Razor compartilhado:

```text
GET /Finance?from=...&to=...
        ->
FinanceController.Index(DateOnly? from, DateOnly? to)
        ->
FinanceAppService.ListAsync(from, to)
        ->
IFinancialSettlementRepository.ListAsync(from, to)
        ->
FinancialSettlementRepository / AppDbContext
        ->
src/App/Views/Finance/Index.cshtml
        ->
src/UI/Components/Finance/FinanceIndexPage.razor
```

O reposititorio `FinancialSettlementRepository` ja aceita filtros opcionais `from` e `to`, monta chave de cache por periodo para a listagem e remove caches com prefixo `finance:` ao adicionar ou excluir acertos financeiros.

O cache da aplicacao e representado por `IApplicationCache`, implementado por `MemoryApplicationCache`, que suporta:

- `GetOrCreateAsync` com expiracao absoluta opcional;
- `Remove`;
- `RemoveByPrefix`.

O `DeliveryConfirmedSettlementHandler` cria acertos financeiros em resposta ao `DeliveryConfirmedEvent` usando o mesmo `IFinancialSettlementRepository.AddAsync`, portanto a invalidacao por prefixo no reposititorio cobre tambem esse fluxo.

## Change Surface

## Direct Impact

- `src/App/Services/FinanceAppService.cs`
- `src/App/Controllers/FinanceController.cs`
- `src/App/Views/Finance/Index.cshtml`
- `src/UI/Components/Finance/FinanceIndexPage.razor`
- `src/Infra/Repositories/FinancialSettlementRepository.cs`
- modelos financeiros em `src/App/Models`
- testes financeiros em `tests/Craftsman.Tests/Finance`
- testes de view em `tests/Craftsman.Tests/Application/SharedInterfaceViewTests.cs`

## Indirect Impact

- `DeliveryConfirmedSettlementHandler`, por depender da invalidacao acionada por `FinancialSettlementRepository.AddAsync`.
- `MemoryApplicationCache`, por ser o mecanismo usado para expirar e remover entradas financeiras.
- Estilos globais em `src/App/wwwroot/css/site.css`, caso o componente precise de classes novas para os cards e comportamento mobile.

## Implementation Strategy

Estender o fluxo atual de indice financeiro em vez de criar uma rota ou endpoint novo.

O `FinanceAppService` deve passar a expor um metodo que retorna a pagina financeira completa para um periodo: lista de acertos e resumo consolidado. O resumo deve ser calculado no servidor a partir dos mesmos acertos financeiros usados pela listagem, preservando o criterio atual de periodo.

Para evitar duplicar regras de filtro, o calculo do resumo deve reutilizar `IFinancialSettlementRepository.ListAsync(from, to)` como fonte. Como a especificacao exige cache proprio de KPIs por 1 hora, o resumo deve ter uma chave de cache separada da listagem, com prefixo `finance:` e expiracao explicita de 1 hora.

A invalidacao deve permanecer centralizada nos pontos de escrita do reposititorio financeiro. Como `AddAsync` e `DeleteByOrderAsync` ja removem `finance:`, novas chaves de resumo que comecem com esse prefixo serao invalidadas junto da listagem.

Na UI, `Index.cshtml` deve mapear o novo modelo de pagina para o componente `FinanceIndexPage`. O componente deve receber linhas e KPIs ja formatados para exibicao, mantendo o padrao atual de componentes Razor estaticos.

## Data Flow

```text
GET /Finance?from=2026-10-01&to=2026-10-31

        ->

FinanceController.Index(from, to)

        ->

FinanceAppService.GetIndexAsync(from, to)

        ->

IFinancialSettlementRepository.ListAsync(from, to)
        |-- usado para linhas da listagem
        `-- usado para calcular KPIs quando nao houver cache valido

        ->

FinancialSettlementIndexViewModel

        ->

Views/Finance/Index.cshtml mapeia valores para FinanceIndexPage

        ->

FinanceIndexPage renderiza filtros, cards e tabela
```

## Existing Components to Reuse

### `IFinancialSettlementRepository.ListAsync`

Deve continuar sendo a fonte dos acertos financeiros do periodo. Isso garante que KPIs e tabela usem o mesmo criterio de filtro.

### `IApplicationCache`

Deve ser usado para cachear os KPIs por 1 hora, seguindo o padrao de `StockAlertAppService`.

### Prefixo `finance:`

As chaves de resumo devem usar o prefixo `finance:` para aproveitar a invalidacao ja usada por `FinancialSettlementRepository.AddAsync` e `DeleteByOrderAsync`.

### Formato de componente Razor estatico

`FinanceIndexPage.razor` deve continuar recebendo parametros do MVC view `Index.cshtml`, mantendo o padrao verificado por `SharedInterfaceViewTests`.

### Font Awesome existente

Os cards devem usar icones disponiveis no bundle existente de Font Awesome, sem nova dependencia visual.

## New Components

### `FinancialSettlementIndexViewModel`

Modelo de pagina para carregar a listagem e o resumo financeiro juntos.

Responsabilidade:

- transportar `Settlements`;
- transportar `Summary`.

### `FinancialSettlementSummaryViewModel`

Modelo de aplicacao para os valores numericos consolidados.

Responsabilidade:

- expor Total de Receita;
- expor Total de Custos;
- expor Total de Margem;
- expor Margem por Pedido;
- expor quantidade de pedidos, caso ajude testes ou exibicao.

### `FinanceIndexPage.KpiCard`

Record interno do componente para renderizar os cards com titulo, icone e valor formatado.

Responsabilidade:

- manter o componente independente dos modelos de aplicacao;
- permitir que a view MVC formate valores e selecione icones.

Nenhuma nova abstracao de dominio e necessaria.

## Files to Modify

### `src/App/Services/FinanceAppService.cs`

Adicionar dependencia opcional de `IApplicationCache`, uma expiracao estatica de 1 hora para KPIs e um metodo de indice financeiro que retorne listagem e resumo.

O metodo atual `ListAsync` pode ser preservado para compatibilidade interna ou passar a delegar para o novo fluxo, evitando quebrar chamadas existentes.

### `src/App/Controllers/FinanceController.cs`

Alterar `Index` para solicitar o modelo de pagina ao `FinanceAppService` e passar esse modelo para a view.

`ViewBag.From` e `ViewBag.To` podem continuar sendo usados, ou o modelo de pagina pode carregar os valores se a implementacao preferir; a rota e os parametros devem permanecer iguais.

### `src/App/Views/Finance/Index.cshtml`

Alterar o tipo do model para o novo modelo de pagina, mapear `Model.Settlements` para `SettlementRow` como hoje e mapear `Model.Summary` para uma colecao de cards.

Preservar:

- `typeof(FinanceIndexPage)`;
- `render-mode="Static"`;
- parametros de mensagens;
- parametros de periodo.

### `src/UI/Components/Finance/FinanceIndexPage.razor`

Adicionar parametro para os cards de KPI e renderizar o painel entre o filtro e a tabela, ou logo antes do filtro se a experiencia visual ficar mais clara.

Cada card deve ter:

- titulo;
- icone;
- valor;
- tooltip/titulo acessivel para mobile.

### `src/App/wwwroot/css/site.css`

Adicionar estilos para o painel de KPIs, incluindo comportamento responsivo mobile que esconda o titulo visual e mantenha tooltip acionavel.

### `src/Infra/Repositories/FinancialSettlementRepository.cs`

Ajustar a chamada existente de cache da listagem para explicitar expiracao de 1 hora, se a implementacao quiser alinhar a listagem financeira ao mesmo ciclo dos KPIs. Essa alteracao e coerente com FR-010, que trata caches financeiros relacionados aos KPIs e a listagem.

Se a expiracao de 1 hora for aplicada apenas aos KPIs, documentar a decisao na implementacao porque a listagem hoje usa o default de 10 minutos.

## Files to Create

### `src/App/Models/FinancialSettlementIndexViewModel.cs`

Modelo de pagina financeira contendo listagem e resumo.

### `src/App/Models/FinancialSettlementSummaryViewModel.cs`

Modelo do resumo financeiro consolidado.

## Files to Remove

None.

## Persistence Changes

Nenhuma mudanca de schema e necessaria.

O calculo do resumo deve usar dados ja persistidos em `FinancialSettlements`. Nao ha necessidade de migracao, indices novos ou tabelas de agregados.

## API Contract Changes

Nenhuma rota nova e necessaria.

A rota existente permanece:

```text
GET /Finance?from=<date>&to=<date>
```

Os parametros `from` e `to` continuam opcionais e com o mesmo significado atual.

Nao ha mudanca de contrato para eventos de dominio. `DeliveryConfirmedEvent` continua igual.

## UI Changes

A pagina de Apuracao financeira passara a exibir um painel com quatro cards:

- Total de Receita;
- Total de Custos;
- Total de Margem;
- Margem por Pedido.

Em desktop, cada card deve exibir titulo e icone.

Em mobile, cada card deve ocultar o titulo visualmente e manter o icone e o valor. O titulo deve ficar disponivel via tooltip ao clique/toque; preferencialmente tambem por `title`/`aria-label` para foco de teclado, desde que isso nao aumente o escopo visual.

A tabela e o filtro de periodo existentes devem permanecer disponiveis.

## Configuration Changes

Nenhuma nova configuracao e necessaria.

A duracao de 1 hora e requisito da especificacao e deve ser codificada como constante local do fluxo financeiro, semelhante ao padrao de `StockAlertAppService`.

## Dependencies

Nenhum novo pacote NuGet e necessario.

O projeto ja possui:

- `IMemoryCache`;
- `IApplicationCache`;
- Font Awesome para icones.

## Security Considerations

- A autorizacao existente `[Authorize(Policy = ApplicationPolicies.Read)]` no controller deve continuar protegendo a pagina e os KPIs.
- Nenhum endpoint publico adicional deve ser criado.
- O resumo deve ser calculado no servidor usando dados ja autorizados pelo fluxo financeiro.
- As chaves de cache devem conter apenas periodo normalizado, sem dados financeiros sensiveis.
- Os valores renderizados no componente devem continuar passando pelo encoding normal do Razor.

## Performance Considerations

- O resumo deve ser calculado apenas quando a chave de KPI do periodo nao estiver em cache.
- A chave do resumo deve incluir `from` e `to`, normalizados da mesma forma que a listagem financeira.
- A expiracao dos KPIs deve ser `TimeSpan.FromHours(1)`.
- A invalidacao por `RemoveByPrefix("finance:")` deve continuar removendo todos os caches financeiros afetados por inclusao ou exclusao de acerto.
- Reutilizar a listagem pode significar que a primeira requisicao sem cache materialize os acertos do periodo uma vez e derive os totais em memoria. Isso e aceitavel para a feature atual e evita duplicar criterios de filtro.

## Observability

Nenhuma nova telemetria e necessaria.

Falhas devem seguir o comportamento existente de excecoes da area financeira. O plano nao adiciona logs por requisicao para evitar ruido.

## Testing Strategy

### Unit and Service Tests

Adicionar ou ampliar testes em `FinanceAppServiceTests` para cobrir:

- resumo com multiplos acertos;
- filtro por periodo;
- periodo sem resultados;
- margem por pedido com zero pedidos;
- cache de resumo reutilizado para chamada equivalente;
- expiracao de cache de 1 hora, se for viavel testar por fake cache;
- invalidacao indireta por `FinancialSettlementRepository.AddAsync` removendo prefixo `finance:`.

Se `MemoryApplicationCache` for dificil de controlar temporalmente, criar um fake `IApplicationCache` no teste de app service para capturar chave, expiracao e reutilizacao.

### Repository / Cache Tests

Adicionar teste focado em `FinancialSettlementRepository` com fake `IApplicationCache` para verificar que:

- `ListAsync` usa chave `finance:settlements:<from>:<to>`;
- `AddAsync` chama remocao por prefixo `finance:`.

Esse teste tambem ajuda a cobrir a invalidacao usada pelo `DeliveryConfirmedSettlementHandler`, porque o handler grava pelo reposititorio.

### Handler Tests

Estender `DeliveryConfirmedSettlementHandlerTests` somente se necessario para garantir que o fluxo de criacao continua usando `AddAsync`. Como o fake atual nao representa cache, a cobertura principal de invalidacao deve ficar no reposititorio.

### View Tests

Atualizar `SharedInterfaceViewTests` para verificar que `Finance/Index.cshtml` passa o novo parametro de KPIs para `FinanceIndexPage`.

Adicionar verificacao textual no componente `FinanceIndexPage.razor` para:

- existencia dos cards;
- uso de icones;
- atributos de tooltip ou `aria-label`.

### Build and Regression

Executar:

```text
dotnet test
```

## Implementation Tasks

### Task 1 - Criar modelos de resumo financeiro

Affected:

- `src/App/Models`

Changes:

- criar `FinancialSettlementSummaryViewModel`;
- criar `FinancialSettlementIndexViewModel`;
- manter os modelos como records simples, seguindo o padrao existente.

Validation:

- projeto compila;
- modelos carregam os campos necessarios para listagem e KPIs.

Dependencies:

None.

---

### Task 2 - Adicionar resumo server-side no `FinanceAppService`

Affected:

- `src/App/Services/FinanceAppService.cs`

Changes:

- injetar `IApplicationCache? cache = null`;
- adicionar constante de expiracao de 1 hora;
- adicionar metodo de indice financeiro que retorna `FinancialSettlementIndexViewModel`;
- calcular Total de Receita, Total de Custos, Total de Margem e Margem por Pedido a partir dos acertos do periodo;
- usar cache para o resumo por periodo com chave iniciada por `finance:`;
- preservar `ListAsync` existente ou faze-lo delegar sem quebrar testes atuais.

Validation:

- resumo correto para multiplos acertos;
- resumo zerado sem acertos;
- margem por pedido nao divide por zero;
- chamada equivalente reutiliza cache.

Dependencies:

Task 1.

---

### Task 3 - Alinhar cache financeiro e invalidacao

Affected:

- `src/Infra/Repositories/FinancialSettlementRepository.cs`
- testes de reposititorio/cache

Changes:

- garantir que chaves de resumo e listagem usam prefixo `finance:`;
- manter `RemoveByPrefix("finance:")` em `AddAsync` e `DeleteByOrderAsync`;
- considerar explicitar expiracao de 1 hora para a listagem financeira, alinhando caches financeiros relacionados.

Validation:

- inclusao de acerto remove caches financeiros;
- `DeliveryConfirmedSettlementHandler` continua invalidando por usar `AddAsync`;
- chamadas sem cache continuam consultando os dados persistidos.

Dependencies:

Task 2 para conhecer a chave de resumo.

---

### Task 4 - Atualizar controller e MVC view

Affected:

- `src/App/Controllers/FinanceController.cs`
- `src/App/Views/Finance/Index.cshtml`

Changes:

- alterar `Index` para usar o novo modelo de pagina;
- atualizar o tipo do model em `Index.cshtml`;
- mapear `Model.Settlements` para linhas da tabela como hoje;
- mapear `Model.Summary` para cards do componente;
- manter mensagens de `TempData` e filtros de periodo.

Validation:

- `/Finance` sem filtros continua funcionando;
- `/Finance?from=...&to=...` continua funcionando;
- view ainda renderiza `FinanceIndexPage` em modo static.

Dependencies:

Tasks 1 and 2.

---

### Task 5 - Renderizar painel de KPIs no componente

Affected:

- `src/UI/Components/Finance/FinanceIndexPage.razor`
- `src/App/wwwroot/css/site.css`

Changes:

- adicionar parametro de cards;
- renderizar painel com quatro cards antes da tabela;
- exibir titulo e icone em desktop;
- ocultar titulo visualmente em mobile e disponibilizar tooltip ao clique/toque;
- manter layout da tabela e filtros.

Validation:

- cards aparecem com os quatro indicadores;
- mobile nao exibe titulo visual no card;
- tooltip/titulo acessivel esta disponivel;
- nenhum texto fica sobreposto em layouts pequenos.

Dependencies:

Task 4.

---

### Task 6 - Adicionar testes automatizados

Affected:

- `tests/Craftsman.Tests/Finance/FinanceAppServiceTests.cs`
- possivel novo arquivo de testes para `FinancialSettlementRepository`
- `tests/Craftsman.Tests/Application/SharedInterfaceViewTests.cs`

Changes:

- cobrir calculos dos KPIs;
- cobrir periodo sem resultados;
- cobrir cache e expiracao passada ao cache;
- cobrir invalidacao por prefixo em inclusao;
- cobrir mapeamento da view para o componente;
- cobrir presenca de cards/tooltip no componente.

Validation:

- `dotnet test` passa.

Dependencies:

Tasks 1-5.

---

### Task 7 - Validacao final

Affected:

- solucao inteira

Changes:

- executar build/testes relevantes;
- revisar diffs contra a especificacao;
- documentar qualquer desvio tecnico no fechamento da implementacao.

Validation:

- `dotnet test`;
- revisao manual dos criterios de aceite.

Dependencies:

Tasks 1-6.

## Requirement Traceability

| Requirement | Implementation |
|---|---|
| FR-001 | Tasks 4 and 5 |
| FR-002 | Task 2 |
| FR-003 | Task 2 |
| FR-004 | Task 2 |
| FR-005 | Task 2 |
| FR-006 | Tasks 2 and 4 |
| FR-007 | Task 2 |
| FR-008 | Task 2 |
| FR-009 | Task 2 |
| FR-010 | Task 3 |
| FR-011 | Task 3 |
| FR-012 | Task 5 |
| FR-013 | Task 5 |
| FR-014 | Task 2 |
| BR-001 | Tasks 2 and 4 |
| BR-002 | Task 2 |
| BR-003 | Task 2 |
| BR-004 | Tasks 2 and 3 |
| Acceptance Criteria | Tasks 2-7 |

## Backward Compatibility

- `GET /Finance` continua existindo.
- `from` e `to` continuam opcionais.
- A listagem financeira permanece disponivel.
- As rotas `Details`, `Pending` e `Generate` nao precisam mudar.
- Nenhum dado persistido muda.
- Nenhum evento de dominio muda.

## Migration Strategy

Nenhuma migracao de banco e necessaria.

O rollout e apenas de aplicacao. Se houver rollback, caches em memoria podem ser perdidos sem impacto funcional.

## Risks

### Resumo e listagem divergirem

Se o resumo usar uma consulta diferente da listagem, os KPIs podem nao bater com as linhas exibidas.

Mitigation:

Calcular o resumo a partir de `IFinancialSettlementRepository.ListAsync(from, to)`.

### Cache com expiracao diferente da especificacao

O default de `MemoryApplicationCache` e 10 minutos quando nenhuma expiracao e informada.

Mitigation:

Passar explicitamente `TimeSpan.FromHours(1)` para o cache dos KPIs.

### Invalidacao incompleta

Se a chave dos KPIs nao usar prefixo `finance:`, `RemoveByPrefix("finance:")` nao removera o resumo.

Mitigation:

Padronizar chaves de resumo com prefixo `finance:`.

### Comportamento mobile fragil

Tooltip em componente estatico pode depender de atributos HTML/CSS em vez de interatividade JavaScript.

Mitigation:

Usar atributos nativos como `title`, `aria-label` e foco/click no card quando possivel, evitando dependencias novas.

## Assumptions

- O periodo dos KPIs usa a mesma data e regra ja aplicada por `FinancialSettlementRepository.ListAsync`, ou seja, `CalculatedAt`.
- Cada acerto financeiro representa um pedido para o calculo de Margem por Pedido.
- Valores monetarios podem seguir a formatacao atual da view financeira com `ToString("C")`.
- Invalidar todo o prefixo `finance:` e aceitavel e preferivel a invalidacao granular por periodo.

## Open Technical Questions

- A listagem financeira deve ter sua expiracao explicitamente alterada para 1 hora junto dos KPIs, ou apenas o novo cache de KPIs deve seguir 1 hora? O plano recomenda alinhar os caches financeiros relacionados, mas a especificacao exige explicitamente 1 hora para KPIs.
- O tooltip mobile deve depender apenas de atributos nativos (`title`/`aria-label`) ou deve usar o comportamento Bootstrap existente, caso ja esteja inicializado globalmente?
