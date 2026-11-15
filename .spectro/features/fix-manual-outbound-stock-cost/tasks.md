<!-- spectro-managed: true -->
# Tarefas: Corrigir o custo das saídas manuais de estoque

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Ajustar `InventoryAppService.RegisterMovementAsync` para preservar `UnitCostAmount` informado em saídas manuais, sem alterar a regra vigente de entradas e ajustes | `FR-001`, `FR-002`, `CON-003`, `CON-004` | `AC-001`, `AC-002` | nenhuma | completed |
| `TASK-002` | Criar/atualizar testes de aplicação e domínio para saída manual com custo, quantidade assinada negativa, custo total não negativo, saldo e validações existentes | `FR-001`, `FR-002`, `FR-003`, `FR-005`, `NFR-001`, `NFR-002`, `NFR-003`, `CON-001`, `CON-004` | `AC-001`, `AC-002`, `AC-003`, `AC-004` | `TASK-001` | completed |
| `TASK-003` | Adicionar regressão financeira e de histórico para confirmar que saídas manuais não alteram o acerto atual e que registros históricos zerados não são reescritos | `FR-003`, `FR-004`, `NFR-002`, `CON-002`, `CON-005` | `AC-005`, `AC-006`, `AC-007` | `TASK-001` | completed |
| `TASK-004` | Executar revisão final, testes aplicáveis e gates Spectro; verificar diff, cobertura, vulnerabilidades e ausência de migration/escopo indevido | `NFR-003`, `CON-003` | Todos | `TASK-002`, `TASK-003` | pending |

## Regras de execução

- Não alterar o relatório financeiro, migrations, registros históricos ou método de custeio.
- Não usar `SignedQuantity` para calcular custo total; o sinal negativo é apenas da quantidade assinada/saldo.
- Preservar validações server-side, autorização de escrita, antiforgery, cancelamento e `UnitOfWork` existentes.
- Preferir testes comportamentais nos arquivos existentes de Inventory, StockMovement e SettlementCalculator antes de criar novas abstrações.
- Se os testes revelarem que algum componente financeiro inclui saídas manuais por outra rota não observada no planejamento, interromper e registrar o impacto antes de ampliar o escopo.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`
- `TASK-004`: parcialmente verificada; build, suíte completa, diff e cobertura passaram. O comando de vulnerabilidades terminou com código 1 sem produzir relatório, deixando a validação de segurança de dependências pendente.
- Bloqueadas: nenhuma; a pendência é de evidência do ambiente/gate, não de implementação funcional.
