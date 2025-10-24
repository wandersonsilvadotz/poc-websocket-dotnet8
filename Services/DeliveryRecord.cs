namespace AppPlatformWebSocket.Models;

public record DeliveryRecord(
    string ConnectionId,
    string? MessageId,
    DateTime DeliveredAtUtc
);
