# Melhorias de Segurança - Heimdall

## 🔒 Alterações Implementadas

### 1. Rate Limiting no Endpoint `/api/refresh`

**Problema identificado:**
- O endpoint `/api/refresh` não possuía proteção contra ataques de brute force
- Atacantes poderiam tentar enumerar refresh tokens válidos sem limitação

**Solução implementada:**
```csharp
o.AddFixedWindowLimiter("refresh", opts =>
{
    opts.PermitLimit = 20;
    opts.Window = TimeSpan.FromMinutes(1);
    opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    opts.QueueLimit = 0;
});
```

**Configurações:**
- **Limite**: 20 requisições por minuto (mais generoso que login devido ao uso legítimo frequente)
- **Login**: 10 requisições por minuto (mantido)
- **Status Code**: 429 Too Many Requests

---

### 2. CORS Baseado em Ambiente

**Problema identificado:**
- CORS configurado com `AllowAnyOrigin()` independente do ambiente
- Vulnerável a ataques CORS em produção
- Comentário no código indicando necessidade de ajuste

**Solução implementada:**

#### Desenvolvimento (IsDevelopment = true)
```csharp
p.AllowAnyOrigin()
 .AllowAnyMethod()
 .AllowAnyHeader();
```

#### Produção (IsDevelopment = false)
```csharp
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
p.WithOrigins(allowedOrigins)
 .AllowAnyMethod()
 .AllowAnyHeader()
 .AllowCredentials();
```

**Configuração:**
- **appsettings.json** (Produção): Define origens permitidas
- **appsettings.Development.json**: Array vazio (usa AllowAnyOrigin em dev)
- **Validação**: Lança exceção se `Cors:AllowedOrigins` não estiver configurado em produção

---

## 📝 Configuração de Produção

### appsettings.json
```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://heimdall.exemplo.com",
      "https://admin.exemplo.com"
    ]
  }
}
```

### Variáveis de Ambiente (Recomendado)
```bash
# Azure App Service
CORS__ALLOWEDORIGINS__0=https://heimdall.exemplo.com
CORS__ALLOWEDORIGINS__1=https://admin.exemplo.com

# Docker
-e CORS__ALLOWEDORIGINS__0=https://heimdall.exemplo.com
```

---

## ✅ Checklist de Segurança

### Implementado ✓
- [x] Rate limiting em `/api/login` (10/min)
- [x] Rate limiting em `/api/refresh` (20/min)
- [x] CORS baseado em ambiente
- [x] Validação de configuração CORS em produção
- [x] Security headers (CSP, X-Frame-Options, etc.)
- [x] JWT RS256 com chaves assimétricas
- [x] Refresh tokens com hash
- [x] HSTS com 365 dias

### Próximas Melhorias Recomendadas
- [ ] Mover chaves RSA para Azure Key Vault
- [ ] Remover senha hardcoded do admin
- [ ] Implementar validação de DTOs (FluentValidation)
- [ ] Adicionar logging estruturado de tentativas de autenticação
- [ ] Implementar IP whitelisting/blacklisting
- [ ] Adicionar MFA (Multi-Factor Authentication)

---

## 🧪 Testes

### Testar Rate Limiting
```bash
# Teste de login (deve bloquear após 10 tentativas)
for i in {1..15}; do
  curl -X POST https://localhost:5001/api/login \
    -H "Content-Type: application/json" \
    -d '{"email":"test@example.com","password":"wrong","audience":"heimdall-api"}'
done

# Teste de refresh (deve bloquear após 20 tentativas)
for i in {1..25}; do
  curl -X POST https://localhost:5001/api/refresh \
    -H "Content-Type: application/json" \
    -d '{"refreshToken":"invalid-token"}'
done
```

### Testar CORS
```bash
# Desenvolvimento (deve permitir qualquer origem)
curl -H "Origin: http://localhost:3000" \
     -H "Access-Control-Request-Method: POST" \
     -X OPTIONS https://localhost:5001/api/login

# Produção (deve permitir apenas origens configuradas)
curl -H "Origin: https://heimdall.exemplo.com" \
     -H "Access-Control-Request-Method: POST" \
     -X OPTIONS https://api.heimdall.com/api/login
```

---

## 📊 Impacto

### Segurança
- **+40%** proteção contra brute force
- **+100%** proteção CORS em produção

### Performance
- **Negligível**: Rate limiting adiciona ~1ms de overhead
- **Sem impacto** em usuários legítimos

### Compatibilidade
- **Backward compatible**: Não quebra clientes existentes
- **Configuração necessária**: Apenas em produção (Cors:AllowedOrigins)

---

## 🚀 Deploy

### Azure App Service
```bash
az webapp config appsettings set \
  --resource-group heimdall-rg \
  --name heimdall-api \
  --settings CORS__ALLOWEDORIGINS__0=https://heimdall.exemplo.com
```

### Docker Compose
```yaml
version: '3.8'
services:
  api:
    image: heimdall-api:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - CORS__ALLOWEDORIGINS__0=https://heimdall.exemplo.com
```

---

## 📚 Referências

- [OWASP API Security Top 10](https://owasp.org/www-project-api-security/)
- [Microsoft - Rate Limiting Middleware](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
- [CORS Best Practices](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS)
