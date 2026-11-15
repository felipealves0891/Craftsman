<!-- spectro-managed: true -->
# Especificação: Vincular saída manual de estoque a pedido no financeiro

## Problema e objetivo

- Problema: Uma saída manual de estoque pode representar um consumo relacionado a um pedido, mas o fluxo atual não oferece uma forma explícita de informar esse vínculo. Como o relatório financeiro calcula custos de material somente a partir de movimentações associadas a tarefas de produção, o custo de uma saída manual relacionada a um pedido não aparece na apuração financeira correspondente.
- Objetivo: Permitir que o usuário associe opcionalmente uma movimentação manual de estoque do tipo saída a um pedido e fazer com que essa associação seja considerada na visualização/apuração financeira do pedido, preservando a rastreabilidade da movimentação e a consistência dos totais financeiros.

## Escopo

### Incluído

- Disponibilizar no fluxo de criação de movimentação manual uma opção de selecionar um pedido para uma saída de estoque.
- Oferecer somente pedidos em produção e sem apuração financeira persistida como opções de vínculo.
- Permitir registrar a saída manual sem pedido associado, mantendo o comportamento atual para movimentos não relacionados a pedidos.
- Validar o pedido informado no servidor antes de persistir o vínculo.
- Persistir a associação entre a saída manual e o pedido de modo rastreável e compatível com o histórico auditável de movimentações.
- Incluir o custo da saída manual associada no relatório/apuração financeira do pedido conforme a regra financeira aprovada.
- Somar o custo da saída manual vinculada ao custo de material do pedido.
- Atualizar os totais financeiros e a margem apresentados quando o custo da saída vinculada fizer parte da apuração do pedido.
- Cobrir o fluxo com testes de sucesso, ausência de vínculo, pedido inválido, custo, saldo e regressão do cálculo financeiro existente.

### Excluído

- Alterar a regra de saldo, permitir saldo negativo ou remover a exigência de motivo para saída manual.
- Associar entradas, ajustes ou movimentos gerados automaticamente a pedidos por esta feature.
- Alterar a fórmula de receita, desconto, frete ou mão de obra do pedido, salvo o efeito necessário da inclusão do custo da saída manual aprovado.
- Reescrever, apagar ou editar movimentos históricos já persistidos para criar vínculos retroativos automaticamente.
- Atualizar ou recalcular apurações financeiras já persistidas por causa de uma nova saída manual vinculada.
- Editar o pedido associado depois que a movimentação manual for criada.
- Criar associação automática por descrição, matéria-prima, número externo ou outro critério heurístico.
- Alterar autenticação ou autorização além das permissões já exigidas para registrar movimentações e consultar/generar o financeiro.

## Requisitos

### Requisitos funcionais

- `FR-001`: Ao iniciar o registro de uma movimentação manual, a aplicação deve oferecer a seleção opcional de um pedido para movimentos do tipo saída; a opção não deve ser apresentada como vínculo obrigatório para entradas e ajustes.
- `FR-009`: A lista de pedidos disponíveis para vínculo deve conter somente pedidos em produção que ainda não possuam apuração financeira persistida.
- `FR-002`: Ao registrar uma saída manual com pedido selecionado, a aplicação deve validar que o pedido existe e persistir uma referência inequívoca ao pedido junto à movimentação, sem substituir o motivo, material, quantidade, custo ou data da operação.
- `FR-003`: Ao registrar uma saída manual sem pedido selecionado, a aplicação deve manter a movimentação sem vínculo e preservar o comportamento financeiro vigente para movimentos não associados.
- `FR-004`: A nova apuração/relatório financeiro de um pedido deve considerar o custo total das saídas manuais explicitamente vinculadas a esse pedido, somando-o ao custo de material já calculado para o pedido.
- `FR-005`: Quando uma saída manual vinculada for considerada na apuração financeira, os totais de custo e margem do pedido e os resumos agregados do relatório devem refletir esse custo uma única vez.
- `FR-006`: O sistema deve rejeitar o registro quando a referência de pedido informada for inexistente, inválida ou não puder ser resolvida, sem persistir uma movimentação parcialmente vinculada.
- `FR-007`: A listagem/histórico de movimentações deve permitir identificar o pedido associado quando houver vínculo, sem ocultar movimentos que não possuam pedido.
- `FR-008`: Uma saída manual vinculada deve ser permitida somente para pedido em produção sem apuração financeira persistida; apurações já persistidas não devem ser atualizadas ou recalculadas por esta feature.

### Requisitos não funcionais

- `NFR-001`: O custo considerado deve usar a precisão decimal e o formato monetário já adotados pela aplicação, com o custo total da movimentação contado uma única vez.
- `NFR-002`: A associação deve ser rastreável por identificador interno do pedido e da movimentação, sem depender de texto livre ou de identificador externo mutável.
- `NFR-003`: A mudança deve preservar as fronteiras atuais entre App, Domains e Infra, sem introduzir dependência ou camada nova sem necessidade demonstrável.
- `NFR-004`: Consultas e cálculos financeiros devem respeitar cancelamento, filtros de período e o mecanismo de cache existente, sem exibir resumo inconsistente com os dados persistidos após uma alteração relevante.
- `NFR-005`: O fluxo deve manter mensagens de erro seguras, sem expor SQL, stack trace, credenciais ou detalhes internos de persistência.

