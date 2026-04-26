# 🐘 Configuração PostgreSQL no Render

Guia para migrar de SQLite para PostgreSQL e garantir persistência de dados em produção.

---

## 🎯 Por Que PostgreSQL?

### ❌ **Problema com SQLite no Render:**
- Dados são **perdidos** a cada deploy/restart
- Container efêmero não tem volumes persistentes
- Inadequado para produção

### ✅ **Vantagens do PostgreSQL:**
- ✅ **Dados persistentes** (sobrevivem a deploys)
- ✅ **Free tier** Render (512MB storage)
- ✅ **Backups automáticos**
- ✅ **Compartilhado** entre múltiplas instâncias
- ✅ **Melhor performance** para produção

---

## 📋 Passo a Passo (10 minutos)

### 1️⃣ Criar Banco PostgreSQL no Render

1. **Acesse:** https://dashboard.render.com
2. **Clique em:** `New +` → `PostgreSQL`
3. **Configure:**
   - **Name:** `heimdall-db`
   - **Database:** `heimdall` (ou deixe auto-gerar)
   - **User:** `heimdall` (ou deixe auto-gerar)
   - **Region:** Mesma do backend (`Frankfurt` se backend está lá)
   - **Plan:** `Free` (512MB, 90 dias de backup)

4. **Clique em:** `Create Database`

5. **Aguarde:** 2-3 minutos para provisionar

---

### 2️⃣ Obter Connection String

Após criação, na página do banco:

1. **Role até:** `Connections`
2. **Copie:** `External Database URL` ou `Internal Database URL`

**Diferença:**
- **Internal:** Mais rápido, mesma rede privada Render (recomendado)
- **External:** Acesso público (para testar localmente)

**Formato:**
```
postgresql://user:password@host:port/database
```

---

### 3️⃣ Atualizar Variáveis no Render (Backend)

1. **Acesse:** https://dashboard.render.com
2. **Selecione:** `heimdall-api` (seu backend)
3. **Vá em:** `Environment`
4. **Adicione/Atualize:**

```bash
# Habilitar PostgreSQL
Database__UsePostgreSQL=true

# Connection String (cole a URL copiada)
ConnectionStrings__DefaultConnection=postgresql://user:password@host:port/database

# Auto-migration (manter ativado)
Database__AutoMigrate=true
```

5. **Clique em:** `Save Changes`

O Render vai redeploy automaticamente.

---

### 4️⃣ Verificar Deploy

Aguarde 3-5 minutos e verifique logs:

1. **Acesse:** `Logs` do serviço `heimdall-api`
2. **Procure por:**

```
✅ Sucesso:
info: Program[0]
      Database migrated successfully (Production)
info: Program[0]
      Admin user created: admin@heimdall.com
```

```
❌ Erro (connection string errada):
Npgsql.NpgsqlException: Connection failed
```

---

### 5️⃣ Testar Backend

```powershell
# Health check
curl https://heimdall-6afc.onrender.com/health

# Login
$body = @{
    email = "admin@heimdall.com"
    password = "Admin@123!Prod"
    audience = "heimdall-api"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://heimdall-6afc.onrender.com/api/login" `
    -Method POST `
    -ContentType "application/json" `
    -Body $body
```

---

### 6️⃣ Testar Persistência

**Teste 1: Fazer login e criar dados**
1. Acesse https://heimdall-diego-luans-projects.vercel.app
2. Faça login com credenciais admin
3. (Futuramente) Crie usuários/projetos

**Teste 2: Forçar redeploy**
1. No Render, vá em `Manual Deploy` → `Deploy latest commit`
2. Aguarde deploy terminar
3. Faça login novamente
4. ✅ **Dados devem persistir!**

---

## 🔧 Desenvolvimento Local com PostgreSQL (Opcional)

Se quiser testar PostgreSQL localmente:

### Opção A: Docker Compose

Crie `docker-compose.yml`:

```yaml
version: '3.8'
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: heimdall
      POSTGRES_PASSWORD: dev123
      POSTGRES_DB: heimdall
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

Execute:
```bash
docker-compose up -d
```

### Opção B: PostgreSQL Local

1. Instale PostgreSQL 16
2. Crie database:
```sql
CREATE DATABASE heimdall;
CREATE USER heimdall WITH PASSWORD 'dev123';
GRANT ALL PRIVILEGES ON DATABASE heimdall TO heimdall;
```

### Configurar appsettings.Development.json

```json
{
  "Database": {
    "UsePostgreSQL": true
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=heimdall;Username=heimdall;Password=dev123"
  }
}
```

Aplicar migrations:
```bash
dotnet ef database update --project src/Heimdall.Infrastructure --startup-project src/Heimdall.Api
```

---

## 📊 Comparação Final

| Aspecto | SQLite | PostgreSQL |
|---------|--------|------------|
| **Persistência** | ❌ Perdida | ✅ Garantida |
| **Performance** | ⚠️ Limitada | ✅ Excelente |
| **Backup** | ❌ Manual | ✅ Automático |
| **Concorrência** | ⚠️ Limitada | ✅ Full ACID |
| **Custo** | ✅ Free | ✅ Free (Render) |
| **Setup** | ✅ Zero | ⚠️ 10 minutos |

---

## 🆘 Troubleshooting

### Erro: Connection failed

**Causa:** Connection string errada

**Solução:**
1. Verifique se copiou a URL completa
2. Use `Internal Database URL` (mais confiável)
3. Certifique-se de que não há espaços/quebras de linha

### Erro: Password authentication failed

**Causa:** Usuário/senha incorretos

**Solução:**
1. Copie novamente do Render Dashboard
2. Verifique se a senha tem caracteres especiais que precisam de encoding

### Erro: Database does not exist

**Causa:** Database não foi criado

**Solução:**
1. Verifique se o banco foi provisionado completamente
2. Aguarde alguns minutos após criação

### Migrations não aplicadas

**Causa:** `Database__AutoMigrate` não configurado

**Solução:**
```bash
# Adicione no Render
Database__AutoMigrate=true
```

### Dados ainda são perdidos

**Causa:** Ainda usando SQLite

**Solução:**
```bash
# Verifique se configurou:
Database__UsePostgreSQL=true
```

---

## 🎉 Sucesso!

Após configuração:
- ✅ Dados persistem entre deploys
- ✅ Backup automático (90 dias no free tier)
- ✅ Performance melhorada
- ✅ Pronto para produção!

---

## 📚 Próximos Passos

- [ ] Configurar backups manuais (export/import)
- [ ] Monitorar uso de storage (512MB limite)
- [ ] Considerar upgrade para Starter ($7/mês = 1GB + 365 dias backup)
- [ ] Implementar índices para otimização de queries

---

**Configuração completa! Sua aplicação agora tem persistência de dados em produção! 🚀**
