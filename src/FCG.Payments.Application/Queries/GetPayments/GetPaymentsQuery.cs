using FCG.Payments.Application.DTOs;
using MediatR;

namespace FCG.Payments.Application.Queries.GetPayments;

public record GetPaymentsQuery
    : IRequest<IReadOnlyCollection<PaymentDto>>;