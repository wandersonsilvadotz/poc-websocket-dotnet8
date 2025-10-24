namespace AppPlatformWebSocket.Models;

public record ClientAck(
    string ConnectionId,
    string MessageId,
    DateTime ReceivedAtUtc
);
