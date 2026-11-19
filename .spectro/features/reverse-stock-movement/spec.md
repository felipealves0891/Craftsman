<!-- spectro-managed: true -->
# Especificação: Reverter movimentação de estoque

## Problema e objetivo

- Problema: O sistema registra movimentações de estoque que podem precisar ser desfeitas, mas não oferece uma operação explícita de reversão com regras de negócio para preservar o saldo e a rastreabilidade. Uma exceção é necessária para saídas de consumo de produção sem referência, que representam um registro descartável quando não há vínculo a preservar.
- Objetivo: Permitir a reversão controlada de uma movimentação de estoque, impedindo a operação quando o pedido relacionado estiver concluído ou entregue, excluindo somente a saída de `Production consumption` sem referência e, nos demais casos, registrando uma nova movimentação compensatória vinculada à movimentação original.

## Escopo

### Incluído

- Disponibilizar uma ação explícita para solicitar a reversão de uma movimentação existente.
- Validar a existência da movimentação e, quando houver pedido associado, a elegibilidade do pedido antes de alterar o estoque.
- Rejeitar a reversão de movimentações associadas a pedidos com alguma tarefa de produção concluída ou com status `Delivered`.
- Excluir uma movimentação somente quando ela for de saída, não possuir referência e tiver motivo exatamente igual a `Production consumption`.
- Para os demais casos elegíveis, criar uma movimentação de reversão com efeito oposto, preservando a rastreabilidade até a movimentação revertida.
- Manter o histórico original quando a correção ocorrer por uma nova movimentação.
- Impedir efeitos parciais e duplicidade em novas tentativas da mesma reversão.
- Cobrir os fluxos de sucesso, rejeição, limites de saldo, rastreabilidade e repetição.

### Excluído

- Reverter movimentações quando o pedido relacionado estiver concluído ou entregue.
- Apagar movimentações históricas para simular uma compensação, exceto pela regra explícita de exclusão da saída sem referência com motivo `Production consumption`.
- Alterar pedidos, tarefas de produção, envios ou acertos financeiros como efeito automático da reversão.
- Alterar a forma como movimentações originais são criadas ou alterar motivos existentes que não sejam os necessários para identificar a reversão.
- Criar um novo status de pedido `Completed` ou alterar os estados de pedido/produção existentes.

## Requisitos

### Requisitos funcionais

- `FR-001`: O sistema deve disponibilizar uma operação explícita para solicitar a reversão de uma movimentação de estoque existente.
- `FR-002`: Antes de concluir a reversão, o sistema deve validar a movimentação alvo e, quando ela possuir `OrderId`, verificar se o pedido relacionado não possui tarefa de produção em `ProductionTaskStatus.Completed` e não está em `OrderStatus.Delivered`.
- `FR-003`: O sistema deve rejeitar a reversão quando o pedido relacionado possuir tarefa de produção concluída ou estiver entregue, sem excluir a movimentação original nem criar uma compensatória.
- `FR-004`: Quando a movimentação alvo for do tipo saída, não possuir `BusinessReference` e possuir o motivo exatamente `Production consumption`, o sistema deve excluí-la como resultado da reversão.
- `FR-005`: Quando a movimentação alvo for elegível e não atender integralmente à exceção de `FR-004`, o sistema deve criar uma nova movimentação de reversão com efeito de estoque oposto ao da movimentação alvo, mantendo material, quantidade e custo compatíveis com o registro revertido; para `Adjustment`, o tipo deve continuar sendo `Adjustment` e a quantidade deve ser invertida.
- `FR-006`: A movimentação compensatória deve usar o motivo literal `Stock movement reversal` e referenciar de forma inequívoca a movimentação revertida; a movimentação original não deve ser alterada ou excluída nesse caminho.
- `FR-007`: A reversão deve preservar a consistência do saldo e continuar impedindo saldo negativo conforme as regras existentes para saídas.
- `FR-008`: Uma movimentação já revertida não deve produzir nova exclusão ou nova compensação quando a mesma operação for repetida.
- `FR-009`: O resultado da operação deve informar sucesso ou rejeição de forma segura e permitir identificar a movimentação alvo e o efeito realizado. A operação deve usar a mesma permissão já exigida para registrar movimentações de estoque.

### Requisitos não funcionais

- `NFR-001`: A decisão de elegibilidade e o efeito da reversão devem ser persistidos atomicamente; uma falha não pode deixar uma compensação criada sem a conclusão correspondente ou apresentar sucesso falso.
- `NFR-002`: A operação deve tratar concorrência entre duas solicitações de reversão da mesma movimentação sem criar compensações duplicadas nem excluir um registro já tratado de forma indevida.
- `NFR-003`: Mensagens e logs da operação não devem expor credenciais, tokens, SQL, stack traces ou dados sensíveis desnecessários.
- `NFR-004`: O histórico deve permitir relacionar a movimentação original, a reversão, o material, o pedido quando houver, o motivo e a referência de negócio.

### Restrições

