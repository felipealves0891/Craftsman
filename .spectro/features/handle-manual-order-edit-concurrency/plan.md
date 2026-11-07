<!-- spectro-managed: true -->
# Plano: Tratar concorrência ao editar pedido manual

## Contexto e objetivo técnico

- O POST `ManualOrdersController.Edit` atualmente trata apenas erros de validação, argumentos e regras de negócio. Uma `DbUpdateConcurrencyException` durante `ManualOrderService.UpdateAsync` pode alcançar o middleware de exceção.
- `ManualOrderService.UpdateAsync` executa várias leituras, alterações e gravações, incluindo reversão/replanejamento de produção e recálculo financeiro.
- `AppDbContext` já cria auditoria de entidades com `CorrelationId`, e `HttpCurrentUserContext` usa `HttpContext.TraceIdentifier` como correlação.
- O objetivo técnico é tratar o conflito no fluxo de edição, manter o usuário no formulário com uma orientação segura e produzir um registro mínimo do caminho percorrido pela requisição, sem expor payloads ou dados sensíveis.

## Solução proposta

- Estender o uso de `LogData` no POST de edição para iniciar e finalizar um log da tentativa com `OrderId`, correlação da requisição e etapas do fluxo, sem serializar o modelo completo enviado.
- Fazer `ManualOrderService.UpdateAsync` receber o contexto de log já criado e adicionar etapas para as leituras, validações, alterações, cada fronteira de persistência e efeitos relacionados ao pedido.
- Registrar a falha com a etapa, `OrderId`, tipo da exceção, status e duração; manter a correlação já fornecida pelo `HttpCurrentUserContext`/`TraceIdentifier`.
- Capturar `DbUpdateConcurrencyException` especificamente no POST `Edit`, finalizar o log como erro e retornar `View("Create", input)` com uma mensagem segura orientando a recarregar os dados e tentar novamente.
- Garantir que o caminho de sucesso só finalize após a última operação necessária e que nenhum efeito posterior seja iniciado depois de uma exceção de persistência.
- Preservar as auditorias de entidade existentes e não criar tabela, coluna, migration, retry, merge ou sobrescrita automática.

## Arquitetura e contratos

- `src/App/Controllers/ManualOrdersController.cs`: cria o log da tentativa de edição, trata a exceção de concorrência antes do tratamento genérico existente e repopula as opções da tela.
- `src/App/Services/ManualOrderService.cs`: recebe `LogData` no fluxo de atualização e registra etapas mínimas do fluxo. A assinatura deve manter compatibilidade apenas nos chamadores existentes, principalmente testes e controller.
- `src/Domains/LogData.cs`: expõe a correlação e mantém o estado terminal do log sem permitir que o descarte transforme uma falha já registrada em sucesso.
- `src/App/Security/HttpCurrentUserContext.cs` e `src/Infra/Persistence/AppDbContext.cs`: permanecem como fonte e persistência da correlação/auditoria de entidade; não devem receber instrumentação global de todas as requisições.
- Os dados de log ficam limitados a correlação, pedido, etapa, operação, estado, resultado, duração e tipo da exceção. Não incluir `ManualOrderInputModel` completo, contatos de cliente ou snapshots de valores.
- A resposta de conflito é uma `ViewResult` da tela `Create`, com o modelo enviado e erro de ModelState; não há redirecionamento para detalhes nem retry automático.

## Fluxo de dados, erros e persistência

1. O POST de edição cria `LogData` com método, caminho, correlação e apenas o identificador do pedido.
2. O controller registra a entrada e chama `UpdateAsync` com o log.
3. O serviço registra as etapas de leitura/validação, preparação, reversão de produção, gravação do pedido, recálculo financeiro e planejamento.
4. Em sucesso, o controller finaliza o log como sucesso e redireciona para os detalhes.
5. Em `DbUpdateConcurrencyException`, o controller finaliza o log como erro, não executa o redirecionamento de sucesso, adiciona a mensagem segura, repopula as opções e retorna o formulário.
6. Em outras exceções já tratadas, o comportamento atual permanece; elas não devem ser classificadas como conflito de concorrência.
7. A exceção interrompe a cadeia de `await`s, portanto operações posteriores do método não são iniciadas. As gravações concluídas antes de uma falha permanecem sujeitas ao comportamento transacional já existente; o plano não introduz uma nova política de merge ou retry.

