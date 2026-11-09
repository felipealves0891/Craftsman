<!-- spectro-managed: true -->
# Especificação: Adicionar valor das horas ao custo financeiro

## Problema e objetivo

- Problema: O acerto financeiro considera os custos de produção rastreados por movimentações de estoque, mas não considera o custo das horas de produção já registradas nas tarefas.
- Objetivo: Incorporar ao custo de produção o valor correspondente às horas trabalhadas, calculado pelas horas de produção do pedido multiplicadas pelo `HourlyRate` do produto, para que custo total e margem reflitam também o trabalho de produção.

## Escopo

### Incluído

- Utilizar o valor monetário da hora já cadastrado em cada produto (`HourlyRate`).
- Calcular o custo das horas de produção de um pedido a partir das horas registradas nas tarefas de produção e do valor da hora.
- Somar o custo das horas ao custo de produção já calculado a partir das movimentações de estoque.
- Refletir o custo de produção, o custo total e a margem atualizados no acerto financeiro e nas consultas financeiras existentes.
- Preservar o cálculo de receita, descontos, custo de envio e as regras existentes de cálculo após a entrega e de não duplicidade do acerto.

### Excluído

- Alterar a forma como horas de produção são planejadas, registradas ou distribuídas entre tarefas.
- Criar apontamento manual de horas, controle de ponto ou edição de horas na tela financeira.
- Alterar o custo de materiais, o custo de envio, a receita, o desconto ou a fórmula de margem além da inclusão do custo das horas.
- Adicionar ou utilizar um valor-hora global em `appsettings` para este cálculo.

## Requisitos

### Requisitos funcionais

- `FR-001`: A aplicação deve utilizar exclusivamente o `HourlyRate` não negativo cadastrado no produto como valor da hora de produção.
- `FR-002`: Ao calcular o acerto financeiro de um pedido elegível, seguindo a seleção de tarefas do fluxo financeiro existente, a aplicação deve calcular o custo das horas como a soma das horas de produção das tarefas multiplicada pelo `HourlyRate` aplicável ao produto.
- `FR-003`: O custo das horas deve ser somado ao custo de produção derivado das movimentações de estoque, sem substituir nem duplicar esse custo existente, e o resultado calculado deve ser persistido no acerto.
- `FR-004`: O acerto financeiro persistido deve usar o custo de produção atualizado no custo total e, consequentemente, na margem apresentada nas consultas financeiras existentes.
- `FR-005`: O cálculo deve continuar respeitando a regra vigente de que o acerto só ocorre para pedido entregue e não deve criar um segundo acerto para o mesmo pedido.

### Requisitos não funcionais

- `NFR-001`: O valor monetário deve manter a precisão e o formato monetário já adotados pelo domínio financeiro, sem perda silenciosa de centavos.
- `NFR-002`: O custo das horas deve permanecer rastreável às tarefas de produção e ao valor de hora utilizado no cálculo, permitindo explicar a composição do custo de produção.
- `NFR-003`: A mudança deve preservar a separação entre domínio, aplicação e infraestrutura prevista na constituição e não deve expor segredos de configuração em logs ou mensagens de erro.

### Restrições

- `CON-001`: A fórmula funcional esperada é horas de produção × valor da hora; a especificação não define uma arquitetura ou classe específica para implementá-la.
- `CON-002`: As horas utilizadas devem ser as horas de produção já registradas para as tarefas do pedido; não devem ser inferidas a partir de tempo decorrido entre estados, salvo decisão posterior explicitamente aprovada.
- `CON-003`: O `HourlyRate` do produto não pode ser negativo. Não há configuração global de valor-hora em `appsettings` neste escopo.
- `CON-004`: Alterações de persistência, caso sejam necessárias para preservar o valor utilizado no acerto, devem manter compatibilidade com o fluxo de migrations e a rastreabilidade financeira existente.

## Critérios de aceite

- `AC-001`: Dado um produto com `HourlyRate` não negativo, quando o acerto for calculado para um pedido desse produto, então o cálculo utilizará o valor cadastrado no produto e não um valor global de `appsettings`.
- `AC-002`: Dado um pedido entregue com tarefas que totalizam 10 horas e valor de hora aplicável de 25, quando o acerto for calculado, então o custo das horas será 250.
- `AC-003`: Dado um pedido cujo custo de materiais seja 300 e cujo custo de horas seja 250, quando o acerto for calculado, então o custo de produção persistido será 550.
- `AC-004`: Dado um acerto com receita de 1.000, custo de produção de materiais de 300, custo de horas de 250, custo de envio de 0 e desconto de 0, quando a margem for consultada, então o custo total será 550 e a margem será 450.
- `AC-005`: Dado um pedido entregue sem horas de produção registradas, quando o acerto for calculado, então nenhum custo de horas será adicionado e o custo de materiais existente será preservado.
- `AC-006`: Dado um pedido com tarefas e movimentações de estoque já contabilizadas, quando o acerto for recalculado pelo fluxo permitido, então as movimentações não serão somadas duas vezes e o custo de horas será incluído uma única vez.
- `AC-007`: Dado um pedido não entregue, quando alguém tentar calcular o acerto, então a operação continuará sendo rejeitada conforme a regra vigente e nenhum acerto com custo de horas será criado.
- `AC-008`: Dado um pedido que já possui acerto financeiro, quando alguém tentar gerar outro acerto, então a duplicidade continuará sendo rejeitada ou ignorada conforme o comportamento vigente.
- `AC-009`: Dado um produto com `HourlyRate` negativo, quando o valor for validado ou o cálculo for iniciado, então a aplicação rejeitará o valor com diagnóstico útil e não criará um acerto com custo incorreto.

## Riscos, dependências e perguntas abertas

- Dependência: As tarefas de produção existentes, seus campos de horas e a associação delas ao pedido.
- Dependência: O cálculo e a exibição atuais do acerto financeiro, incluindo custo de produção, custo total e margem.
- Dependência: O `HourlyRate` persistido no cadastro dos produtos relacionados às tarefas do pedido.
- Risco: Alterações no `HourlyRate` depois de um acerto podem gerar diferenças entre novos cálculos e o histórico já persistido; o resultado do acerto existente deve permanecer preservado.
- Decisão registrada: O custo de horas calculado deve ser persistido no acerto; o fluxo financeiro existente define quais tarefas participam do cálculo.

