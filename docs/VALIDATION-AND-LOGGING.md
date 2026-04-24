# Validação de DTOs e Logging de Autenticação

## 📋 Alterações Implementadas

### 1. Validação de DTOs com FluentValidation

Implementada validação automática para todos os endpoints da API usando FluentValidation.

#### Pacotes Adicionados
```xml
<PackageReference Include="FluentValidation" Version="12.1.1" />
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="12.1.1" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.7" />
```

#### Validadores Criados

**1. LoginRequestValidator**
- ✅ Email obrigatório e formato válido (max 256 chars)
- ✅ Password obrigatório (8-100 chars)
- ✅ Audience obrigatório (max 256 chars)

**2. CreateUserRequestValidator**
- ✅ Email obrigatório e formato válido (max 256 chars)
- ✅ Password complexa obrigatória:
  - Mínimo 8 caracteres
  - Pelo menos 1 letra maiúscula
  - Pelo menos 1 letra minúscula
  - Pelo menos 1 dígito
  - Pelo menos 1 caractere especial

**3. CreateProjectRequestValidator**
- ✅ Nome obrigatório (3-100 chars)
- ✅ Audience obrigatório (3-256 chars)
- ✅ Audience apenas lowercase, números e hífens

**4. RefreshRequestValidator**
- ✅ Token obrigatório (32-512 chars)

**5. RevokeRequestValidator**
- ✅ Token obrigatório (32-512 chars)

#### ValidationFilter

Criado endpoint filter genérico que intercepta todas as requisições e valida automaticamente os DTOs:

```csharp
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    // Valida request automaticamente
    // Retorna 400 Bad Request com detalhes dos erros se inválido
}
```

#### Integração nos Endpoints

```csharp
app.MapPost("/api/login", ...)
   .AddEndpointFilter<ValidationFilter<LoginRequest>>()
   .RequireRateLimiting("login")
   .WithName("Login");
```

---

### 2. Logging Estruturado de Autenticação

Implementado logging completo para todas as tentativas de autenticação no `AuthService`.

#### Logs de Login

**Tentativa de Login (Information)**
```
Login attempt for email: {Email}, audience: {Audience}, IP: {IP}
```

**Falhas de Login (Warning)**
```
Login failed - User not found or inactive: {Email}, IP: {IP}
Login failed - Invalid password for user: {UserId}, email: {Email}, IP: {IP}
Login failed - Project not found or inactive: {Audience}, user: {UserId}, IP: {IP}
Login failed - User not associated with project: {UserId}, project: {ProjectId}, IP: {IP}
```

**Login Bem-Sucedido (Information)**
```
Login successful for user: {UserId}, email: {Email}, project: {ProjectId}, role: {Role}, IP: {IP}
```

#### Logs de Refresh Token

**Tentativa de Refresh (Information)**
```
Token refresh attempt from IP: {IP}
```

**Falhas de Refresh (Warning)**
```
Token refresh failed - Invalid or inactive refresh token from IP: {IP}
Token refresh failed - User not found or inactive: {UserId}, IP: {IP}
Token refresh failed - User not associated with project: {UserId}, project: {ProjectId}, IP: {IP}
```

**Refresh Bem-Sucedido (Information)**
```
Token refresh successful for user: {UserId}, project: {ProjectId}, IP: {IP}
```

#### Logs de Revogação

**Tentativa de Revogação (Information)**
```
Token revocation attempt
```

**Falha de Revogação (Warning)**
```
Token revocation failed - Token not found
```

**Revogação Bem-Sucedida (Information)**
```
Token revoked successfully for user: {UserId}, project: {ProjectId}
```

---

## 📊 Benefícios

### Segurança
1. **Validação de Entrada**: Previne injeção de dados maliciosos
2. **Auditoria**: Todos os eventos de autenticação são registrados
3. **Detecção de Ataques**: Logs permitem identificar tentativas de brute force
4. **Compliance**: Atende requisitos de auditoria (LGPD, SOC 2, ISO 27001)

### Experiência do Desenvolvedor
1. **Mensagens de Erro Claras**: Validações retornam detalhes específicos
2. **Debugging Facilitado**: Logs estruturados com contexto completo
3. **Menos Código**: Validação automática via filtros

### Performance
- **Validação Early**: Requisições inválidas são rejeitadas antes do processamento
- **Overhead Mínimo**: FluentValidation é altamente otimizado (~0.1ms)

---

## 🧪 Exemplos de Uso

### Exemplo 1: Login com Email Inválido

**Request:**
```bash
POST /api/login
{
  "email": "invalid-email",
  "password": "short",
  "audience": "heimdall-api"
}
```

