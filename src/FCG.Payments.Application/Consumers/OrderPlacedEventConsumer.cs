using FCG.Catalog.Contracts.Events;
using FCG.Payments.Application.Commands.ProcessPayment;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FCG.Payments.Application.Consumers;

public class OrderPlacedEventConsumer
    : IConsumer<OrderPlacedEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<OrderPlacedEventConsumer> _logger;

    public OrderPlacedEventConsumer(
        ISender sender,
        ILogger<OrderPlacedEventConsumer> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Consume(
        ConsumeContext<OrderPlacedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "OrderPlacedEvent recebido. OrderId: {OrderId}, UserId: {UserId}, GameId: {GameId}, Price: {Price}",
            message.OrderId,
            message.UserId,
            message.GameId,
            message.Price);

        await _sender.Send(
            new ProcessPaymentCommand(
                message.OrderId,
                message.UserId,
                message.GameId,
                message.Price),
            context.CancellationToken);
    }
}