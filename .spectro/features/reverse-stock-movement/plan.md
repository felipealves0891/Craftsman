<!-- spectro-managed: true -->
# Plano: Reverter movimentação de estoque

## Contexto e objetivo técnico

- A tela `Inventory/Movements` já registra e lista movimentações por meio de `InventoryController` e `InventoryAppService`.
- A movimentação de domínio já representa entrada, saída e ajuste, mantém `OrderId`, `BusinessReference`, custo e quantidade, e o saldo é calculado pelo repositório.
- O objetivo técnico é acrescentar uma operação de reversão no fluxo existente, preservando as fronteiras App/Domains/Infra e sem criar um novo modelo de estoque.
- A operação deve ser autorizada pela política `ApplicationPolicies.Write` e protegida por antiforgery, seguindo o padrão do registro manual.

## Solução proposta

- Adicionar no repositório de movimentações a consulta por identificador e a exclusão necessária somente para a exceção aprovada.
- Orquestrar a reversão no serviço de aplicação existente, carregando a movimentação alvo, validando matéria-prima/pedido/tarefas e determinando um dos dois efeitos:
  - saída sem referência e motivo exatamente `Production consumption`: excluir a movimentação;
  - demais tipos elegíveis: criar compensação com referência determinística `stock-movement-reversal:{movementId}`, motivo `Stock movement reversal`, mesmo material, quantidade/custo compatíveis e efeito oposto.
- Para entrada, criar saída; para saída, criar entrada; para ajuste, criar ajuste com quantidade oposta.
- Verificar `ProductionTaskStatus.Completed` nas tarefas do pedido e `OrderStatus.Delivered` no pedido. Movimentações sem `OrderId` não sofrem bloqueio de estado de pedido.
- Antes de criar uma compensação, detectar a referência determinística para tornar a operação idempotente. A operação continuará usando a unidade serializável disponível, mas não haverá teste concorrente real contra PostgreSQL nesta entrega; a validação ficará limitada aos testes sequenciais e ao comportamento observável da unidade de trabalho.
- Expor a ação na tabela de movimentos, com confirmação visual e mensagens seguras de sucesso/rejeição; a decisão final deve ocorrer no servidor.

## Arquitetura e contratos

- `src/App/Controllers/InventoryController.cs`: novo POST de reversão com `[Authorize(Policy = ApplicationPolicies.Write)]` e `[ValidateAntiForgeryToken]`; tratar falhas esperadas como o POST de registro.
- `src/App/Services/InventoryAppService.cs`: novo caso de uso de reversão; depender de `IProductionTaskRepository` para verificar tarefas concluídas e de `IOrderRepository`/`IUnitOfWork` já existentes.
- `src/Domains/Inventory/Repositories/IStockMovementRepository.cs`: adicionar operações mínimas para obter por ID, excluir e consultar a referência de reversão; evitar interface genérica nova.
- `src/Infra/Repositories/StockMovementRepository.cs`: implementar as operações com EF Core, invalidação dos caches de saldo/listagem e consultas canceláveis.
- `src/App/Views/Inventory/Movements.cshtml`: adicionar ação por linha somente para usuários com capacidade de escrita, confirmação e token antiforgery.
- `tests/Craftsman.Tests/Inventory/InventoryAppServiceTests.cs`: cobrir regras do caso de uso com doubles das dependências já usadas pelo projeto.
- `tests/Craftsman.Tests/Inventory/InventoryControllerTests.cs`: cobrir autorização/antiforgery por convenção da aplicação e respostas de sucesso/erro.
- `tests/Craftsman.Tests/Inventory/StockMovementTests.cs` e testes de persistência: cobrir transformação do tipo/quantidade e consulta/exclusão/idempotência no repositório.
- Não há alteração de esquema prevista: a referência determinística usa `BusinessReference` existente. Se a persistência exigir unicidade para garantir concorrência, essa necessidade deve ser evidenciada nos testes e tratada por migration criada via `dotnet ef --project src/Infra/Craftsman.Infra.csproj`.

## Alternativas consideradas

