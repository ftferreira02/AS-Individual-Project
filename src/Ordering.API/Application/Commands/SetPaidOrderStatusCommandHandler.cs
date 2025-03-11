using System.Diagnostics.Metrics;
using eShop.Ordering.API.Telemetry;
using Microsoft.Extensions.Logging;

namespace eShop.Ordering.API.Application.Commands;

// Regular CommandHandler
public class SetPaidOrderStatusCommandHandler : IRequestHandler<SetPaidOrderStatusCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<SetPaidOrderStatusCommandHandler> _logger;

    public SetPaidOrderStatusCommandHandler
        (IOrderRepository orderRepository
        ,
        ILogger<SetPaidOrderStatusCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));


    }

    /// <summary>
    /// Handler which processes the command when
    /// Shipment service confirms the payment
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public async Task<bool> Handle(SetPaidOrderStatusCommand command, CancellationToken cancellationToken)
    {
        // Simulate a work time for validating the payment
        await Task.Delay(10000, cancellationToken);

        var orderToUpdate = await _orderRepository.GetAsync(command.OrderNumber);
        if (orderToUpdate == null)
        {
            return false;
        }

        orderToUpdate.SetPaidStatus();
        TelemetryMetrics.ActiveOrdersGauge.Add(-1, new KeyValuePair<string, object>("orderId", command.OrderNumber));
        _logger.LogInformation("Active Orders Decreased for Canceled Order: {OrderId}", command.OrderNumber);


        return await _orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}


// Use for Idempotency in Command process
public class SetPaidIdentifiedOrderStatusCommandHandler : IdentifiedCommandHandler<SetPaidOrderStatusCommand, bool>
{
    public SetPaidIdentifiedOrderStatusCommandHandler(
        IMediator mediator,
        IRequestManager requestManager,
        ILogger<IdentifiedCommandHandler<SetPaidOrderStatusCommand, bool>> logger)
        : base(mediator, requestManager, logger)
    {
    }

    protected override bool CreateResultForDuplicateRequest()
    {
        return true; // Ignore duplicate requests for processing order.
    }
}
