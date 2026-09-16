# Tasks - Interface Compartilhada

## Base de componentes Razor compartilhados
- [x] Compatibilizar `src/UI` com a solution .NET 8.
- [x] Referenciar `Craftsman.UI` no App.
- [x] Migrar uma pagina Razor do App para componente em `src/UI`.
- [x] Importar o componente migrado na pagina MVC correspondente.
- [x] Criar testes automatizados para garantir que a pagina usa o componente compartilhado.
- [ ] Rodar build e testes da solucao.

## Paginas migradas para `src/UI`
- [x] Documentacao.
- [x] Painel inicial.
- [x] Politica de privacidade.
- [x] Acesso negado.
- [x] Detalhes de acerto financeiro.
- [x] Listagem financeira.
- [x] Listagem de materias-primas.
- [x] Saldos de estoque.
- [x] Historico de estoque.
- [x] Listagem de produtos.
- [x] Listagem de usuarios.
- [x] Novo pedido manual.
- [x] Vinculo de itens do pedido manual.
- [x] Listagem de pedidos.
- [x] Detalhes do pedido.
- [x] Agenda de producao.
- [x] Listagem de envios.
- [x] Criacao de envio.

Criterios de aceitacao:
- O projeto `Craftsman.UI` deve compilar junto com a solution principal.
- O App deve conseguir renderizar componentes Razor vindos de `Craftsman.UI`.
- A primeira pagina migrada deve manter o conteudo e links operacionais existentes.
- A view MVC migrada deve conter apenas a configuracao de titulo e a importacao do componente.
- A migracao nao deve alterar regras de dominio, persistencia ou fluxo dos controllers.
