# Production Context Tasks

## Objetivo
Gerar e acompanhar a agenda de producao com base em pedidos normalizados, produtos internos, ficha tecnica e disponibilidade de materia-prima.

## Tarefas

### P-001 - Modelar tarefa de producao
- [x] Criar entidade `ProductionTask`.
- [x] Relacionar tarefa ao pedido e produto interno.
- [x] Definir quantidade, status e datas relevantes.

Criterios de aceitacao:
- Cada tarefa de producao tem rastreabilidade ate o pedido.
- Tarefas usam produtos internos, nao itens externos.
- Status da tarefa segue transicoes validas.

### P-002 - Implementar planejador de producao
- [x] Criar contrato `IProductionPlanner`.
- [x] Gerar tarefas a partir de pedidos normalizados.
- [x] Validar ficha tecnica e disponibilidade de material.

Criterios de aceitacao:
- Pedido normalizado gera uma ou mais tarefas de producao.
- Producao nao e planejada sem produto interno mapeado.
- Producao nao e planejada sem material suficiente.

### P-003 - Criar agenda de producao
- [x] Criar consulta de tarefas por status e data.
- [x] Criar visao Razor para acompanhamento da agenda.
- [x] Permitir avanco de status conforme regras definidas.

Criterios de aceitacao:
- A agenda mostra tarefas pendentes, em producao e concluidas.
- A UI nao contem regras de calculo de materiais.
- Avancos invalidos de status sao bloqueados no dominio ou servico de aplicacao.

### P-004 - Integrar producao com estoque
- [x] Consumir ou reservar materiais ao planejar/iniciar producao.
- [x] Registrar referencia da producao nas movimentacoes de estoque.
- [x] Emitir evento quando producao for planejada ou concluida.

Criterios de aceitacao:
- Toda tarefa planejada tem validacao de materiais.
- Toda baixa de material e auditavel no estoque.
- Eventos de producao permitem continuidade do fluxo sem acoplamento direto.

### P-005 - Persistir tarefas de producao
- [x] Criar entidades de persistencia.
- [x] Criar configuracoes do EF.
- [x] Implementar repositorio de producao.

Criterios de aceitacao:
- Tarefas podem ser salvas e carregadas com seus vinculos.
- O dominio nao depende do EF.
- Consultas da agenda sao consistentes com os status persistidos.

### P-006 - Planejar agenda por capacidade horaria antes da data de envio
- [x] Definir no dominio como a data de envio do pedido orienta o prazo de producao.
- [x] Criar regra de capacidade diaria de producao em horas, com padrao de 6 horas quando nao houver configuracao.
- [x] Alterar o planejamento para alocar tarefas em blocos horarios disponiveis, retrocedendo dias quando a capacidade do dia estiver completa.
- [x] Persistir e consultar informacoes suficientes para a agenda exibir data e horario planejados.
- [x] Atualizar a agenda Razor para mostrar ocupacao por hora e manter os comandos de avanco de status.
- [x] Criar testes automatizados unitarios e de integracao para os cenarios de capacidade, data de envio e fallback de horas diarias.

Criterios de aceitacao:
- O calculo da agenda usa a data de envio do pedido como referencia de prazo, nao a data de criacao do pedido.
- Quando o pedido nao possui data de envio, o sistema usa uma regra explicita e testada, sem falhar silenciosamente.
- O planejador considera a duracao em horas do produto e a quantidade do item para calcular o tempo total de producao.
- A agenda respeita a capacidade diaria configurada em horas.
- Quando a capacidade de um dia esta completa, a tarefa e planejada no horario livre anterior; se nao houver espaco no dia, volta para o dia anterior.
- A capacidade diaria padrao e de 6 horas quando nenhuma configuracao estiver definida.
- A agenda permite identificar em que dia e horario cada tarefa deve ser produzida.
- O planejamento continua independente da origem do pedido e sem regras especificas por marketplace.
- A UI nao contem regra de calculo de capacidade; apenas apresenta o resultado planejado.
- Build e testes devem passar com `DOTNET_CLI_HOME=D:\Source\Repos\Dotnet\Craftsman\.dotnet-home\.dotnet`.
