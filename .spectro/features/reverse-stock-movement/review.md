<!-- spectro-managed: true -->
# Revisão: Reverter movimentação de estoque

## Resumo

- Decisão: `needs_human_review`
- A implementação atende às regras funcionais principais e os quatro gates obrigatórios passaram.
- Não foram identificados achados críticos ou altos.
- Permanecem um risco médio de validação concorrente não exercitada contra PostgreSQL e um achado baixo de rastreabilidade documental causado por intervalos de IDs nas tarefas.

## Artefatos examinados

- `spec.md`, `clarifications.md`, `plan.md`, `tasks.md`
- `gates.json` e evidências da execução `gate-20261119T020240Z`
- `src/App/Services/InventoryAppService.cs`
- `src/App/Controllers/InventoryController.cs`
- `src/App/Views/Inventory/Movements.cshtml`
- `src/App/Models/ManualInventoryModels.cs`
- `src/Domains/Inventory/Repositories/IStockMovementRepository.cs`
- `src/Infra/Repositories/StockMovementRepository.cs`
- `tests/Craftsman.Tests/Inventory/InventoryAppServiceTests.cs`
- `tests/Craftsman.Tests/Inventory/InventoryControllerTests.cs`
- `.spectro/constitution.md` e `.spectro/config.yaml`

## Evidências verificadas

- `gates.json` válido e associado à revisão `3c089bde7eb0390ea2de3b59c41f4e12af65c415`.
- Build com `--warnaserror`: aprovado.
- Testes: 382 aprovados.
- Segurança de pacotes: nenhum pacote vulnerável reportado.
- Cobertura: gate aprovado, incluindo os limiares de linhas e branches novos.
- A operação POST usa a política `ApplicationPolicies.Write` e `ValidateAntiForgeryToken`.
- A decisão de elegibilidade ocorre no servidor; a interface não é a autoridade para saldo, pedido ou tarefa.
- O caminho compensatório preserva a movimentação original e usa referência determinística `stock-movement-reversal:{id}`.
- O caminho excepcional exclui somente saída sem referência com motivo exatamente `Production consumption`.
- Entrada, saída e ajuste possuem efeitos de reversão distintos e testes correspondentes.

## Requisitos e critérios verificados

- `FR-001` a `FR-009`: implementados nos serviços, controller, persistência e testes de inventário.
- `NFR-001`: persistência agrupada em unidade de trabalho; execução serializável quando disponível.
- `NFR-002`: idempotência por referência determinística e proteção transacional implementadas; teste concorrente real permanece pendente.
- `NFR-003` e `NFR-004`: mensagens seguras, autorização e rastreabilidade verificadas.
- `CON-001` a `CON-005`: fronteiras atuais preservadas, histórico compensado e saldo negativo rejeitado.
- `AC-001` a `AC-010`: todos aparecem no plano/tarefas e possuem validação automatizada ou evidência de gate.

## Achados

### `FINDING-001`

- Severidade: Medium
- Categoria: test
- Localização: `tests/Craftsman.Tests/Inventory/InventoryAppServiceTests.cs`; `NFR-002` / `AC-008`
- Evidência: os testes exercitam repetição sequencial da reversão e a implementação usa `ExecuteInSerializableTransactionAsync`, mas não há teste concorrente real contra PostgreSQL verificando duas solicitações simultâneas.
- Impacto: uma diferença entre o comportamento do provider InMemory usado nos testes e o PostgreSQL pode permanecer sem detecção, especialmente em conflitos de serialização ou constraints.
- Recomendação: adicionar teste de integração com PostgreSQL que execute duas reversões simultâneas da mesma movimentação e confirme no máximo uma compensação, tratando o conflito conforme o contrato definido.
- Etapa de retorno: `implement`

### `FINDING-002`

- Severidade: Low
- Categoria: consistency
- Localização: `.spectro/features/reverse-stock-movement/tasks.md`, linhas das tarefas `TASK-002` e `TASK-005`
- Evidência: `validate_traceability.ps1` reportou `FR-003` e `FR-007` como não cobertos porque as tarefas usam a notação de intervalo `FR-002 a FR-008`; semanticamente, os requisitos estão incluídos no intervalo e são cobertos por `AC-002`, `AC-003`, `AC-006` e `AC-009`.
- Impacto: a ferramenta produz um falso gap e pode impedir uma aprovação automatizada ou dificultar auditoria futura.
- Recomendação: substituir intervalos por IDs explícitos nas linhas de tarefas ou ampliar o validador para expandir intervalos antes de classificar a rastreabilidade.
- Etapa de retorno: `plan`

## Riscos residuais

- A garantia de concorrência depende do comportamento de transação serializável do provider de produção e não foi validada em uma execução PostgreSQL concorrente.
- A referência de reversão depende da convenção de `BusinessReference`; uma constraint única futura exigiria migration criada pelo fluxo EF aprovado.
- A decisão de excluir a saída de `Production consumption` sem referência reduz a trilha histórica nesse caso excepcional, conforme regra aprovada.

## Decisão final

`needs_human_review`: não há bloqueio funcional crítico/alto, mas a validação concorrente de produção deve ser avaliada por um responsável antes de considerar a feature plenamente aprovada. O achado documental pode ser corrigido no plano sem alterar produção.

