# Workflows do GitHub Actions

## 📁 Arquivos

### `deploy.yml`
Workflow principal de CI/CD que:
- ✅ Executa testes em todos os pushes e PRs
- ✅ Deploy automático para Vercel (produção) em push para `main`
- ✅ Deploy de preview para Vercel em Pull Requests

## 🔧 Configuração Necessária

Configure estes secrets em **Settings → Secrets and variables → Actions**:

| Secret | Descrição | Como obter |
|--------|-----------|------------|
| `VERCEL_TOKEN` | Token de acesso da Vercel | [vercel.com/account/tokens](https://vercel.com/account/tokens) |
| `VERCEL_ORG_ID` | ID da organização/usuário | `.vercel/project.json` após `vercel link` |
| `VERCEL_PROJECT_ID` | ID do projeto | `.vercel/project.json` após `vercel link` |

## 📚 Documentação Completa

Veja [docs/GITHUB-ACTIONS-SETUP.md](../../docs/GITHUB-ACTIONS-SETUP.md) para instruções detalhadas.