- `CON-001`: A solução deve respeitar a separação entre App, Domains e Infra, mantendo a regra de negócio fora da persistência e preservando os contratos atuais de .NET 8, Entity Framework Core e PostgreSQL.
- `CON-002`: O estoque deve continuar sendo derivado de movimentações auditáveis; a reversão não deve editar registros históricos para recalcular o saldo.
- `CON-003`: A exclusão excepcional só pode ocorrer quando tipo, ausência de referência (`BusinessReference` nula ou vazia) e motivo coincidirem simultaneamente; qualquer divergência deve seguir o caminho de nova movimentação compensatória ou ser rejeitada pela regra de elegibilidade aplicável.
- `CON-004`: A operação deve respeitar as validações existentes de quantidade, tipo, custo e saldo, sem permitir que uma reversão gere saldo negativo.
- `CON-005`: O motivo da movimentação compensatória deve ser exatamente `Stock movement reversal` e a referência deve apontar para a movimentação revertida, sem depender somente de texto livre apresentado ao usuário.

## Critérios de aceite

- `AC-001` (`FR-001`, `FR-002`): Dada uma movimentação existente e elegível, quando um usuário solicitar sua reversão, então o sistema executa a operação e informa o resultado sem alterar movimentações não relacionadas.
- `AC-002` (`FR-003`): Dada uma movimentação associada a um pedido que possua tarefa de produção em `ProductionTaskStatus.Completed`, quando a reversão for solicitada, então a operação é rejeitada, a movimentação permanece intacta e nenhuma compensação é criada.
- `AC-003` (`FR-003`): Dada uma movimentação associada a um pedido em `OrderStatus.Delivered`, quando a reversão for solicitada, então a operação é rejeitada, a movimentação permanece intacta e nenhuma compensação é criada.
- `AC-004` (`FR-004`): Dada uma saída sem `BusinessReference` cujo motivo seja exatamente `Production consumption`, quando a reversão for concluída, então a movimentação alvo é excluída e não é criada uma segunda movimentação para compensá-la.
- `AC-005` (`FR-005`, `FR-006`): Dada uma saída elegível que possua referência ou tenha motivo diferente de `Production consumption`, quando a reversão for concluída, então a saída original permanece no histórico e é criada uma entrada de reversão com a mesma matéria-prima, quantidade e custo compatível, motivo de reversão e referência inequívoca à origem.
- `AC-006` (`FR-005`, `FR-007`): Dada uma entrada ou ajuste elegível, quando a reversão for concluída, então é criada uma movimentação de efeito oposto — saída para entrada ou ajuste com quantidade oposta para ajuste — sem apagar o registro original e sem permitir saldo negativo.
- `AC-007` (`FR-006`, `NFR-004`): Dada uma reversão concluída pelo caminho compensatório, quando o histórico for consultado, então é possível localizar a movimentação original a partir da compensatória e a compensatória a partir da original.
- `AC-008` (`FR-008`, `NFR-001`, `NFR-002`): Dada uma movimentação já revertida, quando a operação for repetida, então nenhuma nova compensação é criada, nenhum registro já processado é excluído indevidamente e o resultado permanece consistente.
- `AC-009` (`FR-007`): Dada uma reversão cujo efeito oposto seria uma saída sem saldo suficiente, quando ela for solicitada, então a operação é rejeitada sem persistir efeitos parciais.
- `AC-010` (`FR-009`, `NFR-003`): Dada uma solicitação inválida ou feita por usuário sem a permissão já exigida para registrar movimentações, quando a reversão for tentada, então nenhum dado é alterado e a resposta não expõe detalhes internos.

## Riscos, dependências e perguntas abertas

- Dependência: O repositório de movimentações precisa localizar a movimentação por identificador e detectar se já existe uma reversão para ela de maneira consistente com o modelo atual de `BusinessReference`.
- Dependência: A operação precisa resolver o pedido associado por `OrderId`, verificar tarefas em `ProductionTaskStatus.Completed` e utilizar `OrderStatus.Delivered` como estado de entrega.
- Risco: Excluir a saída sem referência preserva menos histórico do que uma compensação; a exceção deve ser claramente identificável e auditável para evitar exclusões indevidas.
- Risco: Uma reversão de entrada pode reduzir o saldo; sem transação e validação concorrente, duas reversões podem gerar saldo inconsistente.
- Risco: A referência da compensatória pode colidir com referências existentes se não houver uma convenção determinística e unicidade adequada; a implementação deve usar uma referência derivada do identificador da movimentação original e garantir idempotência.
- Decisão: “Pedido concluído” significa possuir ao menos uma tarefa em `ProductionTaskStatus.Completed`; “pedido entregue” significa `OrderStatus.Delivered`.
- Decisão: Ajustes são revertidos por novo ajuste com quantidade oposta.
- Decisão: O motivo literal das compensações é `Stock movement reversal`.
- Decisão: A reversão reutiliza a permissão existente para registrar movimentações.
- Decisão: `BusinessReference` nula ou vazia significa ausência de referência; movimentações sem `OrderId` não têm bloqueio baseado em estado de pedido.

