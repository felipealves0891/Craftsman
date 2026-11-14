<!-- spectro-managed: true -->
# Revisão: Corrigir a inclusão de custo de mão de obra

## Resumo

- Revisão independente preparada em `review-20261114T230953Z`, com gates finais executados sobre o commit `3460dfe`.
- A rastreabilidade foi validada como completa: 17 requisitos, 10 critérios de aceite e 6 tarefas sem lacunas automáticas.
- O achado anterior sobre a reversibilidade semântica do `Down` foi resolvido.
- Permanece um achado `Medium` relacionado à ausência de aplicação contra PostgreSQL real.
- Decisão final: `changes_requested`.

## Artefatos examinados

- `.spectro/features/repair-labor-cost-inclusion-migration/spec.md`
- `.spectro/features/repair-labor-cost-inclusion-migration/clarifications.md`
- `.spectro/features/repair-labor-cost-inclusion-migration/plan.md`
- `.spectro/features/repair-labor-cost-inclusion-migration/tasks.md`
- `.spectro/features/repair-labor-cost-inclusion-migration/gates.json`
- Evidências em `.spectro/features/repair-labor-cost-inclusion-migration/outputs/gate-20261114T230444Z/evidence/`
- Packet `.spectro/features/repair-labor-cost-inclusion-migration/reviews/review-20261114T230953Z/`
- Diff atual contra `develop`
- `.spectro/constitution.md`
- Código de aplicação, domínio, infraestrutura e testes relacionados à feature

## Verificações realizadas

- O worker histórico não é registrado no DI e suas APIs foram removidas do fluxo de aplicação.
- O cálculo normal usa custos materiais e preserva `LaborCostAmount` sem incluí-lo nos derivados.
- `labor_cost_reconciled` foi removido do modelo e do snapshot; `labor_cost_amount` permanece.
- O `Up` reconstrói produção, custo total e margem usando movimentações materiais.
- O `Down` reconstrói os valores incluindo `labor_cost_amount`.
- O `Down` agora restaura `labor_cost_reconciled` com `labor_cost_amount > 0` em `20261114223224_RepairLaborCostInclusionMigration.cs:49-52`.
- A validação de rastreabilidade terminou com `result: complete` e sem requisitos ou critérios descobertos.
- Na execução final `gate-20261114T231601Z`, build, testes, segurança e cobertura passaram.
- Não foram identificados segredos ou dependências indevidas no projeto App.

## Achados

### FINDING-002

- Severidade: `Medium`
- Categoria: Test / Persistence
- Localização: `.spectro/features/repair-labor-cost-inclusion-migration/tasks.md`, `TASK-004`; migration `20261114223224_RepairLaborCostInclusionMigration.cs`.
- Evidência: os gates validaram compilação, testes e segurança, mas não houve aplicação de `dotnet ef database update` contra uma instância PostgreSQL válida; também não há teste de integração que execute `Up` e `Down` e verifique os valores financeiros no banco.
- Impacto: a semântica SQL específica do PostgreSQL, o cálculo material por joins e o rollback permanecem sem validação de execução real.
- Recomendação: executar `Up`/`Down` em banco PostgreSQL descartável com dados representativos e adicionar teste de integração ou evidência reproduzível para cenários com e sem mão de obra.
- Return stage: `gates`
- Rastreamento: `TASK-004` permanece pendente até haver connection string válida e evidência de banco.

## Achados resolvidos

### FINDING-001 — resolvido

- O `Down` recria `labor_cost_reconciled` e executa `UPDATE financial_settlements SET labor_cost_reconciled = labor_cost_amount > 0`, restaurando a semântica histórica do marcador.
- A correção foi registrada em `TASK-REV-001` e validada por build e suíte completa de 349 testes.

## Riscos residuais

- O `Down` depende do valor histórico de `labor_cost_amount` e dos dados materiais atuais; alterações posteriores nas movimentações podem produzir uma recomposição diferente da fotografia original.
- A execução atual dos gates não fornece nova medição de cobertura enquanto o worktree permanecer sujo.

## Decisão

`changes_requested` — não há achados `Critical` ou `High`, mas `FINDING-002` permanece pendente até a migration ser aplicada e verificada em PostgreSQL real.