- Criar um novo serviço de domínio dedicado: não escolhido inicialmente porque o caso de uso combina o serviço de aplicação existente com repositórios já presentes; só deve ser extraído se os testes demonstrarem lógica pura reutilizável.
- Editar a movimentação original para inverter tipo/quantidade: rejeitado porque viola a rastreabilidade e a constituição do projeto.
- Sempre criar compensação, inclusive para a saída sem referência de `Production consumption`: rejeitado pela exceção de negócio aprovada.
- Permitir que a interface determine a elegibilidade: rejeitado; a validação precisa ocorrer no servidor para impedir bypass.
- Usar referência livre digitada pelo usuário: rejeitado; a referência deve ser determinística e derivada do ID original.

## Riscos e compatibilidade

- Concorrência pode gerar duas compensações se a verificação e a gravação não compartilharem uma fronteira transacional/garantia de unicidade; mitigar com transação existente, referência determinística e tratamento de conflito.
- A exclusão física da exceção reduz a trilha histórica, mas é explicitamente aprovada apenas para saída sem referência com motivo exato.
- A compensação de uma entrada ou ajuste pode reduzir saldo; validar saldo antes da persistência e manter a operação atômica.
- A presença de tarefas concluídas precisa ser consultada no estado atual do pedido; não confiar em dados enviados pelo formulário.
- O motivo da saída original pode conter variação de capitalização/espaços; a exceção aprovada é igualdade após a normalização já feita pelo domínio, sem ampliar para comparação heurística.
- Logs devem registrar operação, ID da movimentação e resultado sem incluir segredos, payloads sensíveis ou stack trace na resposta.

## Estratégia de testes

- Unitários do domínio/transformação: inversão de entrada, saída e ajuste; preservação de material, custo, pedido e quantidade; validação da exceção literal.
- Unitários do serviço: movimentação inexistente, pedido com tarefa concluída, pedido entregue, saída elegível para exclusão, saída com referência, saída com outro motivo, entrada, ajuste, saldo insuficiente, movimentação sem pedido e repetição idempotente.
- Integração/persistência: consulta por ID, exclusão da exceção, criação da compensação, referência persistida, saldo recalculado e invalidação de cache.
- Controller/UI: POST protegido por Write e antiforgery, ação apresentada na listagem, confirmação, redirect/sucesso e mensagem segura em falha esperada.
- Regressão: suíte existente de inventário, produção, entrega e segurança; depois executar os gates configurados de build, testes, vulnerabilidades e cobertura.
- Concorrência: validar idempotência sequencial, referência determinística e delegação à unidade serializável com doubles/provider atual. Não criar ou exigir um teste concorrente real contra PostgreSQL nesta entrega.

## Decisões de escopo pós-revisão

- `FINDING-001`: teste concorrente real contra PostgreSQL fica fora do escopo aprovado desta entrega. O risco residual deve ser aceito por responsável humano; não será criada tarefa de implementação para esse teste.
- `FINDING-002`: corrigido neste plano/tarefas por meio de IDs explícitos, permitindo validação automática sem falso gap.
- Após alterações de código, configuração ou artefatos de requisitos, executar novamente os gates aplicáveis; a ausência do teste PostgreSQL deve permanecer visível como limitação, não como evidência de concorrência validada.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001`, `FR-009` | `AC-001`, `AC-010` | `TASK-003`, `TASK-005` |
| `FR-002` | `AC-002`, `AC-003` | `TASK-002`, `TASK-005` |
| `FR-003` | `AC-002`, `AC-003` | `TASK-002`, `TASK-005` |
| `FR-004` | `AC-004` | `TASK-002`, `TASK-004`, `TASK-005` |
| `FR-005`, `FR-006` | `AC-005`, `AC-006`, `AC-007` | `TASK-001`, `TASK-002`, `TASK-004`, `TASK-005` |
| `FR-007` | `AC-006`, `AC-009` | `TASK-002`, `TASK-004`, `TASK-005` |
| `FR-008` | `AC-008` | `TASK-002`, `TASK-004`, `TASK-005` |
| `NFR-001` | `AC-008`, `AC-009` | `TASK-002`, `TASK-004`, `TASK-005` |
| `NFR-002` | `AC-008` | `TASK-002`, `TASK-005` |
| `NFR-003`, `NFR-004` | `AC-007`, `AC-010` | `TASK-002`, `TASK-003`, `TASK-005` |
| `CON-001`, `CON-002`, `CON-003`, `CON-004`, `CON-005` | `AC-004`, `AC-005`, `AC-006`, `AC-007`, `AC-009` | `TASK-001`, `TASK-002`, `TASK-004`, `TASK-005` |

