# Project Management Tasks

## Objetivo
Gerenciar a execução do projeto Craftsman, deixando explícitas as dependências entre tarefas, a sequência de entrega, os marcos do projeto e os critérios de acompanhamento.

## Premissas
- A especificação base disponível está em `.codex/PRODUCT.md`.
- As tarefas de domínio estão organizadas em `.codex/tasks`.
- Nenhuma implementação deve começar sem critérios de aceitação definidos.
- Toda entrega funcional deve ter testes automatizados cobrindo os critérios de aceitação.

## Fases do projeto

### Fase 1 - Fundação técnica
Objetivo: criar a base da solução, infraestrutura local e contratos arquiteturais.

Tarefas:
- A-001 - Criar estrutura inicial do projeto
- A-002 - Configurar persistência com PostgreSQL e Entity Framework
- A-003 - Definir contratos base de repositório por domínio
- A-004 - Implementar eventos de domínio
- A-006 - Criar Dockerfile e docker-compose

Critérios de conclusão:
- A aplicação compila.
- A aplicação sobe localmente com Docker.
- PostgreSQL fica disponível para a aplicação.
- Domínio não depende de Entity Framework.
- Eventos de domínio possuem abstrações utilizáveis pelos contextos.

### Fase 2 - Núcleo de pedidos e catálogo
Objetivo: permitir que pedidos externos sejam normalizados e associados a produtos internos.

Tarefas:
- S-001 - Modelar pedido interno
- S-002 - Definir ciclo de vida do pedido
- S-004 - Persistir pedidos normalizados
- PC-001 - Modelar produto interno
- PC-002 - Criar mapeamento entre item externo e produto interno
- PC-003 - Modelar ficha técnica do produto
- PC-004 - Persistir produtos, mapeamentos e fichas técnicas
- IN-001 - Definir contrato de fonte de pedidos
- IN-002 - Definir normalizador de pedidos
- IN-005 - Registrar rastreabilidade da origem

Critérios de conclusão:
- Um pedido de origem externa pode ser convertido para o modelo interno.
- Itens externos podem ser resolvidos para produtos internos.
- Pedido normalizado é persistido com rastreabilidade da origem.
- O fluxo não usa `if/else` por origem.

### Fase 3 - Estoque e produção
Objetivo: planejar produção com base em pedido, produto, ficha técnica e disponibilidade de material.

Tarefas:
- I-001 - Modelar matéria-prima
- I-002 - Modelar movimentações de estoque
- I-003 - Validar disponibilidade de material para produção
- I-004 - Reservar ou consumir material para produção
- I-005 - Persistir materiais e movimentações
- P-001 - Modelar tarefa de produção
- P-002 - Implementar planejador de produção
- P-004 - Integrar produção com estoque
- P-005 - Persistir tarefas de produção

Critérios de conclusão:
- Produção não é planejada sem ficha técnica válida.
- Produção não é planejada sem material suficiente.
- Movimentações de estoque são auditáveis.
- Tarefa de produção mantém rastreabilidade até o pedido.

### Fase 4 - Fluxo operacional completo
Objetivo: disponibilizar as primeiras telas e completar o fluxo de importação até envio.

Tarefas:
- IN-003 - Criar pipeline de importação
- IN-004 - Criar primeira integração simulada
- S-003 - Criar serviço de aplicação para consulta e acompanhamento de pedidos
- P-003 - Criar agenda de produção
- SH-001 - Modelar envio
- SH-002 - Definir contrato de rastreamento
- SH-004 - Criar visão de acompanhamento de envio
- SH-005 - Persistir envios
- A-005 - Configurar cache em memória com invalidação

Critérios de conclusão:
- É possível importar pedido por uma origem simulada.
- É possível acompanhar pedido e produção na aplicação.
- É possível criar e acompanhar envio.
- Consultas cacheáveis invalidam cache quando dados mudam.

### Fase 5 - Entrega e financeiro
Objetivo: concluir o fluxo principal com entrega confirmada e cálculo financeiro pós-entrega.

Tarefas:
- SH-003 - Registrar entrega
- F-001 - Modelar acerto financeiro
- F-002 - Implementar calculadora de acerto
- F-003 - Reagir à entrega confirmada
- F-004 - Criar visão financeira
- F-005 - Persistir acertos financeiros

Critérios de conclusão:
- Entrega confirmada emite evento para o financeiro.
- Acerto financeiro só é calculado para pedido entregue.
- Receita, custos reais e margem são persistidos.
- A visão financeira permite auditar o cálculo.

### Fase 6 - Entrada manual operacional
Objetivo: permitir que o usuário cadastre manualmente pedidos, produtos, fichas técnicas, mapeamentos, matérias-primas e movimentações de estoque, mantendo o mesmo modelo interno, rastreabilidade, eventos e regras de negócio usados pelos fluxos automatizados.

