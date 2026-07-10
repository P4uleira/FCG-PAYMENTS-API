namespace FCG.Payments.Application.DTOs;

public record PaymentDto(
    Guid Id,
    Guid OrderId,
    Guid UserId,
    Guid GameId,
    decimal Price,
    string Status,
    DateTime ProcessedAt);