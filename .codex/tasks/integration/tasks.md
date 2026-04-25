# Integration Context Tasks

## Objetivo
Isolar integrações externas, receber pedidos de múltiplas fontes e normalizar dados para o modelo interno sem acoplar regras de negócio às APIs externas.

## Tarefas

### IN-001 - Definir contrato de fonte de pedidos
- [x] Criar contrato `IOrderSource`.
- [x] Criar modelo bruto de pedido recebido da origem.
- [x] Garantir que cada origem implemente o contrato sem alterar o domínio de vendas.

Critérios de aceitação:
- Novas fontes podem ser adicionadas por implementação do contrato.
- O domínio de vendas não conhece APIs externas.
- Não há `if/else` por origem no fluxo de importação.

### IN-002 - Definir normalizador de pedidos
- [x] Criar contrato `IOrderNormalizer`.
- [x] Transformar pedido bruto em modelo interno de pedido.
- [x] Validar dados mínimos obrigatórios para normalização.

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
- [x] Salvar identificador da origem.
- [x] Salvar identificador externo do pedido.
- [x] Evitar duplicidade de importação para o mesmo pedido externo.

Critérios de aceitação:
- O mesmo pedido externo não é importado duas vezes.
- A rastreabilidade fica disponível no pedido interno.
- A origem continua sendo metadado, não parte da regra de negócio central.

### IN-006 - Estudar integração com Elo7
- [ ] Levantar documentação oficial ou canal técnico disponível para integração com Elo7.
- [ ] Identificar formas de autenticação e autorização.
- [ ] Mapear dados disponíveis para pedidos, clientes, itens, pagamento e envio.
- [ ] Identificar limites de API, webhooks, paginação e políticas de uso.
- [ ] Documentar lacunas, riscos e requisitos para uma integração real.

Critérios de aceitação:
- Existe um resumo técnico da viabilidade da integração com Elo7.
- Campos externos relevantes foram mapeados para o modelo interno de pedido.
- Restrições de autenticação, limites e disponibilidade de API estão documentadas.
- O estudo indica se a integração deve ser ativa, manual, semi-automatizada ou inviável no primeiro momento.

### IN-007 - Estudar integração com Shopee
- [ ] Levantar documentação oficial da Shopee Open Platform.
- [ ] Identificar fluxo de autenticação, autorização e renovação de tokens.
- [ ] Mapear APIs de pedidos, itens, pagamentos, cancelamentos e logística.
- [ ] Identificar webhooks, limites de API, ambientes de teste e requisitos de homologação.
- [ ] Documentar riscos de dependência, versionamento e manutenção.

Critérios de aceitação:
- Existe um resumo técnico da viabilidade da integração com Shopee.
- Campos externos relevantes foram mapeados para o modelo interno de pedido.
- Requisitos de credenciais, homologação e renovação de tokens estão documentados.
- O estudo define quais endpoints são necessários para o primeiro fluxo de importação.

### IN-008 - Estudar integração com Correios
- [ ] Levantar documentação oficial dos Correios para postagem, rastreamento e cálculo de frete.
- [ ] Identificar requisitos de contrato, credenciais e ambientes disponíveis.
- [ ] Mapear dados necessários para criação ou acompanhamento de envio.
- [ ] Identificar limites, formatos de etiqueta, rastreio e eventos de entrega.
- [ ] Documentar riscos operacionais e dependências de contrato comercial.

Critérios de aceitação:
- Existe um resumo técnico da viabilidade da integração com Correios.
- Eventos de rastreamento relevantes foram mapeados para o modelo interno de envio.
- Requisitos de credenciais, contrato e ambiente estão documentados.
- O estudo define se o primeiro escopo deve cobrir cálculo de frete, postagem, rastreio ou apenas rastreio.

### IN-009 - Estudar integração com Loggi
- [ ] Levantar documentação oficial ou canal técnico disponível para integração com Loggi.
- [ ] Identificar requisitos de autenticação, contrato e disponibilidade regional.
- [ ] Mapear APIs de cotação, criação de entrega, rastreamento e confirmação.
- [ ] Identificar eventos de entrega, webhooks, limites e políticas de uso.
- [ ] Documentar riscos operacionais, cobertura e dependências comerciais.

Critérios de aceitação:
- Existe um resumo técnico da viabilidade da integração com Loggi.
- Eventos de rastreamento relevantes foram mapeados para o modelo interno de envio.
- Requisitos de credenciais, contrato e cobertura estão documentados.
- O estudo define quais recursos da Loggi são candidatos ao primeiro escopo de integração.
