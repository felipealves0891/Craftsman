<!-- spectro-managed: true -->
# Revisão: Recalcular custo de horas em acertos históricos

## Resumo

- Revisão independente da implementação na revisão `9db8742ee00071a835461ec141b778074177e590`.
- O pacote de revisão e a rastreabilidade foram gerados para `review-20261109T025421Z`.
- Todos os gates obrigatórios passaram na mesma revisão: build, testes, segurança e cobertura.
- Não foram encontrados findings Critical ou High funcionais.
- A aprovação humana foi recebida; a decisão final é `approved`.

## Artefatos examinados

- `.spectro/features/backfill-historical-labor-costs/spec.md`
- `.spectro/features/backfill-historical-labor-costs/clarifications.md`
- `.spectro/features/backfill-historical-labor-costs/plan.md`
- `.spectro/features/backfill-historical-labor-costs/tasks.md`
- `.spectro/features/backfill-historical-labor-costs/gates.json`
- `.spectro/features/backfill-historical-labor-costs/state.json`
- `.spectro/constitution.md`
- `.spectro/config.yaml`
- `.spectro/features/backfill-historical-labor-costs/reviews/review-20261109T025421Z/manifest.json`
- `.spectro/features/backfill-historical-labor-costs/reviews/review-20261109T025421Z/diff.patch`
- `.spectro/features/backfill-historical-labor-costs/reviews/review-20261109T025421Z/traceability.json`
- Código de domínio, aplicação, infraestrutura e testes no diff contra `develop`.

## Validações executadas

- `prepare_review.ps1`: concluído; revisão e hashes dos artefatos correspondem à revisão atual.
- `validate_traceability.ps1`: concluído; nenhum requisito ou critério de aceite descoberto.
- Gates Spectro `gate-20261109T025217Z`: resultado `passed`.
- Build: aprovado com `--warnaserror`, 0 avisos e 0 erros.
- Testes: 360 aprovados, 0 falhas.
- Segurança: `dotnet list package --vulnerable` aprovado.
- Cobertura: 86,97% linhas, 71,05% branches, 98,03% linhas novas e 88,46% branches novos.
- `validate_gates.mjs`: relatório válido.

## Requisitos e comportamento verificados

- `FR-001`/`AC-001`: o worker consulta pendências na inicialização e executa como `BackgroundService`, sem bloquear o host.
- `FR-002`/`AC-002`/`CON-001`/`CON-002`: o cálculo usa `ProductionDurationHours` e `Product.HourlyRate` atual.
- `FR-003`/`FR-004`/`AC-003`/`AC-004`/`AC-009`: o domínio atualiza custo de horas e produção, preservando receita, desconto, envio, identidade e data.
- `FR-005`/`AC-006`: o marcador persistido e `ReconcileLaborCost` impedem reaplicação sequencial.
- `FR-007`/`NFR-001`/`NFR-002`/`NFR-004`/`AC-007`/`AC-008`: cada item é processado em escopo isolado, falhas são registradas, o item permanece pendente e a aplicação continua.
- `CON-003`/`CON-004`: a atualização é feita por identidade, com migration, configuração e snapshot compatíveis.
- A migration classifica registros existentes com `labor_cost_amount > 0` como já reconciliados; registros legados com custo zero permanecem elegíveis, conforme o marcador explícito.

## Findings

### `FINDING-001` — Medium — Consistency / Human approval — Resolved

- **Localização:** `.spectro/constitution.md`, `.spectro/config.yaml:25`, `.spectro/features/backfill-historical-labor-costs/tasks.md:TASK-REV-003`.
- **Evidência:** a Constituição declara mínimos de 90% de linhas e 75% de branches, enquanto o comando configurado usa 85% e 60%. A tarefa `TASK-REV-003` registrava a necessidade de aprovação humana, que foi recebida nesta revisão.
- **Impacto:** a feature pode ser aprovada contra uma política de gate diferente da política declarada pelo projeto.
- **Resolução:** aprovação humana explícita recebida para manter os limiares configurados de 85%/60% nesta revisão. A diferença em relação à Constituição permanece documentada para harmonização futura.
- **Return stage:** `human_review`.

### `FINDING-002` — Medium — Consistency / Traceability — Resolved

- **Localização:** `.spectro/features/backfill-historical-labor-costs/tasks.md:TASK-006`, `TASK-REV-002` e seção “Evidências da execução”.
- **Evidência:** o `state.json` indica a fase de gates como `completed`, o `gates.json` indica `passed` e os status de `TASK-006` e `TASK-REV-002` foram sincronizados; a evidência de segurança foi atualizada para o resultado aprovado.
- **Impacto:** o estado de execução e as evidências textuais não representam o resultado atual, reduzindo a confiabilidade do handoff e da aprovação final.
- **Resolução:** status e evidências das tarefas foram sincronizados após a aprovação humana.
- **Return stage:** `gates`.

### `FINDING-003` — Low — Architecture / Concurrency

- **Localização:** `src/App/Services/HistoricalLaborCostReconciliationService.cs:40-66`.
- **Evidência:** o settlement é lido e marcado como reconciliado antes da transação, mas não há uma nova leitura ou verificação do marcador dentro da transação antes de `UpdateAsync`. O plano menciona revalidar o estado para evitar concorrência.
- **Impacto:** duas instâncias do worker, ou uma execução concorrente externa, podem carregar o mesmo item pendente e a última atualização prevalecer com o `HourlyRate` observado por ela.
- **Recomendação:** revalidar o marcador dentro da transação ou adicionar uma atualização condicional por `Id` e `LaborCostReconciled = false`, tratando zero linhas afetadas como item já reconciliado. O worker atual é sequencial por escopo, portanto o risco não bloqueia a aprovação funcional desta revisão.
- **Return stage:** `implement`.

## Riscos residuais

- O cálculo histórico usa o `HourlyRate` atual, conforme decisão aprovada, e pode divergir do valor vigente na data original.
- Não há smoke test de inicialização contra PostgreSQL real; a atomicidade e o fluxo foram verificados com testes de persistência e unidade transacional.
- A aprovação final depende da decisão humana sobre os limiares de cobertura e da sincronização dos artefatos de tarefa.

## Decisão final

`approved`

A implementação atende aos requisitos funcionais principais, os gates obrigatórios passaram e não há finding Critical/High unresolved. A aprovação humana resolveu os bloqueios de política e rastreabilidade. O risco de concorrência foi registrado como Low e permanece como melhoria futura, sem bloquear a conclusão.
