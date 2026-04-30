# Sales Context Tasks

## Objetivo
Receber pedidos de múltiplas fontes, normalizar para um modelo interno único e manter o pedido independente da origem.

## Tarefas

### S-001 - Modelar pedido interno
- [x] Criar entidade `Order`.
- [x] Criar itens do pedido.
- [x] Criar value objects necessários para identificadores, valores e status.
- [x] Registrar a origem do pedido apenas como metadado.

Critérios de aceitação:
- O pedido interno não depende do formato da origem externa.
- Itens, quantidades, valores e dados do cliente são representados no modelo interno.
- A origem não altera a regra de negócio do pedido.

### S-002 - Definir ciclo de vida do pedido
- [x] Definir status internos do pedido.
- [x] Validar transições permitidas.
- [x] Emitir evento quando o pedido for normalizado.

Critérios de aceitação:
- Transições inválidas são bloqueadas.
- O pedido normalizado pode seguir para produção sem conhecer a integração de origem.
- O evento de pedido normalizado contém os dados necessários para o próximo passo do fluxo.

### S-003 - Criar serviço de aplicação para consulta e acompanhamento de pedidos
- [x] Criar listagem de pedidos.
- [x] Criar detalhe do pedido.
- [x] Exibir origem, status, itens e etapas relacionadas.

Critérios de aceitação:
- A tela de acompanhamento usa o modelo interno.
- A interface não contém lógica de normalização.
- Pedidos de origens diferentes aparecem de forma consistente.

### S-004 - Persistir pedidos normalizados
- [x] Criar entidades de persistência para pedidos.
- [x] Criar configuração do EF.
- [x] Implementar repositório de pedidos.

Critérios de aceitação:
- Pedidos podem ser salvos e carregados com seus itens.
- O mapeamento entre persistência e domínio é explícito.
- Consultas de pedidos não expõem entidades do EF para a aplicação.

### S-005 - Criar entrada manual de pedidos
- [X] Criar serviço de aplicação para cadastrar pedido manual.
- [X] Criar tela Razor para informar cliente, itens, quantidades e valores.
- [X] Registrar origem do pedido como `Manual`.
- [X] Emitir o mesmo evento de pedido normalizado usado por pedidos importados.
- [X] Persistir o pedido manual usando o mesmo repositório de pedidos.

Critérios de aceitação:
- Pedido manual gera o mesmo modelo interno de `Order` usado por integrações externas.
- A origem manual fica registrada como metadado rastreável.
- Itens, quantidades, valores e dados do cliente são obrigatórios conforme regras do domínio.
- A tela não contém regra de negócio de normalização ou ciclo de vida.
- Pedido manual pode seguir para mapeamento, produção, envio e financeiro sem fluxo especial.

### S-006 - Permitir vínculo manual de itens do pedido a produtos internos
- [X] Exibir itens de pedido sem produto interno vinculado.
- [X] Permitir selecionar produto interno para cada item.
- [X] Atualizar o item do pedido preservando rastreabilidade do item externo/manual.
- [X] Impedir planejamento de produção para itens sem produto interno.

Critérios de aceitação:
- Item de pedido manual ou importado pode ser vinculado a um produto interno.
- O vínculo não altera dados originais do item do pedido.
- O processo não cria regra específica por origem.
- O vínculo fica persistido e disponível para o planejador de produção.
