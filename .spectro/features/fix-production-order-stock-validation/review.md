<!-- spectro-managed: true -->
# Revisão: Corrigir validação e consumo de estoque ao enviar pedido para produção

## Resumo

- Decisão: `approved`
- A implementação atende à validação agregada de matéria-prima, impede consumo parcial conhecido e protege o envio contra conflitos de concorrência.
- Os gates obrigatórios passaram no commit `02052329a143374a1218913b657a5ab08e10bc23`.
- A rastreabilidade foi validada com resultado `complete`, sem requisitos ou critérios descobertos.
- Os achados médios anteriores foram remediados e não há achados críticos ou altos pendentes.

## Artefatos examinados

- `.spectro/features/fix-production-order-stock-validation/spec.md`
- `.spectro/features/fix-production-order-stock-validation/clarifications.md`
- `.spectro/features/fix-production-order-stock-validation/plan.md`
- `.spectro/features/fix-production-order-stock-validation/tasks.md`
- `.spectro/features/fix-production-order-stock-validation/gates.json`
- `reviews/review-20261119T004000Z/manifest.json`
- `reviews/review-20261119T004000Z/traceability.json`
- Diff entre `518fd41^` e o commit atual
- Controllers, serviços de produção, UoW transacional e testes relacionados

## Verificações realizadas

- `dotnet build --no-restore --warnaserror -v minimal`: passou.
- `dotnet test --no-restore --no-build`: passou, 370 testes.
- `dotnet list package --vulnerable`: passou sem diagnóstico no gate.
- Cobertura: passou com 86,94% de linhas, 71,74% de branches, 98,10% de linhas novas e 94,00% de branches novos.
- `validate_traceability.ps1`: resultado `complete`, sem requisitos ou critérios descobertos.
- `git diff --check`: passou.
- Revisão manual de escopo, transações, eventos, logs, autorização existente e movimentações históricas.

## Requisitos verificados

- `FR-001`/`FR-002`: necessidades são agregadas por matéria-prima antes da comparação com o saldo.
- `FR-003`/`FR-004`: insuficiência agregada falha antes de adicionar consumo/tarefa; regressão cobre ausência de saída parcial.
- `FR-005`: fluxo de sucesso e alerta de estoque baixo permanecem preservados.
- `FR-006`: o envio usa transação serializável e trata conflitos de concorrência com rollback, auditoria e mensagem segura.
- `FR-007`, `NFR-003`, `NFR-004`: logs de falha mantêm contexto operacional sem expor exceção técnica ao usuário.
- `NFR-001`: planejamento, auditoria e efeitos persistidos respeitam a fronteira transacional quando o UoW transacional está disponível.
- `CON-002`: nenhuma movimentação histórica é apagada ou editada.

## Remediações verificadas

### `FINDING-001` — resolvido

O fluxo de envio agora captura falhas de concorrência/serialização, registra aviso estruturado, audita a falha como `ConcurrencyConflict` e retorna mensagem segura sem sucesso falso. Há cobertura para exceção direta, exceção aninhada, falha de serialização PostgreSQL e caminho sem transação.

### `FINDING-002` — resolvido

O serviço persiste as alterações antes de publicar os eventos de produção. O publisher persistente permanece dentro da transação existente, e o teste de consistência confirma a ordem `SaveChanges` antes da publicação.

## Achados

### `FINDING-003`

- Severidade: `Low`
- Categoria: `Test`
- Localização: `src/Infra/Repositories/UnitOfWork.cs:34-56`
- Evidência: o caminho específico de transação serializável do provedor PostgreSQL está excluído da cobertura unitária; os testes atuais usam InMemory/fakes e não há teste concorrente dedicado contra PostgreSQL.
- Impacto: comportamento específico de serialização, rollback e deadlock do provedor real não é validado diretamente.
- Recomendação: adicionar teste de integração concorrente PostgreSQL quando houver infraestrutura apropriada.
- Return stage: `human_review`
- Status: risco residual não bloqueante, aceito para esta entrega.

## Revisão de segurança

Revisão de segurança: concluída
Escopo revisado: validação e consumo de estoque, transação de produção, tratamento de concorrência, persistência/publicação de eventos, logs e dependências do diff.
Verificações executadas: gate de vulnerabilidades de pacotes, testes automatizados, build com warnings como erros e revisão manual dos caminhos de entrada, erro e persistência.
Achados: nenhum crítico ou alto identificado; `FINDING-003` é baixo e restrito à cobertura do provedor real.
Riscos residuais / limitações: não há teste concorrente PostgreSQL dedicado; isso limita a evidência específica do provedor, sem invalidar a aprovação dos gates e dos testes disponíveis.

## Decisão final

`approved`

Os requisitos e critérios estão rastreados, os gates obrigatórios passaram e as remediações necessárias foram verificadas. O risco baixo remanescente deve ser acompanhado como melhoria de cobertura de integração.
