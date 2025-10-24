using System.Collections.Concurrent;
using AppPlatformWebSocket.Models;

namespace AppPlatformWebSocket.Services;

public class ClientAckStore
{
    private readonly ConcurrentQueue<ClientAck> _acks = new();
    private const int Max = 10000;

    public void Add(ClientAck ack)
    {
        _acks.Enqueue(ack);
        while (_acks.Count > Max && _acks.TryDequeue(out _)) {}
    }

    public IReadOnlyCollection<ClientAck> GetRecent(int max = 1000)
        => _acks.Reverse().Take(max).ToArray();
}
