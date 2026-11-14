<!-- spectro-managed: true -->
# Revisão: Corrigir migrations manuais da feature add-hourly-labor-cost

## Resumo

- A migration manual `20261114150000_AddLaborCostInclusionMarker.cs` foi removida.
- A migration `20261114203603_AddLaborCostInclusionMarker`, seu designer e o snapshot foram gerados pelo fluxo do Entity Framework Core usando `Craftsman.Infra`.
- A migration aplicada contém somente a criação da coluna `labor_cost_included_in_production`, com `boolean`, `NOT NULL` e default `TRUE`; não contém o recálculo SQL manual removido do escopo.
- A aplicação no banco `craftsman` e o registro em `__EFMigrationsHistory` foram confirmados pelo `dotnet ef`.
- Os gates obrigatórios passaram na revisão `c7b468f453421b2be15a9148d785f1b01bd4c2b1`.

## Escopo e evidências

- Especificação: `.spectro/features/repair-labor-cost-inclusion-migration/spec.md`
- Clarificações: `.spectro/features/repair-labor-cost-inclusion-migration/clarifications.md`
- Plano: `.spectro/features/repair-labor-cost-inclusion-migration/plan.md`
- Tarefas: `.spectro/features/repair-labor-cost-inclusion-migration/tasks.md`
- Gates: `.spectro/features/repair-labor-cost-inclusion-migration/gates.json`, execução `gate-20261114T204834Z`
- Pacote de revisão: `.spectro/features/repair-labor-cost-inclusion-migration/reviews/review-20261114T205222Z/`
- Revisão do código: `c7b468f453421b2be15a9148d785f1b01bd4c2b1`
- Diff revisado: `git diff HEAD~2..HEAD`

## Requisitos verificados

- `FR-001`, `FR-002`: migration, designer e snapshot coerentes; a lista do EF Core inclui `20261114203603_AddLaborCostInclusionMarker`.
- `FR-003`, `FR-005`: `dotnet ef database update` aplicou as migrations pendentes e `dotnet ef migrations list` confirmou a sequência registrada.
- `FR-004`: a operação SQL que cria a coluna foi aplicada pelo EF Core; a ausência de `psql` impediu uma consulta SQL independente, mas não houve erro na aplicação da migration.
- `NFR-001`, `NFR-002`, `CON-001`, `CON-002`, `CON-005`: a geração usou o projeto de infraestrutura, preservou as migrations de outras features e manteve a separação arquitetural.
- `NFR-003`, `CON-003`, `CON-004`: não houve segredo registrado, rollback de histórico inexistente ou recálculo histórico na migration corrigida.
- `AC-001` a `AC-006`: rastreabilidade completa em `traceability.json`; build, testes, segurança e cobertura passaram.

## Findings

| ID | Severidade | Categoria | Localização | Evidência | Impacto | Correção recomendada | Etapa de retorno | Status |
|---|---|---|---|---|---|---|---|---|
| `FINDING-001` | Medium | Consistency | `plan.md:27,46` | O plano documenta `--startup-project src/App/Craftsman.csproj`, mas esse comando falhou porque o startup não referencia `Microsoft.EntityFrameworkCore.Design`; a execução válida usou `Craftsman.Infra` como projeto do EF Core. | A documentação pode induzir uma futura execução inválida da migration. | Atualizar o plano para documentar o comando sem `--startup-project` ou adicionar explicitamente a dependência Design ao startup, caso isso seja desejado em outra mudança. | `plan` | accepted residual |
| `FINDING-002` | Low | Test | `tasks.md:9,24` | A aplicação da migration e o histórico foram verificados pelo EF Core, mas uma consulta SQL independente ao schema e uma consulta live de acertos não foram executadas porque `psql`/Docker não estavam disponíveis. | A validação direta do comportamento de leitura no banco fica menos independente, embora o `ALTER TABLE` e os gates tenham passado. | Repetir uma consulta de schema e uma leitura de acerto em ambiente com cliente PostgreSQL ou teste de integração disponível. | `gates` | accepted residual |

## Riscos residuais

- A migration corrigida não normaliza registros históricos; isso foi explicitamente excluído do escopo e deve ser tratado separadamente se necessário.
- O nome/timestamp da migration mudou para `20261114203603_AddLaborCostInclusionMarker`; ambientes devem aplicar a nova sequência pelo mecanismo normal do EF Core, sem tentar reaplicar o artefato manual removido.

## Decisão

- `approved`
- Justificativa: Não há findings Critical ou High, os requisitos têm rastreabilidade, a migration gerada é coerente com o modelo e todos os gates obrigatórios passaram na mesma revisão do código. Os dois findings residuais são documentais/operacionais e não bloqueiam a correção.
