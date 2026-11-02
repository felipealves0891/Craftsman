<!-- spectro-managed: true -->
# Revisão: Suprimir alertas de estoque de matérias-primas inativas

- Review ID: `review-20261102T221447Z`
- Revisão examinada: `d14e42287b15594b82755ac5939e2c68af83592f`
- Base: `main`
- Decisão: `changes_requested`

## Resumo

A implementação aplica a elegibilidade por status no ponto compartilhado dos três fluxos de alerta, preserva saldo, limites e movimentos e invalida o cache depois da persistência. A rastreabilidade está completa e os quatro gates obrigatórios passaram na mesma revisão examinada.

A revisão encontrou, porém, uma corrida entre criação assíncrona e invalidação do cache. Uma consulta iniciada antes da inativação pode concluir depois da invalidação pós-commit e publicar novamente o resultado antigo. Como isso viola diretamente a remoção imediata exigida por `FR-002` e `AC-002`, a decisão é `changes_requested` e o retorno requerido é `spectro-implement`.

## Artefatos examinados

- `spec.md`, `clarifications.md`, `plan.md`, `tasks.md`, `gates.json`, `state.json`, `.spectro/constitution.md` e `.spectro/config.yaml`.
- `StockAlertAppService`, `InventoryAppService`, `RawMaterialRepository` e `MemoryApplicationCache`.
- `StockAlertAppServiceTests`, `InventoryAppServiceTests` e regressões existentes dos fluxos de alerta.
- Evidências de build, testes, segurança e cobertura em `outputs/gate-20261102T221123Z/evidence/`.
- Diff de `main` até `d14e422` e patch isolado do commit `2279f59` da feature.
- Pacote independente em `reviews/review-20261102T221447Z/`.

## Integridade das evidências

O manifesto do pacote e o relatório de gates identificam a mesma revisão `d14e42287b15594b82755ac5939e2c68af83592f`. Os hashes de `spec.md`, `clarifications.md`, `plan.md` e `tasks.md` também coincidem entre o manifesto e `gates.json`. O relatório passou pelo `validate_gates.mjs`.

Build, 310 testes, verificação de pacotes vulneráveis e cobertura passaram. Depois da análise, `TASK-REV-001` foi adicionada por este estágio de revisão; essa alteração de rastreabilidade não modifica produção, mas exige renovar as evidências após a correção.

## Rastreabilidade e requisitos verificados

`validate_traceability.ps1` retornou `complete`: não há requisitos nem critérios de aceite sem vínculo.

- `FR-001`, `AC-001` e `AC-005`: `BuildAlertsAsync` filtra `RawMaterialStatus.Active` antes de consultar saldo, cobrindo alertas globais, críticos e por pedido.
- `FR-003` e `AC-003`: os testes mantêm classificação e ordenação para matérias-primas ativas em aviso e crítico.
- `CON-001` e `AC-004`: o teste de inventário confirma que status, limites, saldo e movimento continuam consultáveis e inalterados.
- `FR-002` e `AC-002`: o caminho sequencial de cache aquecido, salvamento e nova consulta está coberto, mas o caminho concorrente possui o achado bloqueante abaixo.

## Achados

### FINDING-001 — Factory concorrente pode repopular alerta obsoleto após a invalidação pós-commit

- **Severidade:** High
- **Categoria:** Functional
- **Localização:** `src/Infra/Services/MemoryApplicationCache.cs:31-41`; `src/App/Services/InventoryAppService.cs:85`; `tests/Craftsman.Tests/Inventory/StockAlertAppServiceTests.cs:124,228`
- **Evidência:** `MemoryApplicationCache.GetOrCreateAsync` registra a chave e aguarda a factory dentro de `IMemoryCache.GetOrCreateAsync`. `RemoveByPrefix` pode remover a chave enquanto essa factory ainda está em andamento, mas não impede que a entrada criada seja publicada quando a factory terminar. A chave também já terá sido removida do conjunto interno de chaves. O teste atual aquece completamente o cache antes de salvar e usa um fake sem a semântica de criação concorrente do cache de produção, portanto não cobre essa ordem de eventos.
- **Impacto:** uma leitura iniciada antes da confirmação da inativação pode terminar depois das duas invalidações, recolocar a coleção antiga e fazer consultas posteriores exibirem e contabilizarem a matéria-prima inativa por até dois minutos. Isso viola `FR-002`, `AC-002` e o risco de cache explicitamente tratado no plano.
- **Recomendação:** tornar a publicação da factory resistente a invalidação concorrente, por exemplo com geração/versionamento por chave ou outra coordenação que descarte resultados iniciados antes da invalidação. Adicionar teste determinístico que bloqueie a factory, conclua a inativação e só então libere a leitura antiga; a consulta seguinte deve recompor o resultado sem o alerta.
- **Etapa de retorno:** `implement`
- **Tarefa corretiva:** `TASK-REV-001`

### FINDING-002 — Limiar total de cobertura diverge da Constituição

- **Severidade:** Medium
- **Categoria:** Consistency
- **Localização:** `.spectro/constitution.md:35,55`; `.spectro/config.yaml:25`; evidência `04-coverage.log`
- **Evidência:** a Constituição registra mínimos de 90% de linhas e 75% de branches, enquanto o gate executado exige 70% e 50% e passou com 73,09% e 50,32%. Essa exceção de projeto já foi aceita pelo usuário na revisão `production-calendar-month-rollover/review.md` como baseline transitório.
- **Impacto:** o gate passa sem satisfazer a política textual mais alta, reduzindo a clareza sobre qual limiar é obrigatório no momento.
- **Recomendação:** manter a aceitação já registrada e documentar na Constituição a distinção entre baseline vigente e meta futura quando a progressão for definida.
- **Etapa de retorno:** `human_review`

### FINDING-003 — Estado de `TASK-004` não reflete os gates concluídos

- **Severidade:** Low
- **Categoria:** Consistency
- **Localização:** `.spectro/features/suppress-inactive-raw-material-stock-alerts/tasks.md:9,32`; `gates.json`; `state.json`
- **Evidência:** `TASK-004` e o resumo de progresso ainda dizem `pending`, enquanto `gates.json` registra os quatro gates como `passed` e `state.json` registra a fase de gates como `completed`.
- **Impacto:** leitores do plano de tarefas recebem um estado contraditório e podem repetir trabalho já executado.
- **Recomendação:** ao concluir a correção de `FINDING-001`, atualizar o estado de `TASK-004` e a seção de progresso junto com a nova execução dos gates.
- **Etapa de retorno:** `gates`

## Riscos residuais

- O diff entre `main` e a revisão contém a adoção do Spectro e outra feature já revisada. O patch funcional desta feature está isolado no commit `2279f59`; esta revisão não reabriu as decisões humanas já registradas para as mudanças preexistentes da branch.
- Não há medição específica de carga ou latência dos alertas. O filtro reduz consultas de saldo para materiais inativos, portanto não há evidência de regressão de desempenho, mas concorrência real do cache precisa da regressão determinística descrita em `FINDING-001`.
- `gates.json`, `state.json`, o pacote da revisão, `review.md` e a atualização de `tasks.md` ainda precisam ser versionados após o ciclo corretivo.

## Decisão final

`changes_requested`. Existe um achado `High` não resolvido que pode violar a remoção imediata dos alertas após a inativação. Retornar a `spectro-implement` para executar `TASK-REV-001`; depois, renovar `spectro-gates` e `spectro-review`.
