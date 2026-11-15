<!-- spectro-managed: true -->
# Especificação: Corrigir o custo das saídas manuais de estoque

## Problema e objetivo

- Problema: Ao registrar manualmente uma movimentação de saída de estoque, o custo unitário é descartado e armazenado como zero. Como consequência, o custo total da movimentação também fica zerado, comprometendo a informação de custeio usada na consulta e no relatório financeiro.
- Objetivo: Preservar e disponibilizar o custo correto das saídas manuais de estoque, mantendo o custo total consistente com a quantidade efetiva e o custo unitário informado pelo usuário. A integração desse custo ao relatório financeiro permanece fora desta feature.

## Escopo

### Incluído

- Corrigir o registro de movimentações manuais do tipo saída para que o custo unitário informado pelo usuário não seja substituído por zero.
- Garantir que o custo total da saída seja consistente com sua quantidade e custo unitário, respeitando o sinal de exibição já adotado para a movimentação.
- Preservar o custo unitário e o custo total no histórico/listagem de movimentações manuais.
- Manter o relatório financeiro atual sem incluir saídas manuais, pois não existe neste momento um vínculo manual aprovado para associá-las a um pedido ou acerto.
- Cobrir o comportamento com testes de sucesso, validação e regressão nos pontos de registro, persistência, consulta e relatório aplicáveis.

### Excluído

- Alterar a regra de saldo de estoque ou permitir saldo negativo.
- Alterar entradas manuais, ajustes, movimentos gerados automaticamente pela produção ou a fórmula de seus custos, salvo o necessário para preservar consistência compartilhada.
- Alterar o relatório financeiro, receita, descontos, mão de obra, frete ou outras parcelas financeiras.
- Recalcular ou reescrever movimentos históricos existentes sem uma decisão explícita de correção de dados.
- Introduzir novo método de custeio, como custo médio, PEPS ou custo de reposição, sem aprovação da regra correspondente.

## Requisitos

### Requisitos funcionais

- `FR-001`: Ao registrar uma saída manual válida, a aplicação deve persistir o custo unitário positivo informado pelo usuário, sem substituí-lo automaticamente por zero.
- `FR-002`: O custo total persistido e apresentado para uma saída manual deve ser calculado a partir da quantidade efetiva da movimentação e do custo unitário informado. O sinal negativo da quantidade assinada da saída não deve transformar o custo total em negativo.
- `FR-003`: A listagem e o histórico de movimentações devem exibir, para uma saída manual registrada com custo, o custo unitário e o custo total persistidos, sem zerá-los durante a conversão para o modelo de apresentação.
- `FR-004`: O relatório financeiro deve permanecer sem incluir custos de saídas manuais enquanto não existir vínculo ou regra aprovada para associá-los a um pedido ou acerto financeiro.
- `FR-005`: A validação existente de quantidade positiva, motivo obrigatório para saída e saldo não negativo deve continuar sendo aplicada quando o custo da saída for corrigido.

### Requisitos não funcionais

- `NFR-001`: O cálculo e a persistência devem manter a precisão decimal e o formato monetário já utilizados pela aplicação.
- `NFR-002`: A correção deve preservar a rastreabilidade da movimentação manual, incluindo material, data, motivo e referência quando informada.
- `NFR-003`: O comportamento deve ser coberto por testes automatizados nos limites relevantes, incluindo saída válida, custo unitário informado, distinção entre quantidade assinada e custo total, entrada/ajuste sem regressão e preservação do relatório financeiro atual.

### Restrições

- `CON-001`: A saída manual não pode gerar saldo negativo.
- `CON-002`: O histórico de movimentações é auditável; a correção não deve apagar ou editar registros históricos para simular uma nova operação.
- `CON-003`: Não deve ser introduzida dependência ou camada nova sem necessidade demonstrável; a implementação deve respeitar as fronteiras atuais entre App, Domains e Infra.
- `CON-004`: O custo unitário da saída manual é informado pelo usuário e deve ser não negativo; a quantidade assinada pode ser negativa para representar a saída, mas o custo total deve permanecer um valor de custo não negativo.
- `CON-005`: Saídas manuais não participam do relatório financeiro nesta feature, porque não há vínculo manual aprovado com pedido ou acerto.

## Critérios de aceite

- `AC-001`: Dado um material com saldo suficiente, quando o usuário registrar uma saída manual válida com quantidade `Q` e custo unitário não negativo `C`, então a movimentação persistida terá `C` e não terá custo unitário forçado para zero.
- `AC-002`: Dado uma saída manual persistida com quantidade efetiva `Q` e custo unitário `C`, quando o histórico ou a listagem for consultado, então a quantidade assinada poderá ser `-Q`, mas o custo total exibido será `Q × C`, no formato monetário adotado pela aplicação.
- `AC-003`: Dado uma saída manual válida, quando o registro for concluído, então o saldo será reduzido pela quantidade informada e as validações existentes de motivo e saldo não negativo continuarão funcionando.
- `AC-004`: Dado um movimento de entrada ou ajuste existente, quando o fluxo de movimentações for executado após a correção, então seus custos e saldos continuarão obedecendo às regras vigentes, sem regressão causada pela mudança da saída manual.
- `AC-005`: Dado um conjunto de dados contendo saída manual com custo, quando o relatório financeiro atual for gerado, então esse custo não será incluído nem alterará os totais financeiros, pois não há vínculo aprovado com pedido ou acerto.
- `AC-006`: Dado um movimento que não pertença ao escopo do relatório financeiro, quando o relatório for gerado, então seu custo não será incluído apenas por compartilhar a mesma tabela de movimentações.
- `AC-007`: Dado um registro histórico criado antes da correção com custo zerado, quando o histórico for consultado, então a aplicação não o alterará automaticamente nem apresentará um custo inventado; ele permanecerá como está até eventual processo futuro explicitamente aprovado.

## Riscos, dependências e perguntas abertas

- Dependência: O formulário e o modelo de entrada já expõem `UnitCostAmount`; a saída manual deve utilizar esse valor informado pelo usuário.
- Dependência: O relatório financeiro atual calcula custo de material a partir de movimentos associados a tarefas de produção; saídas manuais continuam fora desse relatório.
- Risco: Uma futura inclusão de saídas manuais no relatório sem um critério de associação ao pedido pode atribuir custo ao pedido errado ou duplicar custos de produção.
- Risco: Corrigir apenas novos registros não recuperará custos de movimentos históricos já persistidos como zero.
- Decisão: O custo unitário da saída manual é preenchido pelo usuário. A quantidade assinada negativa é usada para representar a redução do estoque, mas não deve inverter o sinal do custo total.
- Decisão: Nenhum relatório financeiro será alterado nesta feature para incluir saídas manuais; elas permanecem fora do acerto financeiro manual atual.
- Decisão: Movimentos históricos de saída manual com custo zerado permanecem inalterados por enquanto.

