namespace Domain.Models;

public record OutboxItem(
    string FunctionName,
    object Body
);