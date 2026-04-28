 # Plano IN-008: Estudo da Integração com Correios

## Resumo

- Criar o estudo em .codex/tasks/integration/plan-in-008.md, seguindo o padrão de plan-in-006.md e plan-in-007.md.
- Conclusão técnica recomendada: o primeiro escopo deve ser rastreio, não postagem completa.
- Motivo: o modelo atual do Craftsman já possui Shipment, ShipmentStatus e IShippingTracker; cálculo de frete e pré-postagem exigem contrato,
cartão de postagem, credenciais e dados adicionais ainda fora do modelo interno.

## Conteúdo do Estudo

- Documentar APIs e canais oficiais:
    - Token: autenticação REST com login/senha, contrato e/ou cartão de postagem.
    - Preço: cálculo de frete, API restrita a cliente com contrato e serviço 38202 API PRECOS.
    - Prazo: cálculo de prazo, API restrita a cliente com contrato e serviço 38210 API PRAZOS.
    - Rastro: tecnicamente relevante para IShippingTracker, mas com restrição recente para objetos vinculados ao contrato do remetente.
    - Pré-Postagem/PPN: viável para postagem e etiquetas, mas com maior dependência operacional.
- Mapear eventos Correios para ShipmentStatus:
    - Objeto criado/pré-postado/aguardando postagem -> Created.
    - Postado, encaminhado, em trânsito, saiu para entrega -> InTransit.
    - Tentativa não efetuada, destinatário ausente, aguardando retirada -> DeliveryAttempted.
    - Entregue -> Delivered.
    - Cancelado, devolvido sem entrega ou objeto não utilizável -> avaliar como Cancelled somente quando o fluxo interno decidir encerrar o
    envio.
- Mapear dados mínimos:
    - Código de rastreio -> Shipment.TrackingCode.
    - Data/hora do evento externo -> usado para auditoria futura; o modelo atual só guarda ShippedAt e DeliveredAt.
    - Status externo, descrição, unidade/localidade, UF e município -> fora do modelo atual, recomendados para histórico futuro.
    - Frete: CEP origem/destino, serviço, peso, dimensões e adicionais.
    - Postagem: contrato, cartão de postagem, remetente, destinatário, embalagem, declaração de conteúdo/NF-e, serviço e geração de rótulo.
- Registrar riscos:
    - APIs públicas e privadas têm disponibilidade diferente conforme login, contrato e permissões no CWS.
    - Chaves de subdelegação podem expirar em até 180 dias.
    - Rastro restringe consulta a objetos vinculados ao contrato do remetente.
    - Pré-postagem e etiquetas dependem de contrato/cartão, cadastro de remetente/destinatário/embalagem e validações operacionais.
    - O modelo atual não possui histórico de eventos de rastreio nem campos completos de etiqueta/frete.

## Alterações Planejadas

- Adicionar .codex/tasks/integration/plan-in-008.md com:
    - viabilidade;
    - mapeamento para Shipping Context;
- Atualizar .codex/tasks/integration/tasks.md marcando os cinco itens da IN-008 como concluídos somente depois que o estudo estiver escrito.
- Não alterar código de produção nesta tarefa; ela é documental/de descoberta.

## Critérios de Aceitação

- O estudo define claramente que o primeiro escopo recomendado é rastreio, com possível evolução posterior para cálculo de frete e pré-postagem.
- Eventos de rastreamento ficam mapeados para o modelo interno atual de envio.
- Credenciais, contrato, cartão de postagem, CWS, token e restrições de API ficam documentados.
- Riscos operacionais e dependências comerciais ficam explícitos.
- O documento cita fontes oficiais consultadas.

## Fontes Oficiais

- Correios Desenvolvedores: https://www.correios.com.br/atendimento/developers/
- Manual API Token: https://www.correios.com.br/atendimento/developers/manuais/manual-uso-da-api-token
- Manual API Preço: https://www.correios.com.br/atendimento/developers/manuais/manual-api-preco-1
- Manual API Prazo: https://www.correios.com.br/atendimento/developers/manuais/manual-api-prazo
- Manual Correios Web Service: https://www.correios.com.br/atendimento/developers/manuais/correioswebservice
- Manual PPN Web: https://www.correios.com.br/atendimento/developers/manual-do-usuario-ppn

## Assumptions

- O estudo deve seguir o formato dos documentos já existentes em .codex/tasks/integration/plan-in-006.md e plan-in-007.md.
- Não há implementação de cliente HTTP Correios nesta tarefa.
- A integração ativa inicial será desenhada para o contrato IShippingTracker, mantendo o domínio desacoplado das APIs externas.