namespace FCG.Catalog.Contracts.Events;

public record OrderPlacedEvent(
    Guid OrderId,
    Guid UserId,
    Guid GameId,
    decimal Price,
    DateTime PlacedAt);