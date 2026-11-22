<!-- spectro-managed: true -->
# Especificação: Exibir quebra por produto e estoque dos materiais no envio para produção

## Problema e objetivo

- Problema: o modal de conferência de estoque exibido antes do envio de um pedido para produção apresenta todas as matérias-primas em uma lista única. A pessoa usuária não consegue relacionar cada material ao produto do pedido que o utiliza, embora o planejamento seja preparado a partir dos itens/produtos e de suas fichas técnicas.
- Objetivo: tornar a conferência do envio compreensível por produto, apresentando a quantidade do produto no pedido e, dentro de cada produto, os materiais necessários com a quantidade disponível em estoque e os demais dados necessários para decidir se o envio pode ser confirmado.

## Contexto observado

- O modal é exibido na página de detalhes do pedido antes do POST de `Orders/SendToProduction`.
- A prévia atual expõe `ProductionPreflightMaterial` como uma coleção plana, com nome da matéria-prima, unidade, quantidade necessária, quantidade disponível, quantidade faltante, configuração e disponibilidade.
- O preparo do plano percorre os itens do pedido, consulta cada produto e sua ficha técnica, cria requisitos por item e agrega os requisitos por matéria-prima para a validação de estoque.
- A alteração solicitada é de visualização e de informação contextual da prévia; o envio confirmado continua sujeito às validações e regras de estoque existentes.

## Escopo

### Incluído

- Apresentar uma seção/bloco identificável para cada produto do pedido que participa da prévia de produção.
- Exibir a identificação do produto e a quantidade do produto considerada no pedido.
- Exibir, dentro da seção do produto, as matérias-primas relacionadas à ficha técnica e a quantidade necessária para a quantidade do produto apresentada.
- Exibir a quantidade disponível no estoque para cada matéria-prima, sua unidade e a situação de disponibilidade/configuração já usada para habilitar ou bloquear a confirmação.
- Preservar a indicação visual de insuficiência, incluindo a quantidade faltante quando aplicável.
- Manter a confirmação bloqueada quando a prévia não puder confirmar o planejamento, sem alterar a regra de negócio que decide a disponibilidade.

### Excluído

- Alterar a fórmula de disponibilidade, o consumo de estoque, a transação, o planejamento ou as movimentações auditáveis.
- Alterar a ficha técnica, o cadastro de produtos, a quantidade do pedido ou as regras de vínculo de itens.
- Criar reservas de estoque, estoque mínimo adicional, planejamento parcial ou confirmação por produto.
- Alterar o contrato de autenticação, autorização, antiforgery ou o endpoint de envio.
- Alterar a regra de negócio para disponibilidade ou consumo da mesma matéria-prima usada por mais de um produto; a apresentação deve seguir a decisão registrada em `clarifications.md`.

## Requisitos

### Requisitos funcionais

- `FR-001`: O modal de conferência de envio deve organizar a prévia em uma lista expansível de grupos identificáveis por produto do pedido, com uma sublista de materiais em cada grupo, em vez de apresentar somente uma lista global de matérias-primas.
- `FR-002`: Cada grupo de produto deve apresentar a identificação disponível para o produto e a quantidade do produto considerada no planejamento.
- `FR-003`: Cada grupo de produto deve listar as matérias-primas associadas à ficha técnica desse produto, com nome, quantidade necessária para o grupo, quantidade disponível em estoque e unidade de medida.
- `FR-004`: A prévia deve indicar, para cada matéria-prima, se está disponível, insuficiente ou com configuração inválida, preservando a quantidade faltante quando a matéria-prima estiver insuficiente.
- `FR-005`: A prévia deve continuar refletindo a decisão efetiva de confirmação do planejamento: o botão de envio somente pode ser confirmado quando todas as pré-condições já exigidas pelo fluxo forem atendidas.
- `FR-006`: Quando o pedido possuir mais de um produto, o modal deve permitir distinguir visualmente os materiais de cada produto e suas respectivas quantidades e deve apresentar um resumo consolidado por matéria-prima do pedido, sem perder a visão dos materiais necessários ao pedido.
- `FR-007`: Quando não for possível preparar os produtos ou materiais da prévia, o modal deve continuar exibindo uma mensagem segura e orientativa, sem inventar quantidades ou marcar o envio como disponível.
- `FR-008`: O resumo consolidado deve apresentar, para cada matéria-prima compartilhada ou não, a necessidade total do pedido, a disponibilidade global usada na validação, a unidade e a situação correspondente.

### Requisitos não funcionais

- `NFR-001`: A informação exibida deve ser consistente com a mesma fotografia de dados usada pela prévia de produção; a camada de apresentação não deve recalcular estoque ou alterar quantidades.
- `NFR-002`: A organização por produto deve permanecer legível em pedidos com vários produtos e em telas estreitas, sem remover os estados de erro e disponibilidade.
- `NFR-003`: A identificação e as quantidades exibidas devem usar os formatos e unidades já adotados pela aplicação, evitando arredondamento que esconda insuficiência.
- `NFR-004`: A mudança deve preservar os controles existentes de proteção do POST, autorização e antiforgery, sem expor dados sensíveis adicionais.

### Restrições

