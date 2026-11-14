<!-- spectro-managed: true -->
# Tarefas: Exibir mão de obra na tabela sem incorporá-la aos custos

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Componentes afetados | Critério de conclusão/testes esperados | Risco | Status |
|---|---|---|---|---|---|---|---|---|
| `TASK-001` | Separar no domínio o valor monetário de mão de obra do custo financeiro: manter `LaborCostAmount` calculado por duração × valor-hora, mas usar somente custo de materiais em `ProductionCostAmount`, `TotalCostAmount` e `MarginAmount`; ajustar a reconciliação para a mesma semântica. | `FR-001`, `FR-002`, `NFR-002`, `CON-001`, `CON-003` | `AC-001`, `AC-006` | Nenhuma | `SettlementCostCalculator`, `SettlementCalculator`, `FinancialSettlement`, testes de domínio/finanças | Testes cobrem materiais + mão de obra, somente mão de obra, mão de obra zero, produto ausente e preservação de receita/desconto/envio; custo total e margem não incluem mão de obra. | Chamadas existentes e testes podem assumir produção = materiais + mão de obra; revisar todos os usos sem remover o valor informativo. | completed |
| `TASK-002` | Corrigir acertos históricos afetados pela regra anterior: adaptar o serviço/worker para gravar mão de obra informativa e custo material separado, e criar migration idempotente que remova do `ProductionCostAmount` o `LaborCostAmount` anteriormente incorporado, preservando os demais campos. | `FR-002`, `NFR-002`, `CON-001`, `CON-003` | `AC-001`, `AC-006` | `TASK-001` | `HistoricalLaborCostReconciliationService`, worker, repositório financeiro, migrations/snapshot, testes de reconciliação e persistência | Registros antigos e pendentes ficam com `LaborCostAmount` preservado/calculado, `ProductionCostAmount` material-only, totais/margens coerentes; execução repetida não subtrai duas vezes; falha individual não salva parcialmente. | Dados legados podem ter estados mistos; usar o marcador existente e transação/condição idempotente, documentando qualquer registro não reconciliável. | completed |
| `TASK-003` | Expor `LaborCostAmount` no modelo de listagem e no mapeamento da aplicação, sem incluir o valor na soma do resumo nem alterar os cards. | `FR-003`, `FR-004`, `NFR-001`, `NFR-002`, `CON-002` | `AC-002`, `AC-004`, `AC-006` | `TASK-001` | `FinancialSettlementListItemViewModel`, `FinanceAppService`, testes de `FinanceAppService` | Listagem contém o valor monetário; resumo continua somando somente `TotalCostAmount`/`MarginAmount`; cache e demais filtros permanecem compatíveis. | Alteração de assinatura do record pode deixar projeções ou testes inconsistentes; localizar e atualizar todos os construtores. | completed |
| `TASK-004` | Adicionar a coluna monetária de mão de obra na tabela principal de `/Finance`, atualizar o estado vazio e manter os cards sem a informação. | `FR-003`, `FR-004`, `NFR-001`, `CON-002`, `CON-004` | `AC-002`, `AC-003`, `AC-004`, `AC-005` | `TASK-003` | `src/App/Views/Finance/Index.cshtml`, `FinanceIndexPage.razor`, testes de componentes e views | Cabeçalho, células e formatação monetária são renderizados; valor zero/ausente segue o padrão vazio; `colspan` é correto; cards não exibem coluna/KPI de mão de obra. | Alteração visual pode quebrar testes de HTML ou acessibilidade; cobrir tabela populada e vazia. | completed |

## Regras de execução

- Não remover `LaborCostAmount`, `HourlyRate`, horas de produção ou a coluna de mão de obra; o valor monetário continua sendo informação exibida.
- Não somar `LaborCostAmount` em `ProductionCostAmount`, `TotalCostAmount`, `MarginAmount` ou nos cards.
- Não alterar receita, descontos, custo de envio, seleção de tarefas, elegibilidade de entrega ou o formato monetário existente.
- A correção histórica deve preservar identidade do acerto e campos não relacionados; não excluir/recriar registros para simular a reversão.
- Não alterar telas ou relatórios fora da tabela financeira principal, salvo ajustes técnicos indispensáveis aos contratos já compartilhados.
- Manter a separação App/Domains/Infra e atualizar testes comportamentais junto com cada mudança.
- A aprovação humana do estágio de plano é obrigatória conforme `.spectro/config.yaml` antes da implementação.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004`
- Bloqueadas: nenhuma

## Evidências da execução

- Testes direcionados de `SettlementCalculatorTests`: 3 aprovados.
- Testes direcionados de `FinancialSettlementTests` e `HistoricalLaborCostReconciliationServiceTests`: 9 aprovados.
- Testes direcionados de `FinanceAppServiceTests`: 8 aprovados.
- Testes de componentes `CatalogAndFinanceComponentTests`: 18 aprovados.
- Build com `--warnaserror`: aprovado, 0 avisos e 0 erros.
- Suíte completa: 361 aprovados, 0 falhas.
- Limitação: `dotnet ef migrations list` não executou porque o startup project não referencia `Microsoft.EntityFrameworkCore.Design`; a migration foi compilada pelo build.
