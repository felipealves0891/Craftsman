# Integration Context Tasks

## Objetivo
Isolar integrações externas, receber pedidos de múltiplas fontes e normalizar dados para o modelo interno sem acoplar regras de negócio às APIs externas.

## Tarefas

### IN-001 - Definir contrato de fonte de pedidos
- [ ] Criar contrato `IOrderSource`.
- [ ] Criar modelo bruto de pedido recebido da origem.
- [ ] Garantir que cada origem implemente o contrato sem alterar o domínio de vendas.

Critérios de aceitação:
- Novas fontes podem ser adicionadas por implementação do contrato.
- O domínio de vendas não conhece APIs externas.
- Não há `if/else` por origem no fluxo de importação.

### IN-002 - Definir normalizador de pedidos
- [ ] Criar contrato `IOrderNormalizer`.
- [ ] Transformar pedido bruto em modelo interno de pedido.
- [ ] Validar dados mínimos obrigatórios para normalização.

Critérios de aceitação:
- Pedidos de origens diferentes geram o mesmo modelo interno.
- Pedidos inválidos são rejeitados com motivo claro.
- A normalização não executa regras de produção, estoque ou financeiro.

### IN-003 - Criar pipeline de importação
- [ ] Orquestrar leitura de fonte, normalização e persistência do pedido.
- [ ] Registrar falhas de importação.
- [ ] Emitir evento quando pedido for normalizado.

Critérios de aceitação:
- O fluxo segue `Importar -> Normalizar -> Produção`.
- Falhas de uma origem não bloqueiam a arquitetura para outras origens.
- A importação persiste somente pedidos normalizados válidos.

### IN-004 - Criar primeira integração simulada
- [ ] Implementar fonte de pedidos em memória ou arquivo local para desenvolvimento.
- [ ] Cobrir cenários de pedido válido e inválido.
- [ ] Usar o mesmo contrato esperado para integrações reais.

Critérios de aceitação:
- A integração simulada permite testar o fluxo sem API externa.
- A implementação não adiciona lógica especial ao domínio.
- Testes automatizados cobrem normalização bem-sucedida e falha.

### IN-005 - Registrar rastreabilidade da origem
- [ ] Salvar identificador da origem.
- [ ] Salvar identificador externo do pedido.
- [ ] Evitar duplicidade de importação para o mesmo pedido externo.

Critérios de aceitação:
- O mesmo pedido externo não é importado duas vezes.
- A rastreabilidade fica disponível no pedido interno.
- A origem continua sendo metadado, não parte da regra de negócio central.
