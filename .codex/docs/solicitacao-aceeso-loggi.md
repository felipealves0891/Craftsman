Passo A Passo

1. Ter uma conta/contrato Loggi habilitado para API
    - A documentação pública indica que client_id e client_secret são obtidos entrando em contato com o time de vendas/central de ajuda da Loggi.
    - Não parece haver, na documentação pública atual, um fluxo self-service completo para gerar essas credenciais.
2. Solicitar acesso às APIs
    - Peça ao time da Loggi:
        - client_id
        - client_secret
        - ambiente liberado, homologação/staging e/ou produção
        - APIs habilitadas: rastreio, cotação, criação de envio, etiqueta etc.
        - disponibilidade regional e condições comerciais.
3. Se você for integrador para terceiros
    - Não peça client_id e client_secret do cliente.
    - O seller/cliente deve fornecer apenas o integration_code.
    - Com suas credenciais de integrador, você chama POST /v1/integrator/activate para ativar o acesso e obter o companyId do cliente.
4. Gerar token OAuth V2
    - Use o endpoint:
        - POST /v2/oauth2/token
    - Envie client_id e client_secret.
    - A resposta deve trazer o token de acesso para usar nas chamadas seguintes.

    Exemplo conceitual:

    curl --request POST \
    --url https://stg.api.loggi.com/v2/oauth2/token \
    --header "Content-Type: application/json" \
    --data '{
        "client_id": "SEU_CLIENT_ID",
        "client_secret": "SEU_CLIENT_SECRET"
    }'
5. Guardar o companyId
    - Muitas APIs usam o caminho:
        - /v1/companies/{company_id}/...
    - Para integradores, o companyId vem do endpoint de ativação do integrador.
    - Para uso próprio, confirme com a Loggi se o companyId será informado junto das credenciais ou obtido por algum fluxo operacional.
6. Consumir APIs com Bearer token
    - Depois de gerar o token, envie:
        - Authorization: Bearer {token}
    - Exemplo para rastreio:
        - GET /v1/companies/{company_id}/packages/{tracking_code}/tracking
7. Configurar webhook, se necessário
    - Para receber atualizações em tempo real, a Loggi exige um endpoint HTTPS.
    - A configuração do webhook é feita via time de vendas.
    - O endpoint deve responder 200 ou 201.
    - A autenticação do webhook usa Basic Authentication; você precisa fornecer username e password para a Loggi configurar.
8. Validar erros esperados
    - Trate explicitamente:
        - 401: falha de autenticação
        - 403: sem autorização/API não liberada
        - 429: rate limit
        - 503: indisponibilidade temporária

Dados Que Você Deve Ter Para Configurar No Craftsman

"Loggi": {
"Enabled": true,
"BaseUrl": "https://api.loggi.com",
"ClientId": "...",
"ClientSecret": "...",
"CompanyId": "...",
"TokenPath": "/v2/oauth2/token",
"TrackingPathTemplate": "/v1/companies/{companyId}/packages/{trackingCode}/tracking"
}

Fontes oficiais:

- Loggi API: https://docs.api.loggi.com/
- Autenticação V2: https://docs.api.loggi.com/reference/authenticatev2
- Integrador: https://docs.api.loggi.com/reference/createintegratorcompany
- Rastreio: https://docs.api.loggi.com/reference/trackpackagetrackingcode
- Webhook: https://docs.api.loggi.com/reference/webhook