Tarefas:
- S-005 - Criar entrada manual de pedidos
- S-006 - Permitir vínculo manual de itens do pedido a produtos internos
- PC-005 - Criar cadastro manual de produtos
- PC-006 - Criar manutenção manual de ficha técnica
- PC-007 - Criar manutenção manual de mapeamentos externos
- I-006 - Criar cadastro manual de matéria-prima
- I-007 - Criar lançamento manual de movimentações de estoque
- I-008 - Criar consulta operacional de estoque

Critérios de conclusão:
- Pedido manual usa o mesmo modelo interno de pedido normalizado.
- Produtos, fichas técnicas e mapeamentos manuais usam os mesmos modelos e repositórios do catálogo.
- Matérias-primas e movimentações manuais usam os mesmos modelos e repositórios de estoque.
- Toda movimentação manual de estoque é auditável e recalculável.
- Telas Razor não acessam `DbContext` diretamente e não contêm regras de negócio.
- Alterações manuais invalidam caches relacionados.
- Dados cadastrados manualmente podem seguir pelo fluxo de produção, envio, entrega e financeiro sem lógica especial.

## Dependências entre tarefas

| Tarefa | Depende de | Motivo |
| --- | --- | --- |
| A-002 | A-001 | Persistência depende da estrutura da solução. |
| A-003 | A-001 | Contratos devem estar nos projetos/pastas corretos. |
| A-004 | A-001 | Eventos precisam da base do domínio. |
| A-005 | A-001, A-003 | Cache deve ser integrado a serviços e repositórios. |
| A-006 | A-001, A-002 | Docker precisa conhecer aplicação e banco. |
| S-002 | S-001, A-004 | Ciclo de vida depende do pedido e dos eventos. |
| S-004 | S-001, A-002, A-003 | Persistência de pedidos depende do modelo e infraestrutura. |
| S-003 | S-001, S-002, S-004 | Consulta depende do pedido modelado e persistido. |
| PC-002 | PC-001 | Mapeamento externo depende do produto interno. |
| PC-003 | PC-001, I-001 | Ficha técnica depende de produto e matéria-prima. |
| PC-004 | PC-001, PC-002, PC-003, A-002, A-003 | Persistência depende dos modelos e infraestrutura. |
| I-002 | I-001 | Movimentações dependem de matéria-prima. |
| I-003 | I-001, I-002, PC-003 | Disponibilidade depende de estoque e ficha técnica. |
| I-004 | I-002, I-003 | Reserva ou consumo depende da validação de disponibilidade. |
| I-005 | I-001, I-002, A-002, A-003 | Persistência depende dos modelos e infraestrutura. |
| P-001 | S-001, PC-001 | Tarefa de produção depende de pedido e produto. |
| P-002 | P-001, PC-002, PC-003, I-003 | Planejamento depende de mapeamento, ficha técnica e estoque. |
| P-003 | P-001, P-002, P-005 | Agenda depende das tarefas planejadas e persistidas. |
| P-004 | P-002, I-004, A-004 | Integração com estoque depende do planejamento, consumo e eventos. |
| P-005 | P-001, A-002, A-003 | Persistência depende do modelo e infraestrutura. |
| IN-002 | IN-001, S-001 | Normalizador depende da fonte e do pedido interno. |
| IN-003 | IN-001, IN-002, S-004, A-004 | Pipeline depende de fonte, normalização, persistência e eventos. |
| IN-004 | IN-001, IN-002, IN-003 | Integração simulada usa o pipeline real. |
| IN-005 | IN-001, S-001, S-004 | Rastreabilidade depende da origem e da persistência do pedido. |
| SH-002 | SH-001 | Rastreamento depende do envio modelado. |
| SH-003 | SH-001, SH-005, A-004 | Entrega depende do envio persistido e dos eventos. |
| SH-004 | SH-001, SH-002, SH-005 | Visão depende do modelo, rastreamento e persistência. |
| SH-005 | SH-001, A-002, A-003 | Persistência depende do modelo e infraestrutura. |
| F-001 | S-001, SH-003 | Acerto financeiro depende de pedido entregue. |
| F-002 | F-001, S-004, I-002, P-005, SH-005 | Cálculo depende de pedido, custos de produção, estoque e envio. |
| F-003 | F-001, F-002, SH-003, A-004 | Reação financeira depende do evento de entrega e calculadora. |
| F-004 | F-001, F-002, F-005 | Visão financeira depende do cálculo persistido. |
| F-005 | F-001, A-002, A-003 | Persistência depende do modelo e infraestrutura. |
| S-005 | S-001, S-002, S-004, A-004 | Entrada manual deve gerar o mesmo pedido interno, persistência e eventos do fluxo normalizado. |
| S-006 | S-001, S-004, PC-001, PC-004 | Vínculo manual depende de pedido persistido e produtos internos persistidos. |
| PC-005 | PC-001, PC-004, A-005 | Cadastro manual depende do modelo, persistência e invalidação de cache. |
| PC-006 | PC-003, PC-004, I-006 | Ficha técnica manual depende de produtos, persistência e matérias-primas cadastradas. |
| PC-007 | PC-001, PC-002, PC-004 | Mapeamento manual depende de produto interno, regra de conflito e persistência. |
| I-006 | I-001, I-005, A-005 | Cadastro manual depende do modelo, persistência e invalidação de cache. |
| I-007 | I-002, I-003, I-005, I-006 | Movimentação manual depende de matéria-prima, saldo, persistência e auditoria. |
| I-008 | I-002, I-005, I-007, A-005 | Consulta operacional depende das movimentações persistidas e cache invalidável. |

