# 🔧 Troubleshooting - Conexão Render ↔ Vercel

Guia de resolução de problemas para integração entre backend (Render) e frontend (Vercel).

---

## ❌ **Problema 1: CORS Error no Console do Navegador**

### **Sintoma:**
```
Access to fetch at 'https://heimdall-6afc.onrender.com/api/auth/login' from origin 'https://heimdall-diego-luans-projects.vercel.app' has been blocked by CORS policy
```

### **Causa:**
Variável de ambiente CORS incorreta ou ausente na Render.

### **Solução:**
1. Acesse: https://dashboard.render.com → Seu serviço → **Environment**
2. Verifique se existe a variável:
   ```
   Key: Cors__AllowedOrigins__0
   Value: https://heimdall-diego-luans-projects.vercel.app
   ```
3. ⚠️ **IMPORTANTE**: São **DOIS** underscores `__` (não um só)
4. ⚠️ **IMPORTANTE**: A URL deve ser **exata** (sem barra no final)
5. Se não existir ou estiver errada, adicione/corrija e salve
6. Aguarde o redeploy automático (~2 minutos)

### **Validação:**
```bash
# Teste CORS com curl
curl -I -X OPTIONS https://heimdall-6afc.onrender.com/api/auth/login \
  -H "Origin: https://heimdall-diego-luans-projects.vercel.app" \
  -H "Access-Control-Request-Method: POST"

# Deve retornar:
# Access-Control-Allow-Origin: https://heimdall-diego-luans-projects.vercel.app
```

---

## ❌ **Problema 2: 401 Unauthorized ao Fazer Login**

### **Sintoma:**
- Login retorna 401 mesmo com credenciais corretas
- Ou erro: "Invalid token" / "Unable to validate token"

### **Causa Provável:**
Chaves JWT não configuradas ou configuradas incorretamente.

### **Solução:**

#### **Passo 1: Verificar se as chaves existem**
No Render Dashboard → Environment, confirme que existem:
```
Jwt__PrivateKeyPem
Jwt__PublicKeyPem
```

#### **Passo 2: Verificar formato**
As chaves devem incluir:
```
-----BEGIN PRIVATE KEY-----
[conteúdo]
-----END PRIVATE KEY-----
```

E:
```
-----BEGIN PUBLIC KEY-----
[conteúdo]
-----END PUBLIC KEY-----
```

#### **Passo 3: Regenerar se necessário**
Use o script:
```powershell
.\prepare-render-config.ps1
```

Ou manualmente:
```bash
openssl genpkey -algorithm RSA -out jwt_private.key -pkeyopt rsa_keygen_bits:2048
openssl rsa -pubout -in jwt_private.key -out jwt_public.key
```

Copie o conteúdo COMPLETO de cada arquivo para as variáveis.

---

## ❌ **Problema 3: API Retorna 500 Internal Server Error**

### **Sintoma:**
- Requisições retornam 500
- Logs mostram: "Jwt:PublicKeyPem configuration is required"

### **Causa:**
Variáveis JWT não configuradas ou com nomes errados.

### **Solução:**

Confirme que na Render existem estas variáveis **EXATAS**:
```
Jwt__Issuer=https://heimdall-6afc.onrender.com
Jwt__ValidAudiences__0=heimdall-api
Jwt__PrivateKeyPem=[CONTEÚDO COMPLETO DA CHAVE]
Jwt__PublicKeyPem=[CONTEÚDO COMPLETO DA CHAVE]
```

⚠️ **Erros comuns:**
- ❌ `Jwt__Audience` → ✅ `Jwt__ValidAudiences__0`
- ❌ `Jwt__PrivateKeyPath` → ✅ `Jwt__PrivateKeyPem`
- ❌ `Jwt__PublicKeyPath` → ✅ `Jwt__PublicKeyPem`

---

## ❌ **Problema 4: Frontend Não Consegue Conectar na API**

### **Sintoma:**
- Login não funciona
- Network error / Failed to fetch
- ERR_NAME_NOT_RESOLVED

### **Causa:**
Frontend está usando URL errada da API.

### **Solução:**

#### **Passo 1: Verificar `appsettings.Production.json`**
Arquivo: `src/Heimdall.Web/wwwroot/appsettings.Production.json`

Deve conter:
```json
{
  "ApiUrl": "https://heimdall-6afc.onrender.com"
}
```

#### **Passo 2: Verificar se deploy do frontend usou a versão correta**
1. Acesse: https://heimdall-diego-luans-projects.vercel.app
2. Abra DevTools (F12) → Console
3. Digite:
   ```javascript
   fetch('/appsettings.Production.json').then(r => r.json()).then(console.log)
   ```
4. Deve mostrar: `{ ApiUrl: "https://heimdall-6afc.onrender.com" }`

#### **Passo 3: Se estiver errado, corrija e faça redeploy**
```bash
# Corrija src/Heimdall.Web/wwwroot/appsettings.Production.json
git add src/Heimdall.Web/wwwroot/appsettings.Production.json
git commit -m "fix: update API URL to Render"
git push origin main
```

Aguarde ~2 minutos para Vercel fazer deploy.

---

## ❌ **Problema 5: "Pending migrations detected" ao Iniciar API**

