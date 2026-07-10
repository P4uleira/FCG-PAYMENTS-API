using FCG.Payments.Application.DTOs;
using MediatR;

namespace FCG.Payments.Application.Commands.ProcessPayment;

public record ProcessPaymentCommand(
    Guid OrderId,
    Guid UserId,
    Guid GameId,
    decimal Price) : IRequest<PaymentDto>;