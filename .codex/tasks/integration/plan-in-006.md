# Estudo IN-006: Integração com Elo7

Resumo técnico: não encontrei documentação oficial pública de API da Elo7 para pedidos, autenticação por token, webhooks, paginação ou limites de
uso. A documentação oficial disponível aponta para operação via conta de vendedor, painel Pedidos > Minhas vendas, app Talk7 e central de
atendimento. Portanto, uma integração ativa via API deve ser considerada inviável no primeiro momento, salvo liberação privada pelo suporte/canal
técnico da Elo7.

## Viabilidade

- Recomendação inicial: integração manual.
- Possível evolução: semi-automatizada apenas se o vendedor confirmar que consegue exportar pedidos em planilha/relatório oficial ou obter canal
técnico privado.
- Não recomendado agora: scraping do painel, por risco operacional, LGPD, instabilidade e possível violação de termos.

## Autenticação e Autorização

- Não há OAuth, API key, token ou fluxo técnico público identificado.
- Acesso oficial conhecido é por conta Elo7, com usuário/senha e suporte a autenticação em dois fatores.
- Dados completos do comprador só ficam disponíveis ao vendedor após confirmação de pagamento.

Mapeamento Para o Modelo Interno Atual
Modelo interno atual:
RawOrder(Source, ExternalOrderId, CustomerName, CustomerEmail, Items)
RawOrderItem(ExternalItemId, Description, Quantity, UnitPrice)

## Mapeamento proposto:

- Source: valor fixo Elo7.
- ExternalOrderId: identificador/número do pedido Elo7, a confirmar no painel ou e-mail.
- CustomerName: nome do comprador após pagamento confirmado.
- CustomerEmail: e-mail do comprador, se disponibilizado; caso contrário null.
- ExternalItemId: código do produto/anúncio Elo7, quando disponível.
- Description: nome/descrição do produto comprado.
- Quantity: quantidade comprada.
- UnitPrice: preço unitário do item em BRL.

## Dados fora do modelo atual, mas relevantes para futuro:

- status do pedido/pagamento;
- valor de frete;
- modalidade de envio;
- código de rastreio;
- prazo de produção;
- descontos/cupom;
- transportadora;
- data de pagamento;
- data de envio.

Pedidos, Pagamento e Envio

- A central informa que vendas são consultadas em Pedidos > Minhas vendas, com busca e filtros.
- O vendedor recebe e-mail quando há venda.
- O pedido deve constar como pago antes do envio; prazos informados: Pix até 3h e cartão até 48h.
- Para envio, Frete7 pode atualizar automaticamente em até 24h após postagem com transportadoras parceiras; sem etiqueta, o vendedor informa
envio/rastreio manualmente, com menção a Correios.
- A política do vendedor diz que o vendedor deve marcar o pedido como enviado e informar rastreio.

Limites, Webhooks e Paginação

- Não há documentação pública encontrada para:
    - limites de API;
    - webhooks;
    - paginação;
    - ambientes sandbox;
    - rate limits;
    - endpoints de pedidos/clientes/pagamentos/envios.
- Isso bloqueia desenho de integração ativa robusta.

## Lacunas e Riscos

- Falta documentação oficial pública de API de marketplace.
- Falta contrato de dados de pedido exportável.
- Falta confirmação de campos exatos disponíveis ao vendedor.
- 2FA dificulta automações não oficiais.
- Scraping do painel seria frágil e juridicamente arriscado.
- Dados pessoais de comprador exigem cuidado de LGPD e minimização de armazenamento.
- Como o modelo interno ainda não guarda pagamento/envio no pedido bruto, o primeiro escopo deve importar apenas pedido normalizado, deixando
frete/rastreio para contexts próprios.

## Conclusão
IN-006 deve registrar a integração Elo7 como manual no primeiro momento. Para avançar para semi-automatizada, o próximo passo é confirmar com uma
conta vendedora real se existe exportação oficial de pedidos ou solicitar formalmente ao suporte Elo7 um canal técnico/API privada.

Fontes consultadas:

- Central Elo7: Pedidos e Vendas (https://elo7.movidesk.com/kb/category/pedidos-e-vendas?kbCategoryId=116748)
- Central Elo7: Configuração de cadastro de Vendedor
(https://elo7.movidesk.com/kb/category/configuracao-de-cadastro-de-vendedor?kbCategoryId=116766)
- Central Elo7: Frete e Envio (https://elo7.movidesk.com/kb/category/frete-e-envio)
- Política do Vendedor Elo7 (https://www.elo7.com.br/politicas-elo7/politica-do-vendedor/)
- Termos de Uso Elo7 (https://www.elo7.com.br/politicas-elo7/termos-de-uso/)