# Especificacao PC-009 - Ajustes no cadastro de produto

## Summary

Remover o uso de codigo de barras do cadastro de produtos e substituir o tempo de producao em dias por tempo de producao em horas. O cadastro tambem deve passar a armazenar o valor cobrado por hora do produto.

Esses dados pertencem ao Product Catalog Context e devem continuar sendo manipulados pelo dominio, aplicacao e repositorio do catalogo, sem acesso direto da interface ao `DbContext`.

## Objetivo

- Remover `Product.Barcode` do modelo interno, persistencia, telas, scripts e testes.
- Trocar `ProductionDurationDays` por uma duracao em horas.
- Adicionar valor cobrado por hora ao produto.
- Manter a invalidacao de cache do catalogo quando produto for criado ou alterado.
- Preparar o catalogo para calculos futuros de producao e financeiro baseados em horas, sem alterar esses fluxos nesta tarefa.

## Fora de escopo

- Alterar o calculo financeiro de margem ou custo real.
- Replanejar a agenda de producao por hora.
- Alterar ficha tecnica, mapeamentos externos ou estoque.
- Criar regra por origem de pedido.
- Manter compatibilidade funcional com leitura por camera ou codigo de barras.

## Regras de negocio

- Produto nao deve possuir codigo de barras.
- Tempo de producao deve ser informado em horas.
- Tempo de producao em horas e obrigatorio e deve ser maior que zero.
- Valor cobrado por hora e obrigatorio e nao pode ser negativo.
- Produto ativo continua exigindo os mesmos dados minimos ja existentes para ser usado no planejamento de producao.
- Alteracoes em produto nao devem alterar pedidos ja registrados.
- O dominio nao pode depender de entidades de persistencia.

## Modelo de dominio

Atualizar `Product` em `src/Domains/ProductCatalog/Entities/Product.cs`:

- Remover propriedade `Barcode`.
- Remover metodo `SetBarcode`.
- Substituir `ProductionDurationDays` por `ProductionDurationHours`.
- Adicionar propriedade `HourlyRate`.
- Substituir `SetProductionDuration(int days)` por um metodo que valide horas.
- Adicionar metodo para definir o valor por hora, validando valor negativo.

Nomes sugeridos:

- `ProductionDurationHours`: `int`
- `HourlyRate`: `decimal`
- `SetProductionDurationHours(int hours)`
- `SetHourlyRate(decimal hourlyRate)`

## Aplicacao e UI

Atualizar modelos em `src/App/Models/ManualCatalogModels.cs`:

- `ProductListItemViewModel` deve expor horas de producao e valor por hora.
- `ProductInputModel` deve remover `Barcode`.
- `ProductInputModel` deve trocar `ProductionDurationDays` por `ProductionDurationHours`.
- `ProductInputModel` deve adicionar `HourlyRate`.
- Validacoes devem usar Data Annotations coerentes com as regras:
  - horas: obrigatorio, maior que zero;
  - valor por hora: obrigatorio, maior ou igual a zero.

Atualizar `ProductCatalogAppService`:

- Criacao de produto deve preencher horas e valor por hora.
- Edicao de produto deve atualizar nome, status, horas e valor por hora.
- Listagem e carregamento para edicao nao devem expor codigo de barras.
- Salvamento deve continuar usando `IProductRepository` e `IUnitOfWork`.

Atualizar views de produtos:

- `Products/Index.cshtml` deve mostrar horas de producao e valor por hora.
- `Products/Edit.cshtml` deve permitir informar horas e valor por hora.
- Remover campos, labels, validacoes e elementos `data-*` relacionados a barcode.
- Remover referencias ao scanner e ao script de leitura por camera.

## Persistencia

Atualizar entidade e configuracao EF:

- Remover `Barcode` de `ProductEntity`.
- Substituir `ProductionDurationDays` por `ProductionDurationHours`.
- Adicionar `HourlyRate`.
- Mapear colunas com nomes claros, por exemplo:
  - `production_duration_hours`
  - `hourly_rate`

Atualizar `ProductRepository`:

- Mapear dominio para persistencia sem `Barcode`.
- Mapear `ProductionDurationHours`.
- Mapear `HourlyRate`.
- Manter `cache?.RemoveByPrefix("product-catalog:")` em criacao e atualizacao.

Criar migration EF:

- Remover coluna `barcode`.
- Remover ou renomear coluna `production_duration_days`.
- Adicionar coluna `production_duration_hours`.
- Adicionar coluna `hourly_rate`.
- Definir valores padrao seguros para registros existentes.

Regra sugerida para dados existentes:

- `production_duration_hours` pode receber o valor antigo de `production_duration_days * 24` ou um default de `1`, conforme for mais simples e explicito na migration.
- `hourly_rate` deve iniciar com `0`.

## Remocao do scanner

Remover o fluxo de codigo de barras:

- Remover `src/App/wwwroot/js/product-barcode-scanner.js` se nao houver outro uso.
- Remover referencias ao script em Razor views.
- Remover testes especificos do scanner, como `ProductBarcodeScannerViewTests`, ou substitui-los por testes que validem a ausencia do scanner e dos campos de barcode.
- Remover testes de dominio, aplicacao e persistencia que esperam `Barcode`.

## Criterios de aceitacao

- O produto nao possui mais campo, tela, script, teste ou persistencia de codigo de barras.
- O usuario consegue cadastrar e editar a quantidade de horas que o produto leva para ser produzido.
- Horas de producao sao obrigatorias e devem ser maiores que zero.
- O usuario consegue cadastrar e editar o valor cobrado por hora do produto.
- Valor cobrado por hora e obrigatorio e nao pode ser negativo.
- Listagem e edicao de produtos exibem horas de producao e valor por hora.
- A interface nao acessa `DbContext` diretamente.
- O dominio continua independente de entidades de persistencia.
- Alteracoes de produto continuam invalidando consultas cacheadas relacionadas.
- Produtos ativos com ficha tecnica valida continuam disponiveis para planejamento de producao.

## Test Plan

Atualizar ou criar testes para:

- `ProductTests`:
  - cria produto com horas de producao e valor por hora;
  - rejeita horas menores ou iguais a zero;
  - rejeita valor por hora negativo;
  - nao contem comportamento de barcode.
- Testes de aplicacao:
  - cadastro manual salva horas e valor por hora;
  - edicao atualiza horas e valor por hora;
  - listagem retorna horas e valor por hora;
  - cache do catalogo e invalidado ao salvar produto.
- Testes de persistencia:
  - repositorio salva e carrega horas e valor por hora;
  - entidade de persistencia nao expoe barcode.
- Testes de Razor views:
  - tela de produto contem campos de horas e valor por hora;
  - tela de produto nao contem campo de barcode, elementos de scanner ou script de camera.

Rodar ao final:

```powershell
$env:DOTNET_CLI_HOME='D:\Source\Repos\Dotnet\Craftsman\.dotnet-home\.dotnet'
dotnet build Craftsman.slnx -v:minimal /p:UseSharedCompilation=false /nr:false
dotnet test Craftsman.slnx --no-build -v:minimal
```

## Checklist de conclusao

- [x] Dominio atualizado.
- [x] Aplicacao e view models atualizados.
- [x] Views atualizadas.
- [x] Script de scanner removido.
- [x] Persistencia e repositorio atualizados.
- [x] Migration criada.
- [x] Testes atualizados.
- [x] Build executado.
- [x] Testes executados.
- [x] PC-009 marcado como concluido em `.codex/tasks/product-catalog/tasks.md` apos implementacao.
