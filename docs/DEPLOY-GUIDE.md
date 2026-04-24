# Heimdall - Guia de Deploy

## 📦 Arquitetura de Deploy

```
┌─────────────────────────────────────────────────┐
│              FRONTEND (Blazor WASM)             │
│                                                 │
│  Vercel (Static Hosting)                       │
│  URL: https://heimdall-app.vercel.app          │
└─────────────────────────────────────────────────┘
                      ▼
                    HTTPS
                      ▼
┌─────────────────────────────────────────────────┐
│           BACKEND (ASP.NET Core API)            │
│                                                 │
│  Azure App Service / Railway / Render           │
│  URL: https://heimdall-api.azurewebsites.net   │
└─────────────────────────────────────────────────┘
                      ▼
┌─────────────────────────────────────────────────┐
│              DATABASE (SQLite/SQL)              │
│                                                 │
│  Azure SQL / PostgreSQL / SQLite                │
└─────────────────────────────────────────────────┘
```

---

## 🚀 Deploy do Frontend (Blazor WASM) na Vercel

### Pré-requisitos

- Conta na [Vercel](https://vercel.com)
- Repositório GitHub com o código
- .NET 8 SDK instalado localmente

### Passo 1: Preparar o Projeto

Os arquivos já estão configurados:
- ✅ `vercel.json` - Configuração de rotas SPA
- ✅ `.vercelignore` - Arquivos a ignorar no deploy
- ✅ `build-vercel.sh` / `build-vercel.bat` - Scripts de build

### Passo 2: Build Local (Teste)

**Windows:**
```bash
build-vercel.bat
```

**Linux/Mac:**
```bash
chmod +x build-vercel.sh
./build-vercel.sh
```

Isso deve criar a pasta `vercel-output/` com os arquivos estáticos.

### Passo 3: Deploy via Vercel CLI

#### Instalar Vercel CLI

```bash
npm install -g vercel
```

#### Fazer Login

```bash
vercel login
```

#### Deploy

```bash
# Build primeiro
dotnet publish src/Heimdall.Web -c Release -o vercel-output/wwwroot

# Deploy para Vercel
vercel --prod
```

### Passo 4: Deploy via GitHub (Recomendado)

1. **Conectar Repositório:**
   - Acesse [vercel.com/new](https://vercel.com/new)
   - Selecione o repositório `Heimdall`
   - Import

2. **Configurar Build:**

   **Framework Preset:** `Other`

   **Build Command:**
   ```bash
   dotnet publish src/Heimdall.Web -c Release -o vercel-output/wwwroot
   ```

   **Output Directory:**
   ```
   vercel-output/wwwroot/wwwroot
   ```

   **Install Command:**
   ```bash
   curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0
   ```

3. **Environment Variables:**

   Adicione no Vercel Dashboard → Settings → Environment Variables:

   ```
   API_URL=https://heimdall-api.azurewebsites.net
   ```

4. **Deploy:**
   - Click **Deploy**
   - Aguarde o build completar
   - Acesse a URL fornecida (ex: `https://heimdall-app.vercel.app`)

### Passo 5: Configurar Domínio Custom (Opcional)

1. Vercel Dashboard → Settings → Domains
2. Adicionar domínio: `auth.seudominio.com`
3. Configurar DNS conforme instruções da Vercel

---

## 🖥️ Deploy do Backend (ASP.NET Core API)

Vercel **não suporta .NET** backend. Opções:

### Opção 1: Azure App Service (Recomendado)

#### Pré-requisitos
- Conta Azure (gratuita: 12 meses)
- Azure CLI instalado

#### Deploy

**1. Login Azure:**
```bash
az login
```

**2. Criar Resource Group:**
```bash
az group create --name heimdall-rg --location eastus
```

**3. Criar App Service Plan:**
```bash
az appservice plan create \
  --name heimdall-plan \
  --resource-group heimdall-rg \
  --sku FREE \
  --is-linux
```

**4. Criar Web App:**
```bash
az webapp create \
  --name heimdall-api \
  --resource-group heimdall-rg \
  --plan heimdall-plan \
  --runtime "DOTNET|8.0"
```

**5. Configurar Variáveis de Ambiente:**
```bash
az webapp config appsettings set \
  --name heimdall-api \
  --resource-group heimdall-rg \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    Seed__AdminEmail=admin@heimdall.com \
    Seed__AdminPassword=YourSecurePasswordHere \
    AllowedOrigins__0=https://heimdall-app.vercel.app
```

**6. Deploy:**
```bash
cd src/Heimdall.Api
dotnet publish -c Release -o ./publish

# Criar zip
cd publish
zip -r ../heimdall-api.zip .

# Deploy
az webapp deployment source config-zip \
  --name heimdall-api \
  --resource-group heimdall-rg \
  --src ../heimdall-api.zip
```

**7. URL da API:**
```
https://heimdall-api.azurewebsites.net
```

---

### Opção 2: Railway.app

Railway suporta .NET e tem free tier.

**1. Criar conta:** [railway.app](https://railway.app)

**2. Criar New Project → Deploy from GitHub**

**3. Selecionar repositório `Heimdall`**

**4. Configurar:**
   - **Root Directory:** `src/Heimdall.Api`
   - **Build Command:** `dotnet publish -c Release -o out`
   - **Start Command:** `dotnet out/Heimdall.Api.dll`

**5. Environment Variables:**
```
ASPNETCORE_ENVIRONMENT=Production
Seed__AdminEmail=admin@heimdall.com
Seed__AdminPassword=SecurePassword123!
AllowedOrigins__0=https://heimdall-app.vercel.app
```

**6. Deploy:** Click **Deploy**

**7. URL:** Railway fornecerá (ex: `heimdall-api-production.up.railway.app`)

---

### Opção 3: Render.com

**1. Criar conta:** [render.com](https://render.com)

**2. New → Web Service**

**3. Conectar GitHub → Selecionar `Heimdall`**

**4. Configurar:**
   - **Name:** `heimdall-api`
   - **Root Directory:** `src/Heimdall.Api`
   - **Environment:** `Docker` ou `.NET`
   - **Build Command:** `dotnet publish -c Release -o out`
   - **Start Command:** `dotnet out/Heimdall.Api.dll`

**5. Environment Variables:** (mesmo do Railway)

**6. Deploy**

---

## 🔗 Conectar Frontend e Backend

### 1. Atualizar URL da API no Frontend

Edite `src/Heimdall.Web/Program.cs`:

```csharp
// ANTES:
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:5001") });

// DEPOIS:
var apiUrl = builder.Configuration["ApiUrl"] ?? "https://heimdall-api.azurewebsites.net";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiUrl) });
```

### 2. Configurar CORS no Backend

Já está configurado no `Program.cs` do backend:

```csharp
// appsettings.json ou Environment Variables
{
  "AllowedOrigins": [
    "https://heimdall-app.vercel.app",
    "https://auth.seudominio.com"  // Se tiver domínio custom
  ]
}
```

### 3. Rebuild e Redeploy

**Frontend (Vercel):**
- Push para GitHub → Vercel redeploy automático
- Ou `vercel --prod`

**Backend (Azure):**
```bash
cd src/Heimdall.Api
dotnet publish -c Release -o ./publish
cd publish
zip -r ../api.zip .
az webapp deployment source config-zip \
  --name heimdall-api \
  --resource-group heimdall-rg \
  --src ../api.zip
```

---

## ✅ Checklist de Deploy

### Frontend (Vercel)
- [ ] Build local funciona (`build-vercel.bat`)
- [ ] `vercel.json` configurado
- [ ] Repository conectado na Vercel
- [ ] Build command configurado
- [ ] Output directory correto (`vercel-output/wwwroot/wwwroot`)
- [ ] Environment variable `API_URL` configurada
- [ ] Deploy bem-sucedido
- [ ] Domínio custom configurado (opcional)

### Backend (Azure/Railway/Render)
- [ ] Conta criada
- [ ] Web App/Service criado
- [ ] Runtime .NET 8 configurado
- [ ] Environment variables configuradas:
  - [ ] `ASPNETCORE_ENVIRONMENT=Production`
  - [ ] `Seed__AdminEmail`
  - [ ] `Seed__AdminPassword`
  - [ ] `AllowedOrigins__0`
- [ ] Database configurado (SQLite ou Azure SQL)
- [ ] Deploy bem-sucedido
- [ ] API acessível via HTTPS
- [ ] CORS permitindo frontend

### Integração
- [ ] Frontend consegue fazer login
- [ ] Tokens JWT funcionando
- [ ] Refresh token funcionando
- [ ] CORS sem erros no console
- [ ] HTTPS em ambos (frontend e backend)

---

## 🐛 Troubleshooting

### Erro: "CORS policy: No 'Access-Control-Allow-Origin'"

**Solução:** Adicionar URL do frontend no `AllowedOrigins` do backend

```bash
# Azure
az webapp config appsettings set \
  --name heimdall-api \
  --resource-group heimdall-rg \
  --settings AllowedOrigins__0=https://heimdall-app.vercel.app
```

### Erro: "Failed to fetch" no frontend

**Solução:** Verificar se o backend está rodando e acessível via HTTPS

```bash
curl https://heimdall-api.azurewebsites.net/health
```

### Erro: Build falha na Vercel

**Solução:** Verificar build command e output directory

Correto:
- Build: `dotnet publish src/Heimdall.Web -c Release -o vercel-output/wwwroot`
- Output: `vercel-output/wwwroot/wwwroot`

### Erro: "Seed__AdminPassword must be configured"

**Solução:** Adicionar environment variable no backend

```bash
az webapp config appsettings set \
  --name heimdall-api \
  --resource-group heimdall-rg \
  --settings Seed__AdminPassword=YourSecurePassword
```

---

## 📊 Monitoramento

### Vercel (Frontend)
- Analytics: `vercel.com/dashboard/analytics`
- Logs: `vercel logs heimdall-app`

### Azure (Backend)
- Logs: `az webapp log tail --name heimdall-api --resource-group heimdall-rg`
- Metrics: Azure Portal → App Service → Metrics

### Railway (Backend)
- Dashboard → Logs (tempo real)
- Metrics integrados

---

## 💰 Custos Estimados

### Free Tier

| Serviço | Free Tier | Limite |
|---------|-----------|--------|
| **Vercel** | 100 GB bandwidth/mês | Ilimitado para hobby |
| **Azure App Service** | 12 meses grátis | Depois ~$13/mês (Basic) |
| **Railway** | $5 crédito/mês | Depois ~$5/mês |
| **Render** | 750h/mês grátis | Web service free |

**Recomendação para começar:**
- Frontend: Vercel (grátis ilimitado)
- Backend: Railway ou Render (free tier)
- Database: SQLite (grátis) ou PostgreSQL free tier

---

## 🔐 Segurança em Produção

### 1. Secrets Management

**Nunca commitar:**
- Senhas de admin
- Connection strings
- Chaves privadas RSA

**Use:**
- Environment Variables do provedor de cloud
- Azure Key Vault (Azure)
- Vercel Secrets (Frontend)

### 2. HTTPS Obrigatório

Já configurado em `Program.cs`:
```csharp
app.UseHsts();
app.UseHttpsRedirection();
```

### 3. Rate Limiting

Já configurado (10 req/min login, 20 req/min refresh)

### 4. CORS Restrito

Apenas origens específicas em produção:
```json
{
  "AllowedOrigins": ["https://heimdall-app.vercel.app"]
}
```

---

## 📝 Próximos Passos

1. ✅ Deploy do frontend na Vercel
2. ✅ Deploy do backend (Azure/Railway/Render)
3. ✅ Configurar CORS
4. ⬜ Adicionar domínio custom
5. ⬜ Configurar Azure SQL ou PostgreSQL
6. ⬜ Adicionar CI/CD com GitHub Actions
7. ⬜ Adicionar monitoramento (Application Insights)
8. ⬜ Configurar backup do banco de dados

---

## 🆘 Suporte

**Problemas com deploy?**
- 📧 Email: devops@heimdall.com
- 💬 Discord: https://discord.gg/heimdall
- 📚 Docs: https://docs.heimdall.com/deploy

**Versão:** 1.0  
**Atualizado:** 2024-04-24
