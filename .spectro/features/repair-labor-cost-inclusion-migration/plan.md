<!-- spectro-managed: true -->
# Plano: Corrigir migrations manuais da feature add-hourly-labor-cost

## Contexto e objetivo técnico

- A feature `add-hourly-labor-cost` alterou o modelo de `FinancialSettlement` e o mapeamento de persistência, mas as migrations associadas aos commits `d82095eb4e9e9427c4c5086019f62c0e7b93e936` e `e23430cd0b6ad9a3b5e1a3a6bdaf32e21a7173a2` foram criadas/manipuladas manualmente.
- O snapshot foi alterado de forma que o EF Core não identificou a alteração como pendente; por isso o banco não recebeu `financial_settlements.labor_cost_included_in_production` e a aplicação falhou com PostgreSQL 42703.
- O objetivo técnico é remover/substituir somente os artefatos manuais dessa feature, gerar a sequência correta com `dotnet ef` no projeto `src/Infra/Craftsman.Infra.csproj` e validar a aplicação normal das migrations.

## Solução proposta

- Usar os dois commits informados como evidência para delimitar os artefatos de migration a corrigir: `20261114120000_RevertLaborCostFromFinancialTotals` e `20261114150000_AddLaborCostInclusionMarker`.
- Preservar as alterações de domínio, aplicação, UI, repositório e testes introduzidas pela feature; não reverter os commits inteiros.
- Remover os artefatos de migration manuais da feature conforme o estado final desejado e regenerar as migrations com `dotnet ef`, usando `Craftsman.Infra` como projeto de migrations e o `AppDbContext` disponível pela `AppDbContextDesignTimeFactory`.
- Conferir que o resultado contenha o arquivo da migration, o `.Designer.cs` correspondente e um `AppDbContextModelSnapshot` coerente com o modelo atual.
- Manter a alteração de schema necessária para adicionar `labor_cost_included_in_production` e não adicionar SQL manual de recálculo histórico fora do que o fluxo gerado pelo EF Core produzir.
- Validar primeiro a lista e o script das migrations; somente depois aplicar em um banco compatível. A inicialização da aplicação continuará usando `Database.MigrateAsync()`.

## Arquitetura e contratos

- Contexto: `src/Infra/Persistence/AppDbContext.cs`.
- Design-time: `src/Infra/Persistence/AppDbContextDesignTimeFactory.cs`, que resolve as configurações da aplicação e constrói o contexto com Npgsql.
- Projeto de migrations: `src/Infra/Craftsman.Infra.csproj`, que referencia `Microsoft.EntityFrameworkCore.Design` e o provedor Npgsql.
- Artefatos: `src/Infra/Persistence/Migrations/*.cs`, incluindo migration, designer e snapshot.
- Modelo persistente: `FinancialSettlementConfiguration` mapeia `LaborCostIncludedInProduction` para `labor_cost_included_in_production`, obrigatório e com default `true`.
- Execução em runtime: `src/App/Program.cs` aplica migrations pendentes via `dbContext.Database.MigrateAsync()`.
- Comando-base a validar durante a implementação: `dotnet ef migrations add <Nome> --project src/Infra/Craftsman.Infra.csproj --startup-project src/App/Craftsman.csproj --context AppDbContext --output-dir Persistence/Migrations`.

## Alternativas consideradas

- Manter a migration manual e editar somente o snapshot: rejeitado porque reproduz a causa raiz e permite que o EF Core considere a alteração já representada sem gerar uma operação aplicável.
- Criar SQL manual diretamente no banco: rejeitado porque não atualiza de forma confiável o conjunto migration/designer/snapshot e foge do fluxo solicitado.
- Reverter os commits completos: rejeitado porque removeria alterações funcionais da feature que não fazem parte do defeito de migration.
- Criar uma migration de reparo manual para o banco: rejeitado para o cenário confirmado, pois as migrations manuais não estão em `__EFMigrationsHistory`; a correção deve gerar os artefatos oficiais e deixar o EF Core registrá-los.

## Riscos e compatibilidade

- Risco: o nome e a divisão finais das migrations geradas podem diferir dos nomes manuais; a validação deve priorizar operações, ordem e compatibilidade do schema, não preservar nomes artificiais.
- Risco: gerar a migration com startup project ou contexto incorreto pode produzir um snapshot de outro modelo; mitigar validando o comando, o contexto e o diff gerado.
- Risco: a migration manual continha lógica SQL para dados históricos; essa lógica não deve ser reintroduzida automaticamente como operação manual, pois a especificação não autoriza recálculo histórico.
- Compatibilidade: migrations de outras features devem permanecer inalteradas, e alterações funcionais dos commits de referência devem ser preservadas.

## Estratégia de testes

- Teste estrutural: confirmar que a migration gerada possui classe, designer e snapshot reconhecidos pelo EF Core, sem edição manual posterior.
- Teste de ferramenta: executar `dotnet ef migrations list` com o projeto `Craftsman.Infra` e o startup project da aplicação; confirmar a sequência esperada e o registro posterior das migrations aplicadas.
- Teste de script: gerar o SQL da atualização e conferir `ALTER TABLE` para `labor_cost_included_in_production`, tipo boolean, não nulabilidade e default compatíveis, sem operações fora do escopo.
- Teste de aplicação: aplicar as migrations em banco compatível/disponível e verificar a existência da coluna e da entrada correspondente em `__EFMigrationsHistory`.
- Regressão: executar build, testes e gates configurados, com atenção aos testes de finanças, persistência e reconciliação já relacionados à feature.
- Aceitação: consultar um acerto financeiro após a atualização e confirmar ausência do erro 42703.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001`, `NFR-001` | `AC-001` | `TASK-002` |
| `FR-002` | `AC-002` | `TASK-002`, `TASK-003` |
| `FR-003`, `FR-005` | `AC-003` | `TASK-003`, `TASK-004` |
| `FR-004` | `AC-004` | `TASK-004` |
| `CON-003` | `AC-005` | `TASK-001`, `TASK-003` |
| `CON-004` | `AC-006` | `TASK-002`, `TASK-003` |
| `NFR-002`, `CON-005` | preservação de escopo | `TASK-001`, `TASK-002` |
| `NFR-003` | diagnóstico e gates sem segredos | `TASK-003`, `TASK-004` |
