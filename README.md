# Heimdall 🛡️

Sistema de autenticação e autorização centralizada com JWT RS256, refresh tokens e multi-tenancy.

[![Deploy](https://img.shields.io/badge/deploy-vercel-black)](https://vercel.com)
[![CI/CD](https://github.com/diegoluanfs/Heimdall/actions/workflows/deploy.yml/badge.svg)](https://github.com/diegoluanfs/Heimdall/actions/workflows/deploy.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-512BD4)](https://blazor.net/)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

---

## ✨ Features

- ✅ **Autenticação JWT RS256** - Chaves assimétricas para máxima segurança
- ✅ **Refresh Tokens** - Rotação automática de tokens
- ✅ **Multi-tenancy** - Múltiplos projetos com audiences isolados
- ✅ **Role-based Access Control** - Admin, user, viewer, etc.
- ✅ **Rate Limiting** - 10 req/min (login), 20 req/min (refresh)
- ✅ **Auditoria Completa** - Logs estruturados com IP tracking
- ✅ **FluentValidation** - Validação automática de DTOs
- ✅ **CORS Dinâmico** - Permissivo em dev, restrito em produção
- ✅ **Blazor WebAssembly** - Dashboard administrativo moderno

---

## 🏗️ Arquitetura

```
src/
├── Heimdall.Domain/          # Entidades e interfaces
├── Heimdall.Application/     # Services, DTOs, Validators
├── Heimdall.Infrastructure/  # EF Core, Security, Repositories
├── Heimdall.Api/            # ASP.NET Core Minimal API
└── Heimdall.Web/            # Blazor WebAssembly Admin Dashboard

docs/
├── CODE-REVIEW.md           # Análise completa do código
├── INTEGRATION-GUIDE.md     # Guia para desenvolvedores
├── QUICK-START.md           # Integração em 5 minutos
├── DEPLOY-GUIDE.md          # Deploy Vercel + Azure
└── FIXES-APPLIED.md         # Correções implementadas
```

### Clean Architecture

```
┌────────────────────────────────────────┐
│            Blazor WASM (Web)           │
│   Admin Dashboard + Login UI           │
└───────────────┬────────────────────────┘
                │ HTTP/HTTPS
┌───────────────▼────────────────────────┐
│         ASP.NET Core API               │
│   Minimal APIs + JWT Auth + CORS       │
└───────────────┬────────────────────────┘
                │
┌───────────────▼────────────────────────┐
│          Application Layer             │
│  AuthService, UserService, Validators  │
└───────────────┬────────────────────────┘
                │
┌───────────────▼────────────────────────┐
│        Infrastructure Layer            │
│  EF Core, PBKDF2, RSA JWT Service      │
└───────────────┬────────────────────────┘
                │
┌───────────────▼────────────────────────┐
│           Domain Layer                 │
│  User, Project, RefreshToken entities  │
└────────────────────────────────────────┘
```

---

## 🚀 Quick Start

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Editor de código (Visual Studio 2022, VS Code, Rider)

### 1️⃣ Clonar Repositório

```bash
git clone https://github.com/diegoluanfs/Heimdall.git
cd Heimdall
```

### 2️⃣ Gerar Chaves RSA

```bash
dotnet script GenerateKeys.csx
```

Isso criará `private-key.xml` e `public-key.xml` na pasta `src/Heimdall.Api/`.

### 3️⃣ Executar Migrations

```bash
cd src/Heimdall.Api
dotnet ef database update
```

### 4️⃣ Executar Aplicação

**Opção A: Ambos (API + Web)**
```bash
# Terminal 1 - API
cd src/Heimdall.Api
dotnet run

# Terminal 2 - Web
cd src/Heimdall.Web
dotnet run
```

**Opção B: Apenas API**
```bash
cd src/Heimdall.Api
dotnet run
```

### 5️⃣ Acessar

- **API:** https://localhost:5001
- **Admin Dashboard:** https://localhost:5002

**Credenciais padrão:**
- Email: `admin@heimdall.com`
- Senha: `Admin@123` (⚠️ Alterar em produção!)

---

## 📦 Deploy

### Frontend (Blazor WASM) → Vercel

```bash
# 1. Build
dotnet publish src/Heimdall.Web -c Release -o vercel-output/wwwroot

# 2. Deploy
vercel --prod
```

### Backend (API) → Azure

```bash
# 1. Criar Azure Web App
az webapp create \
  --name heimdall-api \
  --resource-group heimdall-rg \
  --plan heimdall-plan \
  --runtime "DOTNET|8.0"

# 2. Deploy
cd src/Heimdall.Api
dotnet publish -c Release -o ./publish
cd publish
zip -r ../api.zip .
az webapp deployment source config-zip \
  --name heimdall-api \
  --resource-group heimdall-rg \
  --src ../api.zip
```

**Guia completo:** [docs/DEPLOY-GUIDE.md](docs/DEPLOY-GUIDE.md)

---

## 📚 Documentação

### Para Desenvolvedores (Integração)

- **[INTEGRATION-GUIDE.md](docs/INTEGRATION-GUIDE.md)** - Guia completo de integração
  - API documentation
  - Exemplos em JavaScript, C#, Python, PHP
  - Security best practices
  - FAQ

- **[QUICK-START.md](docs/QUICK-START.md)** - Integração em 5 minutos
  - Código pronto para copiar/colar
  - Troubleshooting rápido

### Para DevOps

- **[DEPLOY-GUIDE.md](docs/DEPLOY-GUIDE.md)** - Deploy Vercel + Azure/Railway/Render
  - Configuração de CORS
  - Environment variables
  - CI/CD com GitHub Actions

### Para Desenvolvedores do Heimdall

- **[CODE-REVIEW.md](docs/CODE-REVIEW.md)** - Análise completa (14 issues identificados)
- **[FIXES-APPLIED.md](docs/FIXES-APPLIED.md)** - Correções implementadas (P0, P1, P3)

---

## 🔐 Segurança

### JWT RS256

- **Algoritmo:** RSA-SHA256 (chaves assimétricas 2048-bit)
- **Access Token:** 5 minutos de validade
- **Refresh Token:** 7 dias com rotação automática

### Password Hashing

- **Algoritmo:** PBKDF2-HMAC-SHA256
- **Iterations:** 310,000 (NIST compliant 2024)
- **Salt:** 16 bytes aleatórios

### Rate Limiting

| Endpoint | Limite |
|----------|--------|
| `/api/login` | 10 req/min |
| `/api/refresh` | 20 req/min |

### Security Headers

- ✅ HSTS (Strict-Transport-Security)
- ✅ CSP (Content-Security-Policy)
- ✅ X-Frame-Options
- ✅ X-Content-Type-Options

---

## 🛠️ Tecnologias

### Backend

- **Framework:** ASP.NET Core 8.0 (Minimal APIs)
- **ORM:** Entity Framework Core 8
- **Database:** SQLite (dev), SQL Server / PostgreSQL (prod)
- **Validation:** FluentValidation 12.1.1
- **Logging:** Microsoft.Extensions.Logging

### Frontend

- **Framework:** Blazor WebAssembly
- **UI:** Bootstrap 5
- **HTTP:** HttpClient + JSON

### Security

- **JWT:** System.IdentityModel.Tokens.Jwt
- **Crypto:** System.Security.Cryptography (RSA, PBKDF2)

---

## 📊 API Endpoints

### Autenticação

| Método | Endpoint | Auth | Rate Limit | Descrição |
|--------|----------|------|------------|-----------|
| POST | `/api/login` | ❌ | 10/min | Login e obter tokens |
| POST | `/api/refresh` | ❌ | 20/min | Renovar access token |
| POST | `/api/revoke` | ✅ | - | Logout (revogar refresh token) |

### Administração

| Método | Endpoint | Auth | Descrição |
|--------|----------|------|-----------|
| POST | `/api/users` | ✅ Admin | Criar usuário |
| POST | `/api/projects` | ✅ Admin | Criar projeto |

---

## 🧪 Testes

```bash
# Executar todos os testes
dotnet test

# Com cobertura de código
dotnet test /p:CollectCoverage=true
```

---

## 🤝 Contribuindo

1. Fork o repositório
2. Crie uma branch (`git checkout -b feature/MinhaFeature`)
3. Commit suas mudanças (`git commit -m 'feat: adicionar MinhaFeature'`)
4. Push para a branch (`git push origin feature/MinhaFeature`)
5. Abra um Pull Request

**Commits seguem [Conventional Commits](https://www.conventionalcommits.org/):**
- `feat:` Nova funcionalidade
- `fix:` Correção de bug
- `docs:` Documentação
- `refactor:` Refatoração de código
- `test:` Adição de testes

---

## 📝 Roadmap

- [ ] Health checks endpoint (`/health`)
- [ ] OpenAPI/Swagger documentation
- [ ] Password reset flow
- [ ] Email verification
- [ ] Two-factor authentication (2FA)
- [ ] OAuth integration (Google, GitHub)
- [ ] User self-registration
- [ ] Password change endpoint
- [ ] Audit log API
- [ ] Rate limiting por usuário
- [ ] Blacklist de refresh tokens

---

## 📄 Licença

Este projeto está licenciado sob a [MIT License](LICENSE).

---

## 👨‍💻 Autor

**Diego Luan**
- GitHub: [@diegoluanfs](https://github.com/diegoluanfs)
- Email: diego@exemplo.com

---

## 🙏 Agradecimentos

- [.NET Foundation](https://dotnetfoundation.org/)
- [FluentValidation](https://fluentvalidation.net/)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [Blazor](https://blazor.net/)

---

**⭐ Se este projeto foi útil, considere dar uma estrela!**
