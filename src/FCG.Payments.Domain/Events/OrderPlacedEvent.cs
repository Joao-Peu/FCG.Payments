namespace FCG.Payments.Domain.Events;

public record OrderPlacedEvent(
    string OrderId,
    string UserId,
    string GameId,
    decimal Price);
