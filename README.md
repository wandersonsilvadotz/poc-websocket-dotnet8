#  WebSocket Service - PoC (.NET 8)

Prova de Conceito (PoC) para substituir o serviço WebSocket existente em Python por uma implementação em **.NET 8**, mantendo o mesmo contrato JSON e endpoints compatíveis.

---

##  Objetivo

- Reproduzir o comportamento atual do serviço Python.  
- Validar estabilidade, performance e compatibilidade.  
- Demonstrar envio e recebimento de mensagens via WebSocket.  
- Expor endpoints REST e WebSocket para comunicação em tempo real.  

---

##  Requisitos

- .NET SDK 8.0+
- Linux, macOS ou Windows
- cURL (para testes HTTP)
- `websocat` ou `wscat` (para testes WebSocket)

---

##  Estrutura do Projeto

```
AppPlatformWebSocket/
├── Program.cs
├── Models/
├── Services/
├── AppPlatformWebSocket.csproj
└── appsettings.json
```

---

##  Execução local

```bash
dotnet restore
dotnet build
dotnet run
```

---

## 🌐 Endpoints disponíveis

| Método | Endpoint | Descrição |
|--------|-----------|------------|
| `POST` | `/v1/publish` | Publica mensagens para todos os clientes conectados |
| `WS`   | `/ws` | Conecta via WebSocket para receber mensagens em tempo real |
| `GET`  | `/swagger` | Interface Swagger para testes |

---

## 🧪 Teste rápido

### 1️⃣ Conectar via WebSocket:
```bash
websocat ws://localhost:5000/ws
```

### 2️⃣ Publicar mensagem:
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

🟢 **Resultado esperado:**
```
{"status":"sent","totalMessages":2,"activeConnections":1}
```

---

## 🧩 Estrutura JSON esperada para o endpoint `/v1/publish`

Toda requisição válida deve seguir **exatamente este formato**:

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

## 💡 Exemplos completos de mensagens

(12 exemplos completos incluídos conforme especificação do contrato — eventos de login, pedidos, chat, pagamentos, etc.)

---

## 💬 Dicas para testar no Swagger

- No campo **Request body**, substitua o JSON original por qualquer exemplo acima.  
- Clique em **"Try it out" → Execute"**.  
- Se um cliente WebSocket (`wscat`, `websocat`) estiver conectado, você verá o mesmo JSON chegando **em tempo real**.

---

## 🔍 Observações

- `"data"` é uma string Base64.  
- `"publish_time"` segue o formato ISO 8601 UTC.  
- `"subscription"` indica o canal/tópico de origem da mensagem.

---

## 🧑‍💻 Autor

**Wanderson Ferreira da Silva**  
Arquiteto  
Outubro/2025  
