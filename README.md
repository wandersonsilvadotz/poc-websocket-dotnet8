# WebSocket Service - PoC (.NET 8)

Prova de Conceito (PoC) para substituir o serviço WebSocket existente em Python por uma implementação em **.NET 8**, mantendo o mesmo contrato JSON e endpoints compatíveis.

---

## Objetivo

- Reproduzir o comportamento atual do serviço Python.  
- Validar estabilidade, performance e compatibilidade.  
- Demonstrar envio e recebimento de mensagens via WebSocket.  
- Expor endpoints REST e WebSocket para comunicação em tempo real.  
- Fornecer métricas de execução e estado do servidor WebSocket.

---

## Requisitos

- .NET SDK 8.0+
- Linux, macOS ou Windows
- cURL (para testes HTTP)
- `websocat` ou `wscat` (para testes WebSocket)

---

## Estrutura do Projeto

```
AppPlatformWebSocket/
├── Program.cs
├── Models/
├── Services/
├── AppPlatformWebSocket.csproj
└── appsettings.json
```

---

## Execução local

```bash
dotnet restore
dotnet build
dotnet run
```

O servidor será iniciado localmente (por padrão) em `http://localhost:5000`.

---

## Endpoints disponíveis

| Método | Endpoint      | Descrição                                                 |
|-------:|---------------|-----------------------------------------------------------|
|   POST | `/v1/publish` | Publica mensagens para todos os clientes conectados       |
|     WS | `/ws`         | Conecta via WebSocket para receber mensagens em tempo real|
|    GET | `/metrics`    | Exibe estatísticas atuais do servidor e das conexões      |
|    GET | `/swagger`    | Interface Swagger para testes e documentação              |

---

## Detalhes do endpoint `/metrics`

Endpoint de diagnóstico para monitorar o status do serviço WebSocket.  
Fornece informações básicas sobre conexões e mensagens processadas.

Exemplo de requisição:
```bash
curl http://localhost:5000/metrics
```

Exemplo de resposta:
```json
{
  "activeConnections": 1,
  "messagesBroadcasted": 2,
  "publishRequests": 2,
  "timestamp": "2025-10-22T12:00:00Z"
}
```

Campos:
- `activeConnections`: número de conexões WebSocket ativas no momento.  
- `messagesBroadcasted`: total de mensagens enviadas via broadcast desde o start.  
- `publishRequests`: total de requisições recebidas em `/v1/publish` desde o start.  
- `timestamp`: horário da coleta dos dados no formato ISO 8601 UTC.

---

## Teste rápido

Conectar via WebSocket:
```bash
websocat ws://localhost:5000/ws
```

Publicar mensagem:
```bash
curl -X POST http://localhost:5000/v1/publish   -H "Content-Type: application/json"   -d '{
  "message": {
    "data": "SGVsbG8sIFdlYlNvY2tldCE=",
    "message_id": "msg-001",
    "publish_time": "2025-10-21T12:00:00Z",
    "body": {"event": "user_connected"}
  },
  "subscription": "app-notifications"
}'
```

Resposta esperada do endpoint HTTP:
```json
{"status":"sent","totalMessages":2,"activeConnections":1}
```

---

## Estrutura JSON obrigatória para o endpoint `/v1/publish`

Toda requisição válida deve seguir exatamente este formato, alterando apenas os valores:

```json
{
  "message": {
    "data": "IntxxxfSI=",
    "message_id": "string",
    "publish_time": "string",
    "body": {}
  },
  "subscription": "string"
}
```

Observações:
- `data` é uma string Base64.  
- `publish_time` segue o padrão ISO 8601 UTC (ex.: `"2025-10-21T13:00:00Z"`).  
- `subscription` indica o canal/tópico (ex.: `user-events`, `commerce`, `monitoring`).

---

## Exemplos completos de mensagens (3 exemplos)

Use qualquer um dos exemplos abaixo diretamente no Swagger, Postman ou via `curl` para o endpoint `/v1/publish`.

### 1) Evento de login de usuário
```json
{
  "message": {
    "data": "VXNlcjogSm9hbw==",
    "message_id": "msg-1001",
    "publish_time": "2025-10-21T13:00:00Z",
    "body": {
      "event": "user_login",
      "user_id": "u784",
      "device": "android",
      "location": "São Paulo, BR"
    }
  },
  "subscription": "user-events"
}
```

### 2) Criação de pedido
```json
{
  "message": {
    "data": "T3JkZXIgY3JlYXRlZA==",
    "message_id": "msg-1002",
    "publish_time": "2025-10-21T13:10:00Z",
    "body": {
      "event": "order_created",
      "order_id": "o937",
      "user_id": "u111",
      "items": [
        { "id": "p10", "name": "Mouse Gamer", "quantity": 1 },
        { "id": "p12", "name": "Teclado Mecânico", "quantity": 1 }
      ],
      "total": 399.90
    }
  },
  "subscription": "commerce"
}
```

### 3) Alerta de sistema
```json
{
  "message": {
    "data": "U3lzdGVtIGFsZXJ0",
    "message_id": "msg-1006",
    "publish_time": "2025-10-21T13:40:00Z",
    "body": {
      "event": "system_alert",
      "level": "warning",
      "code": "MEMORY_HIGH",
      "description": "Uso de memória acima de 80% detectado",
      "server": "ws-node-02"
    }
  },
  "subscription": "monitoring"
}
```

---

## Dicas para testar no Swagger

- No campo Request body do endpoint `/v1/publish`, substitua o JSON pelo de qualquer exemplo acima.  
- Clique em “Try it out” e depois em “Execute”.  
- Se um cliente WebSocket estiver conectado ao `/ws`, verá o mesmo JSON chegar em tempo real.

---

## Observações

- O contrato foi mantido fiel ao legado: `/v1/publish` para entrada HTTP e `/ws` para entrega WebSocket.  
- O endpoint `/metrics` expõe métricas básicas para observabilidade local e pode ser evoluído (Prometheus/OpenTelemetry).  
- A PoC não implementa autenticação nem backplane; essas funcionalidades fazem parte do plano evolutivo.

---

## Autor

Wanderson Ferreira da Silva  
Arquiteto  
Outubro/2025
