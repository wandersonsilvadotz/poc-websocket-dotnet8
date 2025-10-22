using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using AppPlatformWebSocket.Models;

namespace AppPlatformWebSocket.Services;

public class WebSocketDispatcher
{
    private readonly WebSocketConnectionManager _connMgr;

    public WebSocketDispatcher(WebSocketConnectionManager connMgr)
    {
        _connMgr = connMgr;
    }

    public async Task BroadcastAsync(PublishMessage msg)
    {
        var payload = JsonSerializer.Serialize(msg);
        var bytes = Encoding.UTF8.GetBytes(payload);

        foreach (var socket in _connMgr.GetAllSockets())
        {
            if (socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
        }
    }
}
