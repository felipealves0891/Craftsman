<!-- spectro-managed: true -->
# Plano: Ações de tarefas na produção do detalhe do pedido

## Contexto e objetivo técnico

- A aba `Producao` de `src/UI/Components/Orders/OrderDetailsPage.razor` já recebe `ProductionTasks` do `OrderDetailViewModel`, mas renderiza apenas tarefa, produto, quantidade e status.
- A agenda em `src/UI/Components/Production/ProductionIndexPage.razor` já apresenta os três controles de avanço (`start`, `complete` e `cancel`), com habilitação baseada em `CanStart`, `CanComplete` e `CanCancel`, e envia POST protegido para `/Production/Advance`.
- `ProductionController.Advance` exige a política de escrita e antiforgery, chama `ProductionScheduleService.AdvanceAsync` e atualmente redireciona para a agenda. A nova origem precisa preservar o contrato de mutação e retornar ao detalhe do pedido que iniciou a operação.
- Objetivo técnico: adicionar os controles equivalentes na tabela do detalhe, confirmar cancelamento, bloquear ações incompatíveis e manter o contexto do detalhe após sucesso ou falha.

## Solução proposta

- Estender o modelo de linha de tarefa usado por `OrderDetailsPage` com as capacidades de ação necessárias, derivadas exclusivamente do status atual e das regras já observáveis no calendário/domínio.
- Renderizar uma coluna `Acoes` por linha, usando os mesmos valores de ação, ícones/rótulos acessíveis e estados habilitado/desabilitado da agenda.
- Para `Cancelar`, adicionar confirmação explícita no formulário/controle somente quando o botão estiver habilitado. O cancelamento incompatível continua bloqueado e não deve abrir confirmação executável.
- Reutilizar `/Production/Advance` para a mutação. A submissão originada no detalhe deve carregar apenas um contexto de retorno controlado pelo servidor, associado ao pedido da página; não aceitar uma URL arbitrária fornecida pelo cliente.
- Fazer o controller obter/usar o pedido relacionado à tarefa para redirecionar ao detalhe correto após a ação. Em falha, preservar a mensagem segura e retornar ao mesmo detalhe quando o contexto for válido; manter o redirecionamento atual da agenda para chamadas sem contexto de detalhe.
- Recarregar o detalhe através do fluxo normal de `OrdersController.Details`, garantindo que tarefa e pedido sejam consultados novamente após a operação.

## Arquitetura e contratos

- `OrderQueryService.GetDetailAsync`: permanece a fonte dos dados do detalhe e continua selecionando apenas tarefas cujo `OrderId` corresponde ao pedido consultado.
- `OrderDetailsPage.razor`: recebe as linhas de tarefa e o token antiforgery já usado pela página; renderiza controles POST por tarefa e contexto de retorno do detalhe.
- `ProductionTaskRow`/modelo equivalente: expõe identificador, status e capacidades de `start`, `complete` e `cancel` sem duplicar transições de domínio.
- `ProductionController.Advance`: mantém `[Authorize(Policy = ApplicationPolicies.Write)]` e `[ValidateAntiForgeryToken]`; aceita o contexto opcional de retorno, valida sua associação com a tarefa e redireciona para `OrdersController.Details` apenas quando essa associação for confirmada.
- `ProductionScheduleService.AdvanceAsync`: continua sendo responsável por carregar a tarefa, executar `Start`, `Complete` ou `Cancel`, iniciar o pedido quando aplicável e persistir a alteração. Se necessário para o retorno contextual, deve fornecer ao controller o `OrderId` efetivamente associado à tarefa.
- Persistência: nenhuma migration, alteração de esquema ou novo registro é necessário.
- Concorrência: o status é validado novamente no domínio no momento do POST; uma mudança entre renderização e submissão deve resultar em erro seguro, sem redirecionamento apresentado como sucesso.
- Observabilidade: preservar a auditoria `ProductionAdvanced` existente, registrando a ação efetivamente processada sem incluir token, URL arbitrária ou dados sensíveis.

