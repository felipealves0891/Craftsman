<!-- spectro-managed: true -->
# Esclarecimentos: Corrigir validação e consumo de estoque ao enviar pedido para produção

## Estado

- Fase: `resolved`
- Última atualização: 2026-11-18
- Motivo: as decisões de negócio pendentes foram respondidas; eventual compensação histórica permanece fora desta feature.

## Evidências resolvidas por inspeção

- `CL-001` — Severidade: Major — A implementação atual de `InventoryService.CheckAvailabilityAsync` calcula disponibilidade pelo saldo das movimentações (`GetBalanceAsync`) quando a matéria-prima pode ser consumida. O limite exibido por alertas de estoque não aparece como critério de bloqueio nessa função. A especificação deve tratar o limite como regra separada até que haja decisão diferente.
  - Decisão: manter como requisito a correção do saldo consumível e não assumir que o limite do alerta autoriza consumo ou bloqueia planejamento.
  - Racional: essa é a distinção observável no código atual e evita converter o alerta em regra de negócio sem evidência.
  - Afeta: `FR-001`, `FR-002`, `AC-001`, `AC-002`, `AC-010`.

- `CL-002` — Severidade: Major — `ProductionPlanner.PlanAsync` valida e consome cada item dentro do loop. Portanto, uma falha em item posterior pode ocorrer depois de uma saída de estoque anterior.
  - Decisão: preservar como requisito a ausência de consumo/tarefa parcial em qualquer rejeição ou falha do planejamento.
  - Racional: a constituição exige que falha de disponibilidade não crie efeitos parciais; o comportamento atual explica o sintoma relatado.
  - Afeta: `FR-003`, `FR-004`, `NFR-001`, `AC-004`, `AC-005`.

## Decisões humanas registradas

- `CL-003` — Severidade: Blocking — Decisão: neste contexto, “quantidade mínima” é a quantidade de matéria-prima necessária pela ficha técnica para produzir os itens do pedido. Não há estoque mínimo operacional adicional a preservar para o planejamento.
  - Racional: essa é a quantidade esperada para produção informada pelo responsável; a disponibilidade deve atender à necessidade da ficha técnica.
  - Afeta: `FR-001`, `FR-002`, `CON-005`, `AC-001`, `AC-002`.

- `CL-004` — Severidade: Blocking — Decisão: as duas saídas de 32 do histórico foram geradas pela operação que falhou e são movimentações indevidas.
  - Racional: confirmação do responsável pelo comportamento observado.
  - Afeta: `FR-006`, `AC-010` e o risco de saldo histórico incorreto.

- `CL-005` — Severidade: Major — Decisão: esta feature corrige a validação e adiciona travas para impedir consumo indevido quando o estoque for insuficiente; não altera nem apaga as movimentações históricas. Qualquer compensação será uma operação auditável separada.
  - Racional: preserva a rastreabilidade do estoque e mantém o escopo concentrado na prevenção da recorrência.
  - Afeta: `FR-003`, `FR-004`, `FR-006`, `NFR-001`, `CON-002`, `AC-004`, `AC-005`, `AC-010`.

## Perguntas pendentes

Nenhuma pergunta bloqueante permanece. A necessidade de um fluxo separado para compensar as duas saídas históricas pode ser tratada em uma especificação própria.

