<!-- spectro-managed: true -->
# Especificação: Testes de integração dos controllers de pedidos e produção

## Problema e objetivo

- Problema: os testes existentes cobrem principalmente regras de domínio, serviços e alguns controllers de forma isolada; os fluxos HTTP de pedidos e produção ainda não têm uma cobertura de integração consolidada que valide o comportamento atravessando a aplicação e a persistência.
- Objetivo: criar uma suíte de testes de integração para os controllers de `Orders` e `Production`, usando as documentações em `docs` como referência comportamental, para detectar regressões nos contratos HTTP, transições de estado, planejamento, estoque, eventos e respostas de erro.

### Foco de regressão: `SendToProduction`

O fluxo `OrdersController.SendToProduction` recebe um pedido por POST, exige a política de escrita e antiforgery, tenta planejar a produção, registra auditoria e redireciona para os detalhes do pedido. A documentação de produção e o registro do bug em `.spectro/docs/bug-ao-enviar-pedido-para-producao.md` relatam um risco concreto: uma tentativa rejeitada por estoque insuficiente pode ter deixado movimentos de saída persistidos. Portanto, a suíte deve tratar como invariante verificável que uma rejeição de planejamento não cria tarefas, não altera o status do pedido e não grava consumo de estoque parcial.

## Escopo

### Incluído

- Fluxos expostos pelos controllers de pedidos, incluindo consulta e operações relacionadas ao envio/reversão de planejamento de produção, conforme os endpoints existentes.
- Fluxos expostos pelo controller de produção, incluindo consulta de tarefas/calendário e avanço operacional de tarefas, conforme os endpoints existentes.
- Execução através do pipeline HTTP da aplicação, com autenticação/autorização e antiforgery quando aplicáveis.
- Persistência e efeitos observáveis relacionados: pedido, tarefas de produção, status, movimentos de estoque e eventos persistidos.
- Cenários de sucesso, validação, autorização, estado inválido, estoque insuficiente, recurso inexistente, idempotência e falhas controladas nas fronteiras internas quando o comportamento estiver documentado.
- Uso do agente `integration-tests` na etapa de implementação e das documentações `docs/orders.md`, `docs/production.md` e `docs/manual_orders.md` como fontes de contexto.

### Excluído

- Alteração das regras de negócio, contratos HTTP ou arquitetura da aplicação.
- Correção de problemas descobertos pelos testes; essas correções deverão ser especificadas separadamente, salvo decisão posterior de escopo.
- Cobertura integral de Finance, Inventory, Shipping, integrações externas ou telas, exceto quando forem efeitos necessários para observar um fluxo dos controllers em escopo.
- Testes de carga, concorrência distribuída e comportamento de componentes ou serviços externos.
- Migração de versão do .NET, troca de provedor de banco ou inclusão de infraestrutura não necessária para executar os testes.

## Requisitos

### Requisitos funcionais

- `FR-001`: A suíte deve validar os endpoints de consulta de pedidos e produção através do pipeline HTTP, incluindo `Index`/`Details` de pedidos, `Index`/`CurrentMonth`/`Calendar` de produção, o formato/status da resposta e os dados persistidos ou retornados que constituem o contrato observado.
- `FR-002`: A suíte deve validar as operações mutáveis de pedidos documentadas, incluindo `SendToProduction`, `RevertProduction` e `Delete`, verificando a transição do pedido e os efeitos relacionados.
- `FR-003`: A suíte deve validar o avanço operacional de tarefas de produção (`Advance`), verificando as transições permitidas (`start`, `complete` e `cancel`), o vínculo com o pedido e os efeitos persistidos esperados.
- `FR-004`: A suíte deve validar rejeições de entradas ou pré-condições inválidas documentadas, incluindo pedido inexistente, tarefa inexistente, transição inválida, produto ou ficha técnica inválida e estoque insuficiente quando aplicáveis ao endpoint.
- `FR-005`: A suíte deve validar os controles de acesso e proteção contra requisições mutáveis sem os requisitos de segurança configurados, incluindo autenticação, política de leitura/escrita e antiforgery quando aplicável.
- `FR-006`: A suíte deve validar que operações repetidas ou reprocessadas não criam duplicação indevida de pedido, tarefa, consumo ou compensação, nos fluxos em que a documentação confirma idempotência ou deduplicação.
- `FR-007`: A suíte deve registrar os cenários e resultados de forma reproduzível, permitindo distinguir falha do comportamento testado de indisponibilidade ou configuração do ambiente de integração.
- `FR-008`: A suíte deve reforçar especificamente `SendToProduction` com cenários de planejamento elegível, estoque insuficiente após agregação das necessidades, produto inativo, ficha técnica ausente/inválida, item sem vínculo de produto, pedido inexistente, repetição da mesma operação e requisições concorrentes quando houver suporte observável no fluxo.
- `FR-009`: Para cada rejeição de `SendToProduction` causada por pré-condição do pedido, produto, ficha técnica ou estoque, a suíte deve verificar a consistência conjunta do pedido, tarefas, movimentos de estoque e auditoria, distinguindo a auditoria de tentativa (`Planned = false`) dos efeitos de um planejamento confirmado.

