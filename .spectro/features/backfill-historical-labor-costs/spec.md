<!-- spectro-managed: true -->
# Especificação: Recalcular custo de horas em acertos históricos

## Problema e objetivo

- Problema: A funcionalidade `add-hourly-labor-cost` passou a calcular o custo das horas em novos acertos, mas os acertos financeiros já existentes foram persistidos sem esse valor. Como consequência, o histórico financeiro apresenta custo de produção, custo total e margem que não refletem o trabalho de produção.
- Objetivo: Reconciliar automaticamente os acertos históricos elegíveis com o custo das horas de produção, preservando a rastreabilidade e atualizando os valores financeiros derivados que dependem desse custo.

## Escopo

### Incluído

- Identificar todos os acertos financeiros criados antes da implantação de `add-hourly-labor-cost` que ainda não foram reconciliados.
- Obter as horas das tarefas de produção relacionadas ao pedido e o `HourlyRate` atualmente cadastrado nos produtos correspondentes.
- Calcular o custo de horas de cada acerto histórico conforme a regra vigente de horas de produção × valor-hora aplicável.
- Atualizar o acerto histórico com o custo de horas e com os valores derivados necessários para que custo total e margem sejam coerentes.
- Executar a reconciliação automaticamente em background, em paralelo aos demais serviços durante a inicialização da aplicação, sem exigir uma ação manual na tela financeira e sem bloquear o atendimento de requisições.
- Tornar a rotina segura para reexecução, evitando acrescentar o mesmo custo de horas mais de uma vez.

### Excluído

- Alterar o planejamento, o apontamento ou a duração das tarefas de produção.
- Alterar a fórmula vigente de receita, desconto, custo de envio ou margem além do efeito do custo de horas.
- Recalcular ou substituir custos de materiais já registrados nas movimentações de estoque.
- Criar um valor-hora global ou alterar o cadastro de `HourlyRate`.
- Corrigir automaticamente registros cuja associação entre acerto, pedido, tarefa ou produto esteja inválida sem uma regra de tratamento aprovada.
- Exigir uma tela ou comando manual para iniciar a reconciliação.

## Requisitos

### Requisitos funcionais

- `FR-001`: A aplicação deve localizar, durante a inicialização, todos os acertos criados antes da implantação de `add-hourly-labor-cost` que ainda não foram reconciliados.
- `FR-002`: Para cada acerto elegível, a aplicação deve calcular o custo das horas a partir das tarefas de produção do pedido e do `HourlyRate` atualmente cadastrado nos produtos associados, usando a regra vigente da funcionalidade `add-hourly-labor-cost`.
- `FR-003`: A aplicação deve persistir no acerto histórico o custo de horas calculado e atualizar custo de produção, custo total e margem quando esses valores forem derivados desse custo.
- `FR-004`: A reconciliação deve preservar receita, desconto, custo de envio, pedido de origem, data do acerto e custos de materiais já registrados, salvo o ajuste estritamente necessário nos valores derivados do custo de horas.
- `FR-005`: A rotina deve ser idempotente: uma nova inicialização ou uma nova tentativa não pode duplicar o custo de horas já incorporado ao acerto.
- `FR-006`: A reconciliação deve manter cada atualização rastreável ao acerto, pedido, tarefas de produção e produtos/valores-hora utilizados no cálculo.
- `FR-007`: A rotina deve permitir que a aplicação conclua sua inicialização mesmo quando houver falhas no cálculo de registros individuais; o acerto com falha não pode ser persistido parcialmente e deve permanecer identificável para nova tentativa.

### Requisitos não funcionais

- `NFR-001`: A rotina deve ser executada de forma assíncrona, em background e em paralelo aos demais serviços da aplicação, sem bloquear a inicialização, o atendimento de requisições ou depender de uma requisição de usuário.
- `NFR-002`: A atualização de cada acerto e seus valores derivados deve ser atômica; uma falha não pode deixar somente parte dos valores financeiros persistida.
- `NFR-003`: O cálculo deve preservar a precisão monetária adotada pelo domínio financeiro e invalidar ou reportar valores de entrada que violem suas invariantes.
- `NFR-004`: Falhas e diagnósticos devem identificar o acerto e a causa da falha sem registrar credenciais, tokens ou outros dados sensíveis.
- `NFR-005`: A mudança deve preservar a separação entre domínio, aplicação e infraestrutura e os gates de build, testes, segurança e cobertura definidos pelo projeto.

