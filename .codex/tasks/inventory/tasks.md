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
- [x] Criar serviço de aplicação para cadastrar e editar matéria-prima.
- [x] Criar telas Razor para listar, criar, editar, ativar e inativar matérias-primas.
- [x] Validar nome, unidade de medida e status.
- [x] Persistir alterações pelo repositório de estoque.
- [x] Invalidar cache de estoque quando matéria-prima mudar.

Critérios de aceitação:
- Matéria-prima manual usa a mesma entidade `RawMaterial` do domínio.
- Unidade de medida é obrigatória.
- Matérias-primas inativas não podem ser consumidas em novos planejamentos.
- A interface não acessa `DbContext` diretamente.

### I-007 - Criar lançamento manual de movimentações de estoque
- [x] Criar tela para registrar entrada, saída e ajuste de estoque.
- [x] Permitir informar quantidade, motivo, referência de negócio e custo unitário quando aplicável.
- [x] Bloquear saída manual que gere saldo negativo.
- [x] Persistir toda alteração como `StockMovement`.
- [x] Invalidar cache de saldo e movimentações.

Critérios de aceitação:
- Toda alteração manual gera movimentação auditável.
- Saldo pode ser recalculado a partir das movimentações.
- Movimentações não são editadas ou apagadas por fluxo normal da aplicação.
- Entradas com custo unitário ficam disponíveis para cálculo financeiro de custo real.

### I-008 - Criar consulta operacional de estoque
- [x] Criar tela de saldo por matéria-prima.
- [x] Criar tela de histórico de movimentações por matéria-prima.
- [x] Exibir motivo, data, quantidade, custo unitário e referência de negócio.
- [x] Usar consultas cacheáveis com invalidação quando houver movimentação.

Critérios de aceitação:
- Usuário consegue auditar como o saldo foi formado.
- Consulta usa dados internos de estoque.
- Alterações de estoque refletem na consulta após invalidação de cache.

## I-009 - Alerta de Estoque baixo
- [x] Deve ser possivel criar regra de estoque baixo ao cadastrar produto
- [x] Deve ser possivel cadastrar dois niveis de regra, "Aviso" e "Critico"
- [x] Alerta de quantidade minima alertar no envio para produção
- [x] Alerta de quantidade critica no envio para produção, tela de entrada e no sino de notificações

Exemplos de notificação:
- .codex\startbootstrap-sb-admin-2-gh-pages

Critérios de aceitação:
- Usuario consegue cadastrar os alertas no cadastro/edição do produto
- Quando a quantidade no estoque for igual ou abaixo da quantidade minima alertar no envio para produção e no sino de notificações
- Quando a quantidade no estoque for igual ou abaixo da quantidade critica alertar no envio para produção, tela de entrada e no sino de notificações

## I-010 - Ajustes em movimentos de estoque
- [x] Ocultar o campo de referencia no formulario de lancamento manual de movimentos de estoque.
- [x] Exigir e exibir o campo de motivo apenas quando o movimento for saida (`Outbound`).
- [x] Ajustar os alertas de erro e sucesso do lancamento manual para mensagens claras e consistentes com o padrao da aplicacao.
- [x] Liberar o tipo ajuste (`Adjustment`) quando o movimento for manual.
- [x] Criar ou atualizar testes automatizados cobrindo os criterios de aceitacao.

Criterios de aceitacao:
- O usuario nao visualiza nem preenche referencia no lancamento manual de movimentos de estoque.
- Movimentos gerados pelo fluxo manual continuam auditaveis mesmo sem referencia informada pela tela.
- O campo motivo aparece e e obrigatorio apenas para saidas manuais.
- Entradas e ajustes manuais nao exigem motivo.
- Mensagens de erro devem informar o problema validado sem expor detalhe tecnico.
- Mensagem de sucesso deve confirmar que o movimento foi registrado.
- O tipo ajuste fica disponivel para lancamento manual.
- Ajustes manuais respeitam as regras de saldo ja existentes no dominio e na aplicacao.

Plano de testes:
- Teste de Razor/view garantindo que o campo referencia nao aparece no formulario manual.
- Teste de Razor/view garantindo que o motivo e condicionado ao tipo saida.
- Teste de aplicacao para saida manual sem motivo retornar erro de validacao.
- Teste de aplicacao para entrada manual sem motivo ser aceita.
- Teste de aplicacao para ajuste manual ser aceito quando valido.
- Teste de aplicacao para mensagens de erro e sucesso esperadas no fluxo manual.