### Requisitos não funcionais

- `NFR-001`: Os testes devem usar as fronteiras internas reais de integração definidas pelo projeto, incluindo o pipeline HTTP e o Entity Framework Core InMemory como mecanismo de persistência do ambiente de testes, sem reduzir o cenário a chamadas diretas de métodos internos dos controllers.
- `NFR-002`: Os dados de teste devem ser isolados entre cenários e não podem depender de dados pessoais, credenciais reais, serviços, bancos, containers, rede ou qualquer outra dependência externa, nem de estado deixado por outro teste.
- `NFR-003`: A suíte deve permanecer compatível com o target efetivo do projeto (`net10.0`), as convenções de `tests/Craftsman.Tests` e os gates de build, testes, vulnerabilidades e cobertura definidos pelo projeto; esta feature não propõe migração de target.
- `NFR-004`: Os testes devem propagar cancelamento e liberar recursos de aplicação, banco e servidor de teste ao final de cada execução, quando essas fronteiras fizerem parte do harness.

### Restrições

- `CON-001`: A especificação deve ser implementada sem alterar código de produção nesta etapa de especificação e sem inventar requisitos além dos contratos documentados ou observados no código.
- `CON-002`: O agente `integration-tests` será utilizado para a implementação dos testes; a seleção final de classes auxiliares, fixtures e estratégia de banco deve respeitar as convenções já existentes no repositório.
- `CON-003`: As documentações em `docs` são a fonte de contexto solicitada, mas qualquer comportamento marcado como inferido ou não verificado deve ser tratado como hipótese até ser confirmado no código ou por teste.
- `CON-004`: Não devem ser adicionados pacotes, serviços externos, infraestrutura externa ou mecanismos de autenticação alternativos para executar a suíte; qualquer seam de teste deve reutilizar as convenções existentes e ter necessidade demonstrável e aprovação compatível com o workflow Spectro.

## Critérios de aceite

- `AC-001`: Dado um ambiente de teste configurado, quando uma requisição autenticada de leitura para um endpoint de pedidos ou produção é executada, então o teste valida o status, o conteúdo relevante e a leitura dos dados persistidos conforme o contrato do endpoint.
- `AC-002`: Dado um pedido elegível para planejamento, quando `SendToProduction` é executado com os requisitos de segurança válidos, então o teste confirma a resposta de sucesso e a transição/efeitos persistidos esperados, incluindo tarefas, consumo de estoque e auditoria quando aplicáveis.
- `AC-003`: Dado um pedido planejado sem remessa ou tarefa concluída, quando `RevertProduction` é executado, então o teste confirma o retorno para `Normalized`, a remoção das tarefas e os movimentos compensatórios auditáveis.
- `AC-004`: Dado um pedido elegível para exclusão, quando `Delete` é executado, então o teste confirma a remoção das dependências previstas e a conclusão do processo de exclusão sem compensações duplicadas.
- `AC-005`: Dado uma tarefa de produção em estado avançável, quando `Advance` recebe `start`, `complete` ou `cancel`, então o teste confirma a resposta de sucesso e o novo estado da tarefa e do pedido quando essa transição for prevista.
- `AC-006`: Dado um pedido com pré-condição inválida para planejamento/reversão, uma tarefa inexistente ou uma transição inválida, quando o endpoint é chamado, então o teste confirma a rejeição documentada e verifica que não há criação ou remoção parcial indevida de tarefas ou movimentos.
- `AC-007`: Dado estoque insuficiente, produto inativo, ficha técnica ausente/inválida ou item sem vínculo exigido pelo fluxo, quando o planejamento é solicitado, então o teste confirma a falha e a preservação dos invariantes de pedido, tarefas e estoque descritos na documentação.
- `AC-008`: Dado um usuário não autenticado ou sem a política necessária, quando acessa um endpoint protegido, então o teste confirma a resposta ou redirecionamento de acesso negado esperado, sem executar a operação.
- `AC-009`: Dado um POST mutável sem antiforgery válido, quando a requisição é enviada, então o teste confirma a rejeição e verifica que nenhum efeito persistente foi produzido.
- `AC-010`: Dado o mesmo pedido ou operação processado novamente, quando o endpoint é chamado nas condições em que existe deduplicação/idempotência documentada, então o teste confirma que não há duplicação de pedidos, tarefas, consumo ou compensação.
- `AC-011`: Dado um cenário de teste executado isoladamente e em conjunto com a suíte, quando a execução termina, então os dados e recursos do cenário são isolados/liberados e o resultado pode ser reproduzido sem depender da ordem dos testes.
- `AC-012`: Dado duas operações concorrentes sobre o mesmo pedido ou tarefa, quando executadas dentro da aplicação, então o teste confirma o tratamento documentado de conflito e a ausência de efeitos duplicados ou parcialmente aplicados.
- `AC-013`: Dado uma falha simulada após uma etapa intermediária de uma operação, quando o fluxo é concluído ou falha, então o teste confirma o rollback ou a preservação de consistência definido pelo comportamento da aplicação, sem depender de componente externo.
- `AC-014`: Dado o conjunto de testes implementado, quando os gates aplicáveis do projeto são executados, então build, testes, análise de vulnerabilidades e cobertura são reportados com evidências reais, sem alegar verificações não executadas.
- `AC-015`: Dado um pedido elegível com estoque suficiente para todos os itens, quando `SendToProduction` é chamado por HTTP com autenticação, autorização e antiforgery válidos, então a resposta redireciona para os detalhes e a verificação posterior encontra o status `ReadyForProduction`, exatamente as tarefas esperadas, os consumos correspondentes e uma auditoria `SendToProduction` indicando planejamento confirmado.
- `AC-016`: Dado um pedido cuja necessidade agregada de uma matéria-prima excede o saldo disponível, quando `SendToProduction` é chamado, então a resposta segue o contrato de redirecionamento, o pedido permanece `Normalized`, nenhuma tarefa é criada, nenhum movimento `Outbound` de produção é persistido e a auditoria registra `Planned = false`.
- `AC-017`: Dado um pedido com produto inativo, ficha técnica ausente/inválida ou item sem produto vinculado, quando `SendToProduction` é chamado, então o resultado rejeita o planejamento sem alterações parciais em pedido, tarefas ou estoque; o cenário deve ser verificado separadamente para evitar que apenas o caso de estoque cubra todas as falhas.
- `AC-018`: Dado o mesmo pedido elegível, quando `SendToProduction` é submetido novamente após um planejamento confirmado, então o teste confirma que o fluxo não cria uma segunda tarefa nem um segundo consumo para o mesmo pedido e registra o resultado conforme o contrato efetivo de deduplicação/rejeição.
- `AC-019`: Dadas duas tentativas de `SendToProduction` para o mesmo pedido executadas de forma concorrente no limite suportado pelo harness, quando ambas terminam, então o teste confirma que no máximo um planejamento efetivo e seus consumos correspondentes permanecem persistidos, e que conflitos são tratados pelo resultado documentado sem efeitos parciais adicionais.
- `AC-020`: Dado um POST de `SendToProduction` sem autenticação, sem a política de escrita ou sem antiforgery válido, quando a requisição é enviada, então a operação é rejeitada antes do planejamento e a verificação posterior confirma ausência de tarefas, consumo de estoque e auditoria de planejamento efetivo.

