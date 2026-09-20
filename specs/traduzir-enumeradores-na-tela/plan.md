# Implementation Plan: Traduzir enumeradores na tela

## Summary

Centralizar labels de enums em um helper de apresentacao reutilizavel e substituir renderizacoes diretas de enums nas Views, componentes alimentados por View Models e selects.

## Specification Reference

`specs/traduzir-enumeradores-na-tela/spec.md`

## Current Architecture

A aplicacao usa servicos em `src/App/Services` para montar View Models, Views MVC em `src/App/Views` e componentes Razor em `src/UI/Components`. Algumas Views ja possuem switches locais para status, enquanto outras usam `ToString()` e `Html.GetEnumSelectList`.

## Change Surface

- Novo helper de apresentacao em `src/App`.
- Views de estoque, produtos, pedidos, producao, entregas e financeiro.
- Servicos que projetam status/tipo para View Models.
- Testes de UI/apresentacao existentes em `tests/Craftsman.Tests/Application` e areas relacionadas.

## Implementation Strategy

Criar um helper estatico com labels para os enums de dominio exibidos na interface e uma funcao para gerar `SelectListItem`. Usar o helper nos servicos e Views para manter os componentes Razor recebendo texto pronto para exibicao.

## Files to Modify

- `src/App/Services/*.cs` que usam `Status.ToString()` ou `Type.ToString()` para View Models.
- `src/App/Views/**/*.cshtml` que usam switches locais ou `Html.GetEnumSelectList`.
- `src/UI/Components/**/*.razor` somente se ainda houver exibicao direta de string tecnica.
- Testes de apresentacao.

## Files to Create

- `src/App/Presentation/EnumPresentation.cs`

## Persistence Changes

Nenhuma.

## API Contract Changes

Nenhuma mudanca de rota ou contrato externo. Formularios continuam postando os valores de enum esperados pelo model binding.

## Testing Strategy

- Adicionar testes unitarios para o helper cobrindo os labels dos enums de dominio.
- Atualizar ou adicionar testes de Views para garantir que selects e paginas relevantes usem labels em portugues.
- Executar build e testes relevantes.

## Implementation Tasks

1. Criar helper central de labels e opcoes para enums.
2. Substituir `ToString()` de enums em View Models de apresentacao por labels traduzidos onde o valor e apenas exibido.
3. Substituir `GetEnumSelectList` por opcoes traduzidas preservando valores.
4. Remover switches locais duplicados quando o helper cobre o caso.
5. Adicionar/atualizar testes.
6. Validar build e testes.

## Backward Compatibility

Os valores tecnicos permanecem nos enums e nos posts de formulario. A alteracao e limitada ao texto exibido na UI.
