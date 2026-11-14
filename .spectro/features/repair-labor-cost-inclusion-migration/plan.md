<!-- spectro-managed: true -->
# Plano: Corrigir a inclusão de custo de mão de obra

## Contexto e objetivo técnico

- `add-hourly-labor-cost` deixou dados de mão de obra persistidos e ainda há um worker de reconciliação registrado no startup.
- O reparo deve retirar a mão de obra do cálculo financeiro vigente, recalcular todos os acertos a partir dos dados-base, preservar `labor_cost_amount` como histórico e remover `labor_cost_reconciled`.
- A alteração de schema e dados deve ser gerada, identificada e validada pela CLI do Entity Framework (`dotnet ef`), deixando migration, snapshot e evidências no repositório.

## Solução proposta

- Remover o registro de `HistoricalLaborCostReconciliationWorker` da composição da aplicação e retirar o marcador de reconciliação do modelo de persistência. O worker/serviço legado e APIs que só existem para a reconciliação deixam de participar do fluxo normal.
- Ajustar o cálculo financeiro normal para que o custo de produção venha somente do custo material derivado das movimentações de estoque. `LaborCostAmount` permanece carregado e exposto como histórico, mas não compõe custo de produção, custo total ou margem.
- Gerar uma migration com `dotnet ef migrations add` usando `AppDbContext`, projeto de infraestrutura e projeto de startup. A migration remove `labor_cost_reconciled`, mantém `labor_cost_amount` e atualiza todos os acertos.
- No `Up`, reconstruir os derivados de cada acerto com base nos dados-base: custo material das movimentações relacionadas às tarefas do pedido, custo de envio, desconto e receita. O custo de mão de obra persistido não entra na composição; custo total e margem são derivados novamente.
- No `Down`, recriar `labor_cost_reconciled` com o default compatível e reconstruir os mesmos acertos incluindo o `labor_cost_amount` preservado. O rollback não usa os custos derivados anteriores nem recalcula horas ou `HourlyRate`.
- Manter os dados de pedidos, tarefas, produtos, movimentações e identidade dos acertos. A migration deve executar em transação conforme o comportamento do provedor e falhar sem deixar atualização parcial.

## Arquitetura e contratos

- Aplicação: `src/App/Services/ApplicationServiceCollectionExtensions.cs` deixa de registrar o worker; o serviço histórico e contratos exclusivos da reconciliação devem ser removidos ou desconectados conforme os usos encontrados.
- Domínio financeiro: `SettlementCalculator`/`SettlementCostCalculator` deixam de adicionar horas × `HourlyRate` aos novos acertos. O custo material continua seguindo a regra existente baseada em `StockMovement.BusinessReference`.
- Persistência: `FinancialSettlementEntity` e `FinancialSettlementConfiguration` mantêm `LaborCostAmount` e removem `LaborCostReconciled`; o snapshot deve refletir o modelo final.
- Migration: gerar por `dotnet ef migrations add` em `src/Infra/Persistence/Migrations`. O corpo gerado deve conter a alteração de coluna e SQL de dados explícito, revisado para PostgreSQL.
- Fórmulas do `Up`: `material_cost = soma(stock_movements.total_cost_amount)` por tarefas do pedido; `production_cost = material_cost`; `total_cost = production_cost + shipping_cost + discount`, preservando a convenção atual do domínio; `margin = revenue - total_cost`.
- Fórmulas do `Down`: `production_cost = material_cost + labor_cost_amount`; `total_cost = production_cost + shipping_cost + discount`, preservando a convenção atual do domínio; `margin = revenue - total_cost`.
- Os comandos de geração e verificação devem ser reproduzíveis, com `--project src/Infra/Craftsman.Infra.csproj`, `--startup-project src/App/Craftsman.csproj`, `--context Craftsman.Infra.Persistence.AppDbContext` e saída em `Persistence/Migrations`.
- A criação do contexto em design-time deve usar `src/Infra/Persistence/AppDbContextDesignTimeFactory.cs`, descoberta por estar no mesmo assembly do contexto; não adicionar `Microsoft.EntityFrameworkCore.Design` ao projeto App nem usar Infra como startup.

## Alternativas consideradas

- Recalcular por serviço em background: rejeitado porque o serviço deve ser desabilitado e a correção precisa ser rastreável como migration.
- Subtrair mão de obra dos valores atuais: rejeitado porque não é idempotente e pode subtrair duas vezes; os derivados serão reconstruídos dos dados-base.
- Remover `labor_cost_amount`: rejeitado porque o `Down` precisa do valor histórico para recompor os custos.
- Manter `labor_cost_reconciled`: rejeitado porque todos os acertos serão recalculados em uma migration única e o marcador deixa de ter função.

## Riscos e compatibilidade

- Risco: a associação entre acerto, pedido, tarefas e movimentações pode produzir custo material nulo ou inesperado. Mitigação: reproduzir exatamente a regra vigente, cobrir cenários sem tarefas/movimentações e revisar o SQL contra o modelo.
- Risco: o `Down` pode ser executado depois de dados novos terem sido calculados sem mão de obra. Mitigação: limitar o escopo ao valor histórico preservado e validar o comportamento em banco de teste; documentar que o rollback é da migration, não uma restauração arbitrária de dados posteriores.
- Risco: remover o worker sem remover referências residuais mantém código morto ou falha de build. Mitigação: localizar DI, interfaces, repositórios, entidades e testes antes de editar.
- Compatibilidade: PostgreSQL, EF Core/Npgsql e migrations existentes devem permanecer aplicáveis em ordem; não alterar receita, desconto, envio ou movimentações.
- Segurança/observabilidade: SQL e logs não devem expor credenciais; a migration deve deixar seu registro no histórico padrão do EF.

## Estratégia de testes

- Testes de composição verificam que o worker não está registrado e que a aplicação inicia sem tentar reconciliação.
- Testes de cálculo verificam novos acertos com e sem mão de obra, múltiplas tarefas, ausência de materiais e preservação de `LaborCostAmount` sem inclusão nos derivados.
- Testes de migration/integrados aplicam `Up` a acertos com e sem mão de obra, verificam todos os derivados, preservação de dados-base e remoção da coluna de marcador; aplicam `Down` e verificam a recomposição com `LaborCostAmount`.
- Verificações CLI: gerar a migration com `dotnet ef migrations add`, conferir `dotnet ef migrations list`, gerar script com `dotnet ef migrations script` e validar build/snapshot. A aplicação da migration em banco de teste deve usar `dotnet ef database update`.
- Gates finais: build com warnings-as-errors, testes, segurança de pacotes e cobertura conforme `.spectro/config.yaml`.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001`, `FR-006` | `AC-001`, `AC-007` | `TASK-001` |
| `FR-003`, `FR-004`, `FR-007`, `FR-008` | `AC-002`, `AC-003`, `AC-005`, `AC-009`, `AC-010` | `TASK-002`, `TASK-003` |
| `FR-002` | `AC-002`, `AC-005`, `AC-006`, `AC-010` | `TASK-003` |
| `FR-005` | `AC-006`, `AC-008` | `TASK-004` |
| `NFR-001`, `NFR-003` | `AC-002`, `AC-003`, `AC-009`, `AC-010` | `TASK-003`, `TASK-005` |
| `NFR-002` | `AC-006`, `AC-008` | `TASK-004` |
| `NFR-004`, `CON-001`, `CON-002`, `CON-003`, `CON-004` | `AC-002`, `AC-004`, `AC-006`, `AC-009` | `TASK-001`, `TASK-003`, `TASK-004` |
| `NFR-005` | `AC-001` a `AC-010` | `TASK-005` |
