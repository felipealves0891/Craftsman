<!-- spectro-managed: true -->
# Tarefas: Vincular saída manual de estoque a pedido no financeiro

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Adicionar `OrderId` nullable ao modelo `StockMovement`, à entidade/configuração EF e ao mapeamento do repositório; criar a migration pela CLI do projeto de Infra, sem backfill de históricos | `FR-002`, `FR-007`, `NFR-002`, `CON-002`, `NFR-003` | `AC-002`, `AC-006` | nenhuma | completed |
| `TASK-002` | Estender modelos e `InventoryAppService` para listar pedidos `InProduction` sem settlement e validar no servidor o pedido informado somente para saídas, preservando saldo, custo, motivo, cancelamento e autorização existentes | `FR-001`, `FR-002`, `FR-003`, `FR-006`, `FR-008`, `FR-009`, `NFR-004`, `NFR-005`, `CON-001`, `CON-003`, `CON-005` | `AC-001`, `AC-002`, `AC-003`, `AC-007`, `AC-008` | `TASK-001` | completed |
| `TASK-003` | Atualizar controller, view e listagem/histórico para exibir o seletor condicional de pedido, repopular opções em falhas e mostrar o pedido associado sem ocultar movimentos não vinculados | `FR-001`, `FR-007`, `NFR-005` | `AC-001`, `AC-006`, `AC-008` | `TASK-002` | completed |
| `TASK-004` | Alterar o cálculo de custo material para somar saídas com `OrderId` diretamente ao pedido, manter referências de tarefas separadas, impedir dupla contagem e deixar settlements existentes inalterados | `FR-003`, `FR-004`, `FR-005`, `FR-008`, `NFR-001`, `NFR-004`, `CON-004`, `CON-005` | `AC-003`, `AC-004`, `AC-005`, `AC-007` | `TASK-001` | completed |
| `TASK-005` | Criar/atualizar testes de domínio, aplicação, persistência, financeiro e apresentação para sucesso, filtros de elegibilidade, erros, custo, não duplicidade, histórico nullable e regressões | `FR-001` a `FR-009`, `NFR-001` a `NFR-005`, `CON-001` a `CON-005` | `AC-001` a `AC-008` | `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004` | completed |
| `TASK-006` | Executar revisão final e gates Spectro; verificar migration, diff, build sem warnings, testes, vulnerabilidades de pacotes, cobertura, segurança de entradas e ausência de escopo retroativo | `NFR-003`, `NFR-005`, `CON-002`, `CON-005` | Todos | `TASK-005` | completed |

## Regras de execução

- Não implementar atualização, invalidação ou recálculo de settlements já persistidos.
- Não usar `BusinessReference` para armazenar o pedido; manter referências de tarefa e pedido em campos distintos.
- O POST deve revalidar existência, estado `InProduction` e ausência de settlement, mesmo que a opção tenha sido carregada no GET.
- O vínculo é permitido somente para `Outbound`; entradas e ajustes devem persistir `OrderId = null`.
- Não alterar receita, desconto, frete, mão de obra, método de custeio ou regra de saldo.
- Criar a migration exclusivamente por `dotnet ef ... --project src/Infra/Craftsman.Infra.csproj`, conforme a constituição.
- Preservar autorização de escrita, antiforgery, cancelamento, auditoria automática e mensagens sem detalhes internos.
- Se a implementação revelar necessidade de definir o comportamento para pedidos já apurados ou estados diferentes de `InProduction`, interromper e registrar nova clarificação antes de ampliar o escopo.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004`, `TASK-005`.
- Gates: build, testes, segurança de dependências e cobertura passaram em `gate-20261115T204709Z`; 365 testes aprovados, 86,80% de cobertura de linhas, 71,46% de branches, 97,47% de linhas novas e 100% de branches novos.
- Bloqueadas: nenhuma.
