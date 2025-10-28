using System.Text.Json.Serialization;

namespace AppPlatformWebSocket.Models;

public class PublishMessage
{
    // Mapeia o campo "message" do JSON para esta propriedade
    [JsonPropertyName("message")]
    public PubSubMessage Message { get; set; } = new();

    // Mapeia o campo "subscription" do JSON para esta propriedade
    [JsonPropertyName("subscription")]
    public string Subscription { get; set; } = string.Empty;

    // Mapeia o campo legado "target_user_id"
    [JsonPropertyName("target_user_id")]
    public string? TargetUserId { get; set; }

    // Mapeia o campo legado "trace_id"
    [JsonPropertyName("trace_id")]
    public string? TraceId { get; set; }
}

public class PubSubMessage
{
    // Mapeia o campo "data"
    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;

    // Mapeia o campo "message_id"
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = string.Empty;

    // Mapeia o campo "publish_time"
    [JsonPropertyName("publish_time")]
    public string PublishTime { get; set; } = string.Empty;

    // Mapeia o campo "body"
    [JsonPropertyName("body")]
    public object Body { get; set; } = new();
}
