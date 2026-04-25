# Finance Context Tasks

## Objetivo
Calcular valores financeiros e margem após a entrega, usando custo real e dados internos do fluxo.

## Tarefas

### F-001 - Modelar acerto financeiro
- [x] Criar entidade `FinancialSettlement`.
- [x] Relacionar acerto ao pedido entregue.
- [x] Registrar receita, custos, margem e datas.

Critérios de aceitação:
- Acerto financeiro só pode ser calculado para pedido entregue.
- O acerto mantém rastreabilidade até o pedido.
- Valores calculados ficam persistidos para consulta histórica.

### F-002 - Implementar calculadora de acerto
- [x] Criar contrato `ISettlementCalculator`.
- [x] Calcular receita com base no pedido.
- [x] Calcular custos reais com base em produção, estoque e envio.
- [x] Calcular margem.

Critérios de aceitação:
- O cálculo usa custo real disponível no sistema.
- O cálculo não depende de dados externos em tempo de execução.
- Margem é recalculável de forma determinística para os mesmos dados de entrada.

### F-003 - Reagir à entrega confirmada
- [x] Criar handler para evento de entrega confirmada.
- [x] Gerar ou agendar cálculo financeiro.
- [x] Bloquear duplicidade de acerto para o mesmo pedido.

Critérios de aceitação:
- Entrega confirmada inicia o fluxo financeiro.
- O mesmo pedido não gera acertos duplicados.
- Falhas de cálculo podem ser identificadas e tratadas.

### F-004 - Criar visão financeira
- [x] Listar acertos por período e status.
- [x] Exibir receita, custos e margem por pedido.
- [x] Permitir consulta de detalhe do cálculo.

Critérios de aceitação:
- A visão financeira usa dados persistidos do acerto.
- O detalhe permite entender os componentes do custo.
- A interface não recalcula regra financeira diretamente.

### F-005 - Persistir acertos financeiros
- [x] Criar entidades de persistência.
- [x] Criar configurações do EF.
- [x] Implementar repositório financeiro.

Critérios de aceitação:
- Acertos podem ser consultados por pedido e período.
- O domínio financeiro não depende do EF.
- O histórico de cálculos fica preservado.
