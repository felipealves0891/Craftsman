# Product Catalog Context Tasks

## Objetivo
Manter produtos internos e mapear itens recebidos de pedidos para produtos usados pela produção, estoque e financeiro.

## Tarefas

### PC-001 - Modelar produto interno
- [x] Criar entidade `Product`.
- [x] Definir identificador interno, nome, status e dados necessários para produção.
- [x] Criar cadastro básico de produtos.

Critérios de aceitação:
- Produtos internos são independentes dos nomes ou códigos das origens externas.
- Produtos inativos não podem ser usados em novos planejamentos de produção.
- O produto possui dados mínimos para ser relacionado à ficha técnica.

### PC-002 - Criar mapeamento entre item externo e produto interno
- [x] Criar entidade ou value object para mapeamento de origem.
- [x] Permitir vincular código/nome externo a um produto interno.
- [x] Validar conflito de mapeamentos.

Critérios de aceitação:
- Um item de pedido pode ser resolvido para um produto interno.
- Mapeamentos conflitantes são rejeitados.
- A lógica de mapeamento não usa `if/else` por origem.

### PC-003 - Modelar ficha técnica do produto
- [x] Definir materiais necessários por produto.
- [x] Definir quantidades necessárias por unidade produzida.
- [x] Expor consulta da ficha técnica para o planejamento de produção.

Critérios de aceitação:
- A produção consegue consultar materiais e quantidades por produto.
- Fichas técnicas inválidas ou incompletas não podem ser usadas para planejar produção.
- Alterações em ficha técnica não alteram pedidos já registrados.

### PC-004 - Persistir produtos, mapeamentos e fichas técnicas
- [x] Criar entidades de persistência.
- [x] Criar configurações do EF.
- [x] Implementar repositórios do catálogo.

Critérios de aceitação:
- Produtos, mapeamentos e fichas técnicas podem ser salvos e carregados.
- O domínio não referencia entidades de persistência.
- Consultas de catálogo podem ser cacheadas e invalidadas quando houver alteração.

### PC-005 - Criar cadastro manual de produtos
- [ ] Criar serviço de aplicação para cadastrar e editar produtos.
- [ ] Criar telas Razor para listar, criar, editar, ativar e inativar produtos.
- [ ] Validar nome, status e dados mínimos para produção.
- [ ] Persistir alterações pelo repositório do catálogo.
- [ ] Invalidar cache de catálogo quando produto mudar.

Critérios de aceitação:
- Produto cadastrado manualmente usa a mesma entidade `Product` do domínio.
- Produtos inativos não ficam disponíveis para novos planejamentos de produção.
- A interface não acessa `DbContext` diretamente.
- Alterações de produto invalidam consultas cacheadas relacionadas.

### PC-006 - Criar manutenção manual de ficha técnica
- [x] Criar tela para informar matérias-primas e quantidades por produto.
- [x] Permitir substituir ficha técnica completa de um produto.
- [x] Validar materiais duplicados e quantidades inválidas.
- [x] Preservar pedidos já registrados quando ficha técnica mudar.

Critérios de aceitação:
- Ficha técnica manual é consultada pelo mesmo planejador de produção.
- Ficha técnica vazia, duplicada ou com quantidade inválida é rejeitada.
- Alterações em ficha técnica não alteram pedidos já existentes.
- Alterações invalidam cache de catálogo relacionado.

### PC-007 - Criar manutenção manual de mapeamentos externos
- [x] Criar tela para mapear origem e código externo a produto interno.
- [x] Permitir criar mapeamentos para origens externas e para origem manual quando necessário.
- [x] Bloquear conflitos de origem/código externo.
- [x] Persistir mapeamentos pelo repositório do catálogo.

Critérios de aceitação:
- Mapeamento manual usa a mesma regra de conflito do domínio.
- O sistema não usa `if/else` por origem para resolver produto.
- Mapeamentos ficam disponíveis para importação e planejamento.
- Alterações invalidam cache de catálogo relacionado.

### PC-008 - Criar manutenção manual de mapeamentos externos
- [x] Criar leitor de codigo de barras para cadastro do produto
- [x] Deve funcionar via camera do celular
- [x] Deve salvar o codigo de barras junto com o produto
- [x] Adicionar tratamento de erro, e informe o usuario, com detalhes do erro

Critérios de aceitação:
- O Usuario pode ler o codigo de barras pelo celular.
- O sistema deve salvar o codigo de barras como propriedade do produto.
