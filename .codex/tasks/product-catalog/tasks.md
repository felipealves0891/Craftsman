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