## Alternativas consideradas

- Criar um endpoint separado apenas para o detalhe: rejeitado por duplicar autorização, antiforgery, validação e auditoria do avanço existente.
- Aceitar `returnUrl` livre no POST: rejeitado por introduzir risco de open redirect e por não ser necessário; o retorno deve ser contextual e validado pelo servidor.
- Redirecionar sempre para a agenda: não atende à decisão de manter o usuário na tela atual do detalhe.
- Implementar novas regras de habilitação na UI: rejeitado; a UI deve espelhar as capacidades já calculadas pelo calendário/domínio e o domínio continua sendo a autoridade final.

## Riscos e compatibilidade

- O endpoint atual não recebe explicitamente o pedido; a implementação deve evitar confiar somente em um `orderId` oculto e confirmar a associação com a tarefa antes do redirecionamento.
- A confirmação de cancelamento é uma proteção de interface, não substitui a validação de `ProductionTask.Cancel()` no servidor.
- O controle desabilitado não é uma barreira de segurança; autorização, antiforgery e validação de estado permanecem obrigatórios no POST.
- A tabela usa um componente Razor diferente da agenda; testes devem verificar que os rótulos, estados e nomes de ação não divergem.
- Não há mudança prevista de banco, dependências ou regras de produção. Se a implementação descobrir que o retorno contextual exige uma alteração de contrato público incompatível, deve interromper e registrar mudança de escopo antes de prosseguir.

## Estratégia de testes e validação

- Testes de renderização/UI: coluna de ações, três controles por tarefa, associação do `id`, token antiforgery/contexto de retorno, estados habilitado/desabilitado e confirmação de cancelamento.
- Testes de serviço/domínio: preservar as transições existentes para iniciar, concluir e cancelar, incluindo rejeições por estado incompatível e atualização do pedido ao iniciar.
- Testes de controller/integrados: POST contextual para cada ação retorna ao detalhe do mesmo pedido; falhas retornam ao detalhe com mensagem; POST sem contexto mantém o comportamento da agenda; contexto que não corresponde à tarefa não redireciona indevidamente.
- Testes de segurança: ausência de antiforgery ou política de escrita não altera dados; não aceitar URL arbitrária; confirmar que tarefa de outro pedido não pode ser usada para forjar o destino do detalhe.
- Aceitação manual: abrir um pedido com múltiplas tarefas, executar iniciar/concluir/cancelar, verificar confirmação de cancelar, estados desabilitados e permanência na tela atual.
- Gates finais conforme `.spectro/config.yaml`: build com warnings-as-errors, testes, vulnerabilidades de pacotes e cobertura.

## Rastreabilidade

| Requisito | Critérios de aceite | Tarefas |
|---|---|---|
| `FR-001`, `FR-002`, `FR-007` | `AC-001`, `AC-002`, `AC-006`, `AC-013` | `TASK-001`, `TASK-002`, `TASK-004` |
| `FR-003` | `AC-003`, `AC-004`, `AC-005` | `TASK-001`, `TASK-002`, `TASK-003` |
| `FR-004`, `FR-005` | `AC-006`, `AC-007`, `AC-008`, `AC-009` | `TASK-003`, `TASK-004` |
| `FR-006` | `AC-010`, `AC-011` | `TASK-003`, `TASK-004` |
| `FR-008` | `AC-014` | `TASK-002`, `TASK-004` |
| `NFR-001`, `NFR-003`, `CON-001`, `CON-002`, `CON-003`, `CON-005` | `AC-002`, `AC-006`, `AC-012` | `TASK-003`, `TASK-004` |
| `NFR-002` | `AC-002`, `AC-003`, `AC-004`, `AC-005`, `AC-014` | `TASK-002`, `TASK-004` |
| `NFR-004`, `CON-004` | `AC-005`, `AC-011`, `AC-014` | `TASK-001`, `TASK-003`, `TASK-004` |