## Alternativas consideradas

- Adicionar uma versão explícita ao pedido: rejeitada neste escopo porque alteraria o modelo de concorrência e exigiria migration.
- Fazer retry ou merge automático: rejeitado porque pode sobrescrever a alteração concorrente e não foi aprovado na especificação.
- Instrumentar globalmente todo `AppDbContext`/SQL: rejeitado por ampliar custo, volume e risco de dados; a decisão foi limitar ao fluxo de edição manual.
- Registrar o payload completo em `LogData`: rejeitado por incluir dados pessoais e não ser necessário para reconstruir o caminho da requisição.
- Tratar a exceção somente no middleware global: rejeitado porque não preservaria o formulário nem o contexto específico da edição.

## Riscos e compatibilidade

- Risco: `LogData.Dispose` atualmente pode sobrescrever um erro finalizado com sucesso; a correção deve ser idempotente e coberta por teste para evitar falso sucesso.
- Risco: o fluxo possui múltiplos `SaveChangesAsync`; cada fronteira deve receber uma etapa clara para identificar onde a falha ocorreu.
- Risco: usar o modelo completo como `RequestBody` vazaria dados no log; o controller deve passar somente um resumo seguro.
- Compatibilidade: não há alteração de esquema ou dependência nova; a auditoria existente continua usando `CorrelationId` e a separação App/Domains/Infra é preservada.
- Compatibilidade: testes que chamam `UpdateAsync` diretamente precisarão fornecer o novo contexto de log ou usar uma sobrecarga/auxiliar explicitamente definido no plano de implementação.

## Estratégia de testes

- Teste de controller para `DbUpdateConcurrencyException`: verifica `View("Create")`, modelo enviado, mensagem segura, repopulação das opções, ausência de redirect de sucesso e ausência de detalhes técnicos na resposta.
- Teste de serviço/fluxo para falha em cada fronteira relevante de persistência: verifica que as etapas registradas identificam a operação e que efeitos posteriores não são chamados.
- Teste de `LogData`: verifica correlação, etapas, resultado de erro e que `Dispose` não altera um log já finalizado como erro.
- Teste de persistência com `AppDbContext` e contexto de usuário correlacionado: verifica que auditorias das entidades alteradas mantêm o mesmo `CorrelationId` e que não há payload completo nos registros de fluxo.
- Testes de regressão: criação de pedido e edição bem-sucedida continuam redirecionando e mantendo o tratamento existente de validação/regra de negócio.
- Validação final: `git diff --check`, build com avisos como erros, testes, segurança de pacotes e cobertura conforme `.spectro/config.yaml`.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001` | `AC-001`, `AC-006` | `TASK-003`, `TASK-005` |
| `FR-002` | `AC-002`, `AC-006` | `TASK-003`, `TASK-005` |
| `FR-003` | `AC-003` | `TASK-001`, `TASK-002`, `TASK-005` |
| `FR-004` | `AC-003` | `TASK-002`, `TASK-005` |
| `FR-005` | `AC-004` | `TASK-002`, `TASK-005` |
| `FR-006` | `AC-001`, `AC-005` | `TASK-003`, `TASK-004`, `TASK-005` |
| `NFR-001` | `AC-003` | `TASK-001`, `TASK-005` |
| `NFR-002` | `AC-002` | `TASK-003`, `TASK-005` |
| `NFR-003` | `AC-004` | `TASK-001`, `TASK-002`, `TASK-005` |
| `CON-001` | `AC-001` a `AC-006` | `TASK-001` a `TASK-005` |
| `CON-002` | `AC-003` | `TASK-001`, `TASK-005` |
| `CON-003` | `AC-001`, `AC-004` | `TASK-002`, `TASK-003` |
| `CON-004` | `AC-001`, `AC-005` | `TASK-003`, `TASK-004`, `TASK-005` |

