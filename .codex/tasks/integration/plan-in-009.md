# IN-009: Estudo da Integração com Loggi

## Resumo

- Criar o estudo em .codex/tasks/integration/plan-in-009.md, seguindo o padrão dos estudos de integração já existentes.
- Conclusão técnica recomendada: a Loggi é viável como integração ativa, mas o primeiro escopo deve ser rastreio por API e webhook, não criação
completa de envio.
- Motivo: o Craftsman já possui Shipment, ShipmentStatus e IShippingTracker; cotação, criação de pacotes e etiquetas exigem company_id,
credenciais, dados de endereço/pacote, homologação comercial e campos ainda fora do modelo atual.

## Conteúdo do Estudo

- Documentar APIs oficiais Loggi:
    - Autenticação V2: POST /v2/oauth2/token, com client_id e client_secret.
    - Integrador: POST /v1/integrator/activate, usando integration_code para obter/ativar companyId.
    - Cotação: POST /v1/companies/{company_id}/quotations.
    - Criação assíncrona de envio: POST /v1/companies/{company_id}/async-shipments.
    - Etiqueta: POST /v1/companies/{company_id}/labels.
    - Rastreio: GET /v1/companies/{company_id}/packages/{tracking_code}/tracking.
    - Webhook de rastreio: endpoint HTTPS configurado via time de vendas, com Basic Authentication.
    - Loggi Pontos: POST /dropoff/locations, útil para cobertura/dropoff.
- Mapear eventos Loggi para ShipmentStatus:
    - 1 Adicionado, 28 Coleta pendente -> Created.
    - 3, 4, 11, 13, 14, 15, 16, 17, 24, 25, 30 -> InTransit.
    - 12 Imprevisto, 18 Destinatário ausente, 10 Retirar nos correios, 20 Pendência interna, 21 Endereço errado, 22 Aguardando ação do remetente
    -> DeliveryAttempted.
    - 5 Entregue -> Delivered.
    - 2 Cancelado, 6 Recusado, 7 Devolução iniciada, 8 Devolvido, 9 Extraviado, 19 Avariado, 23 Roubado/furtado, 26 Dados incorretos, 27 Pacote
    não integrado, 29 Retido pela fiscalização -> não converter automaticamente para Cancelled; registrar como ocorrência externa e exigir
    decisão operacional no modelo atual.
- Mapear dados mínimos:
    - trackingCode -> Shipment.TrackingCode.
    - status Loggi atual -> TrackedShipmentStatus.Status.
    - status.updatedTime -> TrackedShipmentStatus.TrackedAt.
    - loggiKey, histórico, localidade, data prometida e ação requerida -> fora do modelo atual; recomendar histórico futuro de eventos de
    rastreio.

## Alterações Planejadas

- Adicionar .codex/tasks/integration/plan-in-009.md com viabilidade, autenticação, contrato/cobertura, APIs, mapeamento de eventos, riscos e
primeiro escopo.
- Atualizar .codex/tasks/integration/tasks.md marcando os cinco itens da IN-009 como concluídos somente depois que o estudo estiver escrito.
## Critérios de Aceitação
- Eventos relevantes da Loggi ficam mapeados para o modelo interno atual de envio.
- Credenciais, company_id, integration_code, contato com vendas/Sales Engineering e disponibilidade regional ficam documentados.
- Riscos operacionais, cobertura, rate limit 429, autorização 403, indisponibilidade 503 e dependências comerciais ficam explícitos.
- O documento cita fontes oficiais consultadas.

## Fontes Oficiais

- Loggi API: https://docs.api.loggi.com/
- Sobre a API: https://docs.api.loggi.com/reference/orienta%C3%A7%C3%B5es
- Autenticação V2: https://docs.api.loggi.com/reference/authenticatev2
- Integrador: https://docs.api.loggi.com/reference/createintegratorcompany
- Cotação: https://docs.api.loggi.com/reference/quote
- Criar Shipment assíncrono: https://docs.api.loggi.com/reference/createasyncshipment
- Etiqueta: https://docs.api.loggi.com/reference/criaretiqueta-1
- Tracking: https://docs.api.loggi.com/reference/trackingapi
- Realizar Rastreio: https://docs.api.loggi.com/reference/trackpackagetrackingcode
- Webhook: https://docs.api.loggi.com/reference/webhook
- Loggi Pontos: https://docs.api.loggi.com/reference/listdropofflocations

## Assumptions

- O estudo deve seguir o formato de .codex/tasks/integration/plan-in-006.md, plan-in-007.md e plan-in-008.md.
- A primeira integração real com Loggi deve preservar o domínio desacoplado, via IShippingTracker.
- Criação de envio, cotação, etiqueta e cancelamento ficam como evolução posterior porque exigem dados e decisões operacionais que o modelo
atual ainda não representa.