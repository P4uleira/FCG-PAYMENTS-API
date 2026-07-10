using FCG.Payments.Application.DTOs;
using FCG.Payments.Domain.Repositories;
using MediatR;

namespace FCG.Payments.Application.Queries.GetPayments;

public class GetPaymentsQueryHandler
    : IRequestHandler<GetPaymentsQuery, IReadOnlyCollection<PaymentDto>>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentsQueryHandler(
        IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<IReadOnlyCollection<PaymentDto>> Handle(
        GetPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        var payments = await _paymentRepository.GetAllAsync(
            cancellationToken);

        return payments
            .Select(payment => new PaymentDto(
                payment.Id,
                payment.OrderId,
                payment.UserId,
                payment.GameId,
                payment.Price,
                payment.Status.ToString(),
                payment.ProcessedAt))
            .ToList();
    }
}