# Production Context Tasks

## Objetivo
Gerar e acompanhar a agenda de produção com base em pedidos normalizados, produtos internos, ficha técnica e disponibilidade de matéria-prima.

## Tarefas

### P-001 - Modelar tarefa de produção
- [ ] Criar entidade `ProductionTask`.
- [ ] Relacionar tarefa ao pedido e produto interno.
- [ ] Definir quantidade, status e datas relevantes.

Critérios de aceitação:
- Cada tarefa de produção tem rastreabilidade até o pedido.
- Tarefas usam produtos internos, não itens externos.
- Status da tarefa segue transições válidas.

### P-002 - Implementar planejador de produção
- [ ] Criar contrato `IProductionPlanner`.
- [ ] Gerar tarefas a partir de pedidos normalizados.
- [ ] Validar ficha técnica e disponibilidade de material.

Critérios de aceitação:
- Pedido normalizado gera uma ou mais tarefas de produção.
- Produção não é planejada sem produto interno mapeado.
- Produção não é planejada sem material suficiente.

### P-003 - Criar agenda de produção
- [ ] Criar consulta de tarefas por status e data.
- [ ] Criar visão Razor para acompanhamento da agenda.
- [ ] Permitir avanço de status conforme regras definidas.

Critérios de aceitação:
- A agenda mostra tarefas pendentes, em produção e concluídas.
- A UI não contém regras de cálculo de materiais.
- Avanços inválidos de status são bloqueados no domínio ou serviço de aplicação.

### P-004 - Integrar produção com estoque
- [ ] Consumir ou reservar materiais ao planejar/iniciar produção.
- [ ] Registrar referência da produção nas movimentações de estoque.
- [ ] Emitir evento quando produção for planejada ou concluída.

Critérios de aceitação:
- Toda tarefa planejada tem validação de materiais.
- Toda baixa de material é auditável no estoque.
- Eventos de produção permitem continuidade do fluxo sem acoplamento direto.

### P-005 - Persistir tarefas de produção
- [ ] Criar entidades de persistência.
- [ ] Criar configurações do EF.
- [ ] Implementar repositório de produção.

Critérios de aceitação:
- Tarefas podem ser salvas e carregadas com seus vínculos.
- O domínio não depende do EF.
- Consultas da agenda são consistentes com os status persistidos.
