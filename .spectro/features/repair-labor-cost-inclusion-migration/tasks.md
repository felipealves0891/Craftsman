<!-- spectro-managed: true -->
# Tarefas: Corrigir migrations manuais da feature add-hourly-labor-cost

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Comparar os commits `d82095eb4e9e9427c4c5086019f62c0e7b93e936` e `e23430cd0b6ad9a3b5e1a3a6bdaf32e21a7173a2` com o estado atual para delimitar os artefatos de migration/snapshot da feature e confirmar que alterações funcionais não serão revertidas. | `FR-001`, `CON-005` | `AC-005` | nenhuma | completed |
| `TASK-002` | Remover/substituir somente as migrations manuais da feature e gerar a sequência oficial com `dotnet ef` usando `src/Infra/Craftsman.Infra.csproj`, `src/App/Craftsman.csproj`, `AppDbContext` e a factory de design-time; conferir migration, designer e snapshot. | `FR-001`, `FR-002`, `NFR-001`, `CON-001`, `CON-002` | `AC-001`, `AC-002`, `AC-006` | `TASK-001` | completed |
| `TASK-003` | Inspecionar a lista e o SQL gerado das migrations, verificando ordem, alteração da coluna, default/nulabilidade, ausência de operações manuais fora do escopo e preservação das migrations de outras features. | `FR-003`, `FR-005`, `NFR-003`, `CON-003`, `CON-004`, `CON-005` | `AC-002`, `AC-003`, `AC-005`, `AC-006` | `TASK-002` | completed |
| `TASK-004` | Aplicar as migrations em banco compatível, verificar schema e `__EFMigrationsHistory`, executar a consulta de acertos financeiros e rodar os gates de build, testes, segurança e cobertura. | `FR-003`, `FR-004`, `FR-005`, `NFR-002`, `NFR-003` | `AC-003`, `AC-004` | `TASK-003` | completed |

## Regras de execução

- O escopo de alteração é limitado às migrations da feature `add-hourly-labor-cost` identificadas pelos commits de referência.
- Não reverter os commits completos e não alterar migrations de outras features.
- Não editar manualmente o snapshot ou o designer depois da geração pelo EF Core.
- Não aplicar rollback destrutivo nem recalcular dados financeiros históricos; a validação deve preservar registros e valores fora do comportamento explicitamente gerado.
- Usar os comandos oficiais do EF Core com `Craftsman.Infra` como projeto de migrations e registrar evidências reproduzíveis dos comandos e resultados.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004`
- Bloqueadas: nenhuma.
- Evidências: `dotnet ef database update` aplicou `20261109005533_AddLaborCost`, `20261109015608_AddLaborCostReconciliationMarker` e `20261114203603_AddLaborCostInclusionMarker` no banco `craftsman`; `dotnet ef migrations list` confirmou a sequência aplicada; build e 364 testes passaram.
- Limitação: `psql` e acesso Docker não estão disponíveis para uma consulta SQL independente; a verificação foi feita pelo executor do EF Core e pelo histórico retornado por `dotnet ef`.
