# Shipping Context Tasks

## Objetivo
Acompanhar envio e entrega dos pedidos após a produção, mantendo rastreamento e confirmação de entrega.

## Tarefas

### SH-001 - Modelar envio
- [x] Criar entidade `Shipment`.
- [x] Relacionar envio ao pedido.
- [x] Definir status, código de rastreio e datas relevantes.

Critérios de aceitação:
- Um envio possui rastreabilidade até o pedido.
- Status de envio seguem transições válidas.
- Código de rastreio é tratado como dado do envio, não como regra de negócio externa.

### SH-002 - Definir contrato de rastreamento
- [x] Criar contrato `IShippingTracker`.
- [x] Implementar serviço de aplicação para atualizar status de envio.
- [x] Manter baixo acoplamento com transportadoras ou APIs externas.

Critérios de aceitação:
- O domínio não chama APIs externas diretamente.
- Novos rastreadores podem ser adicionados sem alterar regras de envio.
- Atualizações inválidas de status são rejeitadas.

### SH-003 - Registrar entrega
- [ ] Criar fluxo de confirmação de entrega.
- [ ] Emitir evento de entrega confirmada.
- [ ] Disponibilizar entrega como gatilho para cálculo financeiro.

Critérios de aceitação:
- Entrega confirmada registra data de entrega.
- O financeiro pode reagir ao evento sem depender da tela de envio.
- Não é possível confirmar entrega para envio inexistente.

### SH-004 - Criar visão de acompanhamento de envio
- [x] Listar envios por status.
- [x] Exibir pedido relacionado, rastreio e datas.
- [x] Permitir atualização controlada de status.

Critérios de aceitação:
- A interface mostra o estado atual do envio.
- A interface não contém regra de integração com rastreadores externos.
- Atualizações passam por serviço de aplicação.

### SH-005 - Persistir envios
- [x] Criar entidades de persistência.
- [x] Criar configurações do EF.
- [x] Implementar repositório de envios.

Critérios de aceitação:
- Envios podem ser salvos e consultados por pedido e status.
- O mapeamento entre persistência e domínio é explícito.
- Alterações de envio emitem os eventos necessários.
