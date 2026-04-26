# 🔧 Troubleshooting Frontend Login Issues

Guia rápido para resolver problemas de login no frontend Blazor WASM.

## 🎯 Problema: "Invalid credentials" sem chamadas HTTP no F12

### Sintomas:
- ✅ Botão "Sign In" funciona
- ❌ Nenhuma requisição aparece no Network (F12)
- ❌ Mensagem de erro: "Invalid credentials. Please check your email, password, and project."

### Causa Principal:
**URL da API incorreta** no `appsettings.json` do frontend

---

## ✅ Solução Passo a Passo

### 1️⃣ Verificar se a API está rodando

```powershell
# Testar health check da API
curl http://localhost:5231/health

# Ou via PowerShell
Invoke-RestMethod -Uri "http://localhost:5231/health"
```

**Resposta esperada:**
```json
{
  "status": "healthy",
  "timestamp": "2024-01-XX...",
  "version": "1.0.0",
  "environment": "Development"
}
```

Se a API NÃO responder:
```powershell
cd src/Heimdall.Api
dotnet run
```

---

### 2️⃣ Verificar URL no Frontend

**Arquivo:** `src/Heimdall.Web/wwwroot/appsettings.json`

✅ **CORRETO (desenvolvimento local):**
```json
{
  "ApiUrl": "http://localhost:5231"
}
```

❌ **INCORRETO:**
```json
{
  "ApiUrl": "https://localhost:5001"  // Porta errada!
}
```

---

### 3️⃣ Verificar Console do Navegador

Após as correções, você deve ver logs no Console (F12):

```
[Home] Starting login for admin@heimdall.com
[AuthService] Sending login request to: http://localhost:5231/api/login
[AuthService] Email: admin@heimdall.com, Audience: heimdall-api
[AuthService] Response status: 200
[AuthService] Login successful!
[Home] Login successful, storing tokens
[Home] Redirecting to dashboard
```

---

### 4️⃣ Verificar Network (F12)

Agora você DEVE ver:

**Request:**
```
POST http://localhost:5231/api/login
Status: 200 OK
```

**Request Payload:**
```json
{
  "email": "admin@heimdall.com",
  "password": "Admin@123!Dev",
  "audience": "heimdall-api"
}
```

**Response:**
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIs...",
  "refreshToken": "xyz123...",
  "expiresIn": 300
}
```

---

## 🔍 Diagnóstico Avançado

### Erro de CORS

**Sintoma no Console:**
```
Access to fetch at 'http://localhost:5231/api/login' from origin 'http://localhost:5173' 
has been blocked by CORS policy
```

**Solução:**

1. Verificar `appsettings.Development.json` da API:
```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173",
      "http://localhost:5000",
      "https://localhost:5001"
    ]
  }
}
```

2. Ou confirmar que `Program.cs` permite any origin em dev:
```csharp
if (builder.Environment.IsDevelopment())
{
    p.AllowAnyOrigin()
     .AllowAnyMethod()
     .AllowAnyHeader();
}
```

---

### Credenciais Incorretas

**Sintoma:**
- Requisição HTTP aparece no Network
- Status: 401 Unauthorized ou 400 Bad Request

**Solução:**

Verifique as credenciais em `appsettings.Development.json` da **API**:

```json
{
  "Seed": {
    "AdminEmail": "admin@heimdall.com",
    "AdminPassword": "Admin@123!Dev"
  }
}
```

E use as **mesmas** credenciais no frontend.

---

### HttpClient não configurado

**Sintoma no Console:**
```
[AuthService] Exception: System.InvalidOperationException: 
An invalid request URI was provided. The request URI must either be an absolute URI or BaseAddress must be set.
```

**Solução:**

Verificar `Program.cs` do frontend:

```csharp
var apiUrl = builder.Configuration["ApiUrl"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiUrl) });
```

---

## 📋 Checklist Completo

- [ ] API rodando em `http://localhost:5231`
- [ ] Health check responde: `curl http://localhost:5231/health`
- [ ] `appsettings.json` do frontend aponta para `http://localhost:5231`
- [ ] Frontend rodando em `http://localhost:5173`
- [ ] Console do navegador mostra logs `[AuthService]`
- [ ] Network (F12) mostra requisição POST para `/api/login`
- [ ] Credenciais corretas: `admin@heimdall.com` / `Admin@123!Dev`
- [ ] Audience correto: `heimdall-api`

---

## 🚀 Teste Rápido

### Opção A: Usar script automatizado

```powershell
.\test-local-login.ps1
```

### Opção B: Teste manual

**Terminal 1 - Backend:**
```powershell
cd src/Heimdall.Api
dotnet run
```

**Terminal 2 - Frontend:**
```powershell
cd src/Heimdall.Web
dotnet run
```

**Navegador:**
1. Abrir: http://localhost:5173
2. Abrir F12 → Console e Network
3. Preencher:
   - Email: `admin@heimdall.com`
   - Password: `Admin@123!Dev`
   - Project: `heimdall-api`
4. Clicar "Sign In"
5. Verificar logs e network

---

## 🆘 Ainda não funciona?

### Limpar cache do navegador

```
Ctrl + Shift + Delete
→ Limpar cache e cookies
→ Recarregar página (Ctrl + F5)
```

### Rebuild completo

```powershell
# Limpar build anterior
dotnet clean

# Rebuild
dotnet build

# Rodar novamente
cd src/Heimdall.Api
dotnet run

# Em outro terminal
cd src/Heimdall.Web
dotnet run
```

### Verificar portas em uso

```powershell
# Ver processos na porta 5231 (API)
Get-NetTCPConnection -LocalPort 5231 | Select-Object -ExpandProperty OwningProcess

# Ver processos na porta 5173 (Frontend)
Get-NetTCPConnection -LocalPort 5173 | Select-Object -ExpandProperty OwningProcess

# Matar processo se necessário
Stop-Process -Id <PROCESS_ID> -Force
```

---

## 📚 Documentação Relacionada

- **LOCAL-TESTING.md** - Guia completo de testes locais
- **TROUBLESHOOTING.md** - Troubleshooting geral do projeto
- **POSTGRESQL-SETUP.md** - Setup de banco de dados

---

**Pronto! Seu login deve estar funcionando agora! 🎉**
