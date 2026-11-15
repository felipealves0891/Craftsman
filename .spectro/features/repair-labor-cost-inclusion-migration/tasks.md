<!-- spectro-managed: true -->
# Tarefas: Corrigir a inclusão de custo de mão de obra

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Desabilitar e remover o fluxo de reconciliação histórica em background: retirar o registro no DI, eliminar referências residuais ao marcador e ajustar/remover serviços, interfaces, repositórios e testes que só suportam o worker. | `FR-001`, `FR-006`, `NFR-005`, `CON-003` | `AC-001`, `AC-007` | Nenhuma | completed |
| `TASK-002` | Reverter a inclusão de mão de obra no cálculo financeiro normal, mantendo `LaborCostAmount` como dado histórico e removendo `LaborCostReconciled` da entidade/configuração e dos contratos necessários. | `FR-003`, `FR-004`, `FR-006`, `FR-007`, `NFR-003`, `CON-002`, `CON-004` | `AC-002`, `AC-003`, `AC-005`, `AC-009`, `AC-010` | `TASK-001` | completed |
| `TASK-003` | Implementar o corpo de dados da migration para reconstruir todos os acertos a partir das movimentações materiais e dados financeiros-base. No `Up`, excluir mão de obra dos derivados; no `Down`, incluí-la usando `labor_cost_amount`, sem usar valores derivados anteriores. | `FR-002`, `FR-003`, `FR-004`, `FR-007`, `FR-008`, `NFR-001`, `NFR-003`, `NFR-004`, `CON-001`, `CON-002` | `AC-002`, `AC-003`, `AC-004`, `AC-005`, `AC-009`, `AC-010` | `TASK-002` | completed |
| `TASK-004` | Gerar e validar a migration exclusivamente pela CLI `dotnet ef`: executar `migrations add` com projeto de infraestrutura, startup, contexto e diretório definidos; revisar os arquivos gerados, snapshot, `migrations list`, `migrations script` e `database update` em banco de teste. | `FR-005`, `NFR-002`, `CON-004` | `AC-006`, `AC-008` | `TASK-002`, `TASK-003` | pending |
| `TASK-005` | Criar/ajustar testes de composição, domínio, persistência e integração da migration e executar os gates obrigatórios, registrando evidências reproduzíveis. | `NFR-001`, `NFR-002`, `NFR-003`, `NFR-004`, `NFR-005` | `AC-001` a `AC-010` | `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004` | pending |

## Regras de execução

- Usar a CLI do Entity Framework para gerar a migration; não criar manualmente uma migration sem passar por `dotnet ef migrations add`.
- Comandos-alvo: `dotnet ef migrations add <Nome> --project src/Infra/Craftsman.Infra.csproj --startup-project src/App/Craftsman.csproj --context Craftsman.Infra.Persistence.AppDbContext --output-dir Persistence/Migrations`; depois `dotnet ef migrations list`, `dotnet ef migrations script` e `dotnet ef database update` em ambiente de teste configurado.
- A CLI deve descobrir `AppDbContextDesignTimeFactory` no assembly de Infra; não adicionar `Microsoft.EntityFrameworkCore.Design` ao App e não definir Infra como startup.
- Manter `labor_cost_amount`; remover `labor_cost_reconciled` no modelo e no schema.
- Recalcular todos os acertos, não apenas aqueles com mão de obra positiva, usando dados-base e não os valores derivados atuais.
- O `Up` exclui mão de obra dos cálculos; o `Down` usa o valor histórico preservado para incluí-la novamente.
- Não alterar horas, tarefas, produtos, `HourlyRate`, receita, desconto, envio, movimentações ou identidade dos acertos; preservar a fórmula existente de custo total.
- Não executar o worker durante a migration nem depender de requisições para corrigir os dados.
- Manter o escopo aprovado; mudanças de escopo exigem aprovação humana conforme `.spectro/config.yaml`.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-REV-001`.
- Bloqueadas: nenhuma; `TASK-004` aguarda validação CLI contra banco/configuração válida.

## Evidências da execução

- `TASK-001`: 5 testes direcionados aprovados.
- `TASK-002`: 71 testes direcionados de finanças/aplicação aprovados.
- `TASK-003`: migration `20261114223224_RepairLaborCostInclusionMigration` gerada por `dotnet ef migrations add`, com `Up`/`Down` de dados e remoção do marcador.
- `TASK-REV-001`: `Down` restaura `labor_cost_reconciled` com base em `labor_cost_amount > 0`; build e suíte completa aprovados.
- Gates `gate-20261114T230444Z`: build, testes e segurança aprovados; cobertura impedida pelo gate enquanto houver alterações tracked não commitadas.
- Build da solução com `--no-restore --warnaserror`: aprovado, 0 avisos e 0 erros.
- Suíte completa: 349 testes aprovados, 0 falhas.
- `dotnet ef migrations script --idempotent`: executado com sucesso; `migrations list` encontrou a factory, mas não conseguiu consultar o status aplicado por connection string local inválida.
- `TASK-004`: com as credenciais do `infra/docker-compose.yml`, `dotnet ef migrations list --project src/Infra/Craftsman.Infra.csproj --context Craftsman.Infra.Persistence.AppDbContext` encontrou a factory e listou as migrations; `dotnet ef database update` falhou ao autenticar no PostgreSQL local com `NpgsqlException: Exception while reading from stream`/timeout. Repetido com `127.0.0.1`, SSL desabilitado e timeout explícito, com o mesmo resultado.
- Cobertura com `-AllowDirtyTree`: falhou no limiar global existente, com 62,57% de linhas (mínimo 85%) e 70,64% de branches (mínimo 60%); não houve linhas novas instrumentadas reportadas.
- Segurança NuGet: o comando configurado terminou com código 1 sem produzir diagnóstico; precisa ser repetido em ambiente que exponha a saída do comando.

## TASK-REV-001 - Remediate FINDING-001

- **Status:** completed
- **Finding:** FINDING-001
- **Severity:** Medium
- **Return stage:** implement
- **Description:** A migration Down deve restaurar o estado semântico de labor_cost_reconciled conforme os dados históricos, preservando true para acertos cujo labor_cost_amount > 0, em vez de recriar todos os registros como false.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.