### Restrições

- `CON-001`: A fonte do valor-hora deve seguir a decisão vigente de `add-hourly-labor-cost`: `HourlyRate` do produto; não deve ser introduzido valor global em `appsettings`.
- `CON-002`: As horas devem ser as horas de produção já registradas nas tarefas relacionadas ao pedido; não devem ser inferidas por tempo decorrido ou por novos apontamentos.
- `CON-003`: A rotina não deve alterar o acerto por meio de exclusão e recriação que perca sua identidade ou rastreabilidade, salvo decisão explícita posterior.
- `CON-004`: Qualquer alteração de persistência deve preservar compatibilidade com PostgreSQL, Entity Framework Core e migrations existentes.
- `CON-005`: O escopo cobre a correção dos acertos históricos existentes; novos acertos devem continuar usando o fluxo normal já definido.

## Critérios de aceite

- `AC-001`: Dado um acerto histórico elegível sem custo de horas, quando a aplicação iniciar, então o serviço de background será executado em paralelo aos demais serviços, localizará o acerto e calculará seu custo de horas a partir das tarefas e produtos do pedido sem bloquear o atendimento de requisições.
- `AC-002`: Dado um pedido com tarefas que totalizam 10 horas e produto com `HourlyRate` de 25, quando seu acerto histórico for reconciliado, então o `LaborCostAmount` persistido será 250.
- `AC-003`: Dado um acerto histórico com custo de materiais de 300 e custo de horas calculado de 250, quando a reconciliação for concluída, então o custo de produção persistido será 550 e o custo de materiais não será somado novamente.
- `AC-004`: Dado um acerto com receita de 1.000, custo de produção atualizado de 550, custo de envio de 0 e desconto de 0, quando a reconciliação for concluída, então o custo total será 550 e a margem será 450.
- `AC-005`: Dado um acerto histórico sem tarefas de produção elegíveis ou com total de horas igual a zero, quando a rotina for executada, então nenhum custo de horas será adicionado e os valores existentes permanecerão coerentes.
- `AC-006`: Dado um acerto já reconciliado, quando a aplicação for reiniciada ou a rotina for executada novamente, então o custo de horas não será aplicado uma segunda vez.
- `AC-007`: Dado um acerto histórico que não possa ser associado a um pedido, tarefa ou produto válido, quando a rotina o processar, então a aplicação continuará funcionando, o registro não será persistido parcialmente e a falha será registrada com diagnóstico útil para nova tentativa.
- `AC-008`: Dado um conjunto com acertos válidos e inválidos, quando a rotina for executada, então cada acerto válido será processado segundo as regras de isolamento adotadas e os inválidos serão identificados sem produzir valores financeiros incorretos.
- `AC-009`: Dado um acerto histórico reconciliado, quando ele for consultado na listagem, no resumo ou nos detalhes financeiros, então custo de produção, custo total, margem e valor das horas refletirão o cálculo persistido.

## Riscos, dependências e perguntas abertas

- Dependência: A implementação vigente de `SettlementCalculator`, os repositórios de acertos, pedidos, tarefas de produção e produtos, além da persistência de `LaborCostAmount`.
- Dependência: O mecanismo de unidade de trabalho/transação usado para atualizar acerto e valores derivados de forma atômica.
- Decisão: O `HourlyRate` atualmente cadastrado no produto é a fonte do valor-hora histórico; o sistema não fará busca por snapshot histórico.
- Risco: Acertos antigos podem ter sido criados com dados incompletos, tarefas removidas ou produtos alterados, impedindo uma reconciliação confiável.
- Decisão: Falhas individuais não interrompem a inicialização; o registro problemático permanece disponível para nova tentativa.
- Decisão: A reconciliação roda em background, em paralelo aos demais serviços, sem bloquear o atendimento.

