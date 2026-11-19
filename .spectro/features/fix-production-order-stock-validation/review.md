<!-- spectro-managed: true -->
# Revisão: Corrigir validação e consumo de estoque ao enviar pedido para produção

## Resumo

- Decisão: `changes_requested`
- A implementação atende à correção principal de validação agregada e impede o consumo parcial por insuficiência conhecida.
- Os gates obrigatórios foram executados na revisão da implementação atual e passaram no código `0ac53fd866990ccad6668eeba56f51f8936bf528`.
- A rastreabilidade foi validada sem requisitos ou critérios descobertos.
- Foram encontrados dois achados médios que precisam de remediação antes da aprovação final.

## Artefatos examinados

- `.spectro/features/fix-production-order-stock-validation/spec.md`
- `.spectro/features/fix-production-order-stock-validation/clarifications.md`
- `.spectro/features/fix-production-order-stock-validation/plan.md`
- `.spectro/features/fix-production-order-stock-validation/tasks.md`
- `.spectro/features/fix-production-order-stock-validation/gates.json`
- `.spectro/features/fix-production-order-stock-validation/reviews/review-20261118T234800Z/manifest.json`
- `.spectro/features/fix-production-order-stock-validation/reviews/review-20261118T234800Z/traceability.json`
- Diff entre `518fd41^` e a revisão atual
- `src/App/Controllers/OrdersController.cs`
- `src/Domains/Production/Services/ProductionPlanner.cs`
- `src/Domains/Production/Services/OrderProductionPlanningService.cs`
- `src/Domains/SharedKernel/Repositories/ITransactionalUnitOfWork.cs`
- `src/Infra/Repositories/UnitOfWork.cs`
- Testes de produção e aplicação relacionados

## Verificações realizadas

- `dotnet build --no-restore --warnaserror -v minimal`: passou.
- `dotnet test --no-restore --no-build`: passou, 366 testes.
- `dotnet list package --vulnerable`: passou sem diagnóstico no gate.
- Cobertura: passou no gate com árvore limpa; os limiares medidos foram registrados na evidência do gate atual.
- `validate_traceability.ps1`: resultado `complete`, sem requisitos ou critérios descobertos.
- `git diff --check`: passou.
- Revisão manual de escopo, transações, eventos, logs, autorização existente e movimentações históricas.

## Requisitos verificados

- `FR-001`/`FR-002`: necessidades são agregadas por matéria-prima antes da comparação com o saldo.
- `FR-003`/`FR-004`: insuficiência agregada falha antes de adicionar consumo/tarefa; regressão cobre ausência de saída parcial.
- `FR-005`: testes existentes preservam o fluxo de sucesso e o alerta de estoque baixo sem bloqueio.
- `FR-006`: transação serializável foi introduzida no envio; ausência de teste concorrente PostgreSQL permanece risco residual.
- `FR-007`, `NFR-003`, `NFR-004`: logs de falha mantêm pedido, etapa, causa, quantidades e correlação pelos escopos existentes; mensagens do usuário não expõem exceção técnica.
- `NFR-001`: planejamento, auditoria e efeitos persistidos são executados dentro da fronteira transacional quando o UoW transacional está disponível.
- `CON-002`: nenhuma movimentação histórica é apagada ou editada.

## Achados

### `FINDING-001`

- Severidade: `Medium`
- Categoria: `Functional` / `Concurrency`
- Localização: `src/App/Controllers/OrdersController.cs:93-99`; `src/Domains/Production/Services/OrderProductionPlanningService.cs:64-73`
- Evidência: o envio usa uma transação serializável, mas o fluxo não captura falhas de serialização/conflitos de persistência. `TryPlanAsync` captura somente `InvalidOperationException`; uma falha concorrente do PostgreSQL pode escapar até o endpoint e resultar em erro técnico não tratado, sem mensagem segura nem resultado de nova tentativa.
- Impacto: sob concorrência real, o requisito de rejeição/revalidação segura (`NFR-002`, `AC-008`, `NFR-003`) não está comprovado e pode produzir resposta 500.
- Recomendação: tratar a exceção de concorrência/serialização no fluxo de envio, garantindo rollback, mensagem segura, log estruturado com correlação e ausência de sucesso falso; adicionar teste de integração PostgreSQL ou evidência equivalente.
- Return stage: `implement`
- Remediação: `TASK-REV-001`

### `FINDING-002`

- Severidade: `Medium`
- Categoria: `Architecture` / `Consistency`
- Localização: `src/Domains/Production/Services/OrderProductionPlanningService.cs:76-85`
- Evidência: eventos de domínio das tarefas são publicados antes de `unitOfWork.SaveChangesAsync` e antes do commit da transação externa. Uma falha de persistência posterior pode deixar consumidores externos informados sobre um planejamento que não foi confirmado.
- Impacto: viola a regra constitucional de publicar eventos somente após a transição confirmada e pode gerar efeitos subsequentes inconsistentes com estoque, tarefas e pedido.
- Recomendação: publicar após confirmação de persistência/commit ou usar mecanismo transacional equivalente, preservando idempotência e testes de falha pós-publicação.
- Return stage: `implement`
- Remediação: `TASK-REV-002`

## Riscos residuais

- As duas saídas históricas indevidas permanecem no ledger, conforme decisão de escopo; não houve compensação automática.
- A cobertura foi medida e aprovada no gate atual, mas o cenário concorrente foi validado apenas pela configuração serializável e não por teste PostgreSQL concorrente dedicado.
- O resultado final permanece `changes_requested` até as tarefas `TASK-REV-001` e `TASK-REV-002` serem implementadas e os gates/revisão serem renovados.

