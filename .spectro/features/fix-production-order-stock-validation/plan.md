<!-- spectro-managed: true -->
# Plano: Corrigir validação e consumo de estoque ao enviar pedido para produção

## Contexto e objetivo técnico

- O fluxo `OrdersController.SendToProduction` chama `OrderProductionPlanningService.TryPlanAsync` e depois registra auditoria. O planner atual percorre os itens, valida e adiciona consumos ao contexto a cada item; uma falha posterior pode deixar movimentos rastreados para o `SaveChangesAsync` executado pela auditoria.
- O objetivo técnico é separar pré-validação de efeitos, agregar a necessidade por matéria-prima, persistir planejamento e consumos como uma operação consistente e impedir que falhas ou concorrência produzam consumo parcial, saldo negativo ou sucesso falso.

## Solução proposta

- Consolidar as necessidades da ordem por `RawMaterialId` antes de validar ou consumir. A quantidade de cada matéria-prima será a soma das quantidades da ficha técnica multiplicadas pelas quantidades dos itens do pedido.
- Validar todos os produtos, fichas técnicas e saldos necessários antes de adicionar tarefas, movimentos ou alterar o pedido. A comparação será entre saldo das movimentações válidas e quantidade necessária pela ficha técnica, sem subtrair estoque mínimo operacional.
- Alterar o fluxo do planner para produzir o conjunto de tarefas e o plano de consumo em memória durante a pré-validação; os efeitos de estoque só serão adicionados depois que nenhuma validação de matéria-prima falhar.
- Reutilizar a unidade de trabalho transacional existente (`ITransactionalUnitOfWork`/`UnitOfWork`) no envio para produção, mantendo planejamento, consumos, tarefas, estado do pedido e auditoria de sucesso na mesma fronteira de confirmação quando aplicável.
- Proteger a leitura/consumo contra concorrência usando mecanismo suportado pelo PostgreSQL e pela persistência atual, com revalidação sob a mesma transação e sem permitir que duas tentativas confirmem consumo incompatível. A implementação deve preferir a menor alteração de contrato necessária e não criar uma nova política de estoque.
- Garantir que uma falha não deixe entidades `Added` pendentes no contexto usado posteriormente para auditoria. Falhas devem retornar resultado seguro e permitir nova tentativa com o saldo persistido atual.
- Manter as duas saídas históricas indevidas intactas e rastreáveis; sua compensação não faz parte deste plano.

## Arquitetura e contratos

- `src/Domains/Inventory/Services/InventoryService.cs`: manter a regra de saldo e disponibilidade, estendendo-a somente se necessário para validar um lote agregado de requisitos e para executar consumo sem efeitos parciais.
- `src/Domains/Production/Services/ProductionPlanner.cs`: mover a validação para antes do consumo e consolidar requisitos por matéria-prima; preservar cálculo de janela, vínculo de produto, ficha técnica e criação de tarefas.
- `src/Domains/Production/Services/OrderProductionPlanningService.cs`: coordenar o fluxo sem persistir efeitos antes da validação, tratar falhas sem deixar mudanças pendentes e manter logs estruturados do pedido, etapa, resultado e quantidades diagnósticas permitidas.
- `src/App/Controllers/OrdersController.cs`: usar a unidade transacional já disponível no envio para produção e manter mensagem de usuário segura; não alterar autorização nem antiforgery existentes.
- `src/Infra/Repositories/UnitOfWork.cs` e repositórios de estoque: aplicar a fronteira transacional e o controle de concorrência na infraestrutura existente, sem apagar ou alterar movimentos históricos.
- `tests/Craftsman.Tests/Inventory`, `tests/Craftsman.Tests/Production` e `tests/Craftsman.Tests/Application`: cobrir cálculo agregado, atomicidade, fluxo HTTP/TempData, auditoria e regressões.
- Não há migração prevista. Se o mecanismo de concorrência escolhido exigir mudança de esquema, o plano deve parar e registrar a necessidade de decisão de escopo antes de implementá-la.

## Alternativas consideradas

- Apenas mover o `SaveChangesAsync` para depois do planner: insuficiente, porque falhas durante consumo ou concorrência ainda podem produzir efeitos parciais.
- Apenas limpar o `ChangeTracker` após uma exceção: reduz o sintoma do `AuditService`, mas não garante que duas operações concorrentes não consumam o mesmo saldo.
- Corrigir somente a mensagem de estoque: rejeitado porque não preserva a integridade entre saldo, tarefas e pedido.
- Criar um fluxo separado para compensar automaticamente as duas saídas históricas: fora do escopo confirmado; o histórico deve ser preservado e qualquer ajuste precisa de operação auditável própria.

## Riscos e compatibilidade

- O projeto usa `AsNoTracking` para leituras de saldo e grava movimentos somente no `SaveChangesAsync`; a implementação precisa evitar que o registro de auditoria persista alterações de uma tentativa falha.
- A unidade de trabalho atual inicia transação explícita, mas o envio para produção ainda não a utiliza; a ordem de `SaveChanges`, publicação de eventos e auditoria deve ser revisada para não publicar sucesso antes da confirmação.
- Testes com `UseInMemoryDatabase` não comprovam locks ou isolamento do PostgreSQL. O cenário de concorrência deve usar uma infraestrutura de integração existente ou ser explicitamente reportado como limitação.
- O histórico já contém saídas indevidas; testes não devem apagá-las nem fazer a nova validação tratá-las como efeitos de uma tentativa recém-confirmada.
- Nenhuma credencial, payload completo de pedido ou dado pessoal deve ser adicionado aos logs.

## Estratégia de testes

- Unitários de inventário: saldo suficiente, saldo insuficiente, quantidade exata, unidade/precisão decimal, matéria-prima inativa e múltiplas necessidades do mesmo material agregadas.
- Unitários do planner: todas as necessidades são validadas antes de qualquer consumo; falha em item posterior não cria movimento/tarefa; sucesso cria tarefas e consumos consistentes.
- Aplicação: envio com saldo suficiente, alerta de estoque baixo sem bloqueio, insuficiência sem efeitos, mensagem segura, reenvio após reposição e auditoria com pedido/resultado/correlação.
- Persistência/transação: rollback quando a gravação falhar e ausência de movimentos pendentes no contexto da auditoria; preservar movimentos históricos.
- Concorrência: duas tentativas sobre o mesmo material não podem confirmar saldo negativo ou consumo incompatível; usar PostgreSQL/integrador disponível para validar o comportamento real.
- Gates obrigatórios: build com warnings-as-errors, testes, vulnerabilidades de pacotes e cobertura conforme `.spectro/config.yaml`.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001`, `FR-002` | `AC-001`, `AC-002` | `TASK-001`, `TASK-004` |
| `FR-003`, `FR-004` | `AC-004`, `AC-005` | `TASK-001`, `TASK-002`, `TASK-004` |
| `FR-005` | `AC-006` | `TASK-001`, `TASK-004`, `TASK-005` |
| `FR-006` | `AC-007`, `AC-010` | `TASK-002`, `TASK-005` |
| `FR-007` | `AC-003`, `AC-009` | `TASK-003`, `TASK-005` |
| `NFR-001` | `AC-004`, `AC-005`, `AC-006` | `TASK-002`, `TASK-005` |
| `NFR-002` | `AC-008` | `TASK-002`, `TASK-005` |
| `NFR-003`, `NFR-004` | `AC-003`, `AC-009` | `TASK-003`, `TASK-005` |
| `CON-001` a `CON-005` | `AC-001` a `AC-010` | `TASK-001` a `TASK-006` |

