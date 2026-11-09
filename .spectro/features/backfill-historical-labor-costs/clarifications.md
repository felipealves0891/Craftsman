<!-- spectro-managed: true -->
# Clarificações: Recalcular custo de horas em acertos históricos

## Decisões

- `DEC-001` — População elegível
  - Pergunta: Quais acertos históricos devem ser processados?
  - Decisão: Considerar todos os acertos criados antes da implantação de `add-hourly-labor-cost`, ignorando os que já tiverem sido reconciliados.
  - Racional: Corrigir o histórico sem reaplicar o custo em registros que já foram atualizados.
  - Requisitos afetados: `FR-001`, `FR-005`, `AC-001`, `AC-006`.
  - Data: 2026-11-08.

- `DEC-002` — Fonte do valor-hora histórico
  - Pergunta: Para o histórico, deve ser usado o `HourlyRate` atualmente cadastrado no produto ou existe alguma fonte histórica do valor-hora que deva prevalecer?
  - Decisão: Usar o `HourlyRate` atualmente cadastrado no produto.
  - Racional: Essa é a fonte vigente do cálculo de custo de horas e não há snapshot histórico definido para essa informação.
  - Requisitos afetados: `FR-002`, `CON-001`, `AC-002`.
  - Data: 2026-11-08.

- `DEC-003` — Falha durante a reconciliação
  - Pergunta: Quando um acerto não puder ser recalculado, a aplicação deve continuar a inicialização e registrar a falha para nova tentativa, ou deve falhar a inicialização?
  - Decisão: A aplicação deve continuar a inicialização; a falha do serviço de cálculo não pode interromper a aplicação. O registro inválido não deve ser persistido parcialmente e deve permanecer identificável para nova tentativa.
  - Racional: A reconciliação histórica é uma correção de dados secundária e não pode impedir a disponibilidade da aplicação.
  - Requisitos afetados: `FR-007`, `NFR-001`, `NFR-004`, `AC-007`, `AC-008`.
  - Data: 2026-11-08.

- `DEC-004` — Momento e isolamento da execução
  - Pergunta: O processamento deve ocorrer uma única vez por inicialização, em segundo plano após a aplicação subir, ou deve concluir antes de aceitar requisições?
  - Decisão: Executar em background, em paralelo aos demais serviços da aplicação, sem bloquear a inicialização nem o atendimento de requisições.
  - Racional: A rotina deve corrigir o histórico automaticamente sem transformar a reconciliação em dependência de disponibilidade para iniciar a aplicação.
  - Requisitos afetados: `FR-001`, `NFR-001`, `AC-001`.
  - Data: 2026-11-08.

## Estado

- Todas as ambiguidades bloqueantes e maiores foram resolvidas.
- A especificação está pronta para planejamento.

