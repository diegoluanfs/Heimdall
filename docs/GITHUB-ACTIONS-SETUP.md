# GitHub Actions CI/CD - Guia de Configuração

## 🎯 Visão Geral

Este projeto usa **GitHub Actions** para CI/CD profissional com deploy automático na Vercel.

### Fluxo de Trabalho

```
┌──────────────────────────────────────────────────────────┐
│  Push para main ou Pull Request                         │
└────────────────┬─────────────────────────────────────────┘
                 │
         ┌───────▼────────┐
         │  1. Build      │
         │  2. Test       │
         └───────┬────────┘
                 │
         ┌───────▼────────────────────────┐
         │  Decisão baseada em evento     │
         └───┬───────────────────┬────────┘
             │                   │
    ┌────────▼────────┐   ┌─────▼──────────┐
    │  Push to main   │   │  Pull Request  │
    │  → Production   │   │  → Preview     │
    └────────┬────────┘   └─────┬──────────┘
             │                   │
    ┌────────▼────────┐   ┌─────▼──────────┐
    │ Vercel (Prod)   │   │ Vercel (Preview)│
    │ heimdall.app    │   │ pr-123.app     │
    └─────────────────┘   └────────────────┘
```

---

## 🔐 Configurar Secrets do GitHub

### Passo 1: Obter Tokens da Vercel