## Caminho crítico

1. A-001 - Criar estrutura inicial do projeto
2. A-002 - Configurar persistência com PostgreSQL e Entity Framework
3. A-003 - Definir contratos base de repositório por domínio
4. A-004 - Implementar eventos de domínio
5. S-001 - Modelar pedido interno
6. PC-001 - Modelar produto interno
7. I-001 - Modelar matéria-prima
8. PC-003 - Modelar ficha técnica do produto
9. I-003 - Validar disponibilidade de material para produção
10. P-002 - Implementar planejador de produção
11. IN-003 - Criar pipeline de importação
12. SH-003 - Registrar entrega
13. F-002 - Implementar calculadora de acerto
14. F-003 - Reagir à entrega confirmada
15. I-006 - Criar cadastro manual de matéria-prima
16. I-007 - Criar lançamento manual de movimentações de estoque
17. PC-005 - Criar cadastro manual de produtos
18. PC-006 - Criar manutenção manual de ficha técnica
19. S-005 - Criar entrada manual de pedidos
20. S-006 - Permitir vínculo manual de itens do pedido a produtos internos

## Plano de gerenciamento

### Estratégia de entrega
- Entregar por fluxo vertical sempre que possível, mantendo os bounded contexts separados.
- Priorizar primeiro um fluxo mínimo executável: importar pedido simulado, normalizar, mapear produto, planejar produção, confirmar entrega e calcular financeiro.
- Evitar telas antes dos modelos, contratos e persistência essenciais estarem definidos.
- Criar testes automatizados junto com cada tarefa de domínio.

### Marcos
- M1 - Fundação técnica pronta: Fase 1 concluída.
- M2 - Pedido normalizado e catálogo funcional: Fase 2 concluída.
- M3 - Produção planejada com estoque auditável: Fase 3 concluída.
- M4 - Fluxo operacional navegável: Fase 4 concluída.
- M5 - Financeiro pós-entrega concluído: Fase 5 concluída.
- M6 - Operação manual rastreável concluída: Fase 6 concluída.

### Controle de qualidade
- Cada tarefa deve ser marcada como concluída somente quando seus critérios de aceitação forem atendidos.
- Alterações de comportamento exigem atualização da especificação antes da implementação.
- Regras de negócio devem ficar no domínio ou em serviços de domínio, não em controllers, views, integrações ou repositórios.
- Testes devem cobrir principalmente regras de negócio, transições de status, mapeamentos e eventos.
- Fluxos manuais devem reutilizar os mesmos serviços de domínio e repositórios dos fluxos automatizados.

### Gestão de riscos
- Risco: acoplamento com APIs externas.
  Mitigação: manter integrações atrás de `IOrderSource`, `IOrderNormalizer` e `IShippingTracker`.
- Risco: domínio depender de Entity Framework.
  Mitigação: usar entidades de persistência próprias e mapeamento explícito em repositórios.
- Risco: cálculo financeiro inconsistente.
  Mitigação: persistir custos reais e testar a calculadora com dados determinísticos.
- Risco: estoque sem auditoria.
  Mitigação: toda alteração de saldo deve gerar movimentação imutável no fluxo normal.
- Risco: excesso de abstração.
  Mitigação: criar abstrações apenas quando exigidas pelo bounded context ou pela extensibilidade descrita.
- Risco: telas manuais criarem atalhos fora do domínio.
  Mitigação: toda ação manual deve passar por serviço de aplicação e domínio, com persistência via repositórios.
- Risco: perda de rastreabilidade em dados informados manualmente.
  Mitigação: registrar origem `Manual`, motivo, data e referência de negócio em pedidos e movimentações.

### Definição de pronto
- Código compila.
- Testes automatizados relevantes passam.
- Critérios de aceitação da tarefa estão atendidos.
- Dependências da tarefa estão concluídas ou explicitamente justificadas.
- Não há regra de negócio nova sem especificação correspondente.
- A separação entre App, Domains e Infra foi preservada.
