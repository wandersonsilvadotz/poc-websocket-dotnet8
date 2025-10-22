# 🧩 WebSocket Service - PoC (.NET 8)

Prova de Conceito (PoC) para substituir o serviço WebSocket existente em Python por uma implementação em **.NET 8**, mantendo o mesmo contrato JSON e endpoints compatíveis.

---

## 🚀 Objetivo

- Reproduzir o comportamento atual do serviço Python.  
- Validar estabilidade, performance e compatibilidade.  
- Demonstrar envio e recebimento de mensagens via WebSocket.

---

## ⚙️ Requisitos

- .NET SDK 8.0+
- Linux, macOS ou Windows
- cURL (para testes HTTP)
- `websocat` ou `wscat` (para testes WebSocket)

---

## 🧱 Estrutura do Projeto

```
AppPlatformWebSocket/
├── Program.cs
├── Models/
├── Services/
├── AppPlatformWebSocket.csproj
└── appsettings.json
```

---

## ▶️ Execução local

```bash
# Restaurar dependências
dotnet restore

# Compilar o projeto
dotnet build

# Executar a aplicação
dotnet run
```

---

## 🌐 Endpoints disponíveis

| Método | Endpoint | Descrição |
|--------|-----------|------------|
| `GET`  | `/metrics` | Exibe métricas de conexões e mensagens |
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

🟢 **Resultado esperado no terminal do cliente:**
```
{"status":"sent","totalMessages":2,"activeConnections":1}
```

---

## 📊 Métricas

Acesse:
```
http://localhost:5000/metrics
```
para ver estatísticas em tempo real:
```json
{
  "activeConnections": 1,
  "messagesBroadcasted": 2,
  "publishRequests": 2,
  "timestamp": "2025-10-22T12:00:00Z"
}
```

---

## 🧩 Documentação complementar

O documento detalhado da PoC, com decisões técnicas e resultados de performance, está incluído neste repositório:
- 📄 `Documento_Arquitetural_WebSocket_DotNet8.docx`

---

## 🧑‍💻 Autor

**Wanderson Ferreira da Silva**  
Arquiteto de Software  
📅 Outubro/2025  
📧 datatronengenharia@gmail.com
