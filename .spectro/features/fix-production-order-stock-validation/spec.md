<!-- spectro-managed: true -->
# Especificação: Corrigir validação e consumo de estoque ao enviar pedido para produção

## Problema e objetivo

- Problema: Ao enviar um pedido para produção, o sistema pode reportar matéria-prima insuficiente mesmo quando há no estoque a quantidade necessária pela ficha técnica para o pedido. O mesmo fluxo também pode registrar saídas de estoque embora o planejamento tenha sido rejeitado. Os logs fornecidos mostram falhas em `Orders/SendToProduction` com quantidades requerida/disponível divergentes do histórico de movimentações, e o histórico informado registra duas saídas de 32 sobre uma entrada de 94; essas saídas pertencem à operação que falhou.
- Objetivo: Garantir que o envio para produção use um cálculo consistente e verificável de disponibilidade e que uma tentativa rejeitada por estoque não altere o estoque, não crie tarefas e não confirme o planejamento do pedido.

## Escopo

### Incluído

- Corrigir a determinação da quantidade disponível de cada matéria-prima a partir das movimentações aplicáveis e compará-la com a quantidade necessária pela ficha técnica para o pedido; não há exigência de preservar um estoque mínimo adicional neste contexto.
- Validar todas as matérias-primas necessárias antes de confirmar qualquer efeito do planejamento.
- Impedir saídas parciais ou outras alterações persistidas quando o planejamento for rejeitado por insuficiência de estoque.
- Garantir consistência entre resultado da validação, movimentações de estoque, tarefas de produção e estado do pedido.
- Registrar diagnóstico suficiente para relacionar pedido, matéria-prima, quantidades calculadas, operação e correlação, sem expor dados sensíveis desnecessários.
- Cobrir o cenário relatado e regressões de sucesso, insuficiência e falha intermediária.

### Excluído

- Alterar a fórmula de estoque mínimo ou definir uma nova política de reservas sem decisão de negócio explícita.
- Apagar ou editar movimentações históricas já persistidas; as duas saídas indevidas identificadas devem permanecer rastreáveis e eventual compensação deve ser uma operação auditável separada.
- Alterar regras de ficha técnica, vínculo de produtos, deduplicação de pedidos ou cálculo financeiro, salvo o mínimo necessário para preservar a consistência deste fluxo.
- Permitir estoque negativo, consumo parcial deliberado ou planejamento parcial de um pedido.
- Implementar mecanismo novo de autorização ou alterar permissões existentes.

## Requisitos

### Requisitos funcionais

- `FR-001`: Ao enviar um pedido para produção, o sistema deve calcular a disponibilidade de cada matéria-prima usando as movimentações aplicáveis e compará-la com a quantidade necessária pela ficha técnica para o pedido, com unidade e precisão coerentes.
- `FR-002`: O sistema deve rejeitar o planejamento somente quando a disponibilidade real de alguma matéria-prima for menor que a quantidade necessária pela ficha técnica, identificando as matérias-primas e quantidades relevantes no resultado de diagnóstico seguro.
- `FR-003`: O sistema deve concluir a validação de todas as matérias-primas antes de persistir tarefas, alterar o estado do pedido ou registrar saídas de estoque decorrentes do planejamento.
- `FR-004`: Quando o planejamento for rejeitado por estoque insuficiente ou por falha durante a operação, o sistema não deve persistir saídas parciais, tarefas parciais ou estado de produção que indique planejamento concluído.
- `FR-005`: Quando todas as matérias-primas forem suficientes, o sistema deve manter o comportamento de sucesso existente: criar as tarefas aplicáveis, registrar os consumos correspondentes e atualizar o pedido conforme a transição vigente.
- `FR-006`: Uma nova tentativa do mesmo envio, após uma rejeição sem efeitos confirmados, deve poder reavaliar o estado atual do estoque sem duplicar saídas ou tarefas da tentativa anterior; movimentações indevidas de uma tentativa histórica não devem ser tratadas como disponibilidade válida sem uma decisão de ajuste auditável.
- `FR-007`: O diagnóstico do envio deve permitir relacionar pedido, matéria-prima, quantidade requerida, quantidade disponível calculada, resultado, etapa e identificador de correlação, sem registrar segredos ou dados sensíveis desnecessários.

### Requisitos não funcionais

- `NFR-001`: A operação deve preservar consistência transacional entre validação, tarefas, movimentações de estoque e estado do pedido; uma falha não pode ser apresentada como sucesso.
- `NFR-002`: O cálculo e o consumo devem tratar concorrência com outras movimentações ou planejamentos, impedindo que uma disponibilidade obsoleta resulte em estoque negativo ou confirmação incompatível.
- `NFR-003`: As mensagens ao usuário devem ser seguras e úteis, sem stack trace, SQL, credenciais, tokens, segredos ou detalhes internos desnecessários.
- `NFR-004`: Os registros de diagnóstico devem ser pesquisáveis pela correlação da requisição e pelo pedido, preservando a rastreabilidade das movimentações sem alterar o histórico.

### Restrições

