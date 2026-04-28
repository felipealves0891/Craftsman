# Estudo IN-007

A integração com Shopee é tecnicamente viável para o primeiro fluxo de importação de pedidos. Diferente da Elo7, existe a Shopee Open Platform com
API pública, autenticação por aplicação/parceiro, ambiente de teste e endpoints específicos para pedidos, pagamento e logística.

O primeiro escopo recomendado é integração ativa por polling, sem webhooks como dependência inicial: buscar pedidos por período/status, buscar
detalhes por order_sn, normalizar para o modelo interno atual e evitar duplicidade por Source = "Shopee" + ExternalOrderId = order_sn.

## Modelo Interno Atual

O Craftsman hoje normaliza pedidos para:

- RawOrder: Source, ExternalOrderId, CustomerName, CustomerEmail, Items
- RawOrderItem: ExternalItemId, Description, Quantity, UnitPrice

Arquivos relevantes:

- src/Domains/Integration/Models/RawOrder.cs
- src/Domains/Integration/Models/RawOrderItem.cs
- src/Domains/Integration/Services/OrderNormalizer.cs
- src/Domains/Integration/Services/OrderImportPipeline.cs

## Autenticação e Credenciais

A Shopee Open Platform usa aplicação com partner_id e partner_key, assinatura HMAC-SHA256, shop_id, access_token e refresh_token.

Fluxo esperado:

1. Criar app na Shopee Open Platform.
2. Direcionar vendedor para autorização da loja.
3. Receber code e shop_id no callback.
4. Trocar code por access_token e refresh_token.
5. Persistir tokens por loja.
6. Renovar access_token periodicamente usando refresh_token.

Pontos importantes:

- access_token é temporário. Fontes técnicas indicam expiração típica de 4 horas.
- refresh_token também expira. Fontes de SDK/comunidade indicam validade típica de 30 dias, mas isso deve ser confirmado na conta/app real.
- Tokens devem ser armazenados separados por shop_id.
- Cada chamada assinada depende de partner_id, path da API, timestamp, token/loja quando aplicável e partner_key.

## Endpoints Necessários Para o Primeiro Fluxo

Mínimo para importação:

- GET /api/v2/order/get_order_list: localizar pedidos por período, status e paginação.
- GET /api/v2/order/get_order_detail: obter dados completos dos pedidos retornados.
- GET /api/v2/payment/get_escrow_detail: opcional no primeiro momento; necessário se o financeiro precisar de valores liquidados/taxas.
- GET /api/v2/logistics/get_tracking_number ou GET /api/v2/logistics/get_tracking_info: fora do primeiro fluxo de pedido, mas candidato para
Shipping Context.

## Endpoints relacionados a cancelamento/logística:

- POST /api/v2/order/cancel_order
- GET /api/v2/order/get_shipment_list
- POST /api/v2/logistics/ship_order
- GET /api/v2/logistics/get_shipping_parameter

## Mapeamento Para o Craftsman

Mapeamento mínimo recomendado:

- RawOrder.Source: "Shopee"
- RawOrder.ExternalOrderId: order_sn
- RawOrder.CustomerName: nome do comprador ou destinatário disponível no detalhe do pedido
- RawOrder.CustomerEmail: provavelmente null, porque marketplaces geralmente não expõem e-mail direto do comprador
- RawOrderItem.ExternalItemId: preferir model_id quando houver variação; caso contrário item_id
- RawOrderItem.Description: item_name + model_name quando houver variação
- RawOrderItem.Quantity: quantidade do item
- RawOrderItem.UnitPrice: preço unitário pago ou preço com desconto disponível no detalhe do pedido

Dados relevantes fora do modelo atual:

- status Shopee do pedido;
- order_sn;
- package_number;
- status de cancelamento;
- transportadora;
- código de rastreio;
- valor de frete;
- taxas, comissões, voucher e repasse;
- endereço de entrega;
- timestamps de criação, pagamento, envio e conclusão.

## Riscos e Lacunas

- A documentação oficial da Shopee é uma SPA e não abriu bem pelo navegador textual; os endpoints foram confirmados por referências técnicas que
apontam para URLs oficiais da Open Platform.
- É necessário validar, com uma conta/app real, permissões disponíveis para seller brasileiro, homologação e políticas de aprovação.
- Webhooks/push existem em SDKs, mas eu não trataria como dependência do primeiro fluxo sem validar a configuração oficial no app.
- Rate limits e janelas exatas de consulta devem ser confirmados na documentação autenticada da aplicação.
- A renovação de token precisa de rotina operacional; falha de refresh deve marcar a loja como “reautorização necessária”.
- Dados pessoais de comprador/endereço devem ser minimizados e segregados por contexto, por LGPD.
- O modelo interno atual não guarda pagamento/envio; isso deve continuar fora da normalização inicial para preservar os bounded contexts.

## Conclusão

A IN-007 deve ser considerada viável como integração ativa, começando por importação de pedidos via polling. O primeiro fluxo deve usar
get_order_list + get_order_detail, mapear somente os campos suportados por RawOrder, e deixar pagamento/logística para etapas posteriores ou
contexts próprios.

Fontes consultadas:

- Shopee Open Platform: https://open.shopee.com/
- OpenAPI 2.0 Overview: https://open.shopee.com/documents?module=87&type=2&id=58&version=2
- Order API refs via Go package: https://pkg.go.dev/github.com/wjpxxx/shopeego/order
- Payment API refs: https://pkg.go.dev/github.com/wjp-letgo/shopeego/payment
- Logistics API refs: https://pkg.go.dev/github.com/wjp-letgo/letgo/x/api/shopee/logistics
- LS Central Shopee API list: https://lscentral.azurewebsites.net/Content/LS-Retail/eCommerce/Shopee/Shopee-APIs.htm