### Restrições

- `CON-001`: A saída manual continua sujeita às validações existentes de quantidade positiva, motivo obrigatório, custo não negativo e saldo não negativo.
- `CON-002`: O histórico de estoque é auditável; a feature não pode apagar ou alterar silenciosamente uma movimentação já persistida para corrigir seu vínculo.
- `CON-003`: A associação deve ser explícita pelo usuário; nenhuma relação deve ser inferida automaticamente a partir de dados coincidentes.
- `CON-004`: O mesmo custo de uma saída manual vinculada não pode ser somado novamente por também coincidir com uma referência de tarefa de produção ou outro caminho de cálculo.
- `CON-005`: O custo da saída manual vinculada pertence à parcela de custo de material e só deve ser considerado em novas apurações; apurações financeiras já persistidas permanecem inalteradas.

## Critérios de aceite

- `AC-001` (`FR-001`, `FR-009`): Dado o formulário de movimentação manual, quando o usuário selecionar o tipo saída, então poderá escolher opcionalmente um pedido, e a lista conterá somente pedidos em produção sem apuração financeira persistida; quando selecionar entrada ou ajuste, então o fluxo não exigirá nem registrará esse vínculo como se fosse uma saída.
- `AC-002` (`FR-002`, `FR-006`, `CON-001`): Dado um pedido existente, matéria-prima com saldo suficiente, quantidade válida, motivo e custo não negativo, quando o usuário registrar uma saída manual vinculada ao pedido, então a saída será persistida com o pedido correto e com os dados da movimentação preservados; se o pedido não existir, a operação será rejeitada sem gravação parcial.
- `AC-003` (`FR-003`): Dado um registro de saída manual sem pedido, quando o relatório financeiro de qualquer pedido for gerado, então o custo desse registro não será incluído apenas por ser uma saída manual.
- `AC-004` (`FR-004`, `NFR-001`): Dada uma saída manual vinculada ao pedido com quantidade efetiva `Q` e custo unitário `C`, quando uma nova apuração financeira do pedido for gerada, então o custo de material incluirá o valor monetário correspondente a `Q × C`, uma única vez e com a precisão adotada.
- `AC-005` (`FR-005`, `CON-004`): Dado um pedido com custos de produção e uma saída manual vinculada, quando o relatório financeiro for consultado, então o custo total e a margem refletirão ambos sem duplicar o custo da saída nem alterar receita, desconto, frete ou mão de obra sem regra aprovada.
- `AC-006` (`FR-007`, `NFR-002`): Dada uma lista de movimentações contendo saídas vinculadas e não vinculadas, quando o histórico for consultado, então o pedido associado será identificável nas vinculadas e as não vinculadas continuarão listadas sem referência de pedido.
- `AC-007` (`FR-008`, `NFR-004`): Dado um pedido com apuração financeira persistida, quando o usuário abrir o formulário de saída manual, então esse pedido não aparecerá nas opções; quando o relatório for consultado, a apuração persistida permanecerá inalterada por esta feature.
- `AC-008` (`NFR-005`): Dada uma tentativa com pedido inválido ou dados de saída inválidos, quando o servidor rejeitar a operação, então a mensagem será orientativa e não conterá stack trace, SQL, credenciais ou detalhes internos.

## Riscos, dependências e perguntas abertas

- Dependência: O fluxo atual já registra custo unitário e custo total de saídas manuais; o vínculo precisa ser representado sem conflitar com a referência operacional atualmente usada por movimentos de produção.
- Dependência: A apuração financeira atual é orientada por pedido e calcula custo de material a partir de movimentos associados a tarefas de produção; a consulta terá de distinguir as fontes para evitar duplicidade.
- Dependência: A tela de movimentações precisa obter uma lista de pedidos em produção sem apuração financeira persistida, sem expor dados desnecessários e respeitando a autorização existente.
- Risco: Incluir a saída manual no campo financeiro errado pode alterar análises e margens históricas de forma incompatível com a linguagem do relatório.
- Risco: Não atualizar apurações já persistidas pode deixar um custo de estoque posterior fora de um relatório histórico; essa é uma decisão explícita de escopo.
- Risco: Reutilizar `BusinessReference` sem uma regra explícita pode misturar identificador de tarefa de produção com identificador de pedido e gerar custo duplicado ou associação ambígua.
- Decisão: O custo da saída manual vinculada será somado ao custo de material do pedido.
- Decisão: Somente pedidos em produção e sem apuração financeira persistida aparecerão para seleção.
- Decisão: O pedido associado será informado somente na criação da movimentação e não poderá ser editado por esta feature.
- Decisão: Apurações financeiras já persistidas não serão atualizadas ou recalculadas; a saída vinculada será considerada somente em novas apurações.

