using System.Text.Json;
using AppPlatformWebSocket.Models;
using StackExchange.Redis; // requer StackExchange.Redis

namespace AppPlatformWebSocket.Services;

public class RedisService : IDisposable
{
    private readonly ILogger<RedisService> _logger;
    private readonly WebSocketDispatcher _dispatcher;
    private  ConnectionMultiplexer? _redis;
    private  ISubscriber? _sub;
    private readonly string _channelName = "websocket-messages";

    public bool IsEnabled => _redis != null;

    public RedisService(string? connectionString, WebSocketDispatcher dispatcher, ILogger<RedisService> logger)
{
    _logger = logger;
    _dispatcher = dispatcher;

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        _logger.LogWarning("Redis desabilitado: ConnectionString não configurada.");
        return;
    }

    // Inicia uma tarefa paralela para tentar conectar até conseguir
    _ = Task.Run(async () =>
    {
        while (true)
        {
            try
            {
                _logger.LogInformation("Tentando conectar ao Redis...");

                // Conecta ao Redis
                var redis = await ConnectionMultiplexer.ConnectAsync(connectionString);

                // Registra eventos de falha e restauração de conexão
                redis.ConnectionFailed += (_, e) =>
                    _logger.LogWarning("Conexão Redis falhou: {FailureType}", e.FailureType);

                redis.ConnectionRestored += (_, e) =>
                    _logger.LogInformation("Conexão Redis restaurada.");

                // Obtém o assinante (subscriber)
                var sub = redis.GetSubscriber();

                // Assina o canal principal
                await sub.SubscribeAsync(_channelName, async (channel, value) =>
                {
                    try
                    {
                        var msg = JsonSerializer.Deserialize<PublishMessage>(value!);
                        if (msg != null)
                        {
                            await _dispatcher.BroadcastAsync(msg);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Falha ao processar mensagem recebida do Redis.");
                    }
                });

                // Atribui aos campos da classe (para permitir publish)
                _redis = redis;
                _sub = sub;

                _logger.LogInformation("Redis conectado e subscrito ao canal {Channel}.", _channelName);
                break; // sucesso — sai do loop
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao conectar no Redis. Tentando novamente em 5 segundos...");
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    });
}


    public async Task PublishAsync(PublishMessage message)
    {
        if (!IsEnabled || _sub == null)
        {
            _logger.LogWarning("Publish chamado, mas Redis está desabilitado.");
            return;
        }

        var json = JsonSerializer.Serialize(message);
        await _sub.PublishAsync(_channelName, json);
    }

    public void Dispose()
    {
        _redis?.Dispose();
    }
}