- `CON-001`: A solução deve respeitar a separação entre App, Domains e Infra e os contratos existentes com Entity Framework Core e PostgreSQL.
- `CON-002`: O estoque continuará sendo derivado de movimentações auditáveis; nenhuma correção deve apagar ou editar movimentações históricas para mascarar o defeito.
- `CON-003`: O fluxo não pode permitir saldo negativo nem confirmar transições de produção antes de todos os pré-requisitos serem satisfeitos.
- `CON-004`: A especificação não escolhe a arquitetura de implementação nem determina em qual camada a transação, validação ou instrumentação devem ser ajustadas.
- `CON-005`: Neste contexto, “quantidade mínima” significa a quantidade de matéria-prima necessária pela ficha técnica para produzir os itens do pedido; não se deve subtrair um estoque mínimo operacional adicional do saldo consumível.

## Critérios de aceite

- `AC-001` (`FR-001`, `FR-002`): Dado um pedido com matérias-primas e movimentações conhecidas, quando o envio para produção for solicitado, então a disponibilidade usada na decisão corresponde ao saldo calculado pelas movimentações válidas e é comparada à quantidade necessária pela ficha técnica, com a mesma unidade e precisão.
- `AC-002` (`FR-002`): Dado um pedido cuja disponibilidade atende à quantidade necessária pela ficha técnica, quando o envio for solicitado, então o planejamento não é rejeitado por insuficiência de estoque e o fluxo de sucesso existente é concluído.
- `AC-003` (`FR-002`, `FR-007`): Dado um pedido com uma ou mais matérias-primas insuficientes, quando o envio for solicitado, então a resposta identifica a causa de forma segura e o diagnóstico registra pedido, matéria-prima, requerida, disponível, resultado e correlação.
- `AC-004` (`FR-003`, `FR-004`, `NFR-001`): Dado um pedido com uma matéria-prima insuficiente, quando o planejamento for rejeitado, então nenhuma saída de estoque, tarefa de produção ou estado de pedido decorrente daquela tentativa é persistido como confirmado.
- `AC-005` (`FR-004`): Dado um pedido com várias matérias-primas em que uma validação falha depois de outra matéria-prima já ter sido avaliada, quando a operação terminar, então não existe consumo parcial nem tarefa parcial persistida.
- `AC-006` (`FR-005`): Dado um pedido em que todas as matérias-primas são suficientes, quando o envio for concluído, então as tarefas, consumos e transição do pedido permanecem consistentes entre si e o histórico de estoque identifica a origem do consumo.
- `AC-007` (`FR-006`): Dada uma tentativa rejeitada sem efeitos confirmados, quando o usuário tentar enviar o pedido novamente após mudança válida no estoque, então a nova tentativa reavalia o saldo atual e não duplica efeitos da tentativa rejeitada.
- `AC-008` (`FR-001`, `FR-004`, `NFR-002`): Dada uma movimentação concorrente que torne o saldo insuficiente após a leitura inicial, quando o envio for confirmado, então a operação rejeita ou revalida de forma segura, sem saldo negativo, consumo incompatível ou confirmação falsa.
- `AC-009` (`NFR-003`, `NFR-004`): Dada uma falha de planejamento, quando o usuário consultar a resposta e os logs, então a resposta não expõe detalhes internos e a tentativa pode ser localizada por pedido e correlação.
- `AC-010` (`CON-002`, `FR-006`): Dado o histórico com entrada de 94 e duas saídas indevidas de 32 da operação que falhou, quando o saldo for consultado ou uma nova tentativa for executada, então o sistema não altera essas movimentações históricas, não as duplica e não as trata como consumo válido de uma tentativa concluída; eventual compensação ocorre somente por operação auditável própria.

## Riscos, dependências e perguntas abertas

- Dependência: O fluxo observado usa `OrdersController.SendToProduction` e `OrderProductionPlanningService`; os pontos exatos de persistência e consumo devem ser confirmados durante o plano.
- Dependência: A regra de disponibilidade deve considerar as movimentações válidas e a quantidade necessária pela ficha técnica, incluindo unidade, arredondamento e vínculo operacional das movimentações.
- Risco: Corrigir somente a mensagem ou somente a validação pode manter o consumo parcial; a solução deve testar o fluxo completo até a persistência.
- Risco: Movimentações indevidas já persistidas podem manter o saldo calculado incorreto até que uma compensação auditável seja registrada; esta especificação não autoriza apagar o histórico.
- Risco: Concorrência entre leitura de disponibilidade e consumo pode reintroduzir o defeito sem controle transacional ou de versão adequado.
- Decisão: “Quantidade mínima” neste fluxo é a quantidade necessária pela ficha técnica para o pedido; não é um estoque mínimo adicional a preservar.
- Decisão: As duas saídas de 32 do histórico informado foram geradas indevidamente pela operação que falhou.
- Decisão: Esta feature deve corrigir a validação e impedir novas remoções indevidas por validações completas, atomicidade e proteção contra concorrência. A correção das movimentações históricas permanece separada e auditável, sem apagar o histórico.

