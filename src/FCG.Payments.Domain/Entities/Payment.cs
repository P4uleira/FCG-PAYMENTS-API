using FCG.Payments.Domain.Enums;

namespace FCG.Payments.Domain.Entities;

public class Payment
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid GameId { get; private set; }
    public decimal Price { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime ProcessedAt { get; private set; }

    private Payment()
    {
    }

    public Payment(
        Guid orderId,
        Guid userId,
        Guid gameId,
        decimal price)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("OrderId is required.", nameof(orderId));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        if (gameId == Guid.Empty)
            throw new ArgumentException("GameId is required.", nameof(gameId));

        if (price <= 0)
            throw new ArgumentException(
                "Payment price must be greater than zero.",
                nameof(price));

        Id = Guid.NewGuid();
        OrderId = orderId;
        UserId = userId;
        GameId = gameId;
        Price = price;
        Status = PaymentStatus.Approved;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Approve()
    {
        Status = PaymentStatus.Approved;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        Status = PaymentStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }
}