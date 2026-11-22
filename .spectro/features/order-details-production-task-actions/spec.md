<!-- spectro-managed: true -->
# Especificação: Ações de tarefas na produção do detalhe do pedido

## Problema e objetivo

- Problema: na aba `Producao` da tela de detalhes do pedido, as tarefas planejadas são exibidas somente para consulta. Para iniciar, concluir ou cancelar uma tarefa, o usuário precisa sair do pedido e acessar a agenda de produção.
- Objetivo: disponibilizar, na tabela de tarefas da aba `Producao`, uma coluna de ações com as opções de iniciar, concluir e cancelar, mantendo o mesmo comportamento e as mesmas regras já oferecidas no calendário de produção.

## Escopo

### Incluído

- Adicionar uma coluna de ações à tabela de tarefas da aba `Producao` do detalhe do pedido.
- Exibir as ações `Iniciar`, `Concluir` e `Cancelar` para cada tarefa, conforme a possibilidade da transição no estado atual.
- Executar as ações pelo fluxo protegido já usado pelo calendário de produção.
- Após uma operação, refletir o estado atualizado da tarefa e do pedido no detalhe, além das mensagens de sucesso ou erro previstas pelo fluxo existente.
- Manter a identificação da tarefa, produto, quantidade e status na tabela.

### Excluído

- Alterar as transições ou invariantes da entidade `ProductionTask`.
- Criar novos estados, novos tipos de ação ou um fluxo de produção paralelo ao calendário.
- Alterar o planejamento inicial, consumo de estoque, reversão do planejamento, envio ou acerto financeiro do pedido.
- Exigir ações para tarefas que não estejam sendo exibidas no detalhe do pedido.
- Alterar as permissões gerais de leitura/escrita ou o mecanismo de antiforgery existente.

## Requisitos

### Requisitos funcionais

- `FR-001`: Quando o pedido possuir tarefas de produção exibidas na aba `Producao`, a tabela deve apresentar uma coluna identificada como ações.
- `FR-002`: Para cada tarefa, a coluna de ações deve oferecer as operações de iniciar, concluir e cancelar equivalentes às operações disponíveis no calendário de produção.
- `FR-003`: A disponibilidade de cada operação deve respeitar o estado atual da tarefa e as regras já aplicadas pelo fluxo de produção; uma ação inválida não deve ser executada como válida pela interface.
- `FR-004`: Ao solicitar uma ação, o sistema deve processar a tarefa específica do pedido e persistir a transição usando o comportamento existente de avanço de produção.
- `FR-005`: Ao iniciar uma tarefa, o sistema deve preservar o comportamento existente de iniciar a produção do pedido relacionado quando aplicável.
- `FR-006`: Após sucesso ou rejeição da operação, o usuário deve retornar ao detalhe do mesmo pedido e visualizar o status atualizado ou a mensagem segura de erro correspondente.
- `FR-007`: As ações devem continuar vinculadas à tarefa correta mesmo quando o pedido possuir mais de uma tarefa de produção.
- `FR-008`: Antes de cancelar uma tarefa, o sistema deve solicitar confirmação explícita do usuário; tarefas incompatíveis com cancelamento devem manter o controle bloqueado e não devem permitir confirmação executável.

### Requisitos não funcionais

- `NFR-001`: As operações devem preservar a autorização de escrita e a proteção contra requisições forjadas já exigidas para o avanço de tarefas no calendário.
- `NFR-002`: A interface deve manter rótulos, semântica de acessibilidade e significado visual equivalentes aos controles do calendário, incluindo identificação textual ou acessível para cada ação.
- `NFR-003`: O fluxo deve reutilizar contratos e regras existentes sempre que atenderem ao requisito, sem introduzir dependência externa ou duplicação de regra de transição.
- `NFR-004`: Falhas de estado inválido, tarefa inexistente ou pedido inconsistente não devem produzir uma confirmação falsa nem ocultar a causa segura já definida pelo fluxo.

### Restrições

- `CON-001`: A implementação deve respeitar a separação entre App, Domains, Infra e UI definida na constituição do projeto.
- `CON-002`: As ações devem usar as operações de domínio existentes (`start`, `complete` e `cancel`) e não alterar status diretamente a partir da tela.
- `CON-003`: O endpoint e os controles devem permanecer protegidos por autenticação/autorização de escrita e antiforgery conforme o fluxo atual de `/Production/Advance`.
- `CON-004`: A especificação não define novas regras de negócio para quando uma ação é permitida; a fonte de verdade é o comportamento existente do domínio e do calendário.
- `CON-005`: A mudança deve permanecer limitada ao detalhe do pedido e às integrações necessárias para exibir e processar as ações, sem refatoração não relacionada.

## Critérios de aceite

