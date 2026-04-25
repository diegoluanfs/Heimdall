# 🚀 Quick Start - Deploy Backend na Render via CI/CD

Guia rápido para configurar deploy automático do backend Heimdall.Api na Render.

## 📋 Checklist Rápido (15 minutos)

### 1️⃣ Criar Serviço na Render (5 min)

1. Acesse: https://dashboard.render.com
2. **New +** → **Web Service**
3. Conecte o repositório: `diegoluanfs/Heimdall`
4. Configure:
   - **Name**: `heimdall-api`
   - **Branch**: `main`
   - **Root Directory**: (deixe vazio)
   - **Environment**: `Docker`
   - **Dockerfile Path**: `./Dockerfile`
   - **Plan**: Free (para testes) ou Starter ($7/mês)

   ✅ O Dockerfile está na raiz do repositório para acessar todos os projetos!

### 2️⃣ Configurar Variáveis de Ambiente (3 min)

No serviço criado, vá em **Environment** e adicione:

```bash
# Básico
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000

# Database (SQLite com auto-migration)
Database__AutoMigrate=true

# JWT (você precisa gerar as chaves primeiro - veja seção 3)
Jwt__Issuer=https://heimdall-6afc.onrender.com
Jwt__ValidAudiences__0=heimdall-api
Jwt__PrivateKeyPem=[COLE AQUI O CONTEÚDO DA CHAVE PRIVADA - veja abaixo]
Jwt__PublicKeyPem=[COLE AQUI O CONTEÚDO DA CHAVE PÚBLICA - veja abaixo]

# Admin
Seed__AdminEmail=admin@heimdall.com
Seed__AdminPassword=Admin@123!Prod

# CORS (ajuste para sua URL Vercel)
Cors__AllowedOrigins__0=https://heimdall-diego-luans-projects.vercel.app
```

### 3️⃣ Gerar e Configurar Chaves JWT (5 min)

**No seu terminal local:**

```bash
# Gerar chave privada
openssl genpkey -algorithm RSA -out jwt_private.key -pkeyopt rsa_keygen_bits:2048

# Gerar chave pública
openssl rsa -pubout -in jwt_private.key -out jwt_public.key
```

**Preparar chaves para Environment Variables:**

As chaves precisam ser colocadas como **variáveis de ambiente** (NÃO como Secret Files).

**Opção A: Manual**
1. Abra `jwt_private.key` no editor
2. Copie TODO o conteúdo (incluindo `-----BEGIN...` e `-----END...`)
3. Na Render, adicione a variável `Jwt__PrivateKeyPem` e cole o conteúdo
4. Repita para `jwt_public.key` → variável `Jwt__PublicKeyPem`

⚠️ **IMPORTANTE**: As quebras de linha (`\n`) devem ser preservadas. A Render aceita multi-linha.

**Opção B: PowerShell Script** (Windows)
```powershell
# Ler e formatar chaves
$privateKey = Get-Content jwt_private.key -Raw
$publicKey = Get-Content jwt_public.key -Raw

Write-Host "=== PRIVATE KEY (cole em Jwt__PrivateKeyPem) ==="
Write-Host $privateKey
Write-Host "`n=== PUBLIC KEY (cole em Jwt__PublicKeyPem) ==="
Write-Host $publicKey
```

**Opção C: Bash Script** (Linux/Mac)
```bash
echo "=== PRIVATE KEY (cole em Jwt__PrivateKeyPem) ==="
cat jwt_private.key
echo -e "\n=== PUBLIC KEY (cole em Jwt__PublicKeyPem) ==="
cat jwt_public.key
```

### 4️⃣ Obter Deploy Hook (2 min)

**Opção A: Manual (mais rápido)**

1. No serviço, vá em **Settings**
2. Role até **Deploy Hook**
3. Copie a URL: `https://api.render.com/deploy/srv-xxxxx?key=yyyyy`

**Opção B: Script PowerShell**

```powershell
.\get-render-deploy-hook.ps1
```

### 5️⃣ Configurar Secret no GitHub (1 min)

1. Acesse: https://github.com/diegoluanfs/Heimdall/settings/secrets/actions
2. **New repository secret**
3. Configure:
   - **Name**: `RENDER_DEPLOY_HOOK_URL`
   - **Value**: Cole a URL do Deploy Hook

### 6️⃣ Testar Deploy (1 min)

```bash
git add .
git commit -m "ci: configure Render backend deployment"
git push origin main
```

Monitore:
- **GitHub Actions**: https://github.com/diegoluanfs/Heimdall/actions
- **Render Dashboard**: https://dashboard.render.com

---

## ✅ Resultado Final

Após configuração bem-sucedida:

### URLs:
- **Frontend (Vercel)**: https://heimdall-diego-luans-projects.vercel.app
- **Backend (Render)**: https://heimdall-api.onrender.com
- **Health Check**: https://heimdall-api.onrender.com/health

### CI/CD Flow:
```
Push to main
  ↓
GitHub Actions
  ↓
├─ Build & Test
├─ Deploy Frontend → Vercel
└─ Deploy Backend → Render
```

### Próximos Deployments:

Todos os pushes para `main` farão deploy automático de:
- ✅ Frontend (Blazor WASM) → Vercel
- ✅ Backend (API .NET 8) → Render

---

## 🔧 Banco de Dados (Opcional)

Se precisar de PostgreSQL:

1. **New +** → **PostgreSQL**
2. Configure:
   - **Name**: `heimdall-db`
   - **Plan**: Free ou Starter
3. **Link Database** ao serviço `heimdall-api`
4. Adicione variável:
   ```bash
   ConnectionStrings__DefaultConnection=#{DATABASE_URL}#
   ```

---

## 🆘 Problemas Comuns

### Deploy falha na Render
- ✅ Verifique logs no Render Dashboard
- ✅ Confirme Build Command e Start Command
- ✅ Verifique se todas as variáveis estão configuradas

### API retorna 500
- ✅ Verifique chaves JWT nas Secret Files
- ✅ Confirme paths: `/etc/secrets/jwt_private.key` e `/etc/secrets/jwt_public.key`
- ✅ Verifique connection string do banco

### CORS Error
- ✅ Confirme `AllowedOrigins__0` com URL exata do Vercel
- ✅ Verifique se frontend está usando a URL correta da API

### GitHub Actions não triggera deploy
- ✅ Confirme secret `RENDER_DEPLOY_HOOK_URL` no GitHub
- ✅ Verifique se commit foi para branch `main`
- ✅ Veja logs do GitHub Actions

---

## 📚 Documentação Completa

Para detalhes completos, consulte:
- **Render Setup Completo**: `docs/RENDER-SETUP.md`
- **Deploy Guide**: `docs/DEPLOY-GUIDE.md`
- **GitHub Actions**: `.github/workflows/deploy.yml`

---

**Pronto! Seu backend está configurado para deploy automático na Render via CI/CD! 🎉**
