# Bug de concorrencia ao editar pedido

## Contexto

O usuario tentou editar um pedido e recebeu o seguinte erro:

```text
Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException: The database operation was expected to affect 1 row(s), but actually affected 0 row(s); data may have been modified or deleted since entities were loaded. See https://go.microsoft.com/fwlink/?LinkId=527962 for information on understanding and handling optimistic concurrency exceptions
```

## Objetivo

Tratar o erro e garantir que retorne uma mensagem segura e log os dados do erro, como é de concorrencia é muito dificil de replicar. Então vamos tentar coletar o maior numero de informações possivel, para assim podermos resolver futuramente.

## Pontos a Passo

1. Tratar o erro corretamente adicionando `try{}catch(DbUpdateConcurrencyException ex){}` ao atualizar o pedido
2. Adicionar logs nas manipulações do ef, para poder rastriar as alterações, sempre usando id de correlações

## StackTrace

Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException: The database operation was expected to affect 1 row(s), but actually affected 0 row(s); data may have been modified or deleted since entities were loaded. See https://go.microsoft.com/fwlink/?LinkId=527962 for information on understanding and handling optimistic concurrency exceptions.
   at Npgsql.EntityFrameworkCore.PostgreSQL.Update.Internal.NpgsqlModificationCommandBatch.ThrowAggregateUpdateConcurrencyExceptionAsync(RelationalDataReader reader, Int32 commandIndex, Int32 expectedRowsAffected, Int32 rowsAffected, CancellationToken cancellationToken)
   at Npgsql.EntityFrameworkCore.PostgreSQL.Update.Internal.NpgsqlModificationCommandBatch.Consume(RelationalDataReader reader, Boolean async, CancellationToken cancellationToken)
   at Microsoft.EntityFrameworkCore.Update.ReaderModificationCommandBatch.ExecuteAsync(IRelationalConnection connection, CancellationToken cancellationToken)
   at Microsoft.EntityFrameworkCore.Update.ReaderModificationCommandBatch.ExecuteAsync(IRelationalConnection connection, CancellationToken cancellationToken)
   at Microsoft.EntityFrameworkCore.Update.Internal.BatchExecutor.ExecuteAsync(IEnumerable\u00601 commandBatches, IRelationalConnection connection, CancellationToken cancellationToken)
   at Microsoft.EntityFrameworkCore.Update.Internal.BatchExecutor.ExecuteAsync(IEnumerable\u00601 commandBatches, IRelationalConnection connection, CancellationToken cancellationToken)
   at Microsoft.EntityFrameworkCore.Update.Internal.BatchExecutor.ExecuteAsync(IEnumerable\u00601 commandBatches, IRelationalConnection connection, CancellationToken cancellationToken)
   at Microsoft.EntityFrameworkCore.ChangeTracking.Internal.StateManager.SaveChangesAsync(IList\u00601 entriesToSave, CancellationToken cancellationToken)
   at Microsoft.EntityFrameworkCore.ChangeTracking.Internal.StateManager.SaveChangesAsync(StateManager stateManager, Boolean acceptAllChangesOnSuccess, CancellationToken cancellationToken)
   at Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.NpgsqlExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func\u00604 operation, Func\u00604 verifySucceeded, CancellationToken cancellationToken)
   at Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync(Boolean acceptAllChangesOnSuccess, CancellationToken cancellationToken)
   at Craftsman.Infra.Persistence.AppDbContext.SaveChangesAsync(CancellationToken cancellationToken) in /src/src/Infra/Persistence/AppDbContext.cs:line 73
   at Craftsman.Infra.Repositories.UnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) in /src/src/Infra/Repositories/UnitOfWork.cs:line 17
   at Craftsman.App.Services.ManualOrderService.UpdateAsync(Guid orderId, ManualOrderInputModel input, CancellationToken cancellationToken) in /src/src/App/Services/ManualOrderService.cs:line 202
   at Craftsman.App.Controllers.ManualOrdersController.Edit(Guid id, ManualOrderInputModel input, CancellationToken cancellationToken) in /src/src/App/Controllers/ManualOrdersController.cs:line 111
   at Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor.TaskOfIActionResultExecutor.Execute(ActionContext actionContext, IActionResultTypeMapper mapper, ObjectMethodExecutor executor, Object controller, Object[] arguments)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.\u003CInvokeActionMethodAsync\u003Eg__Awaited|12_0(ControllerActionInvoker invoker, ValueTask\u00601 actionResultValueTask)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.\u003CInvokeNextActionFilterAsync\u003Eg__Awaited|10_0(ControllerActionInvoker invoker, Task lastTask, State next, Scope scope, Object state, Boolean isCompleted)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Rethrow(ActionExecutedContextSealed context)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Next(State\u0026 next, Scope\u0026 scope, Object\u0026 state, Boolean\u0026 isCompleted)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.\u003CInvokeInnerFilterAsync\u003Eg__Awaited|13_0(ControllerActionInvoker invoker, Task lastTask, State next, Scope scope, Object state, Boolean isCompleted)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.\u003CInvokeNextResourceFilter\u003Eg__Awaited|25_0(ResourceInvoker invoker, Task lastTask, State next, Scope scope, Object state, Boolean isCompleted)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.Rethrow(ResourceExecutedContextSealed context)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.Next(State\u0026 next, Scope\u0026 scope, Object\u0026 state, Boolean\u0026 isCompleted)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.\u003CInvokeFilterPipelineAsync\u003Eg__Awaited|20_0(ResourceInvoker invoker, Task lastTask, State next, Scope scope, Object state, Boolean isCompleted)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.\u003CInvokeAsync\u003Eg__Awaited|17_0(ResourceInvoker invoker, Task task, IDisposable scope)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.\u003CInvokeAsync\u003Eg__Awaited|17_0(ResourceInvoker invoker, Task task, IDisposable scope)
   at Microsoft.AspNetCore.Authorization.AuthorizationMiddleware.Invoke(HttpContext context)
   at Microsoft.AspNetCore.Authentication.AuthenticationMiddleware.Invoke(HttpContext context)
   at Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddlewareImpl.\u003CInvoke\u003Eg__Awaited|10_0(ExceptionHandlerMiddlewareImpl middleware, HttpContext context, Task task)