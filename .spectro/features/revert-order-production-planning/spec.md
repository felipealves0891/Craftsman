<!-- spectro-managed: true -->
# Especificação: Reverter envio do pedido para produção

## Problema e objetivo

- Problema: Depois que um pedido é enviado para produção, o sistema cria tarefas, consome matérias-primas e altera o status do pedido para `ReadyForProduction`, mas não oferece uma operação explícita para desfazer esse planejamento. A edição do pedido possui uma reversão parcial, porém ela não representa uma ação de negócio independente nem atende estados posteriores do fluxo.
- Objetivo: Permitir que um usuário autorizado solicite a reversão do envio para produção, preservando a rastreabilidade do estoque, das tarefas, do pedido e da auditoria, e rejeitando com segurança os casos que não possam ser revertidos conforme a política de negócio aprovada.

## Escopo

### Incluído

- Disponibilizar uma operação explícita e protegida para reverter o planejamento de produção de um pedido.
- Desfazer os efeitos de planejamento que forem elegíveis: tarefas de produção, consumo de matérias-primas e estado do pedido.
- Registrar a operação e seu resultado para auditoria e investigação.
- Informar sucesso ou motivo seguro da rejeição ao usuário.
- Cobrir concorrência, repetição da solicitação e consistência entre pedido, tarefas e estoque.

### Excluído

- Alterar as regras existentes de importação, deduplicação, vínculo de produtos, ficha técnica ou planejamento inicial.
- Excluir ou editar movimentações históricas de estoque para simular a reversão; a correção deve ser registrada por novas movimentações compensatórias.
- Reverter automaticamente pedidos enviados, em trânsito ou entregues.
- Reverter tarefas de produção concluídas ou a produção física correspondente.
- Reverter acertos financeiros já gerados ou envios, enquanto não houver política de negócio específica para esses casos.
- Alterar a fórmula ou o momento de geração do acerto financeiro.

## Requisitos

### Requisitos funcionais

- `FR-001`: O sistema deve disponibilizar uma ação explícita para solicitar a reversão do envio para produção de um pedido, acessível somente a usuários com permissão de escrita e protegida contra requisições forjadas.
- `FR-002`: Ao aceitar a reversão, o sistema deve excluir as tarefas de produção que não estejam `Completed`, registrar compensações de estoque para cada consumo de matéria-prima associado e retornar o pedido a `Normalized`, deixando-o apto a novo planejamento.
- `FR-003`: A reversão deve ser rastreável até o pedido, às tarefas afetadas e às movimentações originais e compensatórias, sem apagar ou alterar o histórico original do estoque.
- `FR-004`: O sistema deve rejeitar a reversão quando o pedido ou qualquer tarefa relacionada estiver em um estado não elegível pela política de negócio, informando uma mensagem segura e sem executar efeitos parciais.
- `FR-005`: Uma repetição da mesma solicitação, inclusive após uma falha recuperável, não deve duplicar compensações de estoque, cancelar novamente tarefas já tratadas ou apresentar sucesso falso.
- `FR-006`: O sistema deve registrar auditoria da solicitação de reversão, contendo ao menos o pedido, o usuário, o resultado, a causa da rejeição quando aplicável e a identificação dos efeitos revertidos.
- `FR-007`: Após uma reversão concluída, as telas de pedido e produção devem refletir o novo estado sem oferecer ações incompatíveis com as tarefas ou o pedido revertidos.

### Requisitos não funcionais

- `NFR-001`: A operação deve preservar consistência transacional entre estado do pedido, tarefas de produção, compensações de estoque e auditoria; uma falha não pode ser apresentada como reversão concluída.
- `NFR-002`: A operação deve tratar concorrência com o início ou avanço da produção, impedindo que uma reversão confirme um estado incompatível com uma alteração concorrente.
- `NFR-003`: Mensagens ao usuário e registros de diagnóstico não devem expor credenciais, tokens, segredos, SQL, stack traces ou dados sensíveis desnecessários.
- `NFR-004`: As evidências da operação devem permitir localizar a tentativa pelo pedido e por um identificador de correlação ou auditoria já existente.

### Restrições

