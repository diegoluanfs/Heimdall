# Script para gerar chaves JWT usando .NET (sem OpenSSL)
# Funciona em qualquer Windows com .NET 6+ instalado

Write-Host "=== Heimdall - Gerador de Chaves JWT com .NET ===" -ForegroundColor Cyan
Write-Host ""

# Criar diretorio temporario
$scriptDir = $PSScriptRoot
$tempDir = Join-Path -Path $scriptDir -ChildPath "render-keys-temp"

if (-not (Test-Path $tempDir)) {
    New-Item -ItemType Directory -Path $tempDir | Out-Null
}

Write-Host "Gerando chaves RSA 2048-bit usando .NET..." -ForegroundColor Yellow
Write-Host ""

# Codigo C# para gerar chaves RSA
$csharpCode = @"
using System;
using System.Security.Cryptography;
using System.IO;

public class KeyGenerator
{
    public static void GenerateKeys(string privateKeyPath, string publicKeyPath)
    {
        using (var rsa = RSA.Create(2048))
        {
            // Exportar chave privada em formato PEM
            var privateKeyPem = rsa.ExportRSAPrivateKeyPem();
            File.WriteAllText(privateKeyPath, privateKeyPem);
            
            // Exportar chave publica em formato PEM
            var publicKeyPem = rsa.ExportRSAPublicKeyPem();
            File.WriteAllText(publicKeyPath, publicKeyPem);
        }
    }
}
"@

# Compilar e executar o codigo C#
try {
    Add-Type -TypeDefinition $csharpCode -Language CSharp
    
    $privateKeyPath = Join-Path -Path $tempDir -ChildPath "jwt_private.key"
    $publicKeyPath = Join-Path -Path $tempDir -ChildPath "jwt_public.key"
    
    [KeyGenerator]::GenerateKeys($privateKeyPath, $publicKeyPath)
    
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
    Write-Host "1. Acesse: https://dashboard.render.com" -ForegroundColor Yellow
    Write-Host "2. Va no seu servico -> Environment" -ForegroundColor Yellow
    Write-Host "3. Adicione cada variavel acima (Key e Value)" -ForegroundColor Yellow
    Write-Host "4. Salve e aguarde o deploy automatico" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "IMPORTANTE:" -ForegroundColor Red
    Write-Host "- Cole as chaves COMPLETAS (com BEGIN/END)" -ForegroundColor Gray
    Write-Host "- Render aceita valores multi-linha" -ForegroundColor Gray
    Write-Host "- NAO compartilhe a chave privada!" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "Concluido!" -ForegroundColor Green
    
} catch {
    Write-Host "ERRO ao gerar chaves:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Certifique-se de que .NET 6+ esta instalado." -ForegroundColor Yellow
    Write-Host "Verifique com: dotnet --version" -ForegroundColor Gray
    exit 1
}
