<!-- spectro-managed: true -->
# Revisão: Reverter envio do pedido para produção

## Resumo

- A implementação cobre o fluxo principal de reversão, compensação idempotente de estoque, exclusão de tarefas, retorno a `Normalized`, autorização, auditoria e replanejamento.
- Os gates obrigatórios registrados em `gates.json` passaram: build, testes, segurança e cobertura.
- A rastreabilidade está completa: todos os requisitos e critérios de aceite possuem tarefas e indicação de validação.
- Foi identificado um caso de fronteira em que a validação de elegibilidade existe na interface, mas não está completa no endpoint.

## Escopo e evidências

- Especificação: `.spectro/features/revert-order-production-planning/spec.md`
- Esclarecimentos: `.spectro/features/revert-order-production-planning/clarifications.md`
- Plano: `.spectro/features/revert-order-production-planning/plan.md`
- Tarefas: `.spectro/features/revert-order-production-planning/tasks.md`
- Gates: `.spectro/features/revert-order-production-planning/gates.json`
- Pacote independente: `.spectro/features/revert-order-production-planning/reviews/review-20261107T183000Z/`
- Revisão do código: `git diff 39c7e97^..a88e084`
- Evidência de gates: execução `gate-20261107T182411Z`, revisão de código `a88e084`; a diferença posterior contém apenas os artefatos de gates e estado.

## Requisitos verificados

- `FR-001`: ação POST com autorização `Write` e antiforgery; confirmado por código e testes de política/UI.
- `FR-002` e `CON-002`: compensação dos movimentos de saída, exclusão das tarefas e retorno para `Normalized`; confirmado pelos testes de reversão.
- `FR-003`: movimentos originais preservados e compensações rastreáveis por `BusinessReference`; confirmado no serviço e nos testes.
- `FR-004` e `CON-005`: tarefa `Completed` bloqueia a operação sem efeitos parciais; confirmado por teste.
- `FR-005`: repetição após reversão não duplica compensação; confirmado por teste.
- `FR-006` e `NFR-004`: ação específica de auditoria com correlação existente; confirmado por controller e integração transacional.
- `FR-007`: ação aparece somente para estados elegíveis e as tarefas deixam de aparecer após exclusão; confirmado por componente e fluxo de consulta.
- `NFR-001` e `NFR-002`: unidade transacional e tratamento de conflito previstos na implementação; gates de build/testes aprovados.
- `NFR-003`: mensagens controladas, sem stack trace ou detalhes de banco; confirmado por testes de controller.

## Findings

| ID | Severidade | Categoria | Localização | Evidência | Impacto | Correção recomendada | Etapa de retorno | Status |
|---|---|---|---|---|---|---|---|---|
| `FINDING-001` | Medium | Functional / Security | `src/Domains/Production/Services/OrderProductionPlanningService.cs:96-150` | `RevertAsync` valida tarefa `Completed` e status do pedido, mas não consulta `IShipmentRepository`. A interface apenas oculta a ação com `!Shipments.Any()` em `src/UI/Components/Orders/OrderDetailsPage.razor`. `ShippingService.CreateAsync` permite criar uma remessa sem alterar o status do pedido. | Uma chamada direta ao endpoint pode reverter tarefas e estoque de um pedido que já possui remessa criada, contrariando o escopo que exclui reversão de envios. | Validar remessas no servidor antes de qualquer compensação/exclusão; rejeitar qualquer pedido com remessa relacionada e adicionar teste de endpoint direto. | `implement` | resolved |

## Riscos residuais

- A política aprovada permite compensar tarefas `InProduction`; isso pode divergir do consumo físico real, embora o comportamento esteja explicitamente registrado e auditado.
- O componente usa rótulos localizados para decidir elegibilidade visual; o servidor continua sendo a autoridade, mas uma futura mudança de tradução pode ocultar ou exibir incorretamente o botão.
- A auditoria e os efeitos principais usam a mesma unidade transacional em produção; a evidência de gates foi gerada antes do commit que adicionou somente os artefatos de gates.

## Decisão

- `approved`
- Justificativa: `TASK-REV-001` adicionou a validação server-side de remessas antes da compensação/exclusão e um teste de regressão do endpoint direto. Os gates foram renovados com sucesso na revisão corrigida.