## Riscos, dependências e perguntas abertas

- Risco: a documentação registra lacunas sobre autorização por recurso/tenant, concorrência e atomicidade entre pedido, tarefas e estoque; os testes não devem transformar a ausência de evidência em garantia.
- Risco: o fluxo de criação manual pode conter dados de cliente em logs; os testes devem evitar credenciais e PII reais e avaliar apenas o comportamento necessário ao contrato HTTP.
- Dependência: controllers, serviços, repositórios, configuração de DI, banco de testes e fixtures existentes; a implementação deve confirmar os endpoints e nomes efetivos antes de escrever os testes.
- Dependência: o agente `integration-tests` deve identificar o harness de integração existente e os comandos de execução compatíveis com a solução.
- Dependência: os cenários de `SendToProduction` dependem de dados de teste que representem quantidade do pedido, composição da ficha técnica, saldo inicial e eventuais movimentos existentes, para comprovar a agregação da necessidade e a ausência de baixa parcial.
- Pergunta: o comportamento esperado para uma segunda chamada depois de um planejamento confirmado é rejeição silenciosa, mensagem de erro ou outro resultado observável? A especificação exige ausência de duplicação, mas deixa o texto exato dependente do contrato confirmado durante a implementação.
- Pergunta: o harness de integração consegue exercitar duas requisições concorrentes com o mesmo pedido e observar uma garantia transacional real? Caso não consiga, o teste deve registrar a limitação do InMemory em vez de afirmar equivalência com PostgreSQL.

Revisão de segurança: parcial
Escopo revisado: documentação e contratos descritos para controllers de pedidos e produção, incluindo autenticação, autorização, antiforgery, entradas, persistência, estoque, tarefas, eventos e idempotência.
Verificações executadas: inspeção estática das documentações em `docs` e da constituição Spectro; nenhum teste, scanner de segredos ou verificação de dependências foi executado nesta etapa.
Achados: nenhum achado crítico/alto confirmado; permanecem não verificadas a autorização por recurso/tenant e a eficácia do tratamento de concorrência/atomicidade até a implementação e execução dos testes.
Riscos residuais / limitações: a concorrência distribuída e diferenças específicas do PostgreSQL permanecem fora do escopo; esta especificação não comprova a segurança ou a ausência de vulnerabilidades da aplicação.
