using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Claims;

namespace AppPlatformWebSocket.Services;

public class WebSocketConnectionManager
{
    // Mapeia connectionId -> (socket, principal, userId, connectedAt)
    private readonly ConcurrentDictionary<string, (WebSocket Socket, ClaimsPrincipal? Principal, string? UserId, DateTime ConnectedAt)> _connections = new();

    // Mapeia userId -> várias connectionIds (para fan-out intrausuário)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _byUser = new();

    /// <summary>
    /// Adiciona uma nova conexão e a associa ao userId (caso exista).
    /// </summary>
    public string AddSocket(WebSocket socket, ClaimsPrincipal? principal = null, string? userId = null)
    {
        var id = Guid.NewGuid().ToString("N");
        var connectedAt = DateTime.UtcNow;

        _connections[id] = (socket, principal, userId, connectedAt);

        // Se a conexão tiver um usuário vinculado (claim "sub"), registra no mapa de usuários
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var bucket = _byUser.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
            bucket[id] = 1;
        }

        return id;
    }

    /// <summary>
    /// Remove uma conexão (fecha o socket e limpa os índices).
    /// </summary>
    public async Task RemoveSocketAsync(string id)
    {
        if (_connections.TryRemove(id, out var tuple))
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
                // Ignora exceções de fechamento
            }
            finally
            {
                tuple.Socket.Dispose();

                // Remove também do índice de usuários
                if (!string.IsNullOrWhiteSpace(tuple.UserId) &&
                    _byUser.TryGetValue(tuple.UserId, out var bucket))
                {
                    bucket.TryRemove(id, out _);
                    if (bucket.IsEmpty)
                        _byUser.TryRemove(tuple.UserId, out _);
                }
            }
        }
    }

    /// <summary>
    /// Retorna todas as conexões ativas.
    /// </summary>
    public IEnumerable<KeyValuePair<string, (WebSocket Socket, ClaimsPrincipal? Principal, string? UserId, DateTime ConnectedAt)>> GetAll()
        => _connections;

    /// <summary>
    /// Retorna todas as conexões pertencentes a um usuário específico.
    /// </summary>
    public IReadOnlyCollection<(string ConnectionId, WebSocket Socket)> GetConnectionsByUser(string userId)
    {
        if (_byUser.TryGetValue(userId, out var conns))
        {
            var list = new List<(string, WebSocket)>(conns.Count);
            foreach (var connId in conns.Keys)
            {
                if (_connections.TryGetValue(connId, out var tuple) && tuple.Socket.State == WebSocketState.Open)
                    list.Add((connId, tuple.Socket));
            }
            return list;
        }

        return Array.Empty<(string, WebSocket)>();
    }

    public int Count => _connections.Count;
}
