# Feature: Exclusão Completa de Pedido e Referências

## Summary

Permitir que usuários autorizados executem hard delete completo de um pedido, removendo fisicamente o pedido e todas as referências diretas associadas a ele.

Antes da remoção, o sistema deve criar um registro de auditoria da solicitação contendo o usuário, data/hora e informações básicas do pedido. Movimentos históricos de estoque não devem ser apagados nem editados; quando necessário, o sistema deve criar movimentos compensatórios vinculados ao processo de exclusão.

## Goals

Remover fisicamente o pedido e seus itens.
Remover referências diretas ao pedido em produção, envio e financeiro.
Preservar rastreabilidade mínima por auditoria da exclusão.
Preservar histórico de estoque por modelo append-only.
Garantir que retentativas não dupliquem reversões ou auditorias indevidas.

## Non-Goals

Esta feature não inclui:

restauração de pedidos excluídos;
manutenção do pedido como cancelado;
edição retroativa de movimentos históricos de estoque;
exclusão de produtos, matérias-primas, origens de pedido ou usuários;
tela completa de histórico/auditoria.

## Functional Requirements

### FR-001 - Hard delete completo

O sistema deve excluir fisicamente o pedido e seus itens.

### FR-002 - Excluir referências de produção

O sistema deve remover referências de produção associadas ao pedido, incluindo tarefas planejadas, em andamento ou concluídas.

### FR-003 - Excluir referências de envio

O sistema deve remover envios associados ao pedido.

### FR-004 - Excluir referências financeiras

O sistema deve remover acertos financeiros associados ao pedido.

### FR-005 - Preservar movimentos históricos de estoque

O sistema não deve apagar nem editar movimentos históricos de estoque existentes.

### FR-006 - Compensar estoque quando aplicável

Quando existirem movimentos de estoque Outbound associados às tarefas do pedido, o sistema deve criar movimentos Inbound compensatórios para neutralizar o impacto no saldo.

### FR-007 - Referenciar processo de exclusão

Movimentos compensatórios devem possuir BusinessReference apontando para o processo de exclusão, por exemplo order-delete:<processId>:<movementId>.

### FR-008 - Registrar auditoria da exclusão

Antes da exclusão física, o sistema deve registrar uma auditoria contendo, no mínimo:

identificador do processo de exclusão;
usuário que solicitou;
data/hora da solicitação;
id do pedido;
origem e referência externa;
status do pedido;
data de envio;
quantidade de itens;
resumo dos itens;
quantidade de tarefas de produção removidas;
quantidade de envios removidos;
indicação de existência de financeiro removido.

### FR-009 - Idempotência

Retentativas do mesmo processo de exclusão não devem duplicar movimentos compensatórios, registros de auditoria ou falhar por referências já removidas.

### FR-010 - Operação recuperável

Se ocorrer falha durante a exclusão, o sistema deve manter estado suficiente para retomar o processo usando o mesmo identificador de exclusão.

## Business Rules

### BR-001 - Auditoria substitui a consulta futura do pedido

Como o pedido será removido fisicamente, o registro de auditoria deve conter informações básicas suficientes para identificar o que foi excluído e por quem.

### BR-002 - Estoque permanece append-only

A exclusão do pedido nunca pode apagar movimentos históricos de estoque. Correções devem ser feitas por movimentos compensatórios.

### BR-003 - Remoção de referências é obrigatória

Ao final da exclusão bem-sucedida, não devem permanecer registros de pedido, itens, produção, envio ou financeiro apontando para o pedido excluído.

### BR-004 - Exclusão não remove cadastros independentes

Produtos, matérias-primas, origens de pedido, usuários e demais cadastros compartilhados não devem ser removidos.

## User Flow

Usuário abre os detalhes do pedido.
Usuário solicita exclusão.
Sistema revalida permissão no servidor.
Sistema cria ou reutiliza um processo de exclusão.
Sistema registra auditoria com informações básicas do pedido.
Sistema cria movimentos compensatórios de estoque quando aplicável.
Sistema remove referências de produção, envio e financeiro.
Sistema remove pedido e itens.
Sistema retorna à listagem com mensagem de sucesso.

## Edge Cases

Pedido não existe.
Pedido já foi excluído.
Pedido possui produção planejada sem movimento de estoque.
Pedido possui produção iniciada ou concluída.
Pedido possui envio entregue.
Pedido possui acerto financeiro calculado.
Movimento compensatório já existe por retry.
Falha ocorre após auditoria e antes da remoção completa.
Falha ocorre após compensação de estoque e antes da remoção do pedido.

## Security

Exclusão deve exigir política de escrita existente.
A ação deve validar anti-forgery token.
A auditoria deve registrar o usuário autenticado.
Detalhes técnicos de falha não devem ser expostos ao usuário.

## Acceptance Criteria

Pedido e itens são removidos fisicamente.
Tarefas de produção associadas são removidas.
Envios associados são removidos.
Acerto financeiro associado é removido.
Movimentos históricos de estoque permanecem intactos.
Movimentos compensatórios são criados quando há consumo a reverter.
Auditoria registra usuário solicitante e dados básicos do pedido.
Retentativa do mesmo processo não duplica compensações.
Após sucesso, nenhuma referência direta ao pedido permanece nas tabelas operacionais.