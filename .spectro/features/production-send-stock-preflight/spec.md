<!-- spectro-managed: true -->
# Especificação: Pré-validação de estoque antes do envio para produção

## Problema e objetivo

- Problema: A tela de detalhes do pedido permite enviar diretamente um produto/pedido para produção. O usuário não consegue confirmar previamente quais matérias-primas serão necessárias nem comparar as quantidades requeridas com o estoque disponível. Quando o envio falha, a mensagem atual agrupa causas diferentes em um aviso genérico, dificultando a correção.
- Objetivo: Apresentar uma etapa intermediária de conferência antes do envio para produção, informar os itens necessários e seus saldos, impedir o envio quando faltar estoque e comunicar falhas posteriores com mensagens claras, úteis e seguras.

## Escopo

### Incluído

- Exibir, antes da confirmação do envio, as matérias-primas necessárias para produzir os itens do pedido, a quantidade necessária, a quantidade disponível e a unidade de medida quando esses dados existirem.
- Destacar em vermelho cada matéria-prima cuja disponibilidade seja menor que a quantidade necessária e informar a diferença ou a condição de insuficiência.
- Bloquear a confirmação do envio enquanto existir qualquer matéria-prima insuficiente ou enquanto a pré-validação não puder ser concluída com segurança.
- Permitir a confirmação quando todas as matérias-primas necessárias estiverem disponíveis, sem substituir a validação definitiva do fluxo de produção.
- Apresentar mensagens distintas e orientadas à ação para falhas conhecidas do envio, sem expor detalhes internos.
- Manter a consistência, atomicidade, rastreabilidade e proteção contra estoque negativo definidas para o envio para produção.

### Excluído

- Alterar a fórmula de disponibilidade, a ficha técnica, as unidades, o arredondamento ou a política de estoque mínimo sem decisão de negócio explícita.
- Permitir envio parcial, consumo parcial ou saldo negativo.
- Corrigir ou apagar movimentações históricas de estoque.
- Alterar regras de autenticação, autorização, deduplicação de pedidos, cronograma de produção ou cálculo financeiro.
- Substituir a validação transacional no servidor por uma validação exclusivamente visual na interface.

## Requisitos

### Requisitos funcionais

- `FR-001`: Ao iniciar o envio de um pedido elegível para produção, o sistema deve apresentar uma etapa intermediária de conferência antes de executar o envio.
- `FR-002`: A etapa intermediária deve listar cada matéria-prima necessária pela ficha técnica agregada do pedido, com nome ou identificação, quantidade requerida, quantidade disponível e unidade de medida aplicável.
- `FR-003`: O sistema deve marcar visualmente em vermelho cada matéria-prima cuja quantidade disponível seja menor que a quantidade requerida e informar que o estoque é insuficiente.
- `FR-004`: O sistema deve manter desabilitada ou indisponível a ação de confirmar o envio quando houver ao menos uma matéria-prima insuficiente, quando houver erro na obtenção da conferência ou quando os pré-requisitos do planejamento não puderem ser determinados. Produto sem vínculo, ficha técnica inválida ou matéria-prima ausente/inativa deve ser apresentado com mensagem específica de erro de configuração e permanecer bloqueado.
- `FR-005`: Quando a conferência indicar disponibilidade suficiente para todas as matérias-primas e os demais pré-requisitos forem atendidos, o usuário deve poder confirmar o envio para produção.
- `FR-006`: No momento da confirmação, o servidor deve repetir a validação necessária e impedir o envio se o estoque tiver mudado desde a conferência, sem criar consumo, tarefa ou transição parcial.
- `FR-007`: Após uma tentativa de envio, o sistema deve informar o resultado com mensagem específica para a causa conhecida, incluindo pelo menos: estoque insuficiente, produto sem vínculo ou ficha técnica válida, planejamento já existente, conflito de concorrência e falha inesperada segura.
- `FR-008`: As mensagens de falha devem indicar a operação afetada e a próxima ação possível, como revisar os itens, atualizar a tela, corrigir o vínculo/ficha técnica ou tentar novamente, quando aplicável. Em falha inesperada, devem exibir também o identificador de correlação seguro para suporte.

### Requisitos não funcionais

- `NFR-001`: A conferência deve refletir os mesmos conceitos de disponibilidade, unidade e precisão usados pela validação definitiva do planejamento; não deve apresentar como disponível um saldo que o servidor rejeitaria por regra conhecida.
- `NFR-002`: O fluxo deve preservar consistência transacional entre validação, tarefas, movimentações de estoque e estado do pedido, inclusive quando houver concorrência após a conferência.
- `NFR-003`: As mensagens exibidas ao usuário e os diagnósticos associados devem ser claros e pesquisáveis, mas não podem expor stack trace, SQL, credenciais, tokens, segredos ou detalhes internos desnecessários.
- `NFR-004`: A etapa intermediária deve permitir que o usuário identifique rapidamente todos os itens insuficientes, sem depender apenas de cor para comunicar o bloqueio.

### Restrições

