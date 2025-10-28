using System;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text.Json;

namespace AppPlatformWebSocket.Services;

public static class DiagnosticsHelper
{
    private static readonly object _lock = new();

    private static void Log(string label, string message, ConsoleColor color)
    {
        lock (_lock)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] {label,-15} | {message}");
            Console.ForegroundColor = old;
        }
    }

    public static void ConnectionAdded(string connId, string? userId, ClaimsPrincipal? principal)
    {
        Log("🟢 CONNECT", $"ConnId={connId}, UserId={userId ?? "null"}, Subs=[{string.Join(",", principal?.FindAll("subscriptions").Select(c => c.Value) ?? Array.Empty<string>())}]", ConsoleColor.Green);
    }

    public static void ConnectionRemoved(string connId, string? userId)
    {
        Log("🔴 DISCONNECT", $"ConnId={connId}, UserId={userId ?? "null"}", ConsoleColor.Red);
    }

    public static void UnicastSent(string userId, string messageId, string? traceId, int connections)
    {
        Log("📨 UNICAST", $"UserId={userId}, MsgId={messageId}, Trace={traceId}, To={connections} conn(s)", ConsoleColor.Cyan);
    }

    public static void BroadcastSent(string subscription, string messageId, string? traceId)
    {
        Log("📡 BROADCAST", $"Channel={subscription}, MsgId={messageId}, Trace={traceId}", ConsoleColor.Yellow);
    }

    public static void DebugMessage(string message)
    {
        Log("🔍 DEBUG", message, ConsoleColor.DarkGray);
    }

    public static void Error(string label, Exception ex)
    {
        Log("💥 ERROR", $"{label}: {ex.Message}", ConsoleColor.Magenta);
    }
}
