<!-- spectro-managed: true -->
# Plano: Pré-validação de estoque antes do envio para produção

## Contexto e objetivo técnico

- O fluxo atual de `Orders/Details` renderiza o componente Blazor estático `OrderDetailsPage` dentro de uma view MVC e exibe um POST direto para `Orders/SendToProduction`.
- `OrderProductionPlanningService`/`ProductionPlanner` já implementam a validação e o consumo do planejamento, mas o resultado público do envio é booleano e o controller perde a causa específica da rejeição.
- O objetivo técnico é disponibilizar uma prévia de produção com requisitos agregados por matéria-prima, saldo atual e estado de disponibilidade; renderizá-la em uma modal; manter a confirmação protegida por servidor/revalidação; e transformar falhas conhecidas em mensagens seguras e acionáveis.
- A solução deve complementar a feature concluída `fix-production-order-stock-validation`, sem reabrir a correção histórica nem alterar movimentações já persistidas.

## Solução proposta

- Extrair ou reutilizar, no domínio de produção, a preparação dos requisitos agregados do pedido para que a prévia e o planejamento usem a mesma fórmula de ficha técnica, unidade e precisão. Não duplicar `quantity-per-unit * order-quantity` em App/UI.
- Expor um resultado de prévia que contenha cada matéria-prima necessária, quantidade requerida, saldo disponível, unidade, status de disponibilidade e eventual diagnóstico seguro de configuração. A ordem da apresentação deve ser determinística.
- Obter a prévia durante `Orders/Details` e passá-la ao `OrderDetailsPage`, mantendo o padrão atual de MVC + componente Blazor estático. A modal será aberta pelo botão existente e conterá a confirmação POST com antiforgery.
- Desabilitar a confirmação na renderização quando houver insuficiência, erro de consulta ou pré-requisito de configuração inválido. A cor vermelha será acompanhada de texto/estado acessível.
- Alterar o contrato interno do planejamento para preservar uma causa tipada de falha (por exemplo, insuficiência, configuração, planejamento existente ou concorrência), sem expor exceções técnicas à UI. O controller mapeará cada causa para uma mensagem em português e usará a correlação já disponível no contexto da requisição para falhas inesperadas.
- Manter `Orders/SendToProduction` como fronteira de autoridade: a prévia é informativa, a confirmação reavalia o estado atual e a transação/atomicidade existente continua responsável por impedir consumo, tarefa ou estado parcial.

## Arquitetura e contratos

- `src/Domains/Production/Services` e `src/Domains/Inventory/Services`: introduzir o menor contrato/result type necessário para preparar requisitos e consultar disponibilidade completa, reutilizando `InventoryService` e repositórios existentes. O domínio não deve depender de MVC, Razor, TempData ou Entity Framework.
- `src/Domains/Production/Services/ProductionPlanner.cs`: compartilhar a preparação de requisitos com a prévia e preservar criação de tarefas, janela de produção e consumo existentes. A extração não deve mudar a regra de estoque mínimo operacional.
- `src/App/Services/OrderQueryService.cs` ou serviço de aplicação coeso: compor os dados do pedido com a prévia e converter o resultado de domínio para um view model de apresentação, incluindo estado bloqueante e mensagens seguras.
- `src/App/Models/OrderDetailViewModel.cs` e modelos relacionados: transportar a coleção imutável de linhas de prévia; não transportar entidades de domínio nem detalhes de exceção.
- `src/App/Views/Orders/Details.cshtml` e `src/UI/Components/Orders/OrderDetailsPage.razor`: renderizar a modal Bootstrap existente, tabela de matéria-prima, estados textual/visual, ação confirmável e mensagem de correlação quando fornecida. Preservar autorização, antiforgery e ações existentes.
- `src/App/Controllers/OrdersController.cs`: carregar o resultado tipado do planejamento, registrar auditoria/diagnóstico com pedido, resultado e correlação segura e mapear falhas conhecidas para TempData. Não registrar stack trace, SQL, segredo ou payload sensível.
- Persistência: não há alteração de esquema, entidade ou migration prevista. A prévia é leitura; o envio continua usando as transações e movimentações auditáveis já existentes.
- Contrato de concorrência: o valor exibido pode ficar obsoleto; a confirmação deve revalidar e, em divergência, retornar mensagem específica de estoque alterado/conflito sem persistir efeitos parciais.

