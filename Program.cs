using System.Net.WebSockets;
using System.Text.Json;
using AppPlatformWebSocket.Models;
using AppPlatformWebSocket.Services;

var builder = WebApplication.CreateBuilder(args);

// Serviços
builder.Services.AddSingleton<WebSocketConnectionManager>();
builder.Services.AddSingleton<WebSocketDispatcher>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuração de logs no console
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

// Swagger apenas em Desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseWebSockets();

// 🔹 Métricas internas
var metrics = new
{
    ActiveConnections = 0,
    MessagesBroadcasted = 0,
    PublishRequests = 0
};
int activeConnections = 0;
int messagesBroadcasted = 0;
int publishRequests = 0;

// WebSocket endpoint
app.Map("/ws", async (HttpContext context, WebSocketConnectionManager connMgr, ILogger<Program> logger) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var socket = await context.WebSockets.AcceptWebSocketAsync();
        var id = connMgr.AddSocket(socket);

        Interlocked.Increment(ref activeConnections);
        logger.LogInformation("🟢 Nova conexão: {ConnectionId} | Ativas: {Active}", id, activeConnections);

        var buffer = new byte[1024 * 4];
        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await connMgr.RemoveSocket(id);
                Interlocked.Decrement(ref activeConnections);
                logger.LogInformation("🔴 Conexão encerrada: {ConnectionId} | Ativas: {Active}", id, activeConnections);
                break;
            }
        }
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

// HTTP endpoint (Pub/Sub)
app.MapPost("/v1/publish", async (PublishMessage msg, WebSocketDispatcher dispatcher, ILogger<Program> logger) =>
{
    Interlocked.Increment(ref publishRequests);
    logger.LogInformation("📨 Mensagem recebida: {Message}", JsonSerializer.Serialize(msg));

    await dispatcher.BroadcastAsync(msg);
    Interlocked.Increment(ref messagesBroadcasted);

    logger.LogInformation("📡 Mensagem broadcast concluído. Total enviados: {Total}", messagesBroadcasted);

    return Results.Ok(new
    {
        status = "sent",
        totalMessages = messagesBroadcasted,
        activeConnections
    });
});

// 🔹 Endpoint de métricas (acessível via Swagger)
app.MapGet("/metrics", () =>
{
    var snapshot = new
    {
        ActiveConnections = activeConnections,
        MessagesBroadcasted = messagesBroadcasted,
        PublishRequests = publishRequests,
        Timestamp = DateTime.UtcNow
    };
    return Results.Ok(snapshot);
})
.WithName("Obter métricas em tempo real");

app.Run();
