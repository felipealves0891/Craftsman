<!-- spectro-managed: true -->
# Plano: Testes de integração dos controllers de pedidos e produção

## Contexto e objetivo técnico

O repositório possui testes de domínio, serviços e controllers instanciados diretamente, além de testes de persistência com `UseInMemoryDatabase`. Não foi localizado um harness HTTP com `WebApplicationFactory` ou `TestServer`. O objetivo deste plano é adicionar uma camada de testes que atravesse roteamento MVC, filtros de autorização/antiforgery, controllers, serviços e persistência InMemory para os endpoints aprovados de `OrdersController` e `ProductionController`.

O escopo não inclui `Import`, integrações externas, concorrência distribuída ou diferenças específicas do PostgreSQL. Concorrência e rollback serão exercitados somente nos limites internos da aplicação e devem declarar as limitações do provedor InMemory.

## Solução proposta

1. Criar um harness de teste HTTP no projeto `tests/Craftsman.Tests`, reutilizando `Program` e as extensões de DI existentes quando possível.
2. Substituir o registro de `AppDbContext` do host de teste por um banco InMemory com nome único por cenário, preservando os repositórios e serviços reais.
3. Evitar migrações relacionais no host de teste; se o startup atual impedir a inicialização com InMemory, introduzir o menor seam de testabilidade possível, isolado por ambiente de teste e sem alterar regras de produção.
4. Criar autenticação de teste controlada por claims/roles (`Admin`, `Operador`, `Consulta`) e obter antiforgery por fluxo HTTP, sem credenciais reais.
5. Criar builders/fixtures de dados para pedidos, produtos, fichas técnicas, matérias-primas, estoque, tarefas, remessas e usuários, mantendo cada cenário isolado.
6. Cobrir primeiro leituras e mutações normais; depois rejeições, segurança, idempotência e efeitos persistidos.
7. Cobrir concorrência/rollback com mecanismos internos controlados pela aplicação, sem alegar que o InMemory prova locks, isolamento serializável ou comportamento específico do PostgreSQL.

## Arquitetura e contratos

### Componentes existentes

- `src/App/Program.cs`: composição do host, autenticação, autorização, antiforgery e rotas MVC.
- `src/App/Controllers/OrdersController.cs`: `Index`, `Details`, `SendToProduction`, `RevertProduction` e `Delete`.
- `src/App/Controllers/ProductionController.cs`: `Index`, `CurrentMonth`, `Calendar` e `Advance`.
- `src/Infra/Services/ServiceCollectionExtensions.cs`: registro de `AppDbContext` com PostgreSQL e serviços de infraestrutura.
- `tests/Craftsman.Tests/Craftsman.Tests.csproj`: já referencia `Microsoft.EntityFrameworkCore.InMemory` e os projetos da aplicação.

### Componentes de teste previstos

- Harness HTTP compartilhado e fixture de host.
- Handler de autenticação de teste e usuários por role.
- Fixture de antiforgery para POSTs.
- Factory/builder de dados persistidos e helpers de leitura pós-requisição.
- Classes de teste separadas por controller e por fluxo, preferencialmente em `tests/Craftsman.Tests/Integration/Controllers`.

### Fluxo de dados

Requisição HTTP → middleware/roteamento → autenticação e autorização → antiforgery (POST) → controller → serviço real → repositórios/`AppDbContext` InMemory → resposta HTTP e efeitos persistidos.

Para concorrência/rollback, o teste deve observar o resultado no banco InMemory e nos registros de auditoria/estoque/tarefas, usando falhas controladas nas fronteiras internas já existentes. Não deve simular um sucesso de transação relacional que o provedor não suporta.

### Contratos observáveis

- Consultas: status, view/partial/json retornado e dados de pedidos/tarefas.
- Mutação: redirect, `TempData`, status do pedido/tarefa, tarefas criadas/removidas, movimentos de estoque, auditoria e ausência de duplicação.
- Segurança: desafio/redirecionamento de anônimo, acesso negado para role sem `Write` e rejeição de antiforgery inválido.
- Falhas: mensagens seguras e nenhuma alteração parcial observável quando o contrato da aplicação garantir isso.

