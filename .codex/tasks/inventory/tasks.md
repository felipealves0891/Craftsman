# Inventory Context Tasks

## Objetivo
Controlar matéria-prima e estoque de forma auditável, garantindo que a produção dependa da disponibilidade de material.

## Tarefas

### I-001 - Modelar matéria-prima
- [x] Criar entidade `RawMaterial`.
- [x] Definir unidade de medida, status e dados básicos.
- [x] Criar cadastro de matéria-prima.

Critérios de aceitação:
- Matérias-primas inativas não podem ser consumidas em novos planejamentos.
- Unidade de medida é obrigatória.
- O cadastro é independente de fornecedor externo.

### I-002 - Modelar movimentações de estoque
- [x] Criar entidade de movimentação de estoque.
- [x] Registrar entradas, saídas e ajustes.
- [x] Armazenar motivo, data e referência de negócio.

Critérios de aceitação:
- Toda alteração de estoque gera uma movimentação auditável.
- O saldo pode ser recalculado a partir das movimentações.
- Movimentações não podem ser apagadas por fluxo normal da aplicação.

### I-003 - Validar disponibilidade de material para produção
- [x] Criar serviço de consulta de disponibilidade.
- [x] Considerar ficha técnica e quantidade a produzir.
- [x] Informar materiais faltantes quando houver indisponibilidade.

Critérios de aceitação:
- Produção não é planejada quando há material insuficiente.
- A resposta indica quais materiais faltam e em qual quantidade.
- A validação usa dados internos de estoque, não dados externos.

### I-004 - Reservar ou consumir material para produção
- [x] Definir estratégia de reserva ou consumo conforme etapa da produção.
- [x] Criar movimentações correspondentes.
- [x] Relacionar movimentação ao pedido ou tarefa de produção.

Critérios de aceitação:
- O estoque é reduzido de forma auditável.
- O vínculo com produção permite rastrear a origem do consumo.
- Não é possível gerar saldo negativo por consumo de produção.

### I-005 - Persistir materiais e movimentações
- [x] Criar entidades de persistência.
- [x] Criar configurações do EF.
- [x] Implementar repositórios de estoque.

Critérios de aceitação:
- Saldos e movimentações podem ser consultados.
- A camada de aplicação não manipula `DbContext` diretamente.
- Alterações de estoque invalidam cache relacionado.

### I-006 - Criar cadastro manual de matéria-prima
- [ ] Criar serviço de aplicação para cadastrar e editar matéria-prima.
- [ ] Criar telas Razor para listar, criar, editar, ativar e inativar matérias-primas.
- [ ] Validar nome, unidade de medida e status.
- [ ] Persistir alterações pelo repositório de estoque.
- [ ] Invalidar cache de estoque quando matéria-prima mudar.

Critérios de aceitação:
- Matéria-prima manual usa a mesma entidade `RawMaterial` do domínio.
- Unidade de medida é obrigatória.
- Matérias-primas inativas não podem ser consumidas em novos planejamentos.
- A interface não acessa `DbContext` diretamente.

### I-007 - Criar lançamento manual de movimentações de estoque
- [ ] Criar tela para registrar entrada, saída e ajuste de estoque.
- [ ] Permitir informar quantidade, motivo, referência de negócio e custo unitário quando aplicável.
- [ ] Bloquear saída manual que gere saldo negativo.
- [ ] Persistir toda alteração como `StockMovement`.
- [ ] Invalidar cache de saldo e movimentações.

Critérios de aceitação:
- Toda alteração manual gera movimentação auditável.
- Saldo pode ser recalculado a partir das movimentações.
- Movimentações não são editadas ou apagadas por fluxo normal da aplicação.
- Entradas com custo unitário ficam disponíveis para cálculo financeiro de custo real.

### I-008 - Criar consulta operacional de estoque
- [ ] Criar tela de saldo por matéria-prima.
- [ ] Criar tela de histórico de movimentações por matéria-prima.
- [ ] Exibir motivo, data, quantidade, custo unitário e referência de negócio.
- [ ] Usar consultas cacheáveis com invalidação quando houver movimentação.

Critérios de aceitação:
- Usuário consegue auditar como o saldo foi formado.
- Consulta usa dados internos de estoque.
- Alterações de estoque refletem na consulta após invalidação de cache.