- `CON-001`: A especificação não escolhe a arquitetura de implementação; a solução deve respeitar as separações existentes entre App, Domains, Infra e UI.
- `CON-002`: A disponibilidade de estoque continua sendo derivada pelas regras e movimentações auditáveis existentes; a visualização não autoriza saldo negativo, consumo parcial ou alteração de histórico.
- `CON-003`: O fluxo deve continuar enviando uma única decisão de planejamento para o pedido; não deve existir confirmação parcial de apenas alguns produtos.
- `CON-004`: Nenhuma alteração de esquema, dependência externa ou pacote novo é necessária por esta especificação sem justificativa e aprovação compatível com o workflow.
- `CON-005`: Requisitos de apresentação devem ser implementados sem transformar hipótese sobre a regra de agregação de materiais em regra de negócio confirmada.

## Critérios de aceite

- `AC-001` (`FR-001`, `FR-002`): Dado um pedido elegível com um produto vinculado, quando o modal for aberto, então ele exibe um grupo identificável desse produto e sua quantidade considerada no planejamento.
- `AC-002` (`FR-003`): Dado um produto com ficha técnica, quando o modal for aberto, então o grupo do produto exibe cada matéria-prima relacionada com quantidade necessária, quantidade disponível no estoque e unidade de medida.
- `AC-003` (`FR-001`, `FR-006`): Dado um pedido com dois ou mais produtos, quando o modal for aberto, então os produtos aparecem em uma lista expansível, cada produto possui uma sublista de materiais e existe um resumo consolidado por matéria-prima, sem uma lista única sem referência de origem.
- `AC-004` (`FR-004`): Dado um material cuja quantidade disponível seja menor que a necessária, quando o modal for aberto, então o material aparece sinalizado como insuficiente e a quantidade faltante é exibida com a unidade correspondente.
- `AC-005` (`FR-004`): Dado um produto ou material com configuração inválida, quando o modal for aberto, então o problema aparece sinalizado e o controle de confirmação permanece bloqueado.
- `AC-006` (`FR-005`, `NFR-001`): Dado um pedido em que todas as pré-condições existentes sejam atendidas, quando a prévia for carregada, então a organização por produto não impede a confirmação e os valores exibidos correspondem aos dados usados para a decisão.
- `AC-007` (`FR-005`, `CON-003`): Dado um pedido com qualquer produto ou material indisponível, quando a prévia for carregada, então o botão de envio permanece bloqueado e não é possível confirmar somente os produtos disponíveis.
- `AC-008` (`FR-007`): Dado um pedido sem vínculo, produto, ficha técnica ou materiais determináveis, quando o modal for aberto, então uma mensagem orienta a correção e nenhuma quantidade ausente é apresentada como disponível.
- `AC-009` (`NFR-002`, `NFR-003`): Dado um pedido com vários grupos e quantidades decimais aplicáveis, quando o modal for visualizado em largura reduzida, então os grupos, unidades, estados de erro e quantidades continuam legíveis sem perda de informação relevante.
- `AC-010` (`NFR-004`): Dado o modal com a nova apresentação, quando o usuário confirmar o envio, então a requisição continua protegida pelos controles existentes e o fluxo de planejamento/estoque não sofre alteração de autorização ou antiforgery.
- `AC-011` (`FR-008`, `NFR-001`): Dado um pedido em que a mesma matéria-prima seja usada por mais de um produto, quando o modal for aberto, então o detalhe de cada produto exibe sua necessidade específica e o resumo consolidado exibe a necessidade total e a disponibilidade global sem recalcular uma disponibilidade diferente.

## Dependências, riscos e perguntas abertas

- Dependência: o modelo atual de preparação mantém os itens planejados por produto, mas a prévia de estoque consumida pela página é construída a partir dos requisitos agregados por matéria-prima; a implementação deverá decidir como transportar a relação produto → material sem duplicar ou distorcer a decisão global.
- Dependência: nomes/identificadores exibidos devem vir dos produtos e matérias-primas já carregados pelo fluxo, respeitando os contratos existentes.
- Risco: se a mesma matéria-prima for usada por vários produtos, repetir a disponibilidade global nos grupos pode induzir a pessoa usuária a somar saldos; a interface deve rotular claramente o resumo consolidado como a visão global e diferenciar necessidade por produto de disponibilidade compartilhada.
- Risco: recalcular a disponibilidade separadamente por grupo pode produzir valores divergentes da validação efetiva ou introduzir uma nova regra de negócio; isso não é autorizado por esta especificação.
- Decisão registrada: matérias-primas serão exibidas por produto em uma lista expansível e também em um resumo consolidado do pedido (`CL-001`).
- Decisão registrada: o cabeçalho do grupo usa o nome cadastrado do produto; não há código comercial dedicado no agregado observado (`CL-002`).

Revisão de segurança: concluída
Escopo revisado: modal de preflight em `src/UI/Components/Orders/OrderDetailsPage.razor`, preparação de plano, modelo de prévia, consulta de pedido e endpoint `Orders/SendToProduction`.
Verificações executadas: inspeção estática do código e da constituição Spectro; nenhuma alteração de produção, teste, scanner de segredos ou verificação de dependências foi executada nesta etapa de especificação.
Achados: nenhum achado crítico/alto identificado no escopo da mudança; os controles existentes de autorização, antiforgery e bloqueio da confirmação foram registrados como invariantes a preservar.
Riscos residuais / limitações: a regra de apresentação para matéria-prima compartilhada entre produtos permanece aberta; esta especificação não comprova a segurança ou a consistência da implementação futura.
