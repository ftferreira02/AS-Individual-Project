using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace eShop.Ordering.API.Application.Commands;

// Regular CommandHandler
public class SetPaidOrderStatusCommandHandler : IRequestHandler<SetPaidOrderStatusCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly UpDownCounter<int> _activeOrdersGauge;
    private readonly ILogger<ShipOrderCommandHandler> _logger;

    public SetPaidOrderStatusCommandHandler
        (IOrderRepository orderRepository
        , UpDownCounter<int> activeOrdersGauge,
        ILogger<ShipOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _activeOrdersGauge = activeOrdersGauge ?? throw new ArgumentNullException(nameof(activeOrdersGauge));
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
        // Decrement the active orders counter when an order is shipped
        _activeOrdersGauge.Add(-1,
            new KeyValuePair<string, object>("orderId", command.OrderNumber.ToString()));

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
