<!-- spectro-managed: true -->
# Revisão: Suprimir alertas de estoque de matérias-primas inativas

- Review ID: `review-20261102T224500Z`
- Revisão examinada: `0c00450015b6fc1378babba749866dae4ace3d87`
- Base: `develop`
- Decisão: `approved`

## Resumo

A implementação atende à especificação: o filtro de elegibilidade está no caminho compartilhado de construção dos alertas, cobre sino global, alertas críticos e alertas relacionados a pedidos, preserva saldo/limites/movimentações e invalida o cache após persistência bem-sucedida. A correção de concorrência por geração impede que uma leitura iniciada antes da invalidação publique novamente um resultado obsoleto.

A rastreabilidade foi validada como completa e os quatro gates formais passaram na revisão examinada: build com avisos como erros, 330 testes, segurança de pacotes e cobertura. Não há achados Critical ou High. Há um ponto residual de consistência dos artefatos, registrado abaixo, que não altera o código revisado nem bloqueia a aprovação.

## Artefatos e evidências examinados

- `spec.md`, `clarifications.md`, `plan.md`, `tasks.md`, `gates.json`, `state.json`, `.spectro/constitution.md` e `.spectro/config.yaml`.
- `src/App/Services/StockAlertAppService.cs`.
- `src/App/Services/InventoryAppService.cs`.
- `src/Infra/Services/MemoryApplicationCache.cs`.
- Testes focados de alertas, inventário e regressão concorrente em `tests/Craftsman.Tests`.
- Pacote independente em `reviews/review-20261102T224500Z/`, incluindo `manifest.json`, `diff.patch`, `review-packet.md` e `traceability.json`.
- Evidências formais em `outputs/gate-20261102T223956Z/evidence/`.

## Integridade e rastreabilidade

`validate_traceability.ps1` retornou `complete`. Todos os requisitos (`FR-001` a `FR-003`, `CON-001` e `CON-002`) estão ligados a tarefas e critérios de aceite; `AC-001` a `AC-005` possuem validação registrada.

O manifesto do review e `gates.json` apontam para a mesma revisão de código `0c00450015b6fc1378babba749866dae4ace3d87`. O relatório de gates foi validado por `validate_gates.mjs` e todos os gates estão `passed`.

## Verificações por requisito

- `FR-001`, `AC-001` e `AC-005`: `BuildAlertsAsync` considera apenas `RawMaterialStatus.Active` antes de consultar saldo, de modo uniforme nos três fluxos públicos.
- `FR-002`, `AC-002`: `SaveRawMaterialAsync` invalida `inventory:` somente após `SaveChangesAsync`; a cache usa geração por chave para descartar publicação de factories iniciadas antes da invalidação.
- `FR-003`, `AC-003`: os testes preservam classificação e ordenação de matérias-primas ativas em aviso e crítico.
- `CON-001`, `AC-004`: os testes confirmam que status, limites, saldo e movimentações continuam consultáveis e inalterados.
- `CON-002`: a regra foi centralizada no serviço de alertas, sem filtros duplicados em controllers ou views.

## Achados

### FINDING-004 — Hash de `tasks.md` posterior à execução formal dos gates

- **Severidade:** Low
- **Categoria:** Consistency
- **Localização:** `.spectro/features/suppress-inactive-raw-material-stock-alerts/tasks.md`; `gates.json:artifact_hashes.tasks.md`
- **Evidência:** o hash de `tasks.md` registrado em `gates.json` (`f88171aaf0d10ab2916b43bc816074074c96d271a0a2b038e0fcc8b864c20e45`) difere do hash atual no pacote de review (`e88c1dbbd6cb901d84c2749f7d2399c4637e91f1114c46a9fd36bffaa9570e7a`). A diferença corresponde ao fechamento de `TASK-004` e ao registro da execução formal, feitos após a geração do relatório.
- **Impacto:** a evidência formal continua válida para a mesma revisão de código, mas o relatório não representa exatamente a versão final de todos os metadados Spectro.
- **Recomendação:** após versionar os metadados finais, renovar os gates para atualizar os hashes e manter o pacote de evidências autoconsistente.
- **Etapa de retorno:** `gates`

## Riscos residuais

- Os limiares totais do gate de cobertura (75% de linhas e 55% de branches) permanecem abaixo das metas textuais de 90%/75% da Constituição; essa divergência já foi aceita como baseline transitório em revisão humana anterior.
- Não foi executada medição específica de carga. A mudança reduz consultas de saldo para materiais inativos e preserva o cache; o risco residual é operacional, não funcional.
- Os arquivos de metadados e este relatório ainda precisam ser incluídos no próximo commit do ciclo Spectro.

## Decisão final

`approved`. Não há achados Critical ou High, a rastreabilidade está completa, os gates formais passaram na mesma revisão de código e a correção concorrente cobre o risco funcional anteriormente identificado. O achado Low de consistência deve ser tratado na próxima execução de `spectro-gates` após o commit dos metadados.
