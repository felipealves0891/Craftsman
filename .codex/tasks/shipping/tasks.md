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
- [x] Criar fluxo de confirmação de entrega.
- [x] Emitir evento de entrega confirmada.
- [x] Disponibilizar entrega como gatilho para cálculo financeiro.

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

### SH-006 - Selecionar pedido em modal ao criar envio
- [ ] Substituir o campo livre de `OrderId` por um fluxo de selecao em modal na tela de criacao de envio.
- [ ] Listar pedidos disponiveis para envio no modal.
- [ ] Exibir detalhes do pedido no modal antes da selecao, incluindo itens.
- [ ] Preencher o pedido selecionado no formulario de envio.
- [ ] Manter a criacao do envio passando pelo servico de aplicacao existente.
- [ ] Criar ou atualizar testes automatizados cobrindo os criterios de aceitacao.

Criterios de aceitacao:
- Usuario consegue abrir um modal na tela de criacao de envio para selecionar um pedido.
- O modal lista pedidos disponiveis para envio.
- O modal exibe detalhes do pedido antes da selecao, incluindo itens.
- Ao selecionar um pedido, o formulario mostra o pedido escolhido e envia seu `OrderId`.
- O usuario nao precisa digitar manualmente o `OrderId`.
- A criacao do envio continua validando pedido existente e codigo de rastreio pelo fluxo atual de aplicacao/dominio.
- A interface nao acessa `DbContext` diretamente.

Plano de testes:
- Teste de Razor/view garantindo que a tela de criacao de envio possui acionador do modal de pedidos.
- Teste de Razor/view garantindo que o campo de `OrderId` nao fica exposto como entrada manual principal.
- Teste de Razor/view garantindo que o modal exibe dados e itens do pedido.
- Teste de aplicacao para consulta de pedidos disponiveis ao modal.
- Teste de aplicacao para criacao de envio com o `OrderId` selecionado.
