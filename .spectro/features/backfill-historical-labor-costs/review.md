<!-- spectro-managed: true -->
# Revisão: Recalcular custo de horas em acertos históricos

## Resumo

- Revisão independente da revisão `00e2f7e12507f403ac4efbc237e6180412460293`, comparada com `develop`.
- A rastreabilidade está completa: todos os requisitos e critérios de aceite possuem tarefas e indicação de validação.
- Os testes e o build foram executados nesta revisão e passaram.
- A implementação precisa de correção funcional no backfill do marcador de reconciliação e de gates aprovados antes da conclusão.

## Artefatos examinados

- `.spectro/features/backfill-historical-labor-costs/spec.md`
- `.spectro/features/backfill-historical-labor-costs/clarifications.md`
- `.spectro/features/backfill-historical-labor-costs/plan.md`
- `.spectro/features/backfill-historical-labor-costs/tasks.md`
- `.spectro/features/backfill-historical-labor-costs/reviews/review-20261108T230800Z/manifest.json`
- `.spectro/features/backfill-historical-labor-costs/reviews/review-20261108T230800Z/diff.patch`
- `.spectro/features/backfill-historical-labor-costs/reviews/review-20261108T230800Z/traceability.json`
- `.spectro/constitution.md` e `.spectro/config.yaml`
- Código, migration e testes incluídos no diff contra `develop`

## Validações executadas

- `validate_traceability.ps1`: completo, sem requisitos ou critérios descobertos.
- `dotnet test tests/Craftsman.Tests/Craftsman.Tests.csproj --no-restore -v minimal -m:1 -p:UseSharedCompilation=false`: 359 aprovados, 0 falhas.
- `dotnet build src/App/Craftsman.csproj --no-restore --warnaserror -v minimal -m:1 -p:UseSharedCompilation=false`: aprovado, 0 avisos e 0 erros.
- `git diff --check develop..HEAD`: encontrou apenas linhas em branco extras ao final dos quatro artefatos Spectro.
- Cobertura focada dos componentes novos: linhas instrumentadas cobertas no serviço, cálculo compartilhado, domínio e repositório; ramos instrumentados do worker cobertos.
- Não há `gates.json` para esta feature. A execução anterior do gate global registrou 61,86% de linhas e 56,70% de branches, abaixo dos limiares configurados; o comando de vulnerabilidades retornou código 1 sem diagnóstico.

## Requisitos verificados

- `FR-002`/`FR-003`: o serviço usa a duração persistida, `HourlyRate` atual e atualiza labor/produção em transação.
- `FR-004`/`CON-003`: a atualização por identidade preserva campos do acerto e não usa exclusão/recriação.
- `FR-005`: o marcador e o método de domínio impedem reaplicação após reconciliação.
- `FR-007`/`NFR-001`/`NFR-004`: o worker isola escopos, captura falhas individuais, respeita cancelamento e não propaga falha ao host.
- `AC-001` a `AC-009`: todos possuem mapeamento de tarefa e validação indicada; os cenários principais têm testes comportamentais correspondentes.

## Findings

### `FINDING-001` — High — Functional / Consistency

- **Localização:** `src/Infra/Persistence/Migrations/20261109015608_AddLaborCostReconciliationMarker.cs:11-16`; `FinancialSettlementConfiguration.cs`.
- **Evidência:** a migration adiciona `labor_cost_reconciled` com `defaultValue: false` para todas as linhas existentes, sem distinguir acertos já calculados pela funcionalidade `add-hourly-labor-cost`. O worker seleciona todos esses registros por `!LaborCostReconciled`.
- **Impacto:** acertos históricos já reconciliados podem ser processados novamente e ter sua composição reescrita usando o `HourlyRate` atual, contrariando a decisão de ignorá-los e podendo alterar o histórico após mudança de cadastro do produto.
- **Recomendação:** ajustar a migration/backfill para classificar registros já reconciliados (incluindo uma política explícita para custo de horas zero), adicionar cenários de migration e reexecução e garantir que somente legados não reconciliados permaneçam pendentes.
- **Return stage:** `implement`.
- **Remediação:** `TASK-REV-001`.

### `FINDING-002` — High — Quality / Gates

- **Localização:** `.spectro/features/backfill-historical-labor-costs/tasks.md`, `TASK-006`; ausência de `.spectro/features/backfill-historical-labor-costs/gates.json`.
- **Evidência:** testes e build passaram, mas os gates obrigatórios não estão aprovados nem registrados. A cobertura global anterior ficou em 61,86% de linhas e 56,70% de branches contra 85% e 60%; o comando de vulnerabilidades terminou com código 1 sem diagnóstico.
- **Impacto:** não há evidência reprodutível de segurança e cobertura conforme a constituição; a feature não pode ser declarada concluída.
- **Recomendação:** corrigir/diagnosticar os gates no mesmo revisionamento, executar `spectro-gates` e gerar `gates.json` com evidências. Não reduzir limiares para obter aprovação.
- **Return stage:** `gates`.
- **Remediação:** `TASK-REV-002`.

### `FINDING-003` — Medium — Scope / Human approval

- **Localização:** `.spectro/config.yaml` e commit `00e2f7e`.
- **Evidência:** os limiares de cobertura foram alterados de 82/56 para 85/60. A alteração não está descrita como requisito da feature, e a constituição exige aprovação humana explícita para mudanças nos limiares de qualidade.
- **Impacto:** a política de qualidade do repositório foi alterada e passou a bloquear o gate global; não é possível concluir se a mudança foi intencional nesta feature.
- **Recomendação:** obter aprovação humana explícita e registrar a decisão, ou separar/reverter a alteração de configuração antes da aprovação da feature.
- **Return stage:** `human_review`.
- **Remediação:** `TASK-REV-003`.

## Riscos residuais

- A rotina depende do `HourlyRate` atual, conforme decisão aprovada; alterações no catálogo continuam podendo mudar o valor usado para registros ainda pendentes.
- A execução em background foi testada por unidade e registro DI, mas não há smoke test de host real contra PostgreSQL durante a inicialização.
- A cobertura global do repositório permanece abaixo do limiar configurado.

## Decisão final

`changes_requested`

A implementação tem boa cobertura comportamental e passa em testes/build, mas há um finding funcional High sobre a classificação dos registros existentes e os gates obrigatórios ainda não estão aprovados. O retorno necessário é `implement`, seguido de `gates`; a alteração dos limiares requer `human_review`.