### **Sintoma:**
Logs mostram:
```
Pending migrations detected: 20260324180154_InitialCreate.
Unhandled exception. System.InvalidOperationException: Pending migrations detected.
```

### **Solução:**
Adicione variável de ambiente na Render:
```
Database__AutoMigrate=true
```

Salve e aguarde redeploy.

---

## ❌ **Problema 6: API Responde mas Retorna 404 para Todos os Endpoints**

### **Sintoma:**
- Health check funciona: `https://heimdall-6afc.onrender.com/health` → 200 OK
- Mas `/api/auth/login` retorna 404

### **Causa Provável:**
Problema no mapeamento de rotas ou configuração de URL.

### **Solução:**

#### **Teste direto:**
```bash
curl -X POST https://heimdall-6afc.onrender.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@heimdall.com","password":"Admin@123!Prod","projectName":"heimdall-api"}'
```

Se retornar 404:
1. Verifique logs da Render
2. Confirme que a URL base está correta (sem `/api` no final)
3. Verifique se o código foi deployado corretamente

---

## ❌ **Problema 7: Token Vercel Inválido no GitHub Actions**

### **Sintoma:**
GitHub Actions falha com:
```
Error: The token provided via `--token` argument is not valid.
```

### **Solução:**

#### **Gerar novo token Vercel:**
1. Acesse: https://vercel.com/account/tokens
2. **Create Token**
3. Nome: `github-actions-heimdall`
4. Scope: `Full Account`
5. Expiration: `No Expiration`
6. **Create** e **copie o token**

#### **Atualizar secret no GitHub:**
1. https://github.com/diegoluanfs/Heimdall/settings/secrets/actions
2. Localize `VERCEL_TOKEN`
3. **Update** e cole o novo token
4. **Re-run** o workflow falho

---

## 🧪 **Checklist de Validação Completa**

Use este checklist para garantir que tudo está configurado corretamente:

### **Backend (Render):**
```
✅ Serviço está "Live" (não suspenso)
✅ Health check responde: https://heimdall-6afc.onrender.com/health
✅ Variável: ASPNETCORE_ENVIRONMENT=Production
✅ Variável: ASPNETCORE_URLS=http://+:5000
✅ Variável: Database__AutoMigrate=true
✅ Variável: Jwt__Issuer=https://heimdall-6afc.onrender.com
✅ Variável: Jwt__ValidAudiences__0=heimdall-api
✅ Variável: Jwt__PrivateKeyPem=[chave completa com BEGIN/END]
✅ Variável: Jwt__PublicKeyPem=[chave completa com BEGIN/END]
✅ Variável: Seed__AdminEmail=admin@heimdall.com
✅ Variável: Seed__AdminPassword=[sua senha]
✅ Variável: Cors__AllowedOrigins__0=https://heimdall-diego-luans-projects.vercel.app
✅ Logs não mostram erros de startup
```

### **Frontend (Vercel):**
```
✅ Deploy concluído com sucesso
✅ Site acessível: https://heimdall-diego-luans-projects.vercel.app
✅ appsettings.Production.json tem ApiUrl correto
✅ Console do navegador não mostra erros CORS
```

### **GitHub Secrets:**
```
✅ VERCEL_TOKEN (válido e não expirado)
✅ VERCEL_ORG_ID=team_yJqyQMqqqJqzY85VPTgAjUc2
✅ VERCEL_PROJECT_ID=prj_vxFEzrpNS8tYVNt9AGii1sUdtRqt
✅ RENDER_DEPLOY_HOOK_URL=[URL do deploy hook]
```

### **Teste End-to-End:**
```
✅ Acessar https://heimdall-diego-luans-projects.vercel.app
✅ Fazer login com admin@heimdall.com / [senha] / heimdall-api
✅ Login retorna token JWT
✅ Dashboard carrega corretamente
```

---

## 📞 **Ainda com Problemas?**

### **1. Verifique os logs:**
- **Render**: Dashboard → Logs
- **Vercel**: Dashboard → Deployments → [último deploy] → Logs
- **GitHub Actions**: https://github.com/diegoluanfs/Heimdall/actions

### **2. Use o script de diagnóstico:**
```powershell
.\prepare-render-config.ps1
```

Isso gera todas as variáveis corretas.

### **3. Reconfigure do zero (último recurso):**
1. Delete o serviço na Render
2. Recrie seguindo `RENDER-QUICK-START.md`
3. Use o script `prepare-render-config.ps1` para gerar as variáveis
4. Adicione todas as variáveis de uma vez

---

## 🎯 **Comandos Úteis para Debugging**

### **Teste Health Check:**
```bash
curl https://heimdall-6afc.onrender.com/health
```

### **Teste Login:**
```bash
curl -X POST https://heimdall-6afc.onrender.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@heimdall.com","password":"Admin@123!Prod","projectName":"heimdall-api"}'
```

### **Teste CORS:**
```bash
curl -I -X OPTIONS https://heimdall-6afc.onrender.com/api/auth/login \
  -H "Origin: https://heimdall-diego-luans-projects.vercel.app" \
  -H "Access-Control-Request-Method: POST"
```

### **Ver configuração do frontend deployado:**
```
https://heimdall-diego-luans-projects.vercel.app/appsettings.Production.json
```

---

**Este documento cobre 95% dos problemas comuns de integração Render-Vercel!** 🎉