## Alternativas consideradas

- **Instanciar controllers diretamente:** já usado no repositório, mas não valida pipeline HTTP, autenticação, autorização ou antiforgery; não atende `NFR-001`.
- **PostgreSQL real/efêmero:** seria mais fiel ao runtime, mas foi rejeitado pela decisão `CL-003` por custo operacional e por não ser necessário nesta feature.
- **Adicionar um novo servidor externo ou container:** rejeitado por `CON-004`; a solução deve permanecer autocontida nos testes.
- **Usar apenas mocks dos serviços:** insuficiente para validar persistência e efeitos relacionados; doubles ficam restritos a falhas controladas de fronteiras necessárias aos cenários de rollback/concorrência.

## Riscos e compatibilidade

- O projeto alvo dos testes e da aplicação é `net10.0`, enquanto a constituição menciona .NET 8; o plano preserva o target existente e não propõe migração.
- O InMemory não reproduz constraints, SQL, locks, isolamento serializável, deadlocks ou migrações PostgreSQL. Esses limites devem aparecer nos testes e na entrega, sem falsa garantia.
- O startup chama `Database.MigrateAsync`; essa inicialização pode falhar com InMemory. A implementação deve resolver isso no host de teste ou propor uma mudança mínima de testabilidade antes de criar a suíte.
- Autorização por recurso/tenant não foi demonstrada na documentação; os testes cobrirão autenticação e policies globais, registrando a limitação.
- Falhas simuladas podem exigir um seam existente ou uma substituição de serviço no host de teste. Não criar abstrações de produção sem evidência de necessidade.

## Estratégia de testes

- **Consultas:** `Orders.Index`, `Orders.Details`, `Production.Index`, `Production.CurrentMonth` e `Production.Calendar`, incluindo datas controladas por `TimeProvider`, filtros, `404`, partial/json e cabeçalhos `NoStore` quando observáveis.
- **Pedidos:** `SendToProduction`, `RevertProduction` e `Delete`, cobrindo sucesso, pré-condições inválidas, estoque/produto/BOM, remessa/tarefa concluída, idempotência, auditoria, `TempData` e efeitos persistidos.
- **Produção:** `Advance` para `start`, `complete` e `cancel`, estados inválidos, tarefa inexistente, transição do pedido e auditoria.
- **Segurança:** anônimo, role `Consulta` em operação de escrita, role autorizada, antiforgery ausente/inválido e ausência de efeitos após rejeição.
- **Concorrência/rollback:** cenários internos controlados, verificando conflito, não duplicação e consistência após falha; não testar claims sobre PostgreSQL que o InMemory não possa demonstrar.
- **Verificação:** executar build, testes, vulnerabilidades e cobertura conforme `.spectro/config.yaml`; registrar comandos e resultados reais.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001`, `NFR-001` | `AC-001` | `TASK-002`, `TASK-003` |
| `FR-002` | `AC-002`, `AC-003`, `AC-004` | `TASK-004` |
| `FR-003` | `AC-005` | `TASK-005` |
| `FR-004` | `AC-006`, `AC-007` | `TASK-004`, `TASK-005` |
| `FR-005` | `AC-008`, `AC-009` | `TASK-003`, `TASK-004`, `TASK-005`, `TASK-006` |
| `FR-006` | `AC-010` | `TASK-004`, `TASK-005`, `TASK-006` |
| `FR-007`, `NFR-002`, `NFR-004` | `AC-011` | `TASK-001`, `TASK-006` |
| `FR-007`, `NFR-003` | `AC-014` | `TASK-006` |
| `FR-006` | `AC-012` | `TASK-006` |
| `FR-004`, `FR-006` | `AC-013` | `TASK-006` |

