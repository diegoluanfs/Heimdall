# Heimdall - Guia de Integração para Aplicações Cliente

## 📘 Documentação Técnica v1.0

---

## Sumário

1. [Visão Geral](#visão-geral)
2. [Pré-requisitos](#pré-requisitos)
3. [Registro de Aplicação](#registro-de-aplicação)
4. [Fluxo de Autenticação](#fluxo-de-autenticação)
5. [Endpoints da API](#endpoints-da-api)
6. [Validação de Tokens JWT](#validação-de-tokens-jwt)
7. [Exemplos de Integração](#exemplos-de-integração)
8. [Tratamento de Erros](#tratamento-de-erros)
9. [Segurança e Best Practices](#segurança-e-best-practices)
10. [FAQ](#faq)

---

## Visão Geral

**Heimdall** é um sistema de autenticação e autorização centralizada que fornece:

- ✅ **Autenticação JWT RS256** com chaves assimétricas
- ✅ **Refresh Tokens** com rotação automática
- ✅ **Multi-tenancy** baseado em audiences
- ✅ **Controle de acesso por roles** (admin, user, etc.)
- ✅ **Rate limiting** e proteção contra ataques
- ✅ **Auditoria completa** de acessos

### Arquitetura

```
┌─────────────────┐         ┌──────────────┐         ┌─────────────────┐
│  Sua Aplicação  │ ──────▶ │   Heimdall   │ ◀────── │  Outras Apps    │
│  (Frontend/API) │  Login  │   Auth API   │  Login  │  (Mobile/SPA)   │
└─────────────────┘         └──────────────┘         └─────────────────┘
                                    │
                            ┌───────┴────────┐
                            │   PostgreSQL   │
                            │   SQLite/MSSQL │
                            └────────────────┘
```

### Fluxo Simplificado

```mermaid
sequenceDiagram
    participant App as Sua Aplicação
    participant Heimdall as Heimdall API
    participant User as Usuário

    User->>App: Acessar aplicação
    App->>Heimdall: POST /api/login (email, senha, audience)
    Heimdall-->>App: 200 OK (accessToken, refreshToken)
    App->>App: Armazenar tokens
    App->>Heimdall: GET /api/resource (Authorization: Bearer {accessToken})
    Heimdall-->>App: 200 OK (dados protegidos)
    Note over App: Access token expira (5 min)
    App->>Heimdall: POST /api/refresh (refreshToken)
    Heimdall-->>App: 200 OK (novo accessToken, novo refreshToken)
```

---

## Pré-requisitos

### Para Usar o Heimdall

- **URL da API Heimdall**: `https://auth.exemplo.com` (fornecida pelo administrador)
- **Audience**: Identificador único da sua aplicação (ex: `minha-app-api`)
- **Usuários cadastrados**: Seus usuários devem estar registrados no Heimdall
- **Chave Pública RSA**: Para validar JWTs localmente (opcional mas recomendado)

### Requisitos Técnicos

- Suporte a **HTTPS** (obrigatório)
- Capacidade de fazer requisições HTTP
- Biblioteca JWT para validação de tokens
- Armazenamento seguro de tokens (localStorage, sessionStorage, cookies)

---

## Registro de Aplicação

### 1. Solicitar Audience ao Administrador

Entre em contato com o administrador do Heimdall e forneça:

- **Nome da aplicação**: Ex: "Minha Aplicação de Vendas"
- **Audience desejado**: Ex: `vendas-api` (apenas lowercase, números e hífens)
- **Descrição**: Breve descrição do propósito da aplicação

### 2. Receber Credenciais

O administrador criará um projeto no Heimdall e fornecerá:

```json
{
  "projectId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "name": "Minha Aplicação de Vendas",
  "audience": "vendas-api",
  "apiUrl": "https://auth.exemplo.com",
  "publicKey": "-----BEGIN RSA PUBLIC KEY-----\n..."
}
```

### 3. Criar Usuários

Seus usuários precisam ser cadastrados no Heimdall. Opções:

**Opção A: Solicitar ao administrador**
- Envie lista de emails e senhas temporárias
- Administrador cria via endpoint `/api/users`

**Opção B: Self-service (se habilitado)**
- Usuários se registram diretamente
- Sujeito a aprovação do administrador

---

## Fluxo de Autenticação

### 1. Login Inicial

**Endpoint:** `POST /api/login`

**Request:**
```http
POST https://auth.exemplo.com/api/login
Content-Type: application/json

{
  "email": "usuario@exemplo.com",
  "password": "SenhaSegura123!",
  "audience": "vendas-api"
}
```

**Response (200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIs...",
  "refreshToken": "xK7j9mP3qR2vN8bL5cT4w...",
  "expiresIn": 300
}
```

**Response (401 Unauthorized):**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401
}
```

**Response (400 Bad Request - validação):**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": ["Invalid email format."],
    "Password": ["Password must be at least 8 characters."]
  }
}
```

**Response (429 Too Many Requests):**
```json
{
  "type": "https://tools.ietf.org/html/rfc6585#section-4",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Rate limit exceeded. Try again in 60 seconds."
}
```

### 2. Usar Access Token

O `accessToken` deve ser incluído no header `Authorization` de todas as requisições:

```http
GET https://api.exemplo.com/api/vendas
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

### 3. Refresh Token (quando access token expira)

**Endpoint:** `POST /api/refresh`

**Request:**
```http
POST https://auth.exemplo.com/api/refresh
Content-Type: application/json

{
  "refreshToken": "xK7j9mP3qR2vN8bL5cT4w..."
}
```

**Response (200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIs...",  // Novo access token
  "refreshToken": "yM8k0nQ4rS3wO9cM6dU5x...",  // Novo refresh token
  "expiresIn": 300
}
```

**⚠️ IMPORTANTE:** 
- O refresh token anterior é **invalidado** (rotação de tokens)
- Use o **novo** refresh token na próxima renovação
- Atualize ambos os tokens no armazenamento

### 4. Logout (Revogar Refresh Token)

**Endpoint:** `POST /api/revoke`

**Request:**
```http
POST https://auth.exemplo.com/api/revoke
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
Content-Type: application/json

{
  "refreshToken": "xK7j9mP3qR2vN8bL5cT4w..."
}
```

**Response (200 OK):**
```http
200 OK
```

**Response (404 Not Found):**
```http
404 Not Found
```

---

## Endpoints da API

### Autenticação

| Método | Endpoint | Autenticação | Rate Limit | Descrição |
|--------|----------|--------------|------------|-----------|
| POST | `/api/login` | ❌ Não | 10/min | Autenticar usuário e obter tokens |
| POST | `/api/refresh` | ❌ Não | 20/min | Renovar access token usando refresh token |
| POST | `/api/revoke` | ✅ Sim | - | Revogar refresh token (logout) |

### Administração (Requer role "admin")

| Método | Endpoint | Autenticação | Descrição |
|--------|----------|--------------|-----------|
| POST | `/api/users` | ✅ Admin | Criar novo usuário |
| POST | `/api/projects` | ✅ Admin | Criar novo projeto |

---

## Validação de Tokens JWT

### Estrutura do Access Token

O access token é um **JWT RS256** com a seguinte estrutura:

**Header:**
```json
{
  "alg": "RS256",
  "typ": "JWT"
}
```

**Payload:**
```json
{
  "sub": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",  // User ID
  "email": "usuario@exemplo.com",
  "project": "vendas-api",                         // Audience
  "role": "user",                                   // Role do usuário
  "iat": 1735123456,                               // Issued at
  "nbf": 1735123456,                               // Not before
  "exp": 1735123756,                               // Expira em 5 min
  "iss": "heimdall",                               // Issuer
  "aud": "vendas-api"                              // Audience
}
```

### Validar JWT Localmente

**Por que validar localmente?**
- ✅ Performance (evita chamada ao servidor)
- ✅ Reduz latência
- ✅ Funciona offline após obter chave pública

**Configuração de Validação:**

```javascript
// Parâmetros de validação obrigatórios
{
  "ValidateIssuer": true,
  "ValidIssuer": "heimdall",
  "ValidateAudience": true,
  "ValidAudience": "vendas-api",  // Seu audience
  "ValidateLifetime": true,
  "ValidateIssuerSigningKey": true,
  "IssuerSigningKey": {publicKey},  // Chave pública RSA fornecida
  "ClockSkew": 0  // Sem tolerância de clock skew
}
```

### Chave Pública RSA

Solicite ao administrador a chave pública do Heimdall:

```
-----BEGIN RSA PUBLIC KEY-----
MIIBCgKCAQEAslczau1bQMSHT+KPfrWFB2zc3GuOH/sc7cByQJUDVog18f3gqzg0
vpTAk0wH1dVg9CYOCZhQpp8MLnIwG1Hpb+n4Z5J/NbkPTrkHQv791/EyykpRJNQs
...
-----END RSA PUBLIC KEY-----
```

---

## Exemplos de Integração

### JavaScript / TypeScript (Frontend)

#### Instalação

```bash
npm install axios jwt-decode
```

#### Código

```typescript
import axios from 'axios';
import jwtDecode from 'jwt-decode';

interface LoginRequest {
  email: string;
  password: string;
  audience: string;
}

interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

interface TokenPayload {
  sub: string;
  email: string;
  project: string;
  role: string;
  exp: number;
}

class HeimdallClient {
  private apiUrl: string;
  private audience: string;

  constructor(apiUrl: string, audience: string) {
    this.apiUrl = apiUrl;
    this.audience = audience;
  }

  async login(email: string, password: string): Promise<LoginResponse> {
    const response = await axios.post<LoginResponse>(`${this.apiUrl}/api/login`, {
      email,
      password,
      audience: this.audience
    });

    // Armazenar tokens
    localStorage.setItem('accessToken', response.data.accessToken);
    localStorage.setItem('refreshToken', response.data.refreshToken);

    return response.data;
  }

  async refresh(): Promise<LoginResponse> {
    const refreshToken = localStorage.getItem('refreshToken');
    if (!refreshToken) {
      throw new Error('No refresh token available');
    }

    const response = await axios.post<LoginResponse>(`${this.apiUrl}/api/refresh`, {
      refreshToken
    });

    // Atualizar tokens
    localStorage.setItem('accessToken', response.data.accessToken);
    localStorage.setItem('refreshToken', response.data.refreshToken);

    return response.data;
  }

  async logout(): Promise<void> {
    const accessToken = localStorage.getItem('accessToken');
    const refreshToken = localStorage.getItem('refreshToken');

    if (accessToken && refreshToken) {
      try {
        await axios.post(`${this.apiUrl}/api/revoke`, 
          { refreshToken },
          { headers: { Authorization: `Bearer ${accessToken}` } }
        );
      } catch (error) {
        console.error('Logout error:', error);
      }
    }

    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
  }

  getAccessToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  isTokenExpired(token: string): boolean {
    try {
      const decoded = jwtDecode<TokenPayload>(token);
      return decoded.exp * 1000 < Date.now();
    } catch {
      return true;
    }
  }

  async getValidToken(): Promise<string> {
    let token = this.getAccessToken();
    
    if (!token || this.isTokenExpired(token)) {
      await this.refresh();
      token = this.getAccessToken();
    }

    return token!;
  }
}

// Uso
const heimdall = new HeimdallClient('https://auth.exemplo.com', 'vendas-api');

// Login
await heimdall.login('usuario@exemplo.com', 'SenhaSegura123!');

// Fazer requisição autenticada
const token = await heimdall.getValidToken();
const response = await axios.get('https://api.exemplo.com/api/vendas', {
  headers: { Authorization: `Bearer ${token}` }
});

// Logout
await heimdall.logout();
```

---

### C# / .NET (Backend API)

#### Instalação

```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

#### Configuração (Program.cs)

```csharp
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Obter chave pública do Heimdall
var publicKeyPem = builder.Configuration["Heimdall:PublicKeyPem"]
    ?? throw new InvalidOperationException("Heimdall:PublicKeyPem is required");

var rsa = RSA.Create();
rsa.ImportFromPem(publicKeyPem);

// Configurar autenticação JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "heimdall",
            
            ValidateAudience = true,
            ValidAudience = "vendas-api",  // Seu audience
            
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
app.MapGet("/api/vendas", [Authorize] () => 
{
    return new[] { "Venda 1", "Venda 2", "Venda 3" };
});

app.Run();
```

#### appsettings.json

```json
{
  "Heimdall": {
    "ApiUrl": "https://auth.exemplo.com",
    "Audience": "vendas-api",
    "PublicKeyPem": "-----BEGIN RSA PUBLIC KEY-----\n..."
  }
}
```

#### Cliente HTTP para Login

```csharp
public class HeimdallClient
{
    private readonly HttpClient _httpClient;
    private readonly string _audience;

    public HeimdallClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(config["Heimdall:ApiUrl"]!);
        _audience = config["Heimdall:Audience"]!;
    }

    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        var request = new LoginRequest(email, password, _audience);
        var response = await _httpClient.PostAsJsonAsync("/api/login", request);
        
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<LoginResponse>();
        }
        
        return null;
    }

    public async Task<LoginResponse?> RefreshAsync(string refreshToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/refresh", 
            new RefreshRequest(refreshToken));
        
        return response.IsSuccessStatusCode 
            ? await response.Content.ReadFromJsonAsync<LoginResponse>()
            : null;
    }
}

public record LoginRequest(string Email, string Password, string Audience);
public record RefreshRequest(string RefreshToken);
public record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn);
```

---

### Python (Flask/Django)

#### Instalação

```bash
pip install requests PyJWT cryptography
```

#### Cliente

```python
import requests
import jwt
from datetime import datetime, timedelta

class HeimdallClient:
    def __init__(self, api_url: str, audience: str, public_key: str):
        self.api_url = api_url
        self.audience = audience
        self.public_key = public_key
    
    def login(self, email: str, password: str) -> dict:
        response = requests.post(
            f"{self.api_url}/api/login",
            json={
                "email": email,
                "password": password,
                "audience": self.audience
            }
        )
        response.raise_for_status()
        return response.json()
    
    def refresh(self, refresh_token: str) -> dict:
        response = requests.post(
            f"{self.api_url}/api/refresh",
            json={"refreshToken": refresh_token}
        )
        response.raise_for_status()
        return response.json()
    
    def validate_token(self, token: str) -> dict:
        return jwt.decode(
            token,
            self.public_key,
            algorithms=["RS256"],
            audience=self.audience,
            issuer="heimdall",
            options={"verify_exp": True}
        )

# Uso
client = HeimdallClient(
    api_url="https://auth.exemplo.com",
    audience="vendas-api",
    public_key=open("heimdall_public.pem").read()
)

# Login
result = client.login("usuario@exemplo.com", "SenhaSegura123!")
access_token = result["accessToken"]

# Validar token
try:
    payload = client.validate_token(access_token)
    print(f"User: {payload['email']}, Role: {payload['role']}")
except jwt.ExpiredSignatureError:
    print("Token expirado")
except jwt.InvalidTokenError as e:
    print(f"Token inválido: {e}")
```

---

### PHP (Laravel)

#### Instalação

```bash
composer require firebase/php-jwt guzzlehttp/guzzle
```

#### Cliente

```php
<?php

use Firebase\JWT\JWT;
use Firebase\JWT\Key;
use GuzzleHttp\Client;

class HeimdallClient
{
    private $client;
    private $audience;
    private $publicKey;
    
    public function __construct(string $apiUrl, string $audience, string $publicKeyPath)
    {
        $this->client = new Client(['base_uri' => $apiUrl]);
        $this->audience = $audience;
        $this->publicKey = file_get_contents($publicKeyPath);
    }
    
    public function login(string $email, string $password): array
    {
        $response = $this->client->post('/api/login', [
            'json' => [
                'email' => $email,
                'password' => $password,
                'audience' => $this->audience
            ]
        ]);
        
        return json_decode($response->getBody(), true);
    }
    
    public function refresh(string $refreshToken): array
    {
        $response = $this->client->post('/api/refresh', [
            'json' => ['refreshToken' => $refreshToken]
        ]);
        
        return json_decode($response->getBody(), true);
    }
    
    public function validateToken(string $token): object
    {
        return JWT::decode($token, new Key($this->publicKey, 'RS256'));
    }
}

// Uso
$heimdall = new HeimdallClient(
    'https://auth.exemplo.com',
    'vendas-api',
    __DIR__ . '/heimdall_public.pem'
);

$result = $heimdall->login('usuario@exemplo.com', 'SenhaSegura123!');
$decoded = $heimdall->validateToken($result['accessToken']);

echo "User: " . $decoded->email . ", Role: " . $decoded->role;
```

---

## Tratamento de Erros

### Códigos de Status HTTP

| Status | Descrição | Ação Recomendada |
|--------|-----------|------------------|
| 200 | OK | Sucesso |
| 400 | Bad Request | Validar dados de entrada |
| 401 | Unauthorized | Credenciais inválidas, redirecionar para login |
| 404 | Not Found | Refresh token não encontrado |
| 409 | Conflict | Email/Audience já existe |
| 429 | Too Many Requests | Aguardar antes de tentar novamente |
| 500 | Internal Server Error | Tentar novamente, contatar suporte |

### Exemplo de Tratamento (TypeScript)

```typescript
class AuthError extends Error {
  constructor(public status: number, message: string) {
    super(message);
  }
}

async function handleLogin(email: string, password: string) {
  try {
    const result = await heimdall.login(email, password);
    console.log('Login bem-sucedido');
  } catch (error) {
    if (axios.isAxiosError(error)) {
      const status = error.response?.status;
      
      switch (status) {
        case 400:
          const errors = error.response?.data.errors;
          console.error('Validação falhou:', errors);
          // Mostrar erros ao usuário
          break;
          
        case 401:
          console.error('Credenciais inválidas');
          // Mostrar mensagem de erro
          break;
          
        case 429:
          console.error('Muitas tentativas. Aguarde 1 minuto.');
          // Desabilitar botão de login temporariamente
          break;
          
        default:
          console.error('Erro inesperado:', error.message);
      }
    }
  }
}
```

---

## Segurança e Best Practices

### 1. Armazenamento de Tokens

| Método | Segurança | Pros | Cons | Recomendado? |
|--------|-----------|------|------|--------------|
| localStorage | ⚠️ Baixa | Persiste após reload | Vulnerável a XSS | ❌ Evitar |
| sessionStorage | ⚠️ Baixa | Limpa ao fechar aba | Vulnerável a XSS | ⚠️ Aceito |
| Cookie HttpOnly | ✅ Alta | Seguro contra XSS | Precisa CORS config | ✅ Ideal |
| Memory (in-app) | ✅ Alta | Mais seguro | Perde ao recarregar | ✅ Ideal |

**Recomendação:**
- **Backend (API-to-API)**: Memory ou cookie HttpOnly
- **Frontend (SPA)**: sessionStorage + auto-refresh em background
- **Mobile**: Secure Storage do SO (Keychain/Keystore)

### 2. Refresh Token Strategy

```typescript
// Estratégia recomendada: Auto-refresh em background
class TokenManager {
  private refreshTimer?: NodeJS.Timeout;

  scheduleRefresh(expiresIn: number) {
    // Renovar 30 segundos antes de expirar
    const refreshTime = (expiresIn - 30) * 1000;
    
    this.refreshTimer = setTimeout(async () => {
      try {
        await heimdall.refresh();
        this.scheduleRefresh(300); // Reagendar
      } catch (error) {
        console.error('Auto-refresh failed, redirecting to login');
        window.location.href = '/login';
      }
    }, refreshTime);
  }

  clearRefresh() {
    if (this.refreshTimer) {
      clearTimeout(this.refreshTimer);
    }
  }
}
```

### 3. Validação de Audience

**⚠️ CRÍTICO:** Sempre valide o audience do token!

```csharp
// Correto ✅
ValidAudience = "minha-app-api"  // Seu audience específico

// Errado ❌
ValidateAudience = false  // NUNCA faça isso!
```

### 4. HTTPS Obrigatório

```typescript
// Nunca aceitar HTTP em produção
if (window.location.protocol !== 'https:' && process.env.NODE_ENV === 'production') {
  window.location.href = 'https:' + window.location.href.substring(window.location.protocol.length);
}
```

### 5. Rate Limiting do Cliente

```typescript
// Implementar backoff exponencial
class RateLimiter {
  private attempts = 0;
  
  async executeWithBackoff(fn: () => Promise<any>) {
    try {
      const result = await fn();
      this.attempts = 0; // Reset em sucesso
      return result;
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 429) {
        this.attempts++;
        const delay = Math.min(1000 * Math.pow(2, this.attempts), 60000);
        console.log(`Rate limited. Retrying in ${delay}ms`);
        await new Promise(resolve => setTimeout(resolve, delay));
        return this.executeWithBackoff(fn);
      }
      throw error;
    }
  }
}
```

### 6. Logout em Múltiplas Abas

```typescript
// Sincronizar logout entre abas
window.addEventListener('storage', (e) => {
  if (e.key === 'accessToken' && e.newValue === null) {
    // Token removido em outra aba - fazer logout
    window.location.href = '/login';
  }
});
```

---

## FAQ

### P: O que fazer quando o access token expira?

**R:** Use o refresh token para obter um novo access token via `/api/refresh`. Implemente auto-refresh 30 segundos antes da expiração.

### P: Por quanto tempo o refresh token é válido?

**R:** 7 dias. Após esse período, o usuário precisa fazer login novamente.

### P: Posso usar o mesmo refresh token múltiplas vezes?

**R:** ❌ NÃO! Cada refresh token é de uso único. Ao renovar, você recebe um novo refresh token que invalida o anterior.

### P: Como detectar que um token foi revogado?

**R:** Ao tentar usar um refresh token revogado, você receberá `401 Unauthorized` no endpoint `/api/refresh`.

### P: Posso validar o JWT sem a chave pública?

**R:** Não é recomendado. Sem validação, você não tem garantia de que o token é legítimo.

### P: O que é o campo "role" no JWT?

**R:** É o papel do usuário naquele projeto específico (ex: "admin", "user", "viewer"). Um usuário pode ter roles diferentes em projetos diferentes.

### P: Como funciona o multi-tenancy?

**R:** Cada aplicação tem um "audience" único. Um usuário pode estar em múltiplos projetos com roles diferentes. O JWT contém apenas o contexto do projeto atual.

### P: O que acontece se eu tentar usar um token de outro audience?

**R:** A validação falhará com erro `InvalidAudience`, pois o audience no token não corresponde ao esperado.

### P: Posso criar usuários via API?

**R:** Apenas usuários com role "admin" podem criar novos usuários via `/api/users`.

### P: Como alterar a senha de um usuário?

**R:** Atualmente não há endpoint. Entre em contato com o administrador do Heimdall.

---

## Suporte

### Reportar Problemas

- 📧 **Email**: suporte@heimdall.com
- 🐛 **Issues**: https://github.com/suaorg/heimdall/issues
- 📚 **Documentação**: https://docs.heimdall.com

### Recursos Adicionais

- [OpenAPI Specification](https://auth.exemplo.com/swagger)
- [Postman Collection](https://www.postman.com/heimdall-api)
- [Exemplos no GitHub](https://github.com/suaorg/heimdall-examples)

---

**Versão:** 1.0.0  
**Data:** 2024-04-24  
**Licença:** MIT  
**Autor:** Heimdall Team
