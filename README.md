# Craftsman

Craftsman é uma aplicação Razor em .NET 8 para gestão integrada de pedidos, catálogo, estoque, produção, envios e acerto financeiro. A proposta descrita nas referências de `.codex/` é manter o pedido independente da origem, usar produtos internos para produção, controlar estoque por movimentações auditáveis e calcular margem somente depois da entrega.

## Visão Geral

O fluxo principal do sistema é:

```text
Importar ou cadastrar pedido
        -> normalizar pedido interno
        -> vincular itens a produtos internos
        -> validar catálogo, ficha técnica e estoque
        -> acompanhar produção
        -> criar envio
        -> confirmar entrega
        -> calcular acerto financeiro
```

A aplicação é dividida em três camadas:

| Camada | Projeto | Responsabilidade |
| --- | --- | --- |
| App | `src/App` | Controllers, views Razor, view models e serviços de aplicação. |
| Domains | `src/Domains` | Regras de negócio, entidades, value objects, eventos e contratos. |
| Infra | `src/Infra` | Entity Framework, PostgreSQL, repositórios, cache, fontes simuladas e publicação interna de eventos. |

Os domínios principais seguem os bounded contexts definidos em `.codex/PRODUCT.md`: vendas, integração, catálogo, estoque, produção, envio e financeiro.

## Requisitos

- .NET SDK 8
- Docker e Docker Compose, se for usar o ambiente containerizado
- PostgreSQL, se for rodar a aplicação fora do Docker

## Como Rodar Com Docker

Na raiz do repositório:

```powershell
docker compose up --build
```

Serviços expostos:

| Serviço | URL/porta | Uso |
| --- | --- | --- |
| Aplicação | `http://localhost:8080` | Interface web do Craftsman. |
| PostgreSQL | `localhost:5432` | Banco da aplicação. |
| pgAdmin | `http://localhost:5050` | Administração visual do PostgreSQL. |

Credenciais configuradas no `docker-compose.yml`:

| Recurso | Valor |
| --- | --- |
| Banco | `craftsman` |
| Usuário PostgreSQL | `craftsman` |
| Senha PostgreSQL | `craftsman_dev_password` |
| pgAdmin e-mail | `admin@craftsman.com` |
| pgAdmin senha | `craftsman_admin_password` |

O volume do PostgreSQL está configurado para `D:/Volumes/Craftsman/postgres_data`, então os dados sobrevivem a reinicializações dos containers.

## Como Rodar Localmente

Suba um PostgreSQL acessível em `localhost:5432` com:

```text
Database=craftsman
Username=craftsman
Password=craftsman_dev_password
```

Depois execute:

```powershell
dotnet run --project src/App/Craftsman.csproj
```

A connection string local está em:

- `src/App/appsettings.json`
- `src/App/appsettings.Development.json`

Em ambiente `Development`, a aplicação executa migrations automaticamente ao subir e roda o seed de desenvolvimento.

## Seed de Desenvolvimento

Ao iniciar em `Development`, o sistema executa `DevelopmentDataSeeder` depois de aplicar migrations. O seed é idempotente: pode rodar várias vezes sem duplicar dados.

Ele cria dados iniciais para facilitar testes:

- Matérias-primas como argila branca, esmalte azul, caixa kraft, tecido, linha e cartão de agradecimento.
- Produtos como caneca artesanal, ecobag e kit presente.
- Fichas técnicas desses produtos.
- Mapeamentos externos com origem `DevSeed`.
- Entradas iniciais de estoque com referência `dev-seed:initial-stock:*`.

## Navegação da Aplicação

O menu lateral organiza a operação em quatro áreas:

| Área | Telas | Para que serve |
| --- | --- | --- |
| Painel | Painel | Entrada inicial da aplicação. |
| Pedidos | Carteira, Pedido manual, Importação | Criar, importar e acompanhar pedidos. |
| Catálogo | Produtos, Mapeamentos, Matérias-primas, Saldos, Movimentos | Preparar produtos internos, ficha técnica e estoque. |
| Execução | Produção, Envios | Acompanhar produção e entrega. |
| Financeiro | Financeiro | Consultar acertos, custos e margem. |

## Fluxo de Uso Recomendado

### 1. Preparar Catálogo

Antes de operar pedidos, confirme se existem produtos internos e matérias-primas.

Na tela `Catálogo -> Produtos`:

