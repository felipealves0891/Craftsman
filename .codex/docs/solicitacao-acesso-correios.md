Para rastreio, o mínimo prático é:

1. Criar/ter uma conta no Meu Correios.
2. Se o rastreio for de objetos de uma empresa/remetente com contrato, ter ou acessar o contrato Correios dessa empresa.
    - Correios:PostCardNumber, se usar cartão de postagem
    - Correios:Enabled = true

Ponto importante: a API Rastro teve restrição recente. Segundo os Correios, consultas de rastreamento pela API ficam restritas a objetos
vinculados ao contrato do remetente. Ou seja, para rastrear objetos postados por contrato, a credencial precisa ser do próprio remetente ou uma
credencial delegada/autorizada.

Para frete/prazo/postagem, precisa de mais coisas:

- contrato ativo com os Correios;
- cartão de postagem, quando aplicável;
- liberação dos serviços no contrato;
- para preço: serviço 38202 API PREÇOS;
- para prazo: serviço 38210 API PRAZOS;
- para pré-postagem/etiquetas: cadastro operacional de remetente, destinatário, embalagem e documentos.

Fontes oficiais:

- Desenvolvedores Correios: https://www.correios.com.br/atendimento/developers/
- Manual CWS: https://www.correios.com.br/atendimento/developers/manuais/correioswebservice
- API Token: https://www.correios.com.br/atendimento/developers/manuais/manual-uso-da-api-token
- API Preço: https://www.correios.com.br/atendimento/developers/manuais/manual-api-preco-1
- API Prazo: https://www.correios.com.br/atendimento/developers/manuais/manual-api-prazo