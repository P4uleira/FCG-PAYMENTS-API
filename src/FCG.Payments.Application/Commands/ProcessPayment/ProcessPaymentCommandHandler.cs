using FCG.Payments.Application.DTOs;
using FCG.Payments.Contracts.Events;
using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Repositories;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FCG.Payments.Application.Commands.ProcessPayment;

public class ProcessPaymentCommandHandler
    : IRequestHandler<ProcessPaymentCommand, PaymentDto>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;

    public ProcessPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        IPublishEndpoint publishEndpoint,
        ILogger<ProcessPaymentCommandHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<PaymentDto> Handle(
        ProcessPaymentCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Pagamento iniciado. OrderId: {OrderId}, UserId: {UserId}, GameId: {GameId}, Price: {Price}",
            request.OrderId,
            request.UserId,
            request.GameId,
            request.Price);

        var existingPayment =
            await _paymentRepository.GetByOrderIdAsync(
                request.OrderId,
                cancellationToken);

        if (existingPayment is not null)
        {
            _logger.LogWarning(
                "Pagamento já processado para o pedido {OrderId}. O registro existente será retornado.",
                request.OrderId);

            return MapToDto(existingPayment);
        }

        var payment = new Payment(
            request.OrderId,
            request.UserId,
            request.GameId,
            request.Price);

        // Regra inicial do projeto:
        // todas as compras são aprovadas.
        payment.Approve();

        await _paymentRepository.AddAsync(
            payment,
            cancellationToken);

        _logger.LogInformation(
            "Pagamento aprovado. PaymentId: {PaymentId}, OrderId: {OrderId}",
            payment.Id,
            payment.OrderId);

        var paymentProcessedEvent = new PaymentProcessedEvent(
            payment.OrderId,
            payment.UserId,
            payment.GameId,
            payment.Price,
            payment.Status.ToString(),
            payment.ProcessedAt);

        await _publishEndpoint.Publish(
            paymentProcessedEvent,
            cancellationToken);

        _logger.LogInformation(
            "Evento PaymentProcessedEvent publicado. OrderId: {OrderId}, Status: {Status}",
            payment.OrderId,
            payment.Status);

        return MapToDto(payment);
    }

    private static PaymentDto MapToDto(Payment payment)
    {
        return new PaymentDto(
            payment.Id,
            payment.OrderId,
            payment.UserId,
            payment.GameId,
            payment.Price,
            payment.Status.ToString(),
            payment.ProcessedAt);
    }
}