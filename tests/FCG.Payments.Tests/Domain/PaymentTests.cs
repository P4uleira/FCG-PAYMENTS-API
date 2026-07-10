using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Enums;

namespace FCG.Payments.Tests.Domain;

public class PaymentTests
{
    [Fact]
    public void Constructor_ShouldCreateApprovedPayment()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        var payment = new Payment(
            orderId,
            userId,
            gameId,
            149.90m);

        Assert.NotEqual(Guid.Empty, payment.Id);
        Assert.Equal(orderId, payment.OrderId);
        Assert.Equal(userId, payment.UserId);
        Assert.Equal(gameId, payment.GameId);
        Assert.Equal(149.90m, payment.Price);
        Assert.Equal(PaymentStatus.Approved, payment.Status);
        Assert.NotEqual(default, payment.ProcessedAt);
    }

    [Fact]
    public void Reject_ShouldChangeStatusToRejected()
    {
        var payment = new Payment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            99.90m);

        payment.Reject();

        Assert.Equal(
            PaymentStatus.Rejected,
            payment.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldRejectInvalidAmount(
        decimal Price)
    {
        Assert.Throws<ArgumentException>(() =>
            new Payment(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Price));
    }
}