- `CON-001`: A solução deve respeitar a separação entre App, Domains e Infra e os contratos de persistência existentes com Entity Framework Core e PostgreSQL.
- `CON-002`: O estoque deve continuar sendo derivado de movimentações auditáveis; a reversão deve criar movimentos compensatórios rastreáveis e não permitir saldo negativo.
- `CON-003`: As transições de status devem permanecer protegidas pelas regras de domínio, sem alteração direta de estado pela interface ou pela infraestrutura.
- `CON-004`: A implementação não deve transformar a operação de exclusão do pedido em substituto silencioso da reversão do planejamento.
- `CON-005`: A reversão é elegível enquanto nenhuma tarefa relacionada estiver `Completed`; toda tarefa relacionada com status diferente de `Completed` pode ser excluída, respeitando a consistência do estoque e a concorrência.

## Critérios de aceite

- `AC-001` (`FR-001`): Dado um usuário autorizado e um pedido elegível, quando ele confirmar a ação de reversão, então o sistema executa a operação protegida e apresenta o resultado ao usuário.
- `AC-002` (`FR-002`, `CON-002`): Dado um pedido cujas tarefas não estejam `Completed` e que possua consumos de estoque vinculados, quando a reversão for concluída, então cada consumo elegível possui uma movimentação compensatória rastreável, as tarefas são excluídas, o saldo é recalculado corretamente e o pedido fica em `Normalized`, apto a novo planejamento.
- `AC-003` (`FR-003`): Dado um pedido revertido, quando forem consultados o pedido, a produção e o histórico de estoque, então é possível relacionar a operação, as tarefas afetadas, os consumos originais e as compensações sem alteração ou remoção do histórico original.
- `AC-004` (`FR-004`): Dado um pedido com ao menos uma tarefa `Completed`, quando a reversão for solicitada, então a operação é rejeitada com mensagem segura, nenhum consumo é compensado parcialmente, nenhuma tarefa é excluída e nenhum estado relacionado é apresentado como revertido.
- `AC-005` (`FR-005`, `NFR-001`): Dada uma reversão já concluída, quando a mesma solicitação for repetida, então não são criadas compensações duplicadas nem novos efeitos de cancelamento, e o resultado permanece consistente.
- `AC-006` (`FR-005`, `NFR-002`): Dada uma reversão concorrente com o início ou avanço de uma tarefa, quando houver conflito de persistência ou de estado, então a operação não confirma uma reversão incompatível, informa falha segura e preserva a alteração concorrente sem sobrescrevê-la silenciosamente.
- `AC-007` (`FR-006`, `NFR-004`): Dada uma tentativa de reversão, quando a auditoria for consultada, então ela identifica pedido, usuário, resultado, correlação e causa ou efeitos relevantes da operação.
- `AC-008` (`FR-007`): Dada uma reversão concluída, quando as telas de pedido e produção forem abertas novamente, então elas exibem os estados atuais e não oferecem ações incompatíveis com a situação revertida.
- `AC-009` (`FR-001`, `NFR-003`): Dado um usuário sem permissão ou uma requisição sem proteção antiforgery válida, quando tentar executar a reversão, então a operação não altera dados e não expõe detalhes internos.

## Riscos, dependências e perguntas abertas

- Dependência: O consumo de estoque é associado à tarefa por `BusinessReference`; a implementação deve preservar essa relação ou definir uma forma equivalente de rastreabilidade.
- Dependência: A auditoria existente deve ser reutilizada ou estendida para registrar a operação sem criar uma trilha paralela incompatível.
- Risco: Permitir reversão de tarefas `InProduction` pode devolver materiais que já foram consumidos fisicamente ou gerar divergência operacional; essa é uma decisão de negócio confirmada e deve ser evidenciada na operação.
- Risco: Remover tarefas preserva menos histórico do que marcá-las como canceladas; a escolha afeta consultas, auditoria e replanejamento.
- Risco: Uma falha entre a compensação do estoque e a atualização do pedido pode produzir estado parcial se não houver fronteira transacional adequada.
- Decisão: A reversão é permitida quando nenhuma tarefa estiver `Completed`; toda tarefa relacionada com status diferente de `Completed` é elegível.
- Decisão: As tarefas elegíveis são excluídas, permitindo que o pedido seja planejado novamente.
- Decisão: O pedido retorna a `Normalized` e deve permanecer apto a novo planejamento.
- Decisão: A ação é disponibilizada no fluxo de detalhes do pedido; a agenda deve refletir a ausência das tarefas excluídas.
- Decisão provisória: Envio iniciado/entregue e acerto financeiro existente ficam fora da reversão automática até haver política específica.
