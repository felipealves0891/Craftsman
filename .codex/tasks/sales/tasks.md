# Sales Context Tasks

## Objetivo
Receber pedidos de múltiplas fontes, normalizar para um modelo interno único e manter o pedido independente da origem.

## Tarefas

### S-001 - Modelar pedido interno
- [ ] Criar entidade `Order`.
- [ ] Criar itens do pedido.
- [ ] Criar value objects necessários para identificadores, valores e status.
- [ ] Registrar a origem do pedido apenas como metadado.

Critérios de aceitação:
- O pedido interno não depende do formato da origem externa.
- Itens, quantidades, valores e dados do cliente são representados no modelo interno.
- A origem não altera a regra de negócio do pedido.

### S-002 - Definir ciclo de vida do pedido
- [ ] Definir status internos do pedido.
- [ ] Validar transições permitidas.
- [ ] Emitir evento quando o pedido for normalizado.

Critérios de aceitação:
- Transições inválidas são bloqueadas.
- O pedido normalizado pode seguir para produção sem conhecer a integração de origem.
- O evento de pedido normalizado contém os dados necessários para o próximo passo do fluxo.

### S-003 - Criar serviço de aplicação para consulta e acompanhamento de pedidos
- [ ] Criar listagem de pedidos.
- [ ] Criar detalhe do pedido.
- [ ] Exibir origem, status, itens e etapas relacionadas.

Critérios de aceitação:
- A tela de acompanhamento usa o modelo interno.
- A interface não contém lógica de normalização.
- Pedidos de origens diferentes aparecem de forma consistente.

### S-004 - Persistir pedidos normalizados
- [ ] Criar entidades de persistência para pedidos.
- [ ] Criar configuração do EF.
- [ ] Implementar repositório de pedidos.

Critérios de aceitação:
- Pedidos podem ser salvos e carregados com seus itens.
- O mapeamento entre persistência e domínio é explícito.
- Consultas de pedidos não expõem entidades do EF para a aplicação.
