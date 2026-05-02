Geral:
    1. Usar placeholder e valores
    2. Formatação monetaria
    3. Adaptar casa decimal
    4. Ajustar menu para não aparecer sub-menus sempre que carrega a pagina

Pedido:
    1. Remover dados do cliente
    2. Adicionar dados da origem (Shoppe, Elo7, WhatsApp, tornar possivel adicionar novas origens)
    3. Adicionar campo de data de envio
    5. Trocar nome de carteira para lista de pedido
    6. Alterar nome de pedido manual para cadastrar pedido

Produto:
    1. Remover codigo de barras
    2. Campo dias de produção alterar para horas
    3. Adicionar campo de valor por hora
    4. Corrigir erro ao salvar de materia prima
        DbUpdateConcurrencyException: The database operation was expected to affect 1 row(s), but actually affected 0 row(s); data may have been modified or deleted since entities were loaded. See https://go.microsoft.com/fwlink/?LinkId=527962 for information on understanding and handling optimistic concurrency exceptions.
        Npgsql.EntityFrameworkCore.PostgreSQL.Update.Internal.NpgsqlModificationCommandBatch.ThrowAggregateUpdateConcurrencyExceptionAsync(RelationalDataReader reader, int commandIndex, int expectedRowsAffected, int rowsAffected, CancellationToken cancellationToken)
        Npgsql.EntityFrameworkCore.PostgreSQL.Update.Internal.NpgsqlModificationCommandBatch.Consume(RelationalDataReader reader, bool async, CancellationToken cancellationToken)
        Microsoft.EntityFrameworkCore.Update.ReaderModificationCommandBatch.ExecuteAsync(IRelationalConnection connection, CancellationToken cancellationToken)
        Microsoft.EntityFrameworkCore.Update.ReaderModificationCommandBatch.ExecuteAsync(IRelationalConnection connection, CancellationToken cancellationToken)
        Microsoft.EntityFrameworkCore.Update.Internal.BatchExecutor.ExecuteAsync(IEnumerable<ModificationCommandBatch> commandBatches, IRelationalConnection connection, CancellationToken cancellationToken)
        Microsoft.EntityFrameworkCore.Update.Internal.BatchExecutor.ExecuteAsync(IEnumerable<ModificationCommandBatch> commandBatches, IRelationalConnection connection, CancellationToken cancellationToken)
        Microsoft.EntityFrameworkCore.Update.Internal.BatchExecutor.ExecuteAsync(IEnumerable<ModificationCommandBatch> commandBatches, IRelationalConnection connection, CancellationToken cancellationToken)
        Microsoft.EntityFrameworkCore.ChangeTracking.Internal.StateManager.SaveChangesAsync(IList<IUpdateEntry> entriesToSave, CancellationToken cancellationToken)

Produção:
    1. Calculo de tempo de produção deve considerar a data de envio, e não a data do pedido
    2. Deve olhar o tempo livre, então se o dia já estiver completo deve voltar mais um dia
    3. Devemos considerar a agenda por hora tambem
    4. Deve poder configurar quantas horas serão trabalhadas por dia, caso não sejá definido considerar 6 horas

Movimentos de Estoque:
    1. Ocultar o campo referencia
    2. Motivo apenas quando for outbound
    3. Ajustar alertas de erro e sucesso
    4. Liberar ajuste quando for manual

Envios:
    1. Deve poder selecionar o pedido com base, nos que já estão com a produção cocluido
    2. Corrigir erro ao alterar estatos do envio
        DbUpdateConcurrencyException: The database operation was expected to affect 1 row(s), but actually affected 0 row(s); data may have been modified or deleted since entities were loaded. See https://go.microsoft.com/fwlink/?LinkId=527962 for information on understanding and handling optimistic concurrency exceptions.
        Npgsql.EntityFrameworkCore.PostgreSQL.Update.Internal.NpgsqlModificationCommandBatch.ThrowAggregateUpdateConcurrencyExceptionAsync(RelationalDataReader reader, int commandIndex, int expectedRowsAffected, int rowsAffected, CancellationToken cancellationToken)
        Npgsql.EntityFrameworkCore.PostgreSQL.Update.Internal.NpgsqlModificationCommandBatch.Consume(RelationalDataReader reader, bool async, CancellationToken cancellationToken)
        Microsoft.EntityFrameworkCore.Update.ReaderModificationCommandBatch.ExecuteAsync(IRelationalConnection connection, CancellationToken cancellationToken)
        Microsoft.EntityFrameworkCore.Update.ReaderModificationCommandBatch.ExecuteAsync(IRelationalConnection connection, CancellationToken cancellationToken)
        Microsoft.EntityFrameworkCore.Update.Internal.BatchExecutor.ExecuteAsync(IEnumerable<ModificationCommandBatch> commandBatches, IRelationalConnection connection, CancellationToken cancellationToken)
        Microsoft.EntityFrameworkCore.Update.Internal.BatchExecutor.ExecuteAsync(IEnumerable<ModificationCommandBatch> commandBatches, IRelationalConnection connection, CancellationToken cancellationToken)
        Microsoft.EntityFrameworkCore.Update.Internal.BatchExecutor.ExecuteAsync(IEnumerable<ModificationCommandBatch> commandBatches, IRelationalConnection connection, CancellationToken cancellationToken)
        Microsoft.EntityFrameworkCore.ChangeTracking.Internal.StateManager.SaveChangesAsync(IList<IUpdateEntry> entriesToSave, CancellationToken cancellationToken)

Financeiro:
    1. Adicionar filtro por origem
    2. Fazer calculo por percentual

## Novas Features

Alerta de Estoque baixo:
    1. Criar regra de estoque baixo, nesta regra devemos definir a quantidade minima e critica de estoque por materia prima
    2. Quando for cadastrar a materia prima definir os limites
    3. Quando a quantidade no estoque for igual ou abaixo da quantidade minima alertar no envio para produção
    4. Quando for critico apresentar na tela de entrada e no sino de notificações