**Response:** `400 Bad Request`
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

**Log:**
```
[2024-04-24 15:30:45] [Information] Login attempt for email: invalid-email, audience: heimdall-api, IP: 192.168.1.100
```

### Exemplo 2: Criar Usuário com Senha Fraca

**Request:**
```bash
POST /api/users
{
  "email": "user@example.com",
  "password": "abc123"
}
```

**Response:** `400 Bad Request`
```json
{
  "errors": {
    "Password": [
      "Password must be at least 8 characters.",
      "Password must contain at least one uppercase letter.",
      "Password must contain at least one special character."
    ]
  }
}
```

### Exemplo 3: Login Bem-Sucedido

**Request:**
```bash
POST /api/login
{
  "email": "admin@heimdall.com",
  "password": "Admin@123",
  "audience": "heimdall-api"
}
```

**Logs:**
```
[Information] Login attempt for email: admin@heimdall.com, audience: heimdall-api, IP: 192.168.1.100
[Information] Login successful for user: a1b2c3d4-..., email: admin@heimdall.com, project: e5f6g7h8-..., role: admin, IP: 192.168.1.100
```

### Exemplo 4: Tentativa de Login Falha

**Request:**
```bash
POST /api/login
{
  "email": "admin@heimdall.com",
  "password": "WrongPassword123!",
  "audience": "heimdall-api"
}
```

**Logs:**
```
[Information] Login attempt for email: admin@heimdall.com, audience: heimdall-api, IP: 192.168.1.100
[Warning] Login failed - Invalid password for user: a1b2c3d4-..., email: admin@heimdall.com, IP: 192.168.1.100
```

---

## 🔍 Monitoramento e Alertas

### Queries Úteis para Logs

**Detectar ataques de brute force:**
```kusto
// Application Insights / Azure Monitor
traces
| where message contains "Login failed - Invalid password"
| summarize FailedAttempts = count() by IP = tostring(customDimensions.IP)
| where FailedAttempts > 10
| order by FailedAttempts desc
```

**Usuários mais ativos:**
```kusto
traces
| where message contains "Login successful"
| summarize Logins = count() by UserId = tostring(customDimensions.UserId)
| order by Logins desc
| take 10
```

**Refresh tokens suspeitos:**
```kusto
traces
| where message contains "Token refresh failed - Invalid"
| summarize count() by bin(timestamp, 1h), IP = tostring(customDimensions.IP)
| where count_ > 20
```

---

## 📝 Configuração de Logs

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Heimdall.Application.Services.AuthService": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Heimdall.Application.Services.AuthService": "Debug"
    }
  }
}
```

### Integração com Serilog (Opcional)

```csharp
builder.Host.UseSerilog((context, config) =>
{
    config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .WriteTo.Console()
        .WriteTo.File("logs/heimdall-.log", rollingInterval: RollingInterval.Day)
        .WriteTo.ApplicationInsights(TelemetryConfiguration.Active, TelemetryConverter.Traces);
});
```

---

## ✅ Checklist de Implementação

### Validação ✓
- [x] FluentValidation instalado
- [x] Validadores criados para todos os DTOs
- [x] ValidationFilter implementado
- [x] Validadores registrados no DI
- [x] Filtros aplicados em todos os endpoints

### Logging ✓
- [x] ILogger injetado no AuthService
- [x] Logs de tentativas de login
- [x] Logs de falhas de autenticação
- [x] Logs de sucessos
- [x] Logs de refresh token
- [x] Logs de revogação
- [x] Logs estruturados com IP e contexto

---

## 🚀 Próximos Passos Sugeridos

1. **Logging Avançado**
   - [ ] Adicionar Serilog para logs estruturados
   - [ ] Integrar com Application Insights
   - [ ] Configurar alertas automáticos

2. **Validação Adicional**
   - [ ] Validar força de senha em tempo real
   - [ ] Validar email contra lista de domínios permitidos
   - [ ] Adicionar validação de business rules

3. **Segurança**
   - [ ] Implementar IP whitelisting/blacklisting
   - [ ] Adicionar CAPTCHA após múltiplas falhas
   - [ ] Implementar MFA (Multi-Factor Authentication)

4. **Observabilidade**
   - [ ] Adicionar métricas (Prometheus)
   - [ ] Implementar distributed tracing (OpenTelemetry)
   - [ ] Dashboard de monitoramento

---

## 📚 Referências

- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [ASP.NET Core Logging](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/)
- [Structured Logging Best Practices](https://www.loggly.com/ultimate-guide/c-logging-basics/)
- [OWASP Logging Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html)
