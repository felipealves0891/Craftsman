# Tasks - DbUpdateConcurrencyException

## Corrigir falsas excecoes de concorrencia EF Core
- [x] Confirmar os repositorios que atualizam entidades desconectadas com `Update(ToEntity(...))`.
- [x] Corrigir updates para carregar a entidade atual rastreada e mapear campos nela.
- [x] Tratar update de registro inexistente com `InvalidOperationException` clara.
- [x] Adicionar testes automatizados para producao, estoque, envio, registro inexistente e auditoria.
- [x] Rodar build e testes da solucao.

Criterios de aceitacao:
- Iniciar, concluir ou cancelar uma tarefa de producao nao deve gerar erro de concorrencia do EF Core.
- Editar materia-prima nao deve gerar erro de concorrencia do EF Core.
- Atualizar status de envio nao deve gerar erro de concorrencia do EF Core.
- Atualizar registro inexistente deve falhar com erro controlado e mensagem clara.
- Auditoria deve continuar registrando alteracoes persistidas.
