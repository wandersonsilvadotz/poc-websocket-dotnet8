using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Claims;

namespace AppPlatformWebSocket.Services;

public class WebSocketConnectionManager
{
private readonly ConcurrentDictionary<string, (WebSocket Socket, ClaimsPrincipal? Principal, DateTime ConnectedAt)> _sockets = new();

    public string AddSocket(WebSocket socket, ClaimsPrincipal? principal = null)
{
    var id = Guid.NewGuid().ToString("N");
    var connectedAt = DateTime.UtcNow; // registra o horário de conexão
    _sockets.TryAdd(id, (socket, principal, connectedAt));
    return id;
}


    public async Task RemoveSocketAsync(string id)
    {
        if (_sockets.TryRemove(id, out var tuple))
        {
            try
            {
                if (tuple.Socket.State == WebSocketState.Open || tuple.Socket.State == WebSocketState.CloseReceived)
                {
                    await tuple.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                }
            }
            catch
            {
                // swallow
            }
            finally
            {
                tuple.Socket.Dispose();
            }
        }
    }

    public IEnumerable<KeyValuePair<string, (WebSocket Socket, ClaimsPrincipal? Principal, DateTime ConnectedAt)>> GetAll()
    => _sockets;


    public int Count => _sockets.Count;
}
