<!-- spectro-managed: true -->
# Tarefas: Suprimir alertas de estoque de matérias-primas inativas

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Componentes afetados | Conclusão verificável, testes esperados e risco | Status |
|---|---|---|---|---|---|---|---|
| `TASK-001` | Criar testes de comportamento para os três fluxos públicos e centralizar em `StockAlertAppService.BuildAlertsAsync` a elegibilidade que permite gerar alerta somente para `RawMaterialStatus.Active`. | `FR-001`, `FR-002`, `FR-003`, `CON-002` | `AC-001`, `AC-003`, `AC-005` | Nenhuma | `src/App/Services/StockAlertAppService.cs`; testes focados do serviço de alertas em `tests/Craftsman.Tests` | `ListAlertsAsync`, `ListCriticalAlertsAsync` e `ListAlertsForOrderAsync` omitem materiais inativos em faixas de aviso/crítica; materiais ativos mantêm nível e ordenação. Incluir cenário de pedido e nova consulta enquanto inativo. Risco: filtrar apenas um chamador e deixar outro canal divergente; validar todos os contratos públicos. | completed |
| `TASK-002` | Garantir consistência do cache depois do salvamento: adicionar `IApplicationCache?` ao final do construtor de `InventoryAppService` e invalidar `inventory:` após `SaveChangesAsync` bem-sucedido; cobrir o ciclo alerta aquecido → inativação → nova consulta. | `FR-002`, `CON-002` | `AC-002`, `AC-005` | `TASK-001` | `src/App/Services/InventoryAppService.cs`; composição de serviços existente; testes de cache/inativação em `tests/Craftsman.Tests` | Com cache previamente preenchido, salvar a inativação remove o resultado anterior e a consulta seguinte não retorna nem contabiliza o material. Confirmar que a invalidação pós-commit não ocorre quando `SaveChangesAsync` falha. Risco: quebra de instanciações manuais; manter dependência opcional e atualizar somente testes que precisam observar o cache. | completed |
| `TASK-003` | Adicionar regressão de preservação de dados para comprovar que inativar e suprimir alertas não remove nem altera saldo, limites ou movimentações, e que consultas de inventário continuam exibindo o material. | `FR-001`, `CON-001` | `AC-004` | `TASK-002` | `tests/Craftsman.Tests/Inventory/InventoryAppServiceTests.cs` ou novo teste focado; serviços e repositórios de inventário apenas como objetos sob teste | Após inativar, saldo, limites e movimentos anteriores permanecem iguais e consultáveis, enquanto o serviço de alertas omite o material. Nenhuma migration ou mutação adicional é criada. Risco: um teste apenas de alerta não detecta exclusão acidental de dados; consultar explicitamente balanços e histórico. | completed |
| `TASK-004` | Executar e registrar os gates obrigatórios e fechar a matriz de aceitação da feature. | `FR-001`, `FR-002`, `FR-003`, `CON-001`, `CON-002` | `AC-001`, `AC-002`, `AC-003`, `AC-004`, `AC-005` | `TASK-001`, `TASK-002`, `TASK-003` | Solução, `tests/Craftsman.Tests` e evidências Spectro | Passar build com `--warnaserror`, suíte de testes, segurança de pacotes e cobertura conforme `.spectro/config.yaml`; registrar evidências reproduzíveis para cada critério. Risco: falha ambiental ou de cobertura deve ser registrada, sem reduzir limiares nem ampliar escopo. | completed |

## Regras de execução

- Implementar a mudança com testes de comportamento antes ou junto de cada alteração, mantendo a ordem das dependências.
- Não alterar `RawMaterial.GetStockAlertLevel`, limites, saldos, movimentos, regras de produção ou telas fora das notificações.
- Não adicionar migration, pacote ou contrato externo.
- Usar `StockAlertAppService` como ponto único da regra; não duplicar filtros em controllers, view component ou views.
- Invalidar o cache pós-commit somente depois de persistência bem-sucedida e preservar a invalidação existente dos repositórios.
- Qualquer necessidade de mudar escopo deve voltar para aprovação humana conforme `.spectro/config.yaml`.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`.
- Concluídas: `TASK-REV-001`.
- Concluída: `TASK-004`; gates formais renovados na execução `gate-20261102T223956Z`.
- Bloqueadas: nenhuma.

## Evidências da implementação

- `TASK-001`: os 2 novos testes de alertas globais/críticos e por pedido falharam antes do filtro por status e passaram depois da alteração em `StockAlertAppService`.
- `TASK-002`: testes cobrem cache de alertas previamente aquecido, duas invalidações do prefixo `inventory:` no fluxo bem-sucedido (repositório e pós-commit) e ausência da invalidação pós-commit quando a unidade de trabalho falha.
- `TASK-003`: teste confirma que status, limites, saldo e movimento permanecem consultáveis e inalterados depois da inativação, enquanto o material não aparece nos alertas.
- `TASK-REV-001`: o cache usa uma geração por chave para impedir que uma factory iniciada antes de uma invalidação publique seu resultado depois dela. O teste concorrente bloqueia a leitura antiga, conclui a inativação e confirma que a consulta seguinte recompõe os alertas sem a matéria-prima inativa.
- Cobertura direcionada após o diagnóstico de `GATE-004`: novo teste idempotente do `DevelopmentDataSeeder`, cenários de `OrderQueryService`/`ShippingAppService`, `HttpCurrentUserContext`, `IdentitySeeder`, `EnumPresentation`, `ShipmentsController` e `ManualOrdersController` exercitam ramos anteriormente não cobertos sem alterar produção.
- Regressão direcionada: 11 testes aprovados, incluindo os cenários antigos de ordenação dos alertas e aviso de estoque ao enviar pedido para produção.
- Verificação da correção: suíte completa com 330 testes aprovados; build com `--warnaserror` aprovado sem avisos.
- Medição local da cobertura com `-AllowDirtyTree`: linhas totais `81,72%`, branches totais `55,30%`, linhas novas `99,30%` e branches novos `91,43%`; todos os limiares configurados passaram.
- `git diff --check`: aprovado sem erros de espaçamento.
- `TASK-004`: build, testes, segurança e cobertura formais aprovados em `gate-20261102T223956Z`; relatório validado por `validate_gates.mjs`.

## TASK-REV-001 - Remediate FINDING-001

- **Status:** completed
- **Finding:** FINDING-001
- **Severity:** High
- **Return stage:** implement
- **Description:** Impedir que uma criação de cache de alertas iniciada antes da inativação publique um resultado obsoleto depois da invalidação pós-commit; adicionar teste concorrente determinístico que bloqueie a factory, conclua a inativação e confirme que consultas subsequentes não recuperam o alerta antigo.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.