1. Clique em `Novo produto` para cadastrar um produto interno.
2. Informe nome, status e tempo de produção em dias.
3. Salve.
4. Use `Ficha técnica` para informar as matérias-primas necessárias por unidade produzida.

Na tela `Catálogo -> Mapeamentos`:

1. Informe a origem do pedido, por exemplo `Simulated`, `Manual`, `DevSeed` ou outra origem futura.
2. Informe o código externo do item.
3. Selecione o produto interno correspondente.
4. Salve o mapeamento.

O objetivo do mapeamento é resolver itens externos para produtos internos sem criar regras específicas por origem.

### 2. Preparar Estoque

Na tela `Catálogo -> Matérias-primas`:

1. Cadastre ou edite matérias-primas.
2. Informe nome, unidade de medida e status.
3. Mantenha ativo apenas o que pode ser consumido por produção.

Na tela `Catálogo -> Movimentos`:

1. Selecione a matéria-prima.
2. Escolha o tipo de movimento:
   - `Inbound`: entrada de estoque.
   - `Outbound`: saída de estoque.
   - `Adjustment`: ajuste.
3. Informe quantidade, motivo, referência de negócio e custo unitário quando for entrada.
4. Salve.

Na tela `Catálogo -> Saldos`, consulte o saldo atual por matéria-prima. Em `Histórico`, audite as movimentações que formaram o saldo.

Regras importantes:

- Toda alteração de estoque é registrada como movimentação.
- Saída manual não pode gerar saldo negativo.
- O saldo é derivado das movimentações, não de um campo editável diretamente.

### 3. Criar ou Importar Pedidos

Na tela `Pedidos -> Carteira`, há dois caminhos.

Para importar pedidos simulados:

1. Clique em `Importar pedidos`.
2. A aplicação busca pedidos na fonte em memória `Simulated`.
3. Pedidos válidos são normalizados e persistidos.
4. Pedidos já importados são ignorados.
5. Pedidos inválidos aparecem como falha no resumo da importação.

A fonte simulada atual contém:

- `SIM-1001`, pedido válido com um item.
- `SIM-INVALID`, pedido inválido sem itens.

Para criar pedido manual:

1. Acesse `Pedidos -> Pedido manual`.
2. Informe cliente e e-mail.
3. Adicione itens com código externo, descrição, quantidade e valor unitário.
4. Vincule o item a um produto interno quando possível.
5. Salve.

O pedido manual usa origem `Manual` e entra no mesmo modelo interno usado por pedidos importados.

### 4. Vincular Itens a Produtos Internos

Na tela de detalhes do pedido:

1. Abra a aba `Itens`.
2. Clique em `Vincular produtos internos`.
3. Para cada item sem vínculo, selecione o produto interno correto.
4. Salve o vínculo.

Esse passo é necessário porque a produção trabalha com produtos internos, não com descrições ou SKUs externos.

### 5. Planejamento Automático e Acompanhamento de Produção

O envio para produção é automático quando o pedido está completo para planejamento. Isso acontece em dois momentos:

- Ao importar ou criar um pedido que já tenha todos os itens vinculados a produtos internos.
- Ao vincular manualmente o último item sem produto interno.

Para gerar tarefas, o sistema valida:

- Todos os itens do pedido possuem `ProductId`.
- Ainda não existem tarefas de produção para o pedido.
- O produto interno está ativo.
- A ficha técnica do produto é válida.
- Há estoque suficiente para consumir os materiais.

Quando essas condições são atendidas, o sistema cria as tarefas de produção, consome os materiais no estoque e muda o pedido de `Normalized` para `ReadyForProduction`.

Se faltar vínculo, ficha técnica ou estoque, o pedido continua salvo, mas nenhuma tarefa é criada. Corrija o cadastro e vincule os itens novamente quando necessário.

Na tela `Execução -> Produção`, é possível:

1. Filtrar tarefas por status.
2. Filtrar por data planejada.
3. Avançar status da tarefa:
   - `start`: planejada para em produção.
   - `complete`: em produção para concluída.
   - `cancel`: cancela quando a regra permite.

O domínio de produção valida transições de status. A produção também possui serviço de planejamento que valida ficha técnica e disponibilidade de estoque antes de criar tarefas e consumir materiais.

### 6. Criar e Atualizar Envios

Na tela de detalhes do pedido, aba `Envios`, clique em `Criar envio`, ou acesse `Execução -> Envios`.

