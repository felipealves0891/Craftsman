<!-- spectro-managed: true -->
# Revisão: Testes de integração dos controllers de pedidos e produção

## Resumo

- **Revisão:** `review-20261121T210344Z`
- **Implementação avaliada:** commit `b2a8f001338f4d9fa60cfc60a10f53cf8c8a8614`
- **Gates:** execução `gate-20261121T210132Z`, todos os gates obrigatórios aprovados.
- **Decisão:** `changes_requested`

A implementação adiciona cobertura HTTP behavior-focused para `SendToProduction`, incluindo planejamento bem-sucedido, agregação de necessidades, rejeições sem baixa parcial, pedido inexistente, repetição, autorização e antiforgery. Não foram identificadas alterações de produção, dependências externas ou achados funcionais críticos/altos.

A aprovação fica pendente por lacunas de rastreabilidade e uma inconsistência entre o target documentado e o target efetivo do projeto.

## Artefatos examinados

- `.spectro/features/add-orders-productions-integration-tests/spec.md`
- `.spectro/features/add-orders-productions-integration-tests/clarifications.md`
- `.spectro/features/add-orders-productions-integration-tests/plan.md`
- `.spectro/features/add-orders-productions-integration-tests/tasks.md`
- `.spectro/features/add-orders-productions-integration-tests/gates.json`
- `.spectro/features/add-orders-productions-integration-tests/reviews/review-20261121T210344Z/`
- `tests/Craftsman.Tests/Integration/Http/OrdersProductionMutationTests.cs`
- `tests/Craftsman.Tests/Integration/Http/TestDataFixture.cs`
- `tests/Craftsman.Tests/Integration/Http/TestWebApplicationFactory.cs`
- `src/App/Controllers/OrdersController.cs`
- `.spectro/constitution.md` e `.spectro/config.yaml`

## Evidências verificadas

- `gates.json` válido pelo `validate_gates.mjs`.
- Build, testes, segurança de pacotes e cobertura: `passed` na execução `gate-20261121T210132Z`.
- Os testes HTTP específicos passaram: 21 testes.
- A suíte de integração HTTP passou: 23 testes.
- O build direcionado passou com 0 warnings e 0 erros.
- O diff do commit contém alterações apenas nos artefatos Spectro e nos testes/fixtures de integração; não há alteração em `src/`.
- A validação de rastreabilidade foi executada e reportou `result: gaps`, com `CON-001` descoberto e `AC-013` sem validação explícita.

## Requisitos e critérios verificados

- `FR-008`/`FR-009`: cobertura adicional de `SendToProduction` implementada e exercitada por HTTP.
- `AC-015`: sucesso com tarefa, consumo e auditoria.
- `AC-016`: necessidade agregada insuficiente sem tarefa ou `Outbound` parcial.
- `AC-017`: produto/BOM/item inválidos sem efeitos parciais.
- `AC-018`: repetição sem duplicação de tarefa ou consumo.
- `AC-020`: role sem escrita e antiforgery ausente rejeitados antes dos efeitos.
- `NFR-001`/`NFR-002`: pipeline HTTP, InMemory isolado e ausência de dependência externa observados.
- `CON-004`: nenhuma dependência externa ou pacote novo introduzido.

## Achados

### FINDING-001 — Restrição sem rastreabilidade de tarefa

- **Severidade:** Medium
- **Categoria:** consistency
- **Localização:** `spec.md` (`CON-001`), `plan.md`/`tasks.md`
- **Evidência:** `validate_traceability.ps1` retornou `CON-001` em `uncovered_requirements`; a restrição exige não alterar produção nesta etapa, mas não está ligada a uma tarefa.
- **Impacto:** o pacote não demonstra automaticamente como a restrição é validada durante a implementação.
- **Recomendação:** vincular `CON-001` a tarefa e critério de validação explícitos.
- **Retorno:** `plan`
- **Remediação:** `TASK-REV-001`

### FINDING-002 — AC-013 sem validação explícita

- **Severidade:** Medium
- **Categoria:** test
- **Localização:** `spec.md` (`AC-013`), rastreabilidade de `plan.md`/`tasks.md`
- **Evidência:** `validate_traceability.ps1` marcou `AC-013` como `appears_in_plan_or_tasks: false`; o plano menciona limitações de rollback, mas não liga o critério a uma tarefa/validação.
- **Impacto:** não é possível distinguir, pela rastreabilidade, entre cenário de rollback implementado e limitação deliberadamente não comprovada pelo InMemory.
- **Recomendação:** mapear `AC-013` a uma tarefa ou esclarecer formalmente sua exclusão/limitação.
- **Retorno:** `plan`
- **Remediação:** `TASK-REV-002`

### FINDING-003 — Target documentado divergente

- **Severidade:** Medium
- **Categoria:** consistency
- **Localização:** `spec.md` (`NFR-003`), `tests/Craftsman.Tests/Craftsman.Tests.csproj`
- **Evidência:** `NFR-003` exige compatibilidade com .NET 8, enquanto o projeto e os testes observados usam `net10.0`; o próprio plano registra a divergência sem corrigir o requisito.
- **Impacto:** a exigência pode ser interpretada como uma condição de aprovação impossível ou como migração silenciosa não autorizada.
- **Recomendação:** alinhar o requisito ao target efetivo ou registrar decisão formal de compatibilidade.
- **Retorno:** `clarify`
- **Remediação:** `TASK-REV-003`

## Riscos residuais

- EF Core InMemory não comprova locks, isolamento, serialização, deadlocks ou rollback transacional específico do PostgreSQL; concorrência distribuída permanece fora do escopo.
- Autorização por recurso/tenant não foi demonstrada e não foi inventada pelos testes.
- A aprovação dos gates comprova os comandos executados nesta revisão, não ausência absoluta de vulnerabilidades.

## Revisão de segurança

**Revisão de segurança: concluída com limitações residuais.**

- **Escopo revisado:** endpoint HTTP `SendToProduction`, autenticação/autorização, antiforgery, validação de pré-condições, persistência de tarefas/estoque/auditoria, fixtures e dependências.
- **Verificações executadas:** inspeção manual do diff e dos fluxos; gate de segurança de pacotes aprovado; build/testes/cobertura aprovados; nenhum pacote ou serviço externo adicionado.
- **Achados:** nenhum crítico ou alto identificado.
- **Riscos residuais:** limitações do InMemory, autorização por recurso/tenant não demonstrada e ausência de scanner de segredos dedicado nesta revisão.

## Decisão final

`changes_requested`. Não há falha funcional crítica/alta, mas a aprovação deve retornar ao plano/clarificação para resolver `FINDING-001`, `FINDING-002` e `FINDING-003`. Após a correção dos artefatos, a rastreabilidade deve ser revalidada e os gates rerunados se os artefatos referenciados mudarem.
