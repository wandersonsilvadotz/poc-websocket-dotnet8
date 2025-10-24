
# WebSocket Service - PoC (.NET 8) - Versão 2

Prova de Conceito (PoC) para evoluir o serviço WebSocket desenvolvido em **.NET 8**, incorporando **autenticação JWT**, **backplane Redis**, **métricas operacionais**, **rastreamento de mensagens (TraceId)** e **filtragem por canal (subscriptions)**.  
Esta versão mantém compatibilidade total com o contrato JSON do legado em Python.

---

## Objetivo

- Adicionar autenticação JWT nas conexões WebSocket.  
- Implementar Redis como backplane para comunicação entre múltiplas instâncias.  
- Expor métricas operacionais e registros de entrega via endpoints dedicados.  
- Permitir rastreabilidade ponta a ponta com `TraceId` e integração com Elastic APM.  
- Validar isolamento de mensagens por canal (`subscriptions`) usando tokens diferentes.  

---

## Requisitos

- .NET SDK 8.0+  
- Linux, macOS ou Windows  
- Redis (opcional — modo local ou via Docker)  
- cURL e `jq` (para testes HTTP)  
- `websocat` ou `wscat` (para testes WebSocket)

---

## Estrutura do Projeto

```
AppPlatformWebSocket_v2/
├── Program.cs
├── Models/
│   ├── ClientAck.cs
│   └── PublishMessage.cs
├── Services/
│   ├── WebSocketDispatcher.cs
│   ├── WebSocketConnectionManager.cs
│   ├── RedisService.cs
│   ├── DeliveryRecord.cs
│   ├── ClientAckStore.cs
│   ├── JwtValidator.cs
│   ├── MetricsService.cs
│   └── DeliveryStore.cs
├── appsettings.json
├── gitignore
└── README.md
```

---

## Execução local

```bash
dotnet restore
dotnet build
dotnet run
```

O servidor iniciará (por padrão) em `http://localhost:5087`.

---

## Endpoints disponíveis

| Método | Endpoint        | Descrição                                                                 |
|-------:|-----------------|---------------------------------------------------------------------------|
|   POST | `/v1/publish`   | Publica mensagem no canal especificado                                   |
|     WS | `/ws?token=JWT` | Conecta cliente autenticado por JWT; envia/recebe mensagens em tempo real |
|    GET | `/metrics`      | Exibe métricas operacionais do serviço                                   |
|    GET | `/deliveries`   | Lista registros de entregas realizadas                                   |
|    GET | `/acks`         | Exibe confirmações (ACK) recebidas dos clientes                          |
|    GET | `/clients`      | Lista conexões WebSocket ativas e seus claims                            |
|    GET | `/swagger`      | Interface Swagger para testes e documentação                             |

---

## Exemplo de autenticação JWT

O cliente deve se conectar com o token JWT na querystring:

```bash
websocat "ws://localhost:5087/ws?token=<JWT>"
```

**Token exemplo (usuário com acesso aos canais `chat` e `user-events`):**

```json
{
  "sub": "user-123",
  "role": "client",
  "subscriptions": ["chat", "user-events"],
  "iss": "https://issuer.example.com",
  "aud": "app-client",
  "exp": 1761405576
}
```

---

## Estrutura JSON obrigatória para o endpoint `/v1/publish`

Toda requisição válida deve seguir este formato (alterando apenas os valores):

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
- `publish_time` usa formato ISO 8601 UTC.  
- `subscription` indica o canal (ex.: `chat`, `user-events`, `monitoring`).  

---

## Exemplos completos de mensagens

### 1) Mensagem no canal “chat”
```json
{
  "message": {
    "data": "Q2hhbm5lbCBjaGF0",
    "message_id": "msg-chat-001",
    "publish_time": "2025-10-26T12:00:00Z",
    "body": {
      "event": "chat_message",
      "sender": "user123",
      "text": "Mensagem exclusiva do canal chat"
    }
  },
  "subscription": "chat"
}
```

### 2) Mensagem no canal “user-events”
```json
{
  "message": {
    "data": "dXNlciBsb2dpbg==",
    "message_id": "msg-user-001",
    "publish_time": "2025-10-26T12:02:00Z",
    "body": {
      "event": "user_login",
      "user_id": "u456",
      "platform": "mobile"
    }
  },
  "subscription": "user-events"
}
```

### 3) Mensagem com ACK (canal chat)
```json
{
  "message": {
    "data": "YWNrbWVzc2FnZQ==",
    "message_id": "msg-ack-01",
    "publish_time": "2025-10-26T12:15:00Z",
    "body": {
      "event": "requires_ack",
      "description": "Mensagem que exige confirmação"
    }
  },
  "subscription": "chat"
}
```

---

## Exemplo de resposta do endpoint `/metrics`

```bash
curl http://localhost:5087/metrics | jq .
```

Resposta esperada:
```json
{
  "activeConnections": 2,
  "messagesBroadcasted": 7,
  "publishRequests": 7,
  "timestamp": "2025-10-26T14:20:00Z"
}
```

---

## Observabilidade e rastreabilidade (TraceId)

- Cada publicação gera ou propaga um campo `TraceId`.  
- Logs e spans podem ser coletados via Elastic APM.  
- O endpoint `/deliveries` registra cada mensagem entregue:  
  ```json
  {
    "connectionId": "a1b2c3d4e5",
    "messageId": "msg-chat-001",
    "deliveredAtUtc": "2025-10-26T14:20:11Z"
  }
  ```

---

## Riscos e Mitigações

| Risco | Impacto | Mitigação |
|-------|----------|-----------|
| Redis fora do ar | Mensagens não replicadas entre instâncias | Fallback local e retry automático |
| JWT inválido | Falha na autenticação | Gerar tokens válidos com mesmo segredo do servidor |
| Alta carga simultânea | Possível degradação de desempenho | Ajustar thread pool e buffers WebSocket |
| Logs não centralizados | Dificuldade de diagnóstico | Integrar Elastic APM e centralizar observabilidade |

---

## Observações Finais

- O contrato JSON foi mantido conforme o legado em Python.  
- Redis é essencial para escalabilidade horizontal.  
- JWT garante isolamento de mensagens por canal (`subscriptions`).  
- `/metrics`, `/deliveries`, `/acks` e `/clients` oferecem visibilidade operacional.  
- A PoC está pronta para ambiente **staging** e migração futura para produção.

---

## Autor

**Wanderson Ferreira da Silva**  
Arquiteto  
Outubro/2025
