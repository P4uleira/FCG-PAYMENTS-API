using FCG.Payments.Domain.Entities;

namespace FCG.Payments.Domain.Repositories;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment,CancellationToken cancellationToken = default);

    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Payment>> GetAllAsync(CancellationToken cancellationToken = default);
}