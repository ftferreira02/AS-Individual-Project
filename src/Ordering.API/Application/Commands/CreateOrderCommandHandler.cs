namespace eShop.Ordering.API.Application.Commands;

using System.Diagnostics;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using OpenTelemetry.Trace;

// Regular CommandHandler
public class CreateOrderCommandHandler
    : IRequestHandler<CreateOrderCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IIdentityService _identityService;
    private readonly IMediator _mediator;
    private readonly IOrderingIntegrationEventService _orderingIntegrationEventService;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    private static readonly ActivitySource ActivitySource = new("OrderAPI.CreateOrder");


    // Using DI to inject infrastructure persistence Repositories
    public CreateOrderCommandHandler(IMediator mediator,
        IOrderingIntegrationEventService orderingIntegrationEventService,
        IOrderRepository orderRepository,
        IIdentityService identityService,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _orderingIntegrationEventService = orderingIntegrationEventService ?? throw new ArgumentNullException(nameof(orderingIntegrationEventService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> Handle(CreateOrderCommand message, CancellationToken cancellationToken)
    {

        using var activity = ActivitySource.StartActivity("Processing Order");

        try
        {
            _logger.LogInformation("Processing new order for User: {UserId}", message.UserId);

            activity?.SetTag("order.userId", message.UserId);
            activity?.SetTag("order.totalItems", message.OrderItems.Count());
            activity?.SetTag("order.city", message.City);
            activity?.SetTag("order.country", message.Country);

            // Masking sensitive payment details
            activity?.SetTag("order.payment.cardType", message.CardTypeId);
            activity?.SetTag("order.payment.cardNumber", "REDACTED");
            activity?.SetTag("order.payment.cardSecurityNumber", "REDACTED");

            // Add Integration event to clean the basket
            var orderStartedIntegrationEvent = new OrderStartedIntegrationEvent(message.UserId);
            await _orderingIntegrationEventService.AddAndSaveEventAsync(orderStartedIntegrationEvent);

            activity?.AddEvent(new ActivityEvent("Order integration event sent"));

            // Add/Update the Buyer AggregateRoot
            // DDD patterns comment: Add child entities and value-objects through the Order Aggregate-Root
            // methods and constructor so validations, invariants and business logic 
            // make sure that consistency is preserved across the whole aggregate
            var address = new Address(message.Street, message.City, message.State, message.Country, message.ZipCode);
            var order = new Order(message.UserId, message.UserName, address, message.CardTypeId, message.CardNumber, message.CardSecurityNumber, message.CardHolderName, message.CardExpiration);

            foreach (var item in message.OrderItems)
            {
                order.AddOrderItem(item.ProductId, item.ProductName, item.UnitPrice, item.Discount, item.PictureUrl, item.Units);
            }

            _logger.LogInformation("Creating Order - Order: {@Order}", order);
            activity?.AddEvent(new ActivityEvent("Order entity created"));

            _orderRepository.Add(order);

            bool saveResult = await _orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            if (saveResult)
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
                _logger.LogInformation("Order successfully created: {OrderId}", order.Id);
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Failed to save order in database");
            }

            return saveResult;

        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            _logger.LogError(ex, "Error occurred while processing order for User: {UserId}", message.UserId);
            throw;
        }
    }
}


// Use for Idempotency in Command process
public class CreateOrderIdentifiedCommandHandler : IdentifiedCommandHandler<CreateOrderCommand, bool>
{
    public CreateOrderIdentifiedCommandHandler(
        IMediator mediator,
        IRequestManager requestManager,
        ILogger<IdentifiedCommandHandler<CreateOrderCommand, bool>> logger)
        : base(mediator, requestManager, logger)
    {
    }

    protected override bool CreateResultForDuplicateRequest()
    {
        return true; // Ignore duplicate requests for creating order.
    }
}
