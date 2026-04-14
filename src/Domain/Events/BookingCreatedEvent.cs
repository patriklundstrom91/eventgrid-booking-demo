namespace Domain.Events;

public record BookingCreatedEvent(
    Guid BookingId,
    string CustomerName,
    DateTime CreatedAt
);