## Alternativas consideradas

- Criar uma página ou endpoint separado para a prévia: rejeitado nesta feature porque a decisão humana escolheu modal na tela de detalhes e o fluxo atual já fornece os dados do pedido.
- Implementar somente JavaScript no browser para calcular estoque: rejeitado porque a UI não é fronteira de confiança e não possui autoridade sobre ficha técnica, saldo ou autorização.
- Reutilizar apenas `MissingMaterial`: insuficiente porque ele representa somente faltas e IDs; a modal precisa exibir todas as matérias-primas, nomes, unidades, requerido e disponível.
- Manter `bool` e inferir a causa a partir de texto de exceção: rejeitado por fragilidade e mensagens instáveis; o resultado deve preservar causa tipada no limite de aplicação.
- Criar uma nova tabela ou reserva de estoque para a prévia: fora do escopo; a prévia não reserva nem altera saldo.

## Riscos e compatibilidade

- A extração da preparação compartilhada pode afetar o planner já corrigido. Mitigação: testes de regressão para sucesso, insuficiência, produto repetido, matéria-prima compartilhada e falha posterior antes de alterar o fluxo.
- O saldo pode mudar entre GET e POST. Mitigação: revalidação no servidor e testes HTTP/persistência que verifiquem ausência de consumo/tarefa parcial.
- A tela atual usa componente estático; não presumir interatividade Blazor. Mitigação: modal Bootstrap com HTML/formulário renderizado no servidor e comportamento compatível com o JavaScript já carregado.
- A correlação existente vem do contexto de usuário/auditoria. Mitigação: reutilizar o identificador já gerado, exibi-lo somente em falha inesperada e validar que não contém PII/segredo.
- O texto de “erro de configuração” deve distinguir configuração de estoque insuficiente. Mitigação: resultado tipado e critérios HTTP/renderizados para vínculo, produto inativo, ficha ausente e matéria-prima inativa.
- Não há cobertura declarada de comportamento real de locks PostgreSQL neste plano se a infraestrutura de integração não estiver disponível; a limitação deve ser registrada nos gates, nunca mascarada.

## Estratégia de testes

- Domínio: testar agregação de requisitos, quantidade exata como suficiente, insuficiência com diferença, nomes/unidades/saldos, matéria-prima compartilhada, produto sem vínculo, produto inativo, ficha vazia/inválida e ordem determinística.
- Aplicação/modelos: testar mapeamento para linhas da modal, bloqueio quando qualquer linha falha, texto específico de configuração e ausência de detalhes técnicos.
- UI/renderização: testar que a modal existe, exibe requerido/disponível/unidade, marca insuficiência em vermelho com texto, desabilita confirmação e mantém antiforgery/ação correta.
- HTTP: testar prévia em detalhes, envio bem-sucedido, insuficiência, configuração inválida, planejamento já existente, conflito de concorrência, falha inesperada com correlação e autorização/antiforgery.
- Persistência/regressão: confirmar que a prévia não cria movimentações; falhas no POST não criam tarefas, consumo ou alteração de status; sucesso preserva auditoria e rastreabilidade existentes.
- Segurança: revisar autorização Read/Write, antiforgery, IDOR no `id`, saída HTML codificada, não exposição de exceções e ausência de segredos/carga sensível nos logs.
- Gates obrigatórios: build com warnings-as-errors, testes, vulnerabilidades de pacotes e cobertura conforme `.spectro/config.yaml`.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001`, `FR-002`, `NFR-001` | `AC-001`, `AC-002` | `TASK-001`, `TASK-002`, `TASK-003` |
| `FR-003`, `FR-004`, `NFR-004` | `AC-002`, `AC-003`, `AC-004` | `TASK-002`, `TASK-003`, `TASK-005` |
| `FR-005`, `CON-003` | `AC-005` | `TASK-003`, `TASK-005` |
| `FR-006`, `NFR-002`, `CON-002` | `AC-006`, `AC-010` | `TASK-001`, `TASK-004`, `TASK-005` |
| `FR-007`, `FR-008`, `NFR-003` | `AC-007`, `AC-008`, `AC-009` | `TASK-004`, `TASK-005` |
| `CON-001`, `CON-004`, `CON-005` | `AC-001` a `AC-010` | `TASK-001` a `TASK-006` |
