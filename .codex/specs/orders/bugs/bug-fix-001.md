# BUG FIX 001

Na tela de Produção, quando você clica em para iniciar, finalizar ou cancelar a tarefa, recebo o seguinte erro.

## STACK TRACE

InvalidOperationException: The instance of entity type 'ProductionTaskEntity' cannot be tracked because another instance with the same key value for {'Id'} is already being tracked. When attaching existing entities, ensure that only one entity instance with a given key value is attached. Consider using 'DbContextOptionsBuilder.EnableSensitiveDataLogging' to see the conflicting key values.
Microsoft.EntityFrameworkCore.ChangeTracking.Internal.IdentityMap<TKey>.ThrowIdentityConflict(InternalEntityEntry entry)
Microsoft.EntityFrameworkCore.ChangeTracking.Internal.IdentityMap<TKey>.Add(TKey key, InternalEntityEntry entry, bool updateDuplicate)
Microsoft.EntityFrameworkCore.ChangeTracking.Internal.IdentityMap<TKey>.Add(TKey key, InternalEntityEntry entry)
Microsoft.EntityFrameworkCore.ChangeTracking.Internal.IdentityMap<TKey>.Add(InternalEntityEntry entry)
Microsoft.EntityFrameworkCore.ChangeTracking.Internal.StateManager.StartTracking(InternalEntityEntry entry)
Microsoft.EntityFrameworkCore.ChangeTracking.Internal.InternalEntityEntry.SetEntityState(EntityState oldState, EntityState newState, bool acceptChanges, bool modifyProperties)
Microsoft.EntityFrameworkCore.ChangeTracking.Internal.InternalEntityEntry.SetEntityState(EntityState entityState, bool acceptChanges, bool modifyProperties, Nullable<EntityState> forceStateWhenUnknownKey, Nullable<EntityState> fallbackState)
Microsoft.EntityFrameworkCore.ChangeTracking.Internal.EntityGraphAttacher.PaintAction(EntityEntryGraphNode<ValueTuple<EntityState, EntityState, bool>> node)
Microsoft.EntityFrameworkCore.ChangeTracking.Internal.EntityEntryGraphIterator.TraverseGraph<TState>(EntityEntryGraphNode<TState> node, Func<EntityEntryGraphNode<TState>, bool> handleNode)
Microsoft.EntityFrameworkCore.ChangeTracking.Internal.EntityGraphAttacher.AttachGraph(InternalEntityEntry rootEntry, EntityState targetState, EntityState storeGeneratedWithKeySetTargetState, bool forceStateWhenUnknownKey)
Microsoft.EntityFrameworkCore.Internal.InternalDbSet<TEntity>.SetEntityState(InternalEntityEntry entry, EntityState entityState)
Microsoft.EntityFrameworkCore.Internal.InternalDbSet<TEntity>.Update(TEntity entity)
Craftsman.Infra.Repositories.ProductionTaskRepository.UpdateAsync(ProductionTask productionTask, CancellationToken cancellationToken) in ProductionTaskRepository.cs
+
        dbContext.ProductionTasks.Update(ToEntity(productionTask));
Craftsman.App.Services.ProductionScheduleService.AdvanceAsync(Guid id, string action, CancellationToken cancellationToken) in ProductionScheduleService.cs
+
        await productionTaskRepository.UpdateAsync(task, cancellationToken);
Craftsman.App.Controllers.ProductionController.Advance(Guid id, string action, CancellationToken cancellationToken) in ProductionController.cs
+
        await productionScheduleService.AdvanceAsync(id, action, cancellationToken);
Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor+TaskOfIActionResultExecutor.Execute(ActionContext actionContext, IActionResultTypeMapper mapper, ObjectMethodExecutor executor, object controller, object[] arguments)
System.Threading.Tasks.ValueTask<TResult>.get_Result()
Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.<InvokeActionMethodAsync>g__Awaited|12_0(ControllerActionInvoker invoker, ValueTask<IActionResult> actionResultValueTask)
Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.<InvokeNextActionFilterAsync>g__Awaited|10_0(ControllerActionInvoker invoker, Task lastTask, State next, Scope scope, object state, bool isCompleted)
Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Rethrow(ActionExecutedContextSealed context)
Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Next(ref State next, ref Scope scope, ref object state, ref bool isCompleted)
Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.<InvokeInnerFilterAsync>g__Awaited|13_0(ControllerActionInvoker invoker, Task lastTask, State next, Scope scope, object state, bool isCompleted)
Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.<InvokeNextResourceFilter>g__Awaited|25_0(ResourceInvoker invoker, Task lastTask, State next, Scope scope, object state, bool isCompleted)
Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.Rethrow(ResourceExecutedContextSealed context)
Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.Next(ref State next, ref Scope scope, ref object state, ref bool isCompleted)
Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.<InvokeFilterPipelineAsync>g__Awaited|20_0(ResourceInvoker invoker, Task lastTask, State next, Scope scope, object state, bool isCompleted)
Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.<InvokeAsync>g__Awaited|17_0(ResourceInvoker invoker, Task task, IDisposable scope)
Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.<InvokeAsync>g__Awaited|17_0(ResourceInvoker invoker, Task task, IDisposable scope)
Microsoft.AspNetCore.Authorization.AuthorizationMiddleware.Invoke(HttpContext context)
Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddlewareImpl.Invoke(HttpContext context)