Para criar envio:

1. Informe o pedido.
2. Informe o código de rastreio, se houver.
3. Salve.

Na tela `Envios`, atualize o status:

- `Em trânsito`
- `Tentativa`
- `Entregar`
- `Cancelar`

Ao marcar um envio como entregue, o domínio emite evento de entrega confirmada.

### 7. Consultar Financeiro

Na tela `Financeiro`:

1. Filtre por período, se necessário.
2. Consulte os acertos gerados.
3. Abra o detalhe para ver receita, custo de produção, custo de envio, custo total e margem.

O acerto financeiro é calculado depois da entrega confirmada. O cálculo usa:

- Receita dos itens do pedido.
- Custos de produção derivados das movimentações de estoque vinculadas às tarefas.
- Custo de envio atualmente registrado como `0`, pois ainda não há integração ou lançamento de custo de frete.

O sistema bloqueia duplicidade de acerto financeiro para o mesmo pedido.

## Regras de Negócio Principais

### Pedidos

- Pedido interno não depende do formato da origem externa.
- Origem e identificador externo são metadados de rastreabilidade.
- O mesmo pedido externo não deve ser importado duas vezes.
- Pedido manual usa o mesmo modelo interno de pedido normalizado.

### Catálogo

- Produto interno é independente de nome ou código externo.
- Produto inativo não deve ser usado em novos planejamentos.
- Ficha técnica define matérias-primas e quantidades por unidade produzida.
- Mapeamento liga origem + item externo a produto interno.

### Estoque

- Matéria-prima tem nome, unidade de medida e status.
- Estoque é auditável por movimentações.
- Saldo negativo é bloqueado em saída manual e consumo de produção.
- Entrada pode registrar custo unitário, usado para custo médio.

### Produção

- Produção precisa de produto interno vinculado.
- Produção precisa de produto ativo e ficha técnica válida.
- Produção valida estoque antes de consumir material.
- Consumo de produção gera movimentações de saída com referência à tarefa.

### Envio

- Envio é vinculado a um pedido.
- Status seguem transições controladas pelo domínio.
- Entrega confirmada registra data e emite evento para o financeiro.

### Financeiro

- Acerto só é calculado para pedido entregue.
- Receita vem do pedido.
- Custo de produção vem das movimentações de estoque relacionadas às tarefas.
- Margem é receita menos custos.
- O acerto é persistido para consulta histórica.

## Eventos de Domínio

A aplicação usa publicação interna de eventos para desacoplar etapas do fluxo. Os eventos principais são:

| Evento | Quando ocorre | Uso |
| --- | --- | --- |
| `OrderNormalizedEvent` | Pedido é criado ou importado | Rastreabilidade do pedido normalizado. |
| `ProductionPlannedEvent` | Tarefa de produção é planejada | Rastreabilidade da produção. |
| `ShipmentCreatedEvent` | Envio é criado | Rastreabilidade logística. |
| `DeliveryConfirmedEvent` | Envio é entregue | Dispara o acerto financeiro. |
| `FinancialSettlementCalculatedEvent` | Acerto é calculado | Rastreabilidade financeira. |

Os eventos também são persistidos em `persisted_domain_events` pelo handler genérico de infraestrutura.

## Dados e Persistência

O banco é PostgreSQL e o acesso é feito por Entity Framework Core. As entidades de persistência ficam separadas das entidades de domínio:

- Persistência: `src/Infra/Persistence/Entities`
- Configurações EF: `src/Infra/Persistence/Configurations`
- Repositórios: `src/Infra/Repositories`
- Domínio: `src/Domains`

Essa separação preserva a regra definida nas referências de `.codex/`: o domínio não depende de Entity Framework.

## Testes

Para rodar os testes:

```powershell
dotnet test Craftsman.slnx
```

Os testes ficam em `tests/Craftsman.Tests` e cobrem regras de domínio, integração e persistência.

## Referências Usadas

Esta documentação foi escrita com base em:

- `.codex/PRODUCT.md`
- `.codex/AGENTS.md`
- `.codex/tasks/*/tasks.md`
- Implementação atual em `src/App`, `src/Domains` e `src/Infra`

As referências de `.codex/` descrevem o produto-alvo, os bounded contexts, critérios de aceitação e fases de entrega. A documentação acima considera também o comportamento que já está implementado no código.
