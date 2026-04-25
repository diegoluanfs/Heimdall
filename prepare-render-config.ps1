# Script para preparar variáveis de ambiente para Render
# Gera chaves JWT e formata para copiar/colar na Render

Write-Host "🔐 Heimdall - Gerador de Configuração para Render" -ForegroundColor Cyan
Write-Host "================================================`n" -ForegroundColor Cyan

# Verificar se OpenSSL está disponível
try {
    $null = & openssl version 2>&1
} catch {
    Write-Host "❌ OpenSSL não encontrado!" -ForegroundColor Red
    Write-Host "Instale o OpenSSL para gerar as chaves JWT." -ForegroundColor Yellow
    Write-Host "Windows: https://slproweb.com/products/Win32OpenSSL.html`n" -ForegroundColor Gray
    exit 1
}

# Criar diretório temporário
$tempDir = Join-Path $PSScriptRoot "render-keys-temp"
if (-not (Test-Path $tempDir)) {
    New-Item -ItemType Directory -Path $tempDir | Out-Null
}

Write-Host "📁 Gerando chaves JWT em: $tempDir`n" -ForegroundColor Yellow

# Gerar chaves
$privateKeyPath = Join-Path $tempDir "jwt_private.key"
$publicKeyPath = Join-Path $tempDir "jwt_public.key"

Write-Host "🔑 Gerando chave privada RSA 2048-bit..." -ForegroundColor Cyan
& openssl genpkey -algorithm RSA -out $privateKeyPath -pkeyopt rsa_keygen_bits:2048 2>&1 | Out-Null

Write-Host "🔓 Gerando chave pública..." -ForegroundColor Cyan
& openssl rsa -pubout -in $privateKeyPath -out $publicKeyPath 2>&1 | Out-Null

Write-Host "✅ Chaves geradas com sucesso!`n" -ForegroundColor Green

# Ler chaves
$privateKey = Get-Content $privateKeyPath -Raw
$publicKey = Get-Content $publicKeyPath -Raw

# Exibir configuração completa
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📋 VARIÁVEIS DE AMBIENTE PARA RENDER" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════`n" -ForegroundColor Cyan

Write-Host "Copie e cole cada variável abaixo na Render Dashboard:" -ForegroundColor Yellow
Write-Host "(Environment → Environment Variables → Add Environment Variable)`n" -ForegroundColor Gray

Write-Host "───────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "Key: ASPNETCORE_ENVIRONMENT" -ForegroundColor White
Write-Host "Value: Production`n" -ForegroundColor Gray

Write-Host "───────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "Key: ASPNETCORE_URLS" -ForegroundColor White
Write-Host "Value: http://+:5000`n" -ForegroundColor Gray

Write-Host "───────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "Key: Database__AutoMigrate" -ForegroundColor White
Write-Host "Value: true`n" -ForegroundColor Gray

Write-Host "───────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "Key: Jwt__Issuer" -ForegroundColor White
Write-Host "Value: https://heimdall-6afc.onrender.com`n" -ForegroundColor Gray

Write-Host "───────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "Key: Jwt__ValidAudiences__0" -ForegroundColor White
Write-Host "Value: heimdall-api`n" -ForegroundColor Gray

Write-Host "───────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "Key: Jwt__PrivateKeyPem" -ForegroundColor White
Write-Host "Value:" -ForegroundColor Gray
Write-Host $privateKey -ForegroundColor DarkGray

Write-Host "`n───────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "Key: Jwt__PublicKeyPem" -ForegroundColor White
Write-Host "Value:" -ForegroundColor Gray
Write-Host $publicKey -ForegroundColor DarkGray

Write-Host "`n───────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "Key: Seed__AdminEmail" -ForegroundColor White
Write-Host "Value: admin@heimdall.com`n" -ForegroundColor Gray

Write-Host "───────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "Key: Seed__AdminPassword" -ForegroundColor White
Write-Host "Value: Admin@123!Prod`n" -ForegroundColor Gray

Write-Host "───────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "Key: Cors__AllowedOrigins__0" -ForegroundColor White
Write-Host "Value: https://heimdall-diego-luans-projects.vercel.app`n" -ForegroundColor Gray

Write-Host "═══════════════════════════════════════════════════════════`n" -ForegroundColor Cyan

# Salvar em arquivo de texto também
$outputFile = Join-Path $PSScriptRoot "render-config.txt"
@"
RENDER ENVIRONMENT VARIABLES
================================

ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
Database__AutoMigrate=true
Jwt__Issuer=https://heimdall-6afc.onrender.com
Jwt__ValidAudiences__0=heimdall-api
Seed__AdminEmail=admin@heimdall.com
Seed__AdminPassword=Admin@123!Prod
Cors__AllowedOrigins__0=https://heimdall-diego-luans-projects.vercel.app

Jwt__PrivateKeyPem:
$privateKey

Jwt__PublicKeyPem:
$publicKey
"@ | Out-File -FilePath $outputFile -Encoding UTF8

Write-Host "💾 Configuração salva em: $outputFile" -ForegroundColor Green
Write-Host "🔑 Chaves salvas em: $tempDir`n" -ForegroundColor Green

Write-Host "📝 PRÓXIMOS PASSOS:" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════`n" -ForegroundColor Cyan
Write-Host "1. Acesse: https://dashboard.render.com/web/srv-XXX/env" -ForegroundColor Yellow
Write-Host "2. Adicione cada variável acima (Key e Value)" -ForegroundColor Yellow
Write-Host "3. Salve as mudanças" -ForegroundColor Yellow
Write-Host "4. Aguarde o deploy automático`n" -ForegroundColor Yellow

Write-Host "⚠️  IMPORTANTE:" -ForegroundColor Red
Write-Host "- As chaves JWT devem ser coladas COMPLETAS (incluindo BEGIN/END)" -ForegroundColor Gray
Write-Host "- A Render aceita valores multi-linha" -ForegroundColor Gray
Write-Host "- NÃO compartilhe as chaves privadas!" -ForegroundColor Gray
Write-Host "- Adicione $tempDir ao .gitignore`n" -ForegroundColor Gray

Write-Host "✅ Concluído!" -ForegroundColor Green
