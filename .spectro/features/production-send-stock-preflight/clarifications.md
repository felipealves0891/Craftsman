<!-- spectro-managed: true -->
# Esclarecimentos: Pré-validação de estoque antes do envio para produção

## Estado

- Fase: `resolved`
- Última atualização: 2026-11-21
- Motivo: as decisões humanas sobre UX, apresentação de pré-requisitos inválidos e referência de suporte foram respondidas.

## Evidências resolvidas por inspeção

- `CL-001` — Severidade: Major — O fluxo atual apresenta o pedido em `Orders/Details` e executa o envio por `Orders/SendToProduction`; a tela atual não possui uma etapa intermediária.
  - Decisão: a nova etapa deve ser integrada ao fluxo existente de detalhes/envio, preservando o endpoint de confirmação ou seu contrato equivalente.
  - Racional: evita duplicar regras e mantém a mudança concentrada no fluxo já utilizado.
  - Afeta: `FR-001`, `FR-005`, `CON-001`, `CON-004`.

- `CL-002` — Severidade: Major — A constituição e a especificação anterior exigem que a validação definitiva permaneça no servidor e que falhas não produzam consumo ou tarefas parciais.
  - Decisão: a conferência visual nunca substitui a revalidação no servidor; divergência entre conferência e confirmação deve bloquear/rejeitar com segurança.
  - Racional: o estoque pode mudar entre as duas etapas e a tela não é uma fronteira de confiança.
  - Afeta: `FR-006`, `NFR-001`, `NFR-002`, `CON-003`, `AC-006`.

- `CL-003` — Severidade: Major — A especificação anterior define que a quantidade necessária é a quantidade da ficha técnica e que a disponibilidade suficiente ocorre quando o saldo é maior ou igual à necessidade; não há evidência de estoque mínimo operacional adicional.
  - Decisão: manter essa regra para a conferência e para a confirmação.
  - Racional: preserva o contrato de negócio já documentado.
  - Afeta: `FR-002`, `FR-003`, `NFR-001`, `CON-002`, `AC-001`, `AC-002`, `AC-005`.

## Decisões humanas registradas

- `CL-004` — Severidade: Major — Decisão: a etapa intermediária será uma modal na tela de detalhes do pedido.
  - Racional: mantém o contexto do pedido e reduz a navegação necessária para confirmar ou corrigir o envio.
  - Afeta: `FR-001`, `FR-005`, `AC-001`, `AC-005`.

- `CL-005` — Severidade: Major — Decisão: produto sem vínculo, ficha técnica inválida ou matéria-prima ausente/inativa será apresentado com mensagem específica de erro de configuração e bloqueará o envio.
  - Racional: diferencia falta de estoque de erro de cadastro e orienta uma correção mais precisa.
  - Afeta: `FR-004`, `FR-007`, `FR-008`, `AC-004`, `AC-008`.

- `CL-006` — Severidade: Minor — Decisão: falhas inesperadas exibirão o identificador de correlação seguro para suporte, além da mensagem orientando a próxima ação.
  - Racional: facilita a busca nos diagnósticos sem expor detalhes internos, desde que seja usado um identificador não sensível.
  - Afeta: `FR-008`, `NFR-003`, `AC-009`.

## Perguntas pendentes

Nenhuma pergunta bloqueante permanece.

