using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using AppPlatformWebSocket.Models;
using AppPlatformWebSocket.Services;
using System.Security.Claims;
using Elastic.Apm.NetCoreAll; // requer Elastic.Apm.NetCoreAll


var builder = WebApplication.CreateBuilder(args);

// Configurações
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));

// Serviços centrais
builder.Services.AddSingleton<WebSocketConnectionManager>();
builder.Services.AddSingleton<DeliveryStore>();
builder.Services.AddSingleton<MetricsService>();
builder.Services.AddSingleton<WebSocketDispatcher>();
builder.Services.AddSingleton<ClientAckStore>();


// Redis backplane (opcional; ativo quando há connection string)
builder.Services.AddSingleton<RedisService>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var connStr = cfg.GetValue<string>("Redis:ConnectionString");
    var dispatcher = sp.GetRequiredService<WebSocketDispatcher>();
    var logger = sp.GetRequiredService<ILogger<RedisService>>();
    return new RedisService(connStr, dispatcher, logger);
});

// JWT validator
builder.Services.AddSingleton<JwtValidator>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffffffZ";
});

var app = builder.Build();

// Elastic APM (se estiver configurado no appsettings.json)
app.UseAllElasticApm(builder.Configuration);

// Swagger somente em Dev
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseWebSockets();

var metrics = app.Services.GetRequiredService<MetricsService>();
var dispatcher = app.Services.GetRequiredService<WebSocketDispatcher>();
var jwtValidator = app.Services.GetRequiredService<JwtValidator>();
var redis = app.Services.GetRequiredService<RedisService>();
var connMgr = app.Services.GetRequiredService<WebSocketConnectionManager>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Endpoint WebSocket com validação JWT (Header prioritário + fallback query)
app.Map("/ws", async (HttpContext context) =>
{
    // Prioriza Header Authorization: Bearer <token>
    if (!jwtValidator.TryValidateFromHttpContext(context, out var principal, out var error))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync(error ?? "Unauthorized");
        return;
    }

    // Verifica se é uma requisição WS válida
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    // Extrai o userId da claim "sub" (padrão JWT)
    // Extrai o userId corretamente (compatível com "sub" e NameIdentifier)
    var userId = principal?.FindFirst("sub")?.Value
                ?? principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    var socket = await context.WebSockets.AcceptWebSocketAsync();
    var id = connMgr.AddSocket(socket, principal, userId);

    metrics.IncrementActiveConnections();
    logger.LogInformation("Nova conexão aceita: {ConnId} (userId={UserId}) | Ativas: {Active}",
        id, userId, metrics.ActiveConnections);

    var buffer = new byte[4 * 1024];
    try
    {
        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            // Recebimento de mensagens do cliente (ex: ACK)
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("ack", out var ackProp))
                    {
                        var msgId = ackProp.GetString() ?? string.Empty;
                        app.Services.GetRequiredService<ClientAckStore>()
                            .Add(new AppPlatformWebSocket.Models.ClientAck(id, msgId, DateTime.UtcNow));
                    }
                }
                catch
                {
                    // ignora mensagens não reconhecidas
                }
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                await connMgr.RemoveSocketAsync(id);
                metrics.DecrementActiveConnections();
                logger.LogInformation("Conexão encerrada: {ConnId} (userId={UserId}) | Ativas: {Active}",
                    id, userId, metrics.ActiveConnections);
                break;
            }
        }
    }
    catch (Exception ex)
    {
        await connMgr.RemoveSocketAsync(id);
        metrics.DecrementActiveConnections();
        logger.LogWarning(ex, "Erro no socket {ConnId} (userId={UserId}). Conexão removida. Ativas: {Active}",
            id, userId, metrics.ActiveConnections);
    }
});


// Endpoint HTTP de publicação (unicast + broadcast)
app.MapPost("/v1/publish", async (PublishMessage msg, WebSocketDispatcher dispatcher, ILogger<Program> log, MetricsService metrics) =>
{
    metrics.IncrementPublishRequests();
    msg.TraceId ??= Guid.NewGuid().ToString("N");

    if (!string.IsNullOrWhiteSpace(msg.TargetUserId))
    {
        await dispatcher.SendToUserAsync(msg.TargetUserId, msg);
        log.LogInformation("Unicast enviado para userId={UserId} (messageId={MessageId}, traceId={TraceId})",
            msg.TargetUserId, msg.Message?.MessageId, msg.TraceId);

        return Results.Ok(new
        {
            status = "sent-unicast",
            targetUserId = msg.TargetUserId,
            messageId = msg.Message?.MessageId,
            traceId = msg.TraceId
        });
    }

    await dispatcher.BroadcastAsync(msg);
    log.LogInformation("Broadcast enviado (subscription={Subscription}, messageId={MessageId}, traceId={TraceId})",
        msg.Subscription, msg.Message?.MessageId, msg.TraceId);

    metrics.IncrementMessagesBroadcasted();
    return Results.Ok(new
    {
        status = "sent-broadcast",
        subscription = msg.Subscription,
        messageId = msg.Message?.MessageId,
        traceId = msg.TraceId,
        totalMessages = metrics.MessagesBroadcasted,
        activeConnections = metrics.ActiveConnections
    });
});




// Endpoint de métricas
app.MapGet("/metrics", () =>
{
    var snapshot = new
    {
        activeConnections = metrics.ActiveConnections,
        messagesBroadcasted = metrics.MessagesBroadcasted,
        publishRequests = metrics.PublishRequests,
        timestamp = DateTime.UtcNow
    };
    return Results.Ok(snapshot);
})
.WithName("Metrics");

// Histórico de entregas
app.MapGet("/deliveries", (DeliveryStore store) =>
{
    // Opcional: limitar quantidade devolvida
    return Results.Ok(store.GetRecent(1000));
})
.WithName("Deliveries");

app.MapGet("/acks", (ClientAckStore store) =>
{
    return Results.Ok(store.GetRecent(1000));
})
.WithName("Acks");

app.MapGet("/clients", () =>
{
    var list = connMgr.GetAll().Select(kv => new
    {
        connectionId = kv.Key,
        user = kv.Value.Principal?.Identity?.Name ?? kv.Value.Principal?.FindFirst("sub")?.Value,
        connectedAtUtc = kv.Value.ConnectedAt,
        claims = kv.Value.Principal?.Claims.Select(c => new { c.Type, c.Value })
    });
    return Results.Ok(list);
})
.WithName("Clients");


app.Run();


// ====== Options (config) ======
public class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}

public class RedisOptions
{
    public string? ConnectionString { get; set; }
}
