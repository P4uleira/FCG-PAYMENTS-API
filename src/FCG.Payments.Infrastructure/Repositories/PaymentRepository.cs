using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Repositories;
using FCG.Payments.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FCG.Payments.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentsDbContext _context;

    public PaymentRepository(
        PaymentsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        await _context.Payments.AddAsync(
            payment,
            cancellationToken);

        await _context.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<Payment?> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                payment => payment.OrderId == orderId,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<Payment>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .AsNoTracking()
            .OrderByDescending(payment => payment.ProcessedAt)
            .ToListAsync(cancellationToken);
    }
}