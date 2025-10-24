using System.Collections.Concurrent;
using AppPlatformWebSocket.Models;

namespace AppPlatformWebSocket.Services;

public class DeliveryStore
{
    private readonly ConcurrentQueue<DeliveryRecord> _records = new();
    private const int MaxRecords = 10000;

    public void Add(DeliveryRecord record)
    {
        _records.Enqueue(record);
        while (_records.Count > MaxRecords && _records.TryDequeue(out _)) { }
    }

    public IReadOnlyCollection<DeliveryRecord> GetRecent(int max = 1000)
    {
        return _records.Reverse().Take(max).ToArray();
    }
}
