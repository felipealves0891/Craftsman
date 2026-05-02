# DbUpdateConcurrencyException

Em varios lugares da nossa aplicacao, estamos recebendo o erro "The database operation was expected to affect 1 row(s), but actually affected 0 row(s); data may have been modified or deleted since entities were loaded" mesmo com apenas um usuario. Precisamos avaliar o motivo e ajustar o fluxo de persistencia.

## Diagnostico esperado

Os agregados operacionais nao usam token de concorrencia. O erro deve ser tratado como sintoma de atualizacao com entidade desconectada ou registro inexistente, especialmente quando repositorios convertem o modelo de dominio em uma nova entidade EF e chamam `Update`.

## Criterios de aceitacao

- Iniciar, concluir ou cancelar uma tarefa de producao nao deve gerar erro de concorrencia do EF Core.
- Editar materia-prima nao deve gerar erro de concorrencia do EF Core.
- Atualizar status de envio nao deve gerar erro de concorrencia do EF Core.
- Atualizar registro inexistente deve falhar com erro controlado e mensagem clara, nao com `DbUpdateConcurrencyException`.
- Auditoria deve continuar registrando alteracoes persistidas.
- Build e testes da solucao devem passar.
