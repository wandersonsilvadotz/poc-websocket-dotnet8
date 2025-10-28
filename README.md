# WebSocket Service - PoC (.NET 8) - Versão 3

Prova de Conceito (PoC) para evoluir o serviço WebSocket desenvolvido em **.NET 8**, agora com **entrega UNICAST por usuário autenticado**, **autenticação JWT via Header Authorization**, e **rastreabilidade aprimorada** com `TraceId` e integração nativa ao **Elastic APM**. Mantém compatibilidade total com o contrato JSON legado em Python.

---

## Objetivos

- Implementar **entrega UNICAST** com base no campo `target_user_id`.
- Priorizar **autenticação JWT** via Header Authorization (mantendo `?token=` como fallback).
- Manter **compatibilidade com o contrato JSON original**.
- Garantir **isolamento completo de mensagens** por usuário e canal.
- Melhorar **rastreabilidade e observabilidade** via Elastic APM e logs estruturados.

---

## Requisitos

- .NET SDK 8.0+
- Linux, macOS ou Windows
- Redis (opcional, como backplane)
- `curl` e `jq` (para testes HTTP)
- `websocat` ou `wscat` (para testes WebSocket)

---

## Estrutura do Projeto

```bash
AppPlatformWebSocket_MVP/
├── Program.cs
├── Models/
│   ├── PublishMessage.cs
│   ├── ClientAck.cs
│   └── DeliveryRecord.cs
├── Services/
│   ├── WebSocketDispatcher.cs
│   ├── WebSocketConnectionManager.cs
│   ├── JwtValidator.cs
│   ├── RedisService.cs
│   ├── MetricsService.cs
│   ├── DeliveryStore.cs
│   ├── DiagnosticsHelper.cs
│   └── ClientAckStore.cs
├── appsettings.json
└── Documento_Arquitetural_WebSocket_v3.docx
```

---

## Execução Local

```bash
dotnet restore
dotnet build
dotnet run
```

O servidor iniciará por padrão em:

```
http://localhost:5087
```

---

## Endpoints Disponíveis

| Método | Endpoint | Descrição |
|---------|-----------|--------------|
| **POST** | `/v1/publish` | Publica mensagem (unicast se `target_user_id` presente, senão broadcast). |
| **WS** | `/ws` | Conecta cliente autenticado via Header Authorization (JWT) ou `?token=`. |
| **GET** | `/metrics` | Exibe métricas operacionais do serviço. |
| **GET** | `/deliveries` | Lista registros de entregas realizadas. |
| **GET** | `/acks` | Exibe confirmações (ACKs) recebidas dos clientes. |
| **GET** | `/clients` | Lista conexões WebSocket ativas e suas claims. |
| **GET** | `/swagger` | Interface Swagger para testes e documentação. |

---

## Autenticação JWT

A autenticação é feita **prioritariamente via Header Authorization**, conforme o padrão:

```bash
websocat -H "Authorization: Bearer <JWT>" ws://localhost:5087/ws
```

Caso o header não esteja presente, é utilizado o fallback via querystring:

```bash
websocat "ws://localhost:5087/ws?token=<JWT>"
```

### Exemplo de Payload JWT

```json
{
  "sub": "a123",
  "role": "client",
  "subscriptions": ["chat", "user-events"],
  "iss": "https://issuer.example.com",
  "aud": "app-client",
  "exp": 1761405576
}
```

A claim `sub` é usada para identificar e vincular conexões de um mesmo usuário.

---

## Contrato JSON do Endpoint `/v1/publish`

Contrato mantido **100% compatível com o legado em Python**:

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

---

## Exemplos de Mensagens

### Broadcast (canal `chat`)
```json
{
  "message": {
    "data": "Q2hhbm5lbCBjaGF0",
    "message_id": "msg-chat-001",
    "publish_time": "2025-10-27T12:00:00Z",
    "body": {
      "event": "chat_message",
      "sender": "user123",
      "text": "Mensagem broadcast no canal chat"
    }
  },
  "subscription": "chat"
}
```

### Unicast (somente `a123` recebe)
```json
{
  "message": {
    "data": "VXNlcg==",
    "message_id": "msg-uni-005",
    "publish_time": "2025-10-27T23:15:00Z",
    "body": {
      "event": "private_notice",
      "text": "Somente o usuário A deve receber esta mensagem."
    }
  },
  "subscription": "user-events",
  "target_user_id": "a123"
}
```

---

## Testes Rápidos

### Broadcast
```bash
curl -s -X POST http://localhost:5087/v1/publish \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TOKEN_A>" \
  -d '{"message":{"data":"Q2hhbm5lbCBjaGF0","message_id":"msg-chat-001","publish_time":"2025-10-27T12:00:00Z","body":{"event":"chat_message","sender":"user123","text":"Mensagem broadcast"}}, "subscription":"chat"}'
```

### Unicast
```bash
curl -s -X POST http://localhost:5087/v1/publish \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TOKEN_A>" \
  -d '{"message":{"data":"VXNlcg==","message_id":"msg-uni-006","publish_time":"2025-10-27T16:30:00Z","body":{"event":"personal_notification","text":"Teste unicast final!"}}, "subscription":"user-events","target_user_id":"a123"}'
```

**Resultado esperado:**
- HTTP 200 OK
- `status: "sent-unicast"`
- Mensagem recebida apenas pelo WebSocket do usuário `a123`.

---

## Exemplo de Métricas

```bash
curl http://localhost:5087/metrics | jq .
```

```json
{
  "activeConnections": 2,
  "messagesBroadcasted": 5,
  "publishRequests": 7,
  "timestamp": "2025-10-27T23:00:00Z"
}
```

---

## Logs e Observabilidade

- A claim `sub` é usada diretamente para identificar o usuário.
- Logs incluem `traceId`, `target_user_id` e `message_id`.
- `/clients` exibe conexões ativas e claims completas.
- Integração nativa com **Elastic APM** e logs estruturados JSON.

---

## Riscos e Mitigações

| Risco | Impacto | Mitigação |
|--------|----------|-------------|
| JWT inválido ou expirado | Conexão rejeitada (401) | Validação com tolerância (ClockSkew 30s) |
| Claim `sub` ausente | Usuário não identificado | Token rejeitado no handshake |
| Redis indisponível | Sem replicção entre instâncias | Fallback local automático |
| JSON inválido | Erro no /publish | Validação antes do envio |

---

## Resumo Técnico

- **UNICAST funcional** — entrega exclusiva ao usuário via `target_user_id`  
- **JWT via Header Authorization** — autenticação segura e padronizada  
- **Contrato JSON mantido** — compatível com legado em Python  
- **Logs e métricas expostos** — rastreabilidade ponta a ponta  
- **Base pronta para MVP** — escalabilidade e integração futura com provedores JWT  

---

## Autor

**Wanderson Ferreira da Silva**  
Arquiteto de Soluções  
Outubro/2025
