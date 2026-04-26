# 🧪 Testando Heimdall Localmente

Guia rápido para rodar e testar a aplicação localmente.

---

## 🚀 Quick Start (3 minutos)

### 1️⃣ Rodar Backend

```bash
cd src/Heimdall.Api
dotnet run
```

**Aguarde ver:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Program[0]
      Database migrated successfully (Development)
info: Program[0]
      Admin user created: admin@heimdall.com
```

### 2️⃣ Rodar Frontend (em outro terminal)

```bash
cd src/Heimdall.Web
dotnet run
```

**Aguarde ver:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5173
```

### 3️⃣ Acessar Aplicação

**Frontend:** http://localhost:5173

**Credenciais de Desenvolvimento:**
- **Email:** `admin@heimdall.com`
- **Password:** `Admin@123!Dev`
- **Audience:** `heimdall-api`

---

## 🧪 Testar API Diretamente

### Health Check
```powershell
curl http://localhost:5000/health
```

**Resposta esperada:**
```json
{
  "status": "healthy",
  "timestamp": "2026-04-26T...",
  "version": "1.0.0",
  "environment": "Development"
}
```

### Login
```powershell
$body = @{
    email = "admin@heimdall.com"
    password = "Admin@123!Dev"
    audience = "heimdall-api"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/login" `
    -Method POST `
    -ContentType "application/json" `
    -Body $body
```

**Resposta esperada:**
```json
{
  "accessToken": "eyJ...",
  "refreshToken": "...",
  "expiresIn": 300
}
```

### Refresh Token
```powershell
$body = @{
    refreshToken = "SEU_REFRESH_TOKEN_AQUI"
    audience = "heimdall-api"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/refresh" `
    -Method POST `
    -ContentType "application/json" `
    -Body $body
```

---

## ⚙️ Configurações Locais

### SQLite (Padrão)
- **Arquivo:** `src/Heimdall.Api/heimdall.db`
- **Auto-migration:** ✅ Ativado em Development
- **Dados:** Persistem localmente

### CORS
Portas permitidas em desenvolvimento:
- `http://localhost:5173` (Blazor WASM)
- `http://localhost:5000` (API)
- `https://localhost:5001` (API HTTPS)

### JWT
- **Chaves:** Incluídas no `appsettings.json` (PKCS#8)
- **Issuer:** `heimdall-dev`
- **Audience:** `heimdall-api`
- **Token Lifetime:** 5 minutos (access), 7 dias (refresh)

---

## 🔧 Resetar Banco de Dados Local

Se precisar começar do zero:

### Opção 1: Deletar SQLite
```powershell
# Parar aplicação (Ctrl+C)
Remove-Item src/Heimdall.Api/heimdall.db
Remove-Item src/Heimdall.Api/heimdall.db-shm
Remove-Item src/Heimdall.Api/heimdall.db-wal

# Rodar novamente
dotnet run --project src/Heimdall.Api
```

### Opção 2: Aplicar Migrations Manualmente
```bash
# Remover todas migrations
dotnet ef migrations remove --project src/Heimdall.Infrastructure --startup-project src/Heimdall.Api

# Criar nova migration
dotnet ef migrations add InitialCreate --project src/Heimdall.Infrastructure --startup-project src/Heimdall.Api --output-dir Data/Migrations

# Aplicar migration
dotnet ef database update --project src/Heimdall.Infrastructure --startup-project src/Heimdall.Api
```

---

## 🐛 Troubleshooting

### Porta 5000 já em uso
```bash
# Windows: Encontrar processo usando porta 5000
netstat -ano | findstr :5000

# Matar processo (substitua PID)
taskkill /PID <PID> /F

# Ou use porta diferente
dotnet run --project src/Heimdall.Api --urls "http://localhost:5001"
```

### Login não funciona
**Erro:** `Invalid credentials`

**Solução:**
1. Verifique se usou a senha correta: `Admin@123!Dev`
2. Verifique logs da API para erros
3. Delete `heimdall.db` e rode novamente

### CORS Error no navegador
**Erro:** `Access-Control-Allow-Origin`

**Solução:**
1. Verifique se frontend está rodando em `http://localhost:5173`
2. Confira `appsettings.Development.json`:
```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173"
    ]
  }
}
```

### JWT Invalid Signature
**Erro:** `Invalid token signature`

**Solução:**
As chaves JWT foram atualizadas para PKCS#8. Certifique-se de que o `appsettings.json` tem:
```json
{
  "Jwt": {
    "PrivateKeyPem": "-----BEGIN PRIVATE KEY-----...",
    "PublicKeyPem": "-----BEGIN PUBLIC KEY-----..."
  }
}
```

**NÃO** deve ser:
```json
{
  "Jwt": {
    "PrivateKeyPem": "-----BEGIN RSA PRIVATE KEY-----...",  ❌
    "PublicKeyPem": "-----BEGIN RSA PUBLIC KEY-----..."     ❌
  }
}
```

---

## 📊 Verificar Logs

### Logs da API (Verbose)
```bash
dotnet run --project src/Heimdall.Api -- --Logging:LogLevel:Default=Debug
```

### Logs do Entity Framework
```bash
dotnet run --project src/Heimdall.Api -- --Logging:LogLevel:Microsoft.EntityFrameworkCore=Information
```

---

## 🎯 Próximos Passos

Após testar localmente:

1. ✅ **Fazer merge do PR** `feature/postgresql-persistence`
2. ✅ **Configurar PostgreSQL no Render** (seguir `POSTGRESQL-SETUP.md`)
3. ✅ **Testar em produção**

---

## 📚 Mais Informações

- **Integração com outras apps:** `docs/INTEGRATION-GUIDE.md`
- **Deploy em produção:** `RENDER-QUICK-START.md`
- **PostgreSQL setup:** `POSTGRESQL-SETUP.md`
- **Troubleshooting geral:** `TROUBLESHOOTING.md`

---

**Tudo funcionando localmente? Pronto para deploy em produção! 🚀**
