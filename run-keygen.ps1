# Heimdall - JWT Key Generator

Write-Host "=== Heimdall - Gerador de Chaves JWT ===" -ForegroundColor Cyan
Write-Host ""

# Verificar .NET
try {
    $dotnetVersion = dotnet --version
    Write-Host "OK - .NET detectado: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "ERRO: .NET nao encontrado!" -ForegroundColor Red
    Write-Host "Instale .NET de: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Gerando chaves RSA 2048-bit..." -ForegroundColor Yellow

# Caminhos para as chaves
$privateKeyPath = "jwt_private.key"
$publicKeyPath = "jwt_public.key"

# Executar gerador C#
try {
    $csharpFile = Join-Path $PSScriptRoot "tools\GenerateKeys.cs"

    if (-not (Test-Path $csharpFile)) {
        Write-Host "ERRO: Arquivo $csharpFile nao encontrado!" -ForegroundColor Red
        exit 1
    }

    # Criar projeto temporario
    $tempDir = Join-Path $env:TEMP "HeimdallKeyGen"
    if (Test-Path $tempDir) { Remove-Item -Recurse -Force $tempDir }
    New-Item -ItemType Directory -Path $tempDir | Out-Null

    # Copiar arquivo C#
    Copy-Item $csharpFile -Destination (Join-Path $tempDir "Program.cs")

    # Criar .csproj
    $csprojContent = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>'
    Set-Content -Path (Join-Path $tempDir "KeyGen.csproj") -Value $csprojContent

    # Caminhos absolutos
    $absolutePrivateKeyPath = Join-Path $PSScriptRoot $privateKeyPath
    $absolutePublicKeyPath = Join-Path $PSScriptRoot $publicKeyPath

    # Executar
    Push-Location $tempDir
    dotnet run -- "$absolutePrivateKeyPath" "$absolutePublicKeyPath" 2>&1 | Out-Null
    Pop-Location

    # Limpar
    Remove-Item -Recurse -Force $tempDir

    if (-not (Test-Path $privateKeyPath) -or -not (Test-Path $publicKeyPath)) {
        Write-Host "ERRO: Falha ao gerar chaves!" -ForegroundColor Red
        exit 1
    }

    Write-Host "OK - Chaves geradas com sucesso!" -ForegroundColor Green

} catch {
    Write-Host "ERRO ao executar gerador:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Lendo chaves..." -ForegroundColor Yellow

# Ler chaves
$privateKeyPem = Get-Content $privateKeyPath -Raw
$publicKeyPem = Get-Content $publicKeyPath -Raw

Write-Host "OK - Chaves carregadas" -ForegroundColor Green

# Criar configuracao
$linha = "============================================"
$renderConfig = @"
$linha
VARIAVEIS DE AMBIENTE PARA RENDER
$linha

ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
Database__AutoMigrate=true
Jwt__Issuer=https://heimdall-6afc.onrender.com
Jwt__ValidAudiences__0=heimdall-api

Jwt__PrivateKeyPem=$privateKeyPem

Jwt__PublicKeyPem=$publicKeyPem

Seed__AdminEmail=admin@heimdall.com
Seed__AdminPassword=Admin@123!Prod
Cors__AllowedOrigins__0=https://heimdall-diego-luans-projects.vercel.app

$linha
"@

# Salvar
$configFile = "render-config.txt"
Set-Content -Path $configFile -Value $renderConfig

Write-Host ""
Write-Host $linha -ForegroundColor Cyan
Write-Host "CONFIGURACAO SALVA EM: $configFile" -ForegroundColor Green
Write-Host $linha -ForegroundColor Cyan
Write-Host ""
Write-Host "PROXIMOS PASSOS:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Abra o arquivo: $configFile" -ForegroundColor White
Write-Host "2. Acesse: https://dashboard.render.com" -ForegroundColor White
Write-Host "3. Selecione: heimdall-api" -ForegroundColor White
Write-Host "4. Va em: Environment > Environment Variables" -ForegroundColor White
Write-Host "5. Copie cada variavel do arquivo para o Render" -ForegroundColor White
Write-Host "6. Clique em: Save Changes" -ForegroundColor White
Write-Host ""
Write-Host "ARQUIVOS GERADOS:" -ForegroundColor Yellow
Write-Host "  - jwt_private.key" -ForegroundColor White
Write-Host "  - jwt_public.key" -ForegroundColor White
Write-Host "  - render-config.txt" -ForegroundColor White
Write-Host ""
Write-Host "IMPORTANTE: Mantenha as chaves seguras!" -ForegroundColor Red
Write-Host ""