- `AC-001` (`FR-001`): Dado um pedido enviado para produção com ao menos uma tarefa, quando o usuário abrir a aba `Producao` no detalhe, então a tabela exibirá uma coluna de ações além das colunas existentes.
- `AC-002` (`FR-002`, `NFR-002`): Dada uma tarefa exibida no detalhe, quando a coluna de ações for renderizada, então estarão disponíveis controles identificáveis para `Iniciar`, `Concluir` e `Cancelar`, equivalentes aos controles do calendário.
- `AC-003` (`FR-003`): Dada uma tarefa `Planned`, quando a tabela for renderizada, então `Iniciar` estará habilitado e `Concluir` permanecerá indisponível, respeitando as regras observáveis do calendário/domínio.
- `AC-004` (`FR-003`): Dada uma tarefa `InProduction`, quando a tabela for renderizada, então `Concluir` estará habilitado e `Iniciar` permanecerá indisponível; `Cancelar` seguirá exatamente a disponibilidade existente no calendário.
- `AC-005` (`FR-003`): Dada uma tarefa `Completed` ou `Cancelled`, quando a tabela for renderizada, então não será possível executar uma transição incompatível com o estado da tarefa.
- `AC-006` (`FR-004`, `FR-007`): Dado um pedido com duas ou mais tarefas, quando o usuário acionar uma operação, então somente a tarefa correspondente ao controle acionado será processada.
- `AC-007` (`FR-004`, `FR-005`): Dada uma tarefa elegível, quando o usuário acionar `Iniciar`, então a tarefa passará para o estado de produção conforme o fluxo existente e o pedido relacionado será atualizado conforme a regra atual de início.
- `AC-008` (`FR-004`): Dada uma tarefa em produção, quando o usuário acionar `Concluir`, então a tarefa passará para o estado concluído conforme o fluxo existente.
- `AC-009` (`FR-004`): Dada uma tarefa elegível para cancelamento, quando o usuário acionar `Cancelar`, então a tarefa passará para o estado cancelado conforme o fluxo existente.
- `AC-010` (`FR-006`): Dado um detalhe de pedido aberto, quando uma ação for processada com sucesso, então o usuário permanecerá no detalhe do mesmo pedido e verá a tarefa com o estado atualizado.
- `AC-011` (`FR-006`, `NFR-004`): Dado um estado inválido, tarefa inexistente ou falha do fluxo, quando uma ação for submetida, então o sistema não apresentará sucesso falso, permanecerá no detalhe do pedido e exibirá uma mensagem segura compatível com o comportamento existente.
- `AC-012` (`NFR-001`, `CON-003`): Dado um usuário sem autorização de escrita ou uma requisição sem antiforgery válido, quando uma ação for submetida, então a operação será rejeitada antes de alterar a tarefa ou o pedido.
- `AC-013` (`FR-006`): Dado um pedido sem tarefas ou ainda não enviado para produção, quando a aba `Producao` for exibida, então nenhuma ação de tarefa será apresentada para registros inexistentes.
- `AC-014` (`FR-008`): Dada uma tarefa compatível com cancelamento, quando o usuário acionar `Cancelar`, então o sistema solicitará confirmação explícita e somente submeterá a operação após a confirmação; dada uma tarefa incompatível, o botão permanecerá desabilitado.

## Riscos, dependências e perguntas abertas

- Dependência: a tela de detalhe usa `OrderDetailViewModel.ProductionTasks` e a página Razor `OrderDetailsPage`; a implementação deve confirmar o ponto de composição dos dados e do token antiforgery.
- Dependência: o comportamento de processamento existente está exposto por `ProductionController.Advance` e `ProductionScheduleService.AdvanceAsync`, incluindo as ações `start`, `complete` e `cancel`.
- Dependência: a disponibilidade visual deve permanecer alinhada às regras representadas no calendário (`CanStart`, `CanComplete` e `CanCancel`) e às validações do domínio.
- Risco: uma atualização disparada no detalhe pode deixar dados antigos na página se não houver redirecionamento ou recarga adequada; o aceite exige que o detalhe reflita o resultado após a operação.
- Risco: a operação pode falhar por concorrência ou por mudança de estado entre a renderização e o POST; a interface não deve tratar a falha como sucesso.
- Decisão: a ação `Cancelar` exige confirmação explícita do usuário; ações incompatíveis permanecem desabilitadas.
- Decisão: após qualquer ação, o usuário permanece na tela atual do detalhe do pedido, com os dados e mensagens atualizados.

Revisão de segurança: parcial
Escopo revisado: `OrderDetailsPage`, `ProductionIndexPage`, `ProductionController.Advance`, `ProductionScheduleService.AdvanceAsync`, `ProductionTask` e a constituição Spectro.
Verificações executadas: inspeção estática dos contratos e regras existentes; nenhum teste, scanner de segredos ou verificação de dependências foi executado nesta etapa de especificação.
Achados: nenhum achado crítico/alto identificado na especificação; a implementação deve preservar autorização de escrita, antiforgery, validação de estado e associação da tarefa ao pedido.
Riscos residuais / limitações: não foi validado o comportamento em execução nem a autorização por recurso além do fluxo existente; a especificação não afirma segurança absoluta.
