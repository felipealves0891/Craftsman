<!-- spectro-managed: true -->
# Esclarecimentos: Corrigir a migration do marcador de inclusão do custo de mão de obra

## Decisões registradas

- `CL-001` — **14/11/2026**
  - Pergunta: Qual é a causa do EF Core não gerar ou aplicar a alteração da coluna `labor_cost_included_in_production`?
  - Decisão: A causa está confirmada como a criação manual da migration durante a implantação, acompanhada da alteração manual do snapshot. Como o snapshot é usado pelo EF Core para comparar o modelo e decidir se há mudanças, o estado alterado fez o EF Core entender que a alteração já estava representada e que não existiam mudanças pendentes.
  - Racional: Explica diretamente o erro `42703` observado: o modelo consulta a coluna, mas o banco não recebeu a alteração de schema.
  - Requisitos afetados: `FR-001`, `FR-005`, `NFR-001`, `CON-002`, `AC-001`, `AC-005`.

- `CL-002` — **14/11/2026**
  - Pergunta: Qual fluxo deve produzir a correção?
  - Decisão: A migration deve ser desfeita/substituída no repositório e recriada pelo `dotnet ef` usando o projeto `Craftsman.Infra`, seu `AppDbContext` e a factory de design-time existente. O snapshot deve ser resultado do fluxo do EF Core, não uma edição manual.
  - Racional: Mantém migration, designer e snapshot sincronizados e permite que o EF Core identifique corretamente alterações futuras.
  - Requisitos afetados: `FR-001`, `FR-002`, `NFR-001`, `CON-001`, `CON-002`, `AC-001`.

- `CL-003` — **14/11/2026**
  - Pergunta: Qual é o estado das migrations no banco afetado?
  - Decisão: As migrations manuais não estão registradas em `__EFMigrationsHistory`; portanto, não há histórico de migrations manuais para reparar ou desfazer no banco.
  - Racional: A correção deve gerar e aplicar as migrations pelo fluxo normal do EF Core, que então registrará os novos identificadores no histórico.
  - Requisitos afetados: `FR-003`, `FR-005`, `CON-003`, `AC-002`, `AC-003`, `AC-005`.

- `CL-004` — **14/11/2026**
  - Pergunta: Qual tratamento deve ser dado à lógica SQL manual que alterava registros históricos?
  - Decisão: O escopo é somente desfazer os artefatos de migrations criados manualmente e recriar/aplicar as migrations via `dotnet ef`. Não será adicionada uma operação manual separada para recalcular dados históricos.
  - Racional: A correção solicitada é de rastreamento e aplicação de schema; alterar valores financeiros históricos extrapolaria o escopo confirmado.
  - Requisitos afetados: `FR-001`, `FR-002`, `CON-004`, `AC-006`.

- `CL-005` — **14/11/2026**
  - Pergunta: Quais migrations devem ser desfeitas e recriadas?
  - Decisão: Somente as migrations criadas manualmente para a feature `add-hourly-labor-cost`. Migrations de outras features não fazem parte da correção.
  - Racional: Evita alterar a sequência ou o estado de migrations não relacionadas ao erro de custo de mão de obra.
  - Requisitos afetados: `FR-001`, `CON-002`, `CON-005`, `AC-001`.

- `CL-006` — **14/11/2026**
  - Pergunta: Quais artefatos históricos identificam as migrations manuais que devem ser corrigidas?
  - Decisão: Usar como referências os commits `d82095eb4e9e9427c4c5086019f62c0e7b93e936`, que introduziu `20261114120000_RevertLaborCostFromFinancialTotals`, e `e23430cd0b6ad9a3b5e1a3a6bdaf32e21a7173a2`, que removeu essa migration e introduziu `20261114150000_AddLaborCostInclusionMarker`. As demais alterações funcionais desses commits permanecem fora da correção de migration.
  - Racional: Delimita os artefatos a serem substituídos sem reverter a feature inteira.
  - Requisitos afetados: `FR-001`, `CON-005`, `AC-001`, `AC-005`.

## Questões ainda pendentes

- Nenhuma. O banco não possui registros das migrations manuais no `__EFMigrationsHistory`, e o escopo foi limitado às migrations da feature `add-hourly-labor-cost`, recriadas e aplicadas pelo `dotnet ef`.
