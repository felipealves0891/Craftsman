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
- [x] Orquestrar leitura de fonte, normalização e persistência do pedido.
- [x] Registrar falhas de importação.
- [x] Emitir evento quando pedido for normalizado.

Critérios de aceitação:
- O fluxo segue `Importar -> Normalizar -> Produção`.
- Falhas de uma origem não bloqueiam a arquitetura para outras origens.
- A importação persiste somente pedidos normalizados válidos.

### IN-004 - Criar primeira integração simulada
- [x] Implementar fonte de pedidos em memória ou arquivo local para desenvolvimento.
- [x] Cobrir cenários de pedido válido e inválido.
- [x] Usar o mesmo contrato esperado para integrações reais.

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
- [x] Levantar documentação oficial ou canal técnico disponível para integração com Elo7.
- [x] Identificar formas de autenticação e autorização.
- [x] Mapear dados disponíveis para pedidos, clientes, itens, pagamento e envio.
- [x] Identificar limites de API, webhooks, paginação e políticas de uso.
- [x] Documentar lacunas, riscos e requisitos para uma integração real.

Critérios de aceitação:
- Existe um resumo técnico da viabilidade da integração com Elo7.
- Campos externos relevantes foram mapeados para o modelo interno de pedido.
- Restrições de autenticação, limites e disponibilidade de API estão documentadas.
- O estudo indica se a integração deve ser ativa, manual, semi-automatizada ou inviável no primeiro momento.

### IN-007 - Estudar integração com Shopee
- [x] Levantar documentação oficial da Shopee Open Platform.
- [x] Identificar fluxo de autenticação, autorização e renovação de tokens.
- [x] Mapear APIs de pedidos, itens, pagamentos, cancelamentos e logística.
- [x] Identificar webhooks, limites de API, ambientes de teste e requisitos de homologação.
- [x] Documentar riscos de dependência, versionamento e manutenção.

Critérios de aceitação:
- Existe um resumo técnico da viabilidade da integração com Shopee.
- Campos externos relevantes foram mapeados para o modelo interno de pedido.
- Requisitos de credenciais, homologação e renovação de tokens estão documentados.
- O estudo define quais endpoints são necessários para o primeiro fluxo de importação.

### IN-008 - Estudar integração com Correios
- [x] Levantar documentação oficial dos Correios para postagem, rastreamento e cálculo de frete.
- [x] Identificar requisitos de contrato, credenciais e ambientes disponíveis.
- [x] Mapear dados necessários para criação ou acompanhamento de envio.
- [x] Identificar limites, formatos de etiqueta, rastreio e eventos de entrega.
- [x] Documentar riscos operacionais e dependências de contrato comercial.

Critérios de aceitação:
- Existe um resumo técnico da viabilidade da integração com Correios.
- Eventos de rastreamento relevantes foram mapeados para o modelo interno de envio.
- Requisitos de credenciais, contrato e ambiente estão documentados.
- O estudo define se o primeiro escopo deve cobrir cálculo de frete, postagem, rastreio ou apenas rastreio.

### IN-009 - Estudar integração com Loggi
- [x] Levantar documentação oficial ou canal técnico disponível para integração com Loggi.
- [x] Identificar requisitos de autenticação, contrato e disponibilidade regional.
- [x] Mapear APIs de cotação, criação de entrega, rastreamento e confirmação.
- [x] Identificar eventos de entrega, webhooks, limites e políticas de uso.
- [x] Documentar riscos operacionais, cobertura e dependências comerciais.

Critérios de aceitação:
- Existe um resumo técnico da viabilidade da integração com Loggi.
- Eventos de rastreamento relevantes foram mapeados para o modelo interno de envio.
- Requisitos de credenciais, contrato e cobertura estão documentados.
- O estudo define quais recursos da Loggi são candidatos ao primeiro escopo de integração.

### IN-010 - Implementar importacao de pedidos Shopee
- [ ] Criar configuracao `ShopeeOptions` validada no startup.
- [ ] Persistir lojas Shopee autorizadas com tokens protegidos por Data Protection.
- [ ] Implementar assinatura HMAC-SHA256, cliente HTTP Shopee e fluxo de renovacao de tokens.
- [ ] Implementar `ShopeeOrderSource` usando `get_order_list` e `get_order_detail`.
- [ ] Registrar a fonte no DI apenas quando `Shopee:Enabled = true`, preservando a fonte em memoria.
- [ ] Criar migracao EF Core para lojas/tokens Shopee.
- [ ] Criar testes automatizados cobrindo mapeamento, tokens, paginacao, deduplicacao e persistencia protegida.

Critérios de aceitacao:
- Pedidos Shopee validos sao importados pelo pipeline existente sem `if/else` por origem.
- O mesmo `order_sn` nao e importado duas vezes.
- Tokens sao armazenados por `shop_id` e nao ficam em texto puro.
- Access token expirado e renovado antes da chamada de pedidos; refresh invalido exige reautorizacao.
- Falhas da Shopee sao registradas em `OrderImportResult.Failures` sem bloquear outras origens.
- O dominio de vendas, producao, estoque, financeiro e envio nao conhece DTOs ou regras da API Shopee.
