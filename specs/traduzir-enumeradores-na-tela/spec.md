# Feature: Traduzir enumeradores na tela

## Summary

Todos os valores de enumeradores exibidos na interface devem aparecer em portugues brasileiro, em vez dos nomes tecnicos em ingles usados no dominio.

## Context

Algumas telas renderizam enums diretamente com `ToString()` ou `GetEnumSelectList`, expondo valores como `Inbound`, `Active` e `ReadyForProduction` para o usuario.

## Problem

Valores tecnicos em ingles reduzem a clareza da interface e deixam telas inconsistentes com o restante do sistema em portugues brasileiro.

## Goals

- Exibir valores de enums em portugues brasileiro nas telas MVC e componentes Razor.
- Preservar os valores tecnicos dos enums para regras de dominio, binding, filtros, persistencia e testes de dominio.
- Cobrir os exemplos informados: `/Inventory/Balances` ou historico/saldos de estoque para `Tipo`, e `/Inventory` para `Status`.

## Non-Goals

- Renomear membros dos enums.
- Alterar valores persistidos, integracoes, rotas ou regras de negocio.
- Traduzir textos tecnicos que nao sejam apresentados ao usuario.

## Functional Requirements

### FR-001 - Labels em portugues

Quando um enum for exibido ao usuario, o sistema deve apresentar um label em portugues brasileiro.

### FR-002 - Opcoes de formulario traduzidas

Quando um enum for usado em um `select` de UI, as opcoes devem ser apresentadas em portugues brasileiro preservando os valores submetidos pelo formulario.

### FR-003 - Cobertura ampla de enums de dominio

Os enums de dominio usados nas telas de estoque, catalogo, pedidos, producao, entregas e financeiro devem ter labels traduzidos.

## Compatibility

- O binding de formularios deve continuar aceitando os valores existentes.
- Nomes e valores dos enums devem permanecer inalterados.
- Persistencia e regras de dominio devem permanecer inalteradas.

## Acceptance Criteria

- `/Inventory` nao exibe `Active` ou `Inactive` como status de materia-prima.
- Telas de estoque nao exibem `Inbound`, `Outbound` ou `Adjustment` como tipo de movimento.
- Seletores baseados em enums exibem opcoes traduzidas.
- Demais telas que apresentam status de pedidos, produtos, producao, entregas e financeiro exibem labels em portugues.
- O projeto compila e os testes relevantes passam.
