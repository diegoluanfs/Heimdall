# Script para preparar variaveis de ambiente para Render
# Gera chaves JWT e formata para copiar/colar na Render

Write-Host "=== Heimdall - Gerador de Configuracao para Render ===" -ForegroundColor Cyan
Write-Host ""

# Verificar se OpenSSL esta disponivel
try {
    $null = & openssl version 2>&1
} catch {
    Write-Host "ERRO: OpenSSL nao encontrado!" -ForegroundColor Red
    Write-Host "Instale o OpenSSL para gerar as chaves JWT." -ForegroundColor Yellow
    Write-Host "Windows: https://slproweb.com/products/Win32OpenSSL.html" -ForegroundColor Gray
    Write-Host ""
    exit 1
}

# Criar diretorio temporario
$scriptDir = $PSScriptRoot
$tempDir = Join-Path -Path $scriptDir -ChildPath "render-keys-temp"

if (-not (Test-Path $tempDir)) {
    New-Item -ItemType Directory -Path $tempDir | Out-Null
}

Write-Host "Gerando chaves JWT em: $tempDir" -ForegroundColor Yellow
Write-Host ""

# Gerar chaves
$privateKeyPath = Join-Path -Path $tempDir -ChildPath "jwt_private.key"
$publicKeyPath = Join-Path -Path $tempDir -ChildPath "jwt_public.key"

Write-Host "Gerando chave privada RSA 2048-bit..." -ForegroundColor Cyan
& openssl genpkey -algorithm RSA -out $privateKeyPath -pkeyopt rsa_keygen_bits:2048 2>&1 | Out-Null

Write-Host "Gerando chave publica..." -ForegroundColor Cyan
& openssl rsa -pubout -in $privateKeyPath -out $publicKeyPath 2>&1 | Out-Null

Write-Host "Chaves geradas com sucesso!" -ForegroundColor Green
Write-Host ""

# Ler chaves
$privateKey = Get-Content $privateKeyPath -Raw
$publicKey = Get-Content $publicKeyPath -Raw

# Exibir configuracao completa
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "VARIAVEIS DE AMBIENTE PARA RENDER" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Copie e cole cada variavel abaixo na Render Dashboard:" -ForegroundColor Yellow
Write-Host "(Environment -> Environment Variables -> Add Environment Variable)" -ForegroundColor Gray
Write-Host ""

Write-Host "----------------------------------------------------------------" -ForegroundColor Cyan
Write-Host "Key: ASPNETCORE_ENVIRONMENT" -ForegroundColor White
Write-Host "Value: Production" -ForegroundColor Gray
Write-Host ""

Write-Host "----------------------------------------------------------------" -ForegroundColor Cyan
Write-Host "Key: ASPNETCORE_URLS" -ForegroundColor White
Write-Host "Value: http://+:5000" -ForegroundColor Gray
Write-Host ""

Write-Host "----------------------------------------------------------------" -ForegroundColor Cyan
Write-Host "Key: Database__AutoMigrate" -ForegroundColor White
Write-Host "Value: true" -ForegroundColor Gray
Write-Host ""

Write-Host "----------------------------------------------------------------" -ForegroundColor Cyan
Write-Host "Key: Jwt__Issuer" -ForegroundColor White
Write-Host "Value: https://heimdall-6afc.onrender.com" -ForegroundColor Gray
Write-Host ""

Write-Host "----------------------------------------------------------------" -ForegroundColor Cyan
Write-Host "Key: Jwt__ValidAudiences__0" -ForegroundColor White
Write-Host "Value: heimdall-api" -ForegroundColor Gray
Write-Host ""

Write-Host "----------------------------------------------------------------" -ForegroundColor Cyan
Write-Host "Key: Jwt__PrivateKeyPem" -ForegroundColor White
Write-Host "Value:" -ForegroundColor Gray
Write-Host $privateKey -ForegroundColor DarkGray

Write-Host ""
Write-Host "----------------------------------------------------------------" -ForegroundColor Cyan
Write-Host "Key: Jwt__PublicKeyPem" -ForegroundColor White
Write-Host "Value:" -ForegroundColor Gray
Write-Host $publicKey -ForegroundColor DarkGray

Write-Host ""
Write-Host "----------------------------------------------------------------" -ForegroundColor Cyan
Write-Host "Key: Seed__AdminEmail" -ForegroundColor White
Write-Host "Value: admin@heimdall.com" -ForegroundColor Gray
Write-Host ""

Write-Host "----------------------------------------------------------------" -ForegroundColor Cyan
Write-Host "Key: Seed__AdminPassword" -ForegroundColor White
Write-Host "Value: Admin@123!Prod" -ForegroundColor Gray
Write-Host ""

Write-Host "----------------------------------------------------------------" -ForegroundColor Cyan
Write-Host "Key: Cors__AllowedOrigins__0" -ForegroundColor White
Write-Host "Value: https://heimdall-diego-luans-projects.vercel.app" -ForegroundColor Gray
Write-Host ""

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

# Salvar em arquivo de texto tambem
$outputFile = Join-Path -Path $scriptDir -ChildPath "render-config.txt"
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

Write-Host "Configuracao salva em: $outputFile" -ForegroundColor Green
Write-Host "Chaves salvas em: $tempDir" -ForegroundColor Green
Write-Host ""

Write-Host "PROXIMOS PASSOS:" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Acesse: https://dashboard.render.com/web/srv-XXX/env" -ForegroundColor Yellow
Write-Host "2. Adicione cada variavel acima (Key e Value)" -ForegroundColor Yellow
Write-Host "3. Salve as mudancas" -ForegroundColor Yellow
Write-Host "4. Aguarde o deploy automatico" -ForegroundColor Yellow
Write-Host ""

Write-Host "IMPORTANTE:" -ForegroundColor Red
Write-Host "- As chaves JWT devem ser coladas COMPLETAS (incluindo BEGIN/END)" -ForegroundColor Gray
Write-Host "- A Render aceita valores multi-linha" -ForegroundColor Gray
Write-Host "- NAO compartilhe as chaves privadas!" -ForegroundColor Gray
Write-Host "- Adicione render-keys-temp/ ao .gitignore" -ForegroundColor Gray
Write-Host ""

Write-Host "Concluido!" -ForegroundColor Green

