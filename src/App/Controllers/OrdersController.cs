using Craftsman.App.Models;
using Craftsman.App.Services;
using Craftsman.Domain.Integration.Services;
using Craftsman.Domain.Production.Services;
using Craftsman.Domain.Repositories;
using Craftsman.Infra.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Craftsman.App.Controllers;

[Authorize(Policy = ApplicationPolicies.Read)]
public sealed class OrdersController : Controller
{
    private readonly IOrderImportPipeline orderImportPipeline;
    private readonly OrderQueryService orderQueryService;
    private readonly ManualOrderService manualOrderService;
    private readonly OrderProductionPlanningService productionPlanningService;
    private readonly StockAlertAppService stockAlertAppService;
    private readonly IAuditService auditService;
    private readonly ITransactionalUnitOfWork? transactionalUnitOfWork;
    private readonly ILogger<OrdersController>? logger;
    private readonly ICurrentUserContext? currentUserContext;

    public OrdersController(
        OrderQueryService orderQueryService,
        ManualOrderService manualOrderService,
        IOrderImportPipeline orderImportPipeline,
        OrderProductionPlanningService productionPlanningService,
        StockAlertAppService stockAlertAppService,
        IAuditService auditService,
        ITransactionalUnitOfWork? transactionalUnitOfWork = null,
        ILogger<OrdersController>? logger = null,
        ICurrentUserContext? currentUserContext = null)
    {
        this.orderQueryService = orderQueryService;
        this.manualOrderService = manualOrderService;
        this.orderImportPipeline = orderImportPipeline;
        this.productionPlanningService = productionPlanningService;
        this.stockAlertAppService = stockAlertAppService;
        this.auditService = auditService;
        this.transactionalUnitOfWork = transactionalUnitOfWork;
        this.logger = logger;
        this.currentUserContext = currentUserContext;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var orders = await orderQueryService.ListAsync(cancellationToken);
        return View(new OrdersIndexViewModel(orders, null));
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPolicies.Write)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(CancellationToken cancellationToken)
    {
        var result = await orderImportPipeline.ImportAsync(cancellationToken);
        await auditService.RecordAsync(
            AuditAction.ManualImport,
            "OrderImport",
            after: new { result.ImportedCount, result.SkippedCount, FailureCount = result.Failures.Count },
            cancellationToken: cancellationToken);
        var orders = await orderQueryService.ListAsync(cancellationToken);
        var importResult = new ImportResultViewModel(
            result.ImportedCount,
            result.SkippedCount,
            result.Failures.Select(failure => $"{failure.Source}/{failure.ExternalOrderId}: {failure.Reason}").ToList().AsReadOnly());

        return View("Index", new OrdersIndexViewModel(orders, importResult));
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var order = await orderQueryService.GetDetailAsync(id, cancellationToken);

        return order is null ? NotFound() : View(order);
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPolicies.Write)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendToProduction(Guid id, CancellationToken cancellationToken)
    {
        var stockAlerts = await stockAlertAppService.ListAlertsForOrderAsync(id, cancellationToken);

        async Task<ProductionPlanningResult> PlanAndAuditAsync()
        {
            var result = await productionPlanningService.TryPlanAsync(id, cancellationToken);
            await auditService.RecordAsync(
                AuditAction.SendToProduction,
                "Order",
                id.ToString(),
                after: new { Planned = result.Succeeded, Failure = result.FailureReason.ToString() },
                cancellationToken: cancellationToken);
            return result;
        }

        ProductionPlanningResult planningResult;
        try
        {
            planningResult = transactionalUnitOfWork is null
                ? await PlanAndAuditAsync()
                : await transactionalUnitOfWork.ExecuteInSerializableTransactionAsync(PlanAndAuditAsync, cancellationToken);
        }
        catch (Exception exception) when (IsProductionPlanningConcurrencyFailure(exception))
        {
            logger?.LogWarning(exception, "Production planning concurrency conflict for order {OrderId}.", id);
            await auditService.RecordAsync(
                AuditAction.SendToProduction,
                "Order",
                id.ToString(),
                after: new { Planned = false, Failure = "ConcurrencyConflict" },
                cancellationToken: cancellationToken);
            TempData["Error"] = "O pedido foi alterado durante o envio para producao. Atualize os dados e tente novamente.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var correlationId = currentUserContext?.Current.CorrelationId;
            logger?.LogError(exception, "Unexpected production planning failure for order {OrderId}. Correlation {CorrelationId}.", id, correlationId);

            try
            {
                await auditService.RecordAsync(
                    AuditAction.SendToProduction,
                    "Order",
                    id.ToString(),
                    after: new { Planned = false, Failure = "Unexpected", CorrelationId = correlationId },
                    cancellationToken: cancellationToken);
            }
            catch (Exception auditException)
            {
                logger?.LogError(auditException, "Could not record unexpected production planning failure for order {OrderId}.", id);
            }

            TempData["Error"] = GetUnexpectedFailureMessage(correlationId);
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData[planningResult.Succeeded ? "Success" : "Error"] = planningResult.Succeeded
            ? "Pedido enviado para producao."
            : GetPlanningFailureMessage(planningResult);
            
        if (stockAlerts.Count > 0)
        {
            TempData["StockAlerts"] = string.Join(
                " | ",
                stockAlerts.Select(alert => $"{alert.LevelLabel}: {alert.RawMaterialName} com saldo {alert.Balance} {alert.UnitOfMeasure} (limite {alert.ReachedLimit})"));
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private string GetPlanningFailureMessage(ProductionPlanningResult result)
    {
        return result.FailureReason switch
        {
            ProductionPlanningFailureReason.InsufficientStock => "Nao foi possivel enviar para producao: estoque insuficiente. Revise os materiais necessarios e reponha o saldo.",
            ProductionPlanningFailureReason.MissingProductMapping => "Nao foi possivel enviar para producao: existe item sem vinculo com produto interno.",
            ProductionPlanningFailureReason.InvalidProductConfiguration => "Nao foi possivel enviar para producao: existe produto, ficha tecnica ou materia-prima com configuracao invalida.",
            ProductionPlanningFailureReason.AlreadyPlanned => "Nao foi possivel enviar para producao: este pedido ja possui planejamento.",
            ProductionPlanningFailureReason.OrderNotFound => "Nao foi possivel enviar para producao: pedido nao encontrado.",
            ProductionPlanningFailureReason.ConcurrencyConflict => "O pedido foi alterado durante o envio para producao. Atualize os dados e tente novamente.",
            _ => $"O envio para producao nao foi concluido. Tente novamente e informe a referencia {currentUserContext?.Current.CorrelationId ?? "indisponivel"} ao suporte."
        };
    }

    private static string GetUnexpectedFailureMessage(string? correlationId)
    {
        return $"O envio para producao nao foi concluido. Tente novamente e informe a referencia {correlationId ?? "indisponivel"} ao suporte.";
    }

    private static bool IsProductionPlanningConcurrencyFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbUpdateConcurrencyException)
            {
                return true;
            }

            if (current is PostgresException postgresException &&
                postgresException.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected)
            {
                return true;
            }
        }

        return false;
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPolicies.Write)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevertProduction(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            async Task<bool> RevertAndAuditAsync()
            {
                var result = await productionPlanningService.RevertAsync(id, cancellationToken);
                await auditService.RecordAsync(
                    AuditAction.ProductionPlanningReverted,
                    "Order",
                    id.ToString(),
                    after: new { Reverted = result },
                    cancellationToken: cancellationToken);
                return result;
            }

            var reverted = transactionalUnitOfWork is null
                ? await RevertAndAuditAsync()
                : await transactionalUnitOfWork.ExecuteInTransactionAsync(RevertAndAuditAsync, cancellationToken);

            TempData[reverted ? "Success" : "Error"] = reverted
                ? "Planejamento de producao revertido."
                : "O planejamento de producao ja estava revertido.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPolicies.Write)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, Guid? processId, CancellationToken cancellationToken)
    {
        try
        {
            await manualOrderService.DeleteAsync(id, processId, cancellationToken);
            TempData["Success"] = "Pedido excluido.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
