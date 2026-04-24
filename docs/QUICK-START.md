# Heimdall - Guia Rápido de Integração

## 🚀 Quick Start (5 minutos)

Este guia mostra como integrar sua aplicação com o Heimdall em 5 passos simples.

---

## Passo 1: Obter Credenciais

Entre em contato com o administrador do Heimdall e forneça:
- Nome da sua aplicação
- Audience desejado (ex: `minha-app-api`)

Você receberá:
```json
{
  "apiUrl": "https://auth.exemplo.com",
  "audience": "minha-app-api",
  "publicKey": "-----BEGIN RSA PUBLIC KEY-----\n..."
}
```

---

## Passo 2: Fazer Login

**Request:**
```bash
curl -X POST https://auth.exemplo.com/api/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "usuario@exemplo.com",
    "password": "SenhaSegura123!",
    "audience": "minha-app-api"
  }'
```

**Response:**
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIs...",
  "refreshToken": "xK7j9mP3qR2vN8bL5cT4w...",
  "expiresIn": 300
}
```

---

## Passo 3: Usar o Token

Adicione o `accessToken` no header de suas requisições:

```bash
curl -X GET https://api.exemplo.com/api/recursos \
  -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIs..."
```

---

## Passo 4: Renovar Token (quando expirar)

**Request:**
```bash
curl -X POST https://auth.exemplo.com/api/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "xK7j9mP3qR2vN8bL5cT4w..."
  }'
```

**Response:**
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIs...",  // Novo
  "refreshToken": "yM8k0nQ4rS3wO9cM6dU5x...",  // Novo
  "expiresIn": 300
}
```

⚠️ **IMPORTANTE:** Use os **novos** tokens retornados!

---

## Passo 5: Logout

**Request:**
```bash
curl -X POST https://auth.exemplo.com/api/revoke \
  -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIs..." \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "xK7j9mP3qR2vN8bL5cT4w..."
  }'
```

---

## Código Pronto (JavaScript)

```javascript
const HEIMDALL_API = 'https://auth.exemplo.com';
const AUDIENCE = 'minha-app-api';

async function login(email, password) {
  const response = await fetch(`${HEIMDALL_API}/api/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password, audience: AUDIENCE })
  });
  
  if (!response.ok) throw new Error('Login falhou');
  
  const { accessToken, refreshToken } = await response.json();
  localStorage.setItem('accessToken', accessToken);
  localStorage.setItem('refreshToken', refreshToken);
  
  return { accessToken, refreshToken };
}

async function callAPI(endpoint) {
  const token = localStorage.getItem('accessToken');
  
  const response = await fetch(endpoint, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  
  if (response.status === 401) {
    // Token expirado, renovar
    await refreshToken();
    return callAPI(endpoint); // Tentar novamente
  }
  
  return response.json();
}

async function refreshToken() {
  const refreshToken = localStorage.getItem('refreshToken');
  
  const response = await fetch(`${HEIMDALL_API}/api/refresh`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken })
  });
  
  if (!response.ok) {
    // Refresh falhou, redirecionar para login
    window.location.href = '/login';
    return;
  }
  
  const { accessToken, refreshToken: newRefreshToken } = await response.json();
  localStorage.setItem('accessToken', accessToken);
  localStorage.setItem('refreshToken', newRefreshToken);
}

async function logout() {
  const accessToken = localStorage.getItem('accessToken');
  const refreshToken = localStorage.getItem('refreshToken');
  
  await fetch(`${HEIMDALL_API}/api/revoke`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${accessToken}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ refreshToken })
  });
  
  localStorage.removeItem('accessToken');
  localStorage.removeItem('refreshToken');
}

// Uso
await login('usuario@exemplo.com', 'SenhaSegura123!');
const dados = await callAPI('https://api.exemplo.com/api/recursos');
await logout();
```

---

## Validação de Token (C#)

```csharp
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var rsa = RSA.Create();
rsa.ImportFromPem(builder.Configuration["Heimdall:PublicKeyPem"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "heimdall",
            ValidateAudience = true,
            ValidAudience = "minha-app-api",  // SEU AUDIENCE
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

// Endpoint protegido
app.MapGet("/api/recursos", [Authorize] () => "Dados protegidos");

app.Run();
```

---

## Checklist

- [ ] Obter `apiUrl` e `audience` do administrador
- [ ] Implementar função de login
- [ ] Armazenar tokens (accessToken + refreshToken)
- [ ] Adicionar header `Authorization: Bearer {token}` nas requisições
- [ ] Implementar auto-refresh quando token expirar
- [ ] Implementar logout com revogação de refresh token
- [ ] **(Opcional)** Validar JWT localmente com chave pública

---

## Problemas Comuns

### "401 Unauthorized" ao fazer login
- ✅ Verifique email/senha
- ✅ Confirme que o audience está correto
- ✅ Certifique-se que o usuário está ativo

### "Invalid audience" ao validar token
- ✅ Verifique se o `ValidAudience` na configuração corresponde ao seu projeto
- ✅ Confirme que você está usando o audience correto no login

### "429 Too Many Requests"
- ✅ Aguarde 1 minuto antes de tentar novamente
- ✅ Implemente backoff exponencial

### Token expira muito rápido
- ✅ Access tokens expiram em 5 minutos (por design)
- ✅ Use refresh token para renovar automaticamente
- ✅ Implemente auto-refresh 30 segundos antes de expirar

---

## Próximos Passos

📚 **Documentação Completa:** [INTEGRATION-GUIDE.md](./INTEGRATION-GUIDE.md)

Inclui:
- Exemplos em Python, PHP, Java
- Best practices de segurança
- Tratamento avançado de erros
- Multi-tab synchronization
- E muito mais!

---

**Precisa de ajuda?** suporte@heimdall.com
