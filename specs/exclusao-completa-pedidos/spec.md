# Feature: Exclusao Completa de Pedido e Referencias

## Summary

Permitir que usuarios autorizados executem hard delete completo de um pedido, removendo fisicamente o pedido e todas as referencias diretas associadas a ele.

Antes da remocao, o sistema deve criar um registro de auditoria da solicitacao contendo o usuario, data/hora e informacoes basicas do pedido. Movimentos historicos de estoque nao devem ser apagados nem editados; quando necessario, o sistema deve criar movimentos compensatorios vinculados ao processo de exclusao.

## Context

O sistema ja permite excluir pedidos manuais em condicoes restritas, removendo tarefas planejadas e o pedido. Esse comportamento bloqueia pedidos com producao iniciada ou envio associado. A nova feature muda a exclusao para um processo completo, capaz de remover referencias diretas operacionais e preservar rastreabilidade minima fora do pedido excluido.

## Problem

Pedidos que precisam ser removidos fisicamente podem possuir tarefas de producao, envios e acertos financeiros associados. Sem um processo completo, a exclusao pode falhar por referencias existentes ou deixar registros operacionais apontando para um pedido removido.

## Goals

- Remover fisicamente o pedido e seus itens.
- Remover referencias diretas ao pedido em producao, envio e financeiro.
- Preservar rastreabilidade minima por auditoria da exclusao.
- Preservar historico de estoque por modelo append-only.
- Garantir que retentativas nao dupliquem reversoes ou auditorias indevidas.

## Non-Goals

Esta feature nao inclui:

- restauracao de pedidos excluidos;
- manutencao do pedido como cancelado;
- edicao retroativa de movimentos historicos de estoque;
- exclusao de produtos, materias-primas, origens de pedido ou usuarios;
- tela completa de historico/auditoria.

## Functional Requirements

### FR-001 - Hard delete completo

O sistema deve excluir fisicamente o pedido e seus itens.

### FR-002 - Excluir referencias de producao

O sistema deve remover referencias de producao associadas ao pedido, incluindo tarefas planejadas, em andamento ou concluidas.

### FR-003 - Excluir referencias de envio

O sistema deve remover envios associados ao pedido.

### FR-004 - Excluir referencias financeiras

O sistema deve remover acertos financeiros associados ao pedido.

### FR-005 - Preservar movimentos historicos de estoque

O sistema nao deve apagar nem editar movimentos historicos de estoque existentes.

### FR-006 - Compensar estoque quando aplicavel

Quando existirem movimentos de estoque Outbound associados as tarefas do pedido, o sistema deve criar movimentos Inbound compensatorios para neutralizar o impacto no saldo.

### FR-007 - Referenciar processo de exclusao

Movimentos compensatorios devem possuir BusinessReference apontando para o processo de exclusao, por exemplo `order-delete:<processId>:<movementId>`.

### FR-008 - Registrar auditoria da exclusao

Antes da exclusao fisica, o sistema deve registrar uma auditoria contendo, no minimo:

- identificador do processo de exclusao;
- usuario que solicitou;
- data/hora da solicitacao;
- id do pedido;
- origem e referencia externa;
- status do pedido;
- data de envio;
- quantidade de itens;
- resumo dos itens;
- quantidade de tarefas de producao removidas;
- quantidade de envios removidos;
- indicacao de existencia de financeiro removido.

### FR-009 - Idempotencia

Retentativas do mesmo processo de exclusao nao devem duplicar movimentos compensatorios, registros de auditoria ou falhar por referencias ja removidas.

### FR-010 - Operacao recuperavel

Se ocorrer falha durante a exclusao, o sistema deve manter estado suficiente para retomar o processo usando o mesmo identificador de exclusao.

## Business Rules

### BR-001 - Auditoria substitui a consulta futura do pedido

Como o pedido sera removido fisicamente, o registro de auditoria deve conter informacoes basicas suficientes para identificar o que foi excluido e por quem.

### BR-002 - Estoque permanece append-only

A exclusao do pedido nunca pode apagar movimentos historicos de estoque. Correcoes devem ser feitas por movimentos compensatorios.

### BR-003 - Remocao de referencias e obrigatoria

Ao final da exclusao bem-sucedida, nao devem permanecer registros de pedido, itens, producao, envio ou financeiro apontando para o pedido excluido.

### BR-004 - Exclusao nao remove cadastros independentes

Produtos, materias-primas, origens de pedido, usuarios e demais cadastros compartilhados nao devem ser removidos.

## User Flow

1. Usuario abre os detalhes do pedido.
2. Usuario solicita exclusao.
3. Sistema revalida permissao no servidor.
4. Sistema cria ou reutiliza um processo de exclusao.
5. Sistema registra auditoria com informacoes basicas do pedido.
6. Sistema cria movimentos compensatorios de estoque quando aplicavel.
7. Sistema remove referencias de producao, envio e financeiro.
8. Sistema remove pedido e itens.
9. Sistema retorna a listagem com mensagem de sucesso.

## Edge Cases

- Pedido nao existe.
- Pedido ja foi excluido.
- Pedido possui producao planejada sem movimento de estoque.
- Pedido possui producao iniciada ou concluida.
- Pedido possui envio entregue.
- Pedido possui acerto financeiro calculado.
- Movimento compensatorio ja existe por retry.
- Falha ocorre apos auditoria e antes da remocao completa.
- Falha ocorre apos compensacao de estoque e antes da remocao do pedido.

## Security

- Exclusao deve exigir politica de escrita existente.
- A acao deve validar anti-forgery token.
- A auditoria deve registrar o usuario autenticado.
- Detalhes tecnicos de falha nao devem ser expostos ao usuario.

## Compatibility

- O fluxo existente de detalhes do pedido deve continuar acessivel para pedidos nao excluidos.
- Cadastros independentes referenciados pelo pedido nao devem ser removidos.
- Movimentos de estoque ja existentes devem permanecer consultaveis.

## Acceptance Criteria

- Pedido e itens sao removidos fisicamente.
- Tarefas de producao associadas sao removidas.
- Envios associados sao removidos.
- Acerto financeiro associado e removido.
- Movimentos historicos de estoque permanecem intactos.
- Movimentos compensatorios sao criados quando ha consumo a reverter.
- Auditoria registra usuario solicitante e dados basicos do pedido.
- Retentativa do mesmo processo nao duplica compensacoes.
- Apos sucesso, nenhuma referencia direta ao pedido permanece nas tabelas operacionais.

## Open Questions

Nenhuma questao comportamental bloqueante foi identificada no prompt.
