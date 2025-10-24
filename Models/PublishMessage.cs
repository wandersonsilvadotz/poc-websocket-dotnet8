namespace AppPlatformWebSocket.Models;

public class PublishMessage
{
    public PubSubMessage Message { get; set; } = new();
    public string Subscription { get; set; } = string.Empty;
    public string? TraceId { get; set; }
}

public class PubSubMessage
{
    public string Data { get; set; } = string.Empty;


    public string MessageId { get; set; } = string.Empty;
    public string PublishTime { get; set; } = string.Empty;
    public object Body { get; set; } = new();
}
