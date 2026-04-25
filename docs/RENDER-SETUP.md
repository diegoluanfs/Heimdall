# Render Setup Guide - Backend API

Este guia explica como configurar o deploy automático do backend Heimdall.Api na Render via GitHub Actions.

## 📋 Pré-requisitos

- Conta na [Render](https://render.com) (gratuita)
- Repositório GitHub com o código
- GitHub Actions habilitado

## 🚀 Passo a Passo

### 1. Criar Serviço Web na Render

1. Acesse [https://dashboard.render.com](https://dashboard.render.com)
2. Clique em **"New +"** → **"Web Service"**
3. Conecte seu repositório GitHub: `diegoluanfs/Heimdall`
4. Configure o serviço:

   ```
   Name: heimdall-api
   Region: Oregon (US West) ou Frankfurt (EU Central)
   Branch: main
   Root Directory: src/Heimdall.Api
   Runtime: .NET
   Build Command: dotnet publish -c Release -o out
   Start Command: dotnet out/Heimdall.Api.dll
   ```

5. Escolha o plano:
   - **Free** (para testes) - limitations: 750h/mês, suspend após 15min de inatividade
   - **Starter** ($7/mês) - sem suspend, SSL customizado

### 2. Configurar Variáveis de Ambiente na Render

No dashboard do serviço criado, vá em **"Environment"** e adicione:

#### Obrigatórias:
```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
```

#### Conexão com Banco (escolha uma):

**Opção A: Render PostgreSQL (Recomendado)**
```bash
DATABASE_URL={auto-preenchido ao conectar PostgreSQL}
```

**Opção B: Railway PostgreSQL**
```bash
ConnectionStrings__DefaultConnection=Host=containers-us-west-xxx.railway.app;Port=5432;Database=railway;Username=postgres;Password=xxx
```

**Opção C: SQLite (apenas para dev/testes)**
```bash
ConnectionStrings__DefaultConnection=Data Source=/var/data/heimdall.db
```

#### Segurança (JWT):
```bash
Jwt__Issuer=https://heimdall-api.onrender.com
Jwt__Audience=heimdall-web
Jwt__PrivateKeyPath=/etc/secrets/jwt_private.key
Jwt__PublicKeyPath=/etc/secrets/jwt_public.key
```

#### Admin Inicial:
```bash
Seed__AdminEmail=admin@heimdall.com
Seed__AdminPassword=Admin@123!Prod
```

#### CORS:
```bash
AllowedOrigins__0=https://heimdall-diego-luans-projects.vercel.app
```

### 3. Configurar Chaves JWT na Render

#### Gerar as chaves RSA localmente:
```bash
# Chave privada
openssl genpkey -algorithm RSA -out jwt_private.key -pkeyopt rsa_keygen_bits:2048

# Chave pública
openssl rsa -pubout -in jwt_private.key -out jwt_public.key
```

#### Adicionar como Secret Files na Render:
1. No dashboard do serviço, vá em **"Environment"** → **"Secret Files"**
2. Adicione:
   - **Filename**: `/etc/secrets/jwt_private.key` → Cole o conteúdo de `jwt_private.key`
   - **Filename**: `/etc/secrets/jwt_public.key` → Cole o conteúdo de `jwt_public.key`

### 4. Obter Deploy Hook da Render

1. No dashboard do serviço, vá em **"Settings"**
2. Role até **"Deploy Hook"**
3. Copie a URL (formato: `https://api.render.com/deploy/srv-xxxxx?key=yyyyy`)

### 5. Configurar Secret no GitHub

1. Acesse: `https://github.com/diegoluanfs/Heimdall/settings/secrets/actions`
2. Clique em **"New repository secret"**
3. Adicione:
   - **Name**: `RENDER_DEPLOY_HOOK_URL`
   - **Value**: Cole a URL do Deploy Hook copiada acima

### 6. Testar o Deploy

Faça um commit e push para `main`:

```bash
git add .
git commit -m "test: trigger Render deploy via CI/CD"
git push origin main
```

O GitHub Actions irá:
1. ✅ Build e testes
2. ✅ Deploy frontend para Vercel
3. ✅ Trigger deploy backend na Render

Monitore em:
- GitHub Actions: `https://github.com/diegoluanfs/Heimdall/actions`
- Render Dashboard: `https://dashboard.render.com`

## 🔧 Configuração de Banco de Dados

### Opção Recomendada: PostgreSQL na Render

1. No dashboard Render, clique em **"New +"** → **"PostgreSQL"**
2. Configure:
   ```
   Name: heimdall-db
   Database: heimdall
   User: heimdall_user
   Region: Mesma do backend
   ```

3. Conecte ao serviço Web:
   - No serviço `heimdall-api`, vá em **"Environment"**
   - Clique em **"Link Database"**
   - Selecione `heimdall-db`
   - Isso cria automaticamente a variável `DATABASE_URL`

4. Atualize a connection string no código (appsettings.Production.json):
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "#{DATABASE_URL}#"
     }
   }
   ```

## 🛡️ Health Check

Configure health check para monitoramento:

1. No serviço, vá em **"Settings"** → **"Health Check Path"**
2. Adicione: `/health`
3. No código, adicione endpoint de health:

```csharp
// Program.cs
app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow 
}));
```

## 📊 Monitoramento

A Render fornece:
- **Logs em tempo real**: Dashboard → Logs
- **Metrics**: CPU, Memory, Network
- **Events**: Deploy history, crashes

## 🔄 Deploy Manual (emergência)

Se precisar fazer deploy manual:

```bash
curl -X POST "$RENDER_DEPLOY_HOOK_URL"
```

## 🎯 Checklist Final

- [ ] Serviço web criado na Render
- [ ] Variáveis de ambiente configuradas
- [ ] Chaves JWT adicionadas como Secret Files
- [ ] Deploy Hook copiado
- [ ] `RENDER_DEPLOY_HOOK_URL` adicionado aos secrets do GitHub
- [ ] Banco de dados configurado (PostgreSQL recomendado)
- [ ] Health check endpoint implementado
- [ ] CORS configurado com URL do frontend Vercel
- [ ] Primeiro deploy realizado com sucesso
- [ ] API testada em `https://heimdall-api.onrender.com`

## 🌐 URLs Finais

Após o deploy:
- **API Backend**: `https://heimdall-api.onrender.com`
- **Frontend**: `https://heimdall-diego-luans-projects.vercel.app`
- **Health Check**: `https://heimdall-api.onrender.com/health`
- **Swagger**: `https://heimdall-api.onrender.com/swagger` (se habilitado)

## 💡 Dicas

1. **Plano Free**: API "dorme" após 15min de inatividade. Primeira requisição pode levar 30-60s.
2. **Cold Start**: Use serviço de ping (como UptimeRobot) para manter API ativa.
3. **Logs**: Render mantém logs por 7 dias no plano Free.
4. **Rollback**: Use "Manual Deploy" para voltar para commits anteriores.

## 🆘 Troubleshooting

### Deploy falha com "Build failed"
- Verifique logs no Render Dashboard
- Confirme que `Build Command` está correto
- Verifique se todas as dependências estão no .csproj

### API retorna 500
- Verifique variáveis de ambiente
- Confirme connection string do banco
- Verifique se chaves JWT estão configuradas corretamente

### CORS Error no frontend
- Confirme `AllowedOrigins__0` no Render
- Verifique URL exata do frontend Vercel (com/sem trailing slash)

---

**Próximo passo**: Após configurar tudo, atualize o `appsettings.Production.json` no frontend com a URL da API Render!