- `CON-001`: A solução deve respeitar a separação entre App, Domains e Infra e os modelos/regras existentes de pedidos, produção e estoque.
- `CON-002`: A regra de negócio continua sendo que a quantidade disponível deve ser maior ou igual à quantidade necessária; a pré-validação não autoriza estoque negativo nem consumo parcial.
- `CON-003`: A validação da interface é informativa e preventiva; a autorização efetiva do envio permanece no servidor e deve reavaliar o estado atual.
- `CON-004`: A implementação deve reutilizar as capacidades existentes da solução sempre que atenderem ao requisito e não deve introduzir dependência ou arquitetura nova sem necessidade demonstrável.
- `CON-005`: A feature é complementar à especificação `fix-production-order-stock-validation`; suas garantias de atomicidade, rastreabilidade e não alteração do histórico permanecem aplicáveis.

## Critérios de aceite

- `AC-001` (`FR-001`, `FR-002`): Dado um pedido elegível com ficha técnica válida, quando o usuário iniciar o envio para produção, então uma etapa intermediária é exibida antes de qualquer confirmação e lista todas as matérias-primas agregadas com quantidade requerida, disponível e unidade aplicável.
- `AC-002` (`FR-003`, `NFR-004`): Dado que uma matéria-prima requer 10 unidades e possui 6 disponíveis, quando a conferência for exibida, então essa linha é destacada em vermelho, informa a insuficiência de 4 unidades e comunica o problema também por texto ou rótulo acessível.
- `AC-003` (`FR-004`): Dado um pedido com uma ou mais matérias-primas insuficientes, quando o usuário visualizar a conferência, então a confirmação de envio permanece bloqueada e nenhuma movimentação, tarefa ou alteração de status é criada.
- `AC-004` (`FR-004`): Dado que a conferência não consegue determinar com segurança os requisitos ou o estoque, ou que exista produto sem vínculo, ficha técnica inválida ou matéria-prima ausente/inativa, quando o usuário tentar prosseguir, então o envio fica bloqueado e a modal informa especificamente o erro de configuração ou a necessidade de atualizar/corrigir os pré-requisitos, sem tratar a operação como aprovada.
- `AC-005` (`FR-005`): Dado um pedido cujas matérias-primas possuem disponibilidade maior ou igual à quantidade requerida, quando o usuário confirmar o envio, então a solicitação é encaminhada ao fluxo normal de planejamento.
- `AC-006` (`FR-006`, `NFR-002`): Dado que o estoque se torna insuficiente depois da conferência e antes da confirmação efetiva, quando o usuário confirmar, então o servidor rejeita ou revalida de forma segura, informa que o estoque mudou e não persiste consumo, tarefa ou transição parcial.
- `AC-007` (`FR-007`, `FR-008`): Dado um envio rejeitado por estoque insuficiente, quando o resultado for apresentado, então a mensagem identifica estoque insuficiente e orienta revisar ou repor as matérias-primas, sem apresentar a falha como erro genérico de sistema.
- `AC-008` (`FR-007`, `FR-008`): Dado um envio rejeitado por vínculo ausente, ficha técnica inválida, planejamento existente ou conflito de concorrência, quando o resultado for apresentado, então a mensagem diferencia a causa observada e orienta a ação correspondente, incluindo atualização e nova tentativa quando aplicável.
- `AC-009` (`NFR-003`): Dado um erro inesperado durante o envio, quando a resposta for apresentada ao usuário, então ela informa que o envio não foi concluído, exibe o identificador de correlação seguro para suporte/diagnóstico e não expõe stack trace, SQL ou segredos.
- `AC-010` (`CON-005`): Dado qualquer resultado de conferência ou envio, quando a operação terminar, então as garantias da validação transacional existente permanecem válidas: não há saldo negativo, consumo parcial confirmado, tarefa parcial ou alteração de histórico para mascarar falha.

## Riscos, dependências e perguntas abertas

- Dependência: O fluxo atual parte de `Orders/Details` e `Orders/SendToProduction`; a forma exata da tela intermediária e dos modelos de apresentação deve ser definida no plano sem duplicar as regras de domínio.
- Dependência: A conferência precisa obter a composição da ficha técnica e a disponibilidade agregada por matéria-prima usando os mesmos critérios do planejador.
- Dependência: A mensagem de concorrência já possui um caso específico no fluxo atual; as demais causas devem ser diferenciadas sem alterar o contrato de segurança das respostas.
- Risco: O saldo pode mudar entre a consulta da tela e a confirmação. A conferência não elimina a necessidade de revalidação e controle transacional no servidor.
- Risco: Erros de unidade, precisão ou agregação podem fazer a tela divergir da decisão do planejamento; os testes devem cobrir produtos repetidos e matérias-primas compartilhadas.
- Decisão `CL-004`: A etapa intermediária será uma modal na tela de detalhes do pedido.
- Decisão `CL-005`: Produto sem vínculo, ficha técnica inválida ou matéria-prima ausente/inativa será apresentado com mensagem específica de erro de configuração e bloqueará o envio.
- Decisão `CL-006`: Falhas inesperadas exibirão o identificador de correlação seguro para suporte, além da mensagem orientando a próxima ação.

