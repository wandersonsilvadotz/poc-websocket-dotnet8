namespace AppPlatformWebSocket.Services;

public class MetricsService
{
    private int _activeConnections;
    private int _messagesBroadcasted;
    private int _publishRequests;

    public int ActiveConnections => _activeConnections;
    public int MessagesBroadcasted => _messagesBroadcasted;
    public int PublishRequests => _publishRequests;

    public void IncrementActiveConnections() => Interlocked.Increment(ref _activeConnections);
    public void DecrementActiveConnections() => Interlocked.Decrement(ref _activeConnections);
    public void IncrementMessagesBroadcasted() => Interlocked.Increment(ref _messagesBroadcasted);
    public void IncrementPublishRequests() => Interlocked.Increment(ref _publishRequests);
}
