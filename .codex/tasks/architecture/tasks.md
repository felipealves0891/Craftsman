# Architecture Tasks

## Objetivo
Preparar a base da aplicação C# com Razor, DDD, separação por bounded contexts, Entity Framework com PostgreSQL, cache em memória e eventos de domínio.

## Tarefas

### A-001 - Criar estrutura inicial do projeto
- [ ] Criar a estrutura de pastas `/App`, `/Domains` e `/Infra`.
- [ ] Separar `Controllers`, `Models`, `Views`, `wwwroot` e `Services` em `/App`.
- [ ] Separar `Repositories`, `Entities`, `ObjectValues`, `Services` e `Events` em `/Domains/Domain`.
- [ ] Separar `Persistence`, `Configurations`, `Migrations`, `Repositories` e `Services` em `/Infra`.

Critérios de aceitação:
- A solução compila com a estrutura definida.
- Nenhum bounded context depende diretamente de integrações externas.
- A camada de domínio não referencia Entity Framework.

### A-002 - Configurar persistência com PostgreSQL e Entity Framework
- [ ] Configurar `DbContext` em `/Infra/Persistence`.
- [ ] Criar entidades de persistência separadas das entidades de domínio.
- [ ] Configurar migrations.
- [ ] Configurar string de conexão por ambiente.

Critérios de aceitação:
- O banco usado pela aplicação é PostgreSQL.
- As entidades de domínio não são usadas diretamente como entidades do EF.
- Existe mapeamento explícito entre entidades de persistência e modelos de domínio/DTOs.

### A-003 - Definir contratos base de repositório por domínio
- [ ] Criar contratos de repositório na camada de domínio.
- [ ] Implementar repositórios na infraestrutura.
- [ ] Garantir que os repositórios façam o mapeamento entre DTOs/modelos internos e entidades de persistência.

Critérios de aceitação:
- A aplicação não acessa `DbContext` diretamente fora da infraestrutura.
- Cada bounded context possui contratos próprios quando necessário.
- Os repositórios não contêm regras de negócio.

### A-004 - Implementar eventos de domínio
- [ ] Criar abstrações para eventos de domínio.
- [ ] Criar mecanismo simples de publicação interna de eventos.
- [ ] Registrar eventos principais do fluxo: pedido normalizado, produção planejada, envio criado, entrega confirmada e financeiro calculado.

Critérios de aceitação:
- Eventos representam fatos de negócio já ocorridos.
- Eventos não dependem de APIs externas.
- Handlers podem ser adicionados sem alterar os serviços de domínio existentes.

### A-005 - Configurar cache em memória com invalidação
- [ ] Criar serviço de cache em memória.
- [ ] Definir estratégia de invalidação quando dados mudarem.
- [ ] Aplicar cache apenas em consultas apropriadas.

Critérios de aceitação:
- Dados alterados invalidam entradas relacionadas no cache.
- O cache não substitui validações de negócio.
- O sistema continua funcionando corretamente com cache desativado.

### A-006 - Criar Dockerfile e docker-compose
- [ ] Criar `Dockerfile` para build e execução da aplicação C# com Razor.
- [ ] Criar `docker-compose.yml` com os serviços necessários para desenvolvimento.
- [ ] Configurar serviço da aplicação.
- [ ] Configurar serviço do PostgreSQL.
- [ ] Configurar variáveis de ambiente para conexão com banco de dados.
- [ ] Configurar volume persistente para dados do PostgreSQL.

Critérios de aceitação:
- A aplicação pode ser iniciada com `docker compose up`.
- O container da aplicação consegue conectar ao PostgreSQL pelo nome do serviço definido no compose.
- As configurações sensíveis não ficam fixas no código da aplicação.
- O banco mantém dados entre reinicializações dos containers.
- A configuração Docker não acopla regras de negócio à infraestrutura.
