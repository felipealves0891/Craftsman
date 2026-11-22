<!-- spectro-managed: true -->
# Esclarecimentos: Ações de tarefas na produção do detalhe do pedido

## Decisões resolvidas

### `CL-001` — Confirmação antes de cancelar

- Severidade: Minor
- Pergunta: a ação `Cancelar` deve exigir confirmação explícita antes do POST?
- Decisão: exigir confirmação explícita do usuário antes de submeter `Cancelar`; se a tarefa for incompatível com cancelamento, o controle deve permanecer desabilitado e não pode abrir uma confirmação executável.
- Racional: decisão humana específica para esta feature. A disponibilidade continua seguindo o calendário/domínio, mas o cancelamento recebe uma salvaguarda adicional contra acionamento acidental.
- Requisitos afetados: `FR-002`, `FR-003`, `NFR-002`, `CON-004`, `AC-002`, `AC-005`.
- Data: 2026-11-22

### `CL-002` — Ações indisponíveis

- Severidade: Major
- Pergunta: ações incompatíveis com o estado devem ser ocultadas ou exibidas desabilitadas?
- Decisão: exibir os controles equivalentes aos do calendário e mantê-los desabilitados quando a transição não for permitida.
- Racional: a implementação existente do calendário renderiza os três controles por tarefa e usa `disabled` conforme `CanStart`, `CanComplete` e `CanCancel`.
- Requisitos afetados: `FR-002`, `FR-003`, `AC-002`, `AC-003`, `AC-004`, `AC-005`.
- Data: 2026-11-22

### `CL-003` — Destino após a ação

- Severidade: Major
- Pergunta: após iniciar, concluir ou cancelar pela tabela do detalhe, qual deve ser o destino da navegação?
- Decisão: permanecer na tela atual do detalhe do pedido após qualquer ação, exibindo o estado atualizado e a mensagem correspondente.
- Racional: decisão humana específica para manter o contexto da operação na tela em que ela foi iniciada.
- Requisitos afetados: `FR-006`, `AC-010`, `AC-011`.
- Status: resolvida.
- Data: 2026-11-22

