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
