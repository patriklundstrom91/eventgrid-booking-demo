namespace Domain.Events;

public record BookingValidatedEvent(
    Guid BookingId,
    string CustomerName,
    DateTime CreatedAt,
    DateTime ValidatedAt
);