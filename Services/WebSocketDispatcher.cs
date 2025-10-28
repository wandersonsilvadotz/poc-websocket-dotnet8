using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AppPlatformWebSocket.Models;
using Elastic.Apm; // spans opcionais
using Elastic.Apm.Api;

namespace AppPlatformWebSocket.Services;

public class WebSocketDispatcher
{
    private static HashSet<string> GetAllowedSubs(ClaimsPrincipal? principal)
{
    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (principal == null) return set;

    // Caso as subscriptions venham como várias claims com mesmo tipo:
    foreach (var c in principal.FindAll("subscriptions"))
        set.Add(c.Value);

    // Caso venham agregadas em uma única claim como JSON:
    var jsonClaim = principal.FindFirst("subscriptions_json")?.Value;
    if (!string.IsNullOrWhiteSpace(jsonClaim))
    {
        try
        {
            var arr = JsonSerializer.Deserialize<string[]>(jsonClaim);
            if (arr != null) foreach (var s in arr) set.Add(s);
        }
        catch { /* ignore parse errors */ }
    }

    return set;
}

    private readonly WebSocketConnectionManager _connMgr;
    private readonly DeliveryStore _deliveryStore;
    private readonly ILogger<WebSocketDispatcher> _logger;

    public WebSocketDispatcher(WebSocketConnectionManager connMgr, DeliveryStore deliveryStore, ILogger<WebSocketDispatcher> logger)
    {
        _connMgr = connMgr;
        _deliveryStore = deliveryStore;
        _logger = logger;
    }
    //Unicast por usuário (fan-out para todas as conexões do mesmo userId)
public async Task SendToUserAsync(string userId, PublishMessage msg, CancellationToken ct = default)
    {
        var span = Agent.Tracer.CurrentTransaction?.StartSpan("WS-Unicast", "websocket");
        try
        {
            if (!string.IsNullOrWhiteSpace(msg.TraceId)) span?.SetLabel("trace.id", msg.TraceId);

            var json = JsonSerializer.Serialize(msg);
            var payload = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
            var conns = _connMgr.GetConnectionsByUser(userId);

            foreach (var (connId, socket) in conns)
            {
                if (socket.State != WebSocketState.Open) continue;

                try
                {
                    await socket.SendAsync(payload, WebSocketMessageType.Text, true, ct);
                    _deliveryStore.Add(new DeliveryRecord(connId, msg.Message?.MessageId, DateTime.UtcNow));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao enviar unicast para conexão {Id}. Removendo socket.", connId);
                    await _connMgr.RemoveSocketAsync(connId);
                }
            }
        }
        finally
        {
            span?.End();
        }
    }

    public async Task BroadcastAsync(PublishMessage msg, CancellationToken ct = default)
    {
        var span = Agent.Tracer.CurrentTransaction?.StartSpan("WS-Broadcast", "websocket");

        try
        {
            if (!string.IsNullOrWhiteSpace(msg.TraceId))
                span?.SetLabel("trace.id", msg.TraceId);
            var json = JsonSerializer.Serialize(msg);
            var bytes = Encoding.UTF8.GetBytes(json);
            var segment = new ArraySegment<byte>(bytes);

            foreach (var kv in _connMgr.GetAll())
            {
                var id = kv.Key;
                var socket = kv.Value.Socket;

                if (socket.State != WebSocketState.Open)
                    continue;

                var allowed = GetAllowedSubs(kv.Value.Principal);
                // Se a mensagem tiver subscription definida, verifique permissão:
                if (!string.IsNullOrWhiteSpace(msg.Subscription) && allowed.Count > 0 && !allowed.Contains(msg.Subscription))
                {
                    continue; // não autorizado a receber este canal
                }


                try
                {
                    await socket.SendAsync(segment, WebSocketMessageType.Text, true, ct);
                    _deliveryStore.Add(new DeliveryRecord(id, msg.Message?.MessageId, DateTime.UtcNow));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao enviar para conexão {Id}. Removendo socket.", id);
                    await _connMgr.RemoveSocketAsync(id);
                }
            }
        }
        finally
        {
            span?.End();
        }
    }
}