1. **Acesse** [vercel.com/account/tokens](https://vercel.com/account/tokens)
2. **Create Token**
3. **Nome:** `GitHub Actions`
4. **Scope:** Full Account
5. **Copy** o token (só aparece uma vez!)

### Passo 2: Obter IDs do Projeto

#### Opção A: Via Vercel CLI

```bash
# Instalar Vercel CLI
npm install -g vercel

# Login
vercel login

# Link ao projeto (ou criar novo)
vercel link

# Copiar IDs do arquivo .vercel/project.json
cat .vercel/project.json
```

Você verá algo assim:
```json
{
  "orgId": "team_XXXXXXXXXXXXXXXXXXXXX",
  "projectId": "prj_XXXXXXXXXXXXXXXXXXXXXXXXXX"
}
```

#### Opção B: Via Vercel Dashboard

1. **Acesse** [vercel.com/dashboard](https://vercel.com/dashboard)
2. **Settings** do seu projeto
3. **General** → Copie o **Project ID**
4. **Team Settings** → Copie o **Team ID** (ou User ID)

### Passo 3: Adicionar Secrets no GitHub

1. **Acesse** seu repositório no GitHub
2. **Settings** → **Secrets and variables** → **Actions**
3. **New repository secret**

Adicione estes 3 secrets:

| Name | Value | Onde obter |
|------|-------|------------|
| `VERCEL_TOKEN` | `xxxx...` | Passo 1 |
| `VERCEL_ORG_ID` | `team_xxx` ou `user_xxx` | Passo 2 (.vercel/project.json → orgId) |
| `VERCEL_PROJECT_ID` | `prj_xxx` | Passo 2 (.vercel/project.json → projectId) |

**Exemplo:**
```
VERCEL_TOKEN=v1_abc123def456...
VERCEL_ORG_ID=team_1A2B3C4D5E6F
VERCEL_PROJECT_ID=prj_xyz789abc456
```

---

## 🚀 Como Funciona

### Deploy Automático em Produção

Toda vez que você fizer **push para `main`**:

```bash
git add .
git commit -m "feat: nova funcionalidade"
git push origin main
```

**GitHub Actions vai:**
1. ✅ Fazer checkout do código
2. ✅ Restaurar dependências .NET
3. ✅ Build da solution
4. ✅ Executar testes
5. ✅ Build do Blazor WebAssembly
6. ✅ Deploy na Vercel (produção)
7. ✅ Comentar na commit com URL de deploy

### Preview Deploy em Pull Requests

Quando você criar um **Pull Request**:

```bash
git checkout -b feature/minha-feature
git add .
git commit -m "feat: adicionar feature"
git push origin feature/minha-feature
# Criar PR no GitHub
```

**GitHub Actions vai:**
1. ✅ Build e testes (mesmo do produção)
2. ✅ Deploy em URL de preview única
3. ✅ Comentar no PR com URL de preview
4. ✅ Atualizar preview a cada novo commit

**URL de preview:** `https://heimdall-git-feature-minha-feature-USERNAME.vercel.app`

---

## 📊 Monitoramento

### GitHub Actions

**Ver status dos deploys:**
1. Repositório → **Actions**
2. Selecionar workflow run
3. Ver logs detalhados

### Vercel Dashboard

**Ver deploys:**
1. [vercel.com/dashboard](https://vercel.com/dashboard)
2. Selecionar projeto
3. Ver histórico de deployments

---

## 🔧 Configurações Avançadas

### Ambientes GitHub

Os workflows usam **GitHub Environments** para proteção:

**Configurar proteção de produção:**
1. Settings → Environments → **production**
2. **Deployment protection rules:**
   - ✅ Required reviewers (opcional)
   - ✅ Wait timer (opcional)

### Cache de Dependências

O workflow usa cache do NuGet para acelerar builds:

```yaml
- uses: actions/cache@v3
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
```

**Limpar cache:**
- GitHub → Actions → Caches → Delete

### Notificações

**Receber notificações de deploy:**
1. GitHub → Settings → Notifications
2. Ativar **Actions**

---

## ⚙️ Customizações

### Modificar condições de deploy

Editar `.github/workflows/deploy.yml`:

```yaml
# Apenas em tags
if: startsWith(github.ref, 'refs/tags/v')

# Apenas em arquivos específicos
paths:
  - 'src/Heimdall.Web/**'
  - 'src/Heimdall.Application/**'
```

### Adicionar etapas de build

```yaml
- name: Build Documentation
  run: dotnet tool run docfx metadata

- name: Run Security Scan
  uses: aquasecurity/trivy-action@master
```

### Deploy condicional baseado em branch

```yaml
deploy-staging:
  if: github.ref == 'refs/heads/develop'
  # Deploy para staging
```

---

## 🐛 Troubleshooting

### Erro: "VERCEL_TOKEN not found"

**Solução:** Verificar se o secret está configurado corretamente em Settings → Secrets

### Erro: "Build failed: dotnet not found"

**Solução:** O workflow já instala .NET 8. Se persistir, verificar versão em `env.DOTNET_VERSION`

### Erro: "Project not found on Vercel"

**Solução:** 
1. Criar projeto na Vercel manualmente primeiro
2. Ou executar `vercel link` localmente
3. Copiar IDs do `.vercel/project.json`

### Deploy muito lento

**Solução:** 
1. Verificar se cache está funcionando (Actions → Caches)
2. Considerar usar self-hosted runner

---

## 📝 Checklist de Setup

- [ ] Criar conta na Vercel
- [ ] Criar projeto na Vercel (ou executar `vercel link`)
- [ ] Gerar VERCEL_TOKEN
- [ ] Obter VERCEL_ORG_ID e VERCEL_PROJECT_ID
- [ ] Adicionar os 3 secrets no GitHub
- [ ] Fazer push para `main` e verificar Actions
- [ ] Criar PR para testar preview deploy
- [ ] Configurar ambiente de produção (opcional)
- [ ] Configurar notificações (opcional)

---

## 🎯 Comparação: Manual vs CI/CD

| Aspecto | Deploy Manual | CI/CD (GitHub Actions) |
|---------|---------------|------------------------|
| **Velocidade** | 5-10 min | 2-3 min (automático) |
| **Consistência** | ❌ Variável | ✅ Sempre igual |
| **Testes** | ❌ Manual | ✅ Automático |
| **Preview** | ❌ Não | ✅ Sim (em PRs) |
| **Rollback** | ⚠️ Difícil | ✅ Fácil (revert commit) |
| **Auditoria** | ❌ Limitada | ✅ Completa |
| **Profissional** | ⚠️ Hobby | ✅ Enterprise |

---

## 🚀 Próximos Passos

Após configurar CI/CD:

1. ✅ **Adicionar testes unitários** (já configurado no workflow)
2. ✅ **Configurar Environments** para proteção de produção
3. ⬜ **Adicionar Lighthouse CI** para performance
4. ⬜ **Adicionar SonarCloud** para qualidade de código
5. ⬜ **Configurar Dependabot** para atualizações automáticas
6. ⬜ **Adicionar badges** no README com status do build

---

## 📚 Recursos

- [GitHub Actions Docs](https://docs.github.com/en/actions)
- [Vercel GitHub Integration](https://vercel.com/docs/git/vercel-for-github)
- [.NET CI/CD Best Practices](https://docs.microsoft.com/en-us/dotnet/devops/)

---

**Versão:** 1.0  
**Atualizado:** 2024-04-24
