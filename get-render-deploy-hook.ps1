# Script para obter Deploy Hook da Render via API
# Requisito: Token de API da Render (https://dashboard.render.com/u/settings#api-keys)

param(
    [string]$RenderApiKey = ""
)

Write-Host "🚀 Render Deploy Hook Retriever" -ForegroundColor Cyan
Write-Host "================================`n" -ForegroundColor Cyan

# Solicitar API Key se não foi fornecida
if ([string]::IsNullOrWhiteSpace($RenderApiKey)) {
    Write-Host "📝 Para obter sua API Key:" -ForegroundColor Yellow
    Write-Host "1. Acesse: https://dashboard.render.com/u/settings#api-keys" -ForegroundColor Gray
    Write-Host "2. Clique em 'Create API Key'" -ForegroundColor Gray
    Write-Host "3. Copie a chave gerada`n" -ForegroundColor Gray
    
    $RenderApiKey = Read-Host "Cole sua Render API Key aqui"
}

if ([string]::IsNullOrWhiteSpace($RenderApiKey)) {
    Write-Host "❌ API Key não fornecida. Abortando." -ForegroundColor Red
    exit 1
}

# Listar todos os serviços
Write-Host "`n📡 Buscando serviços na Render...`n" -ForegroundColor Cyan

try {
    $headers = @{
        "Authorization" = "Bearer $RenderApiKey"
        "Content-Type" = "application/json"
    }
    
    $response = Invoke-RestMethod -Uri "https://api.render.com/v1/services?limit=20" -Headers $headers -Method Get
    
    if ($response.Count -eq 0) {
        Write-Host "⚠️  Nenhum serviço encontrado na sua conta Render." -ForegroundColor Yellow
        Write-Host "Crie um serviço primeiro em: https://dashboard.render.com`n" -ForegroundColor Gray
        exit 0
    }
    
    Write-Host "📋 Serviços encontrados:`n" -ForegroundColor Green
    
    $services = @()
    $index = 1
    
    foreach ($service in $response) {
        $services += $service.service
        
        Write-Host "[$index] $($service.service.name)" -ForegroundColor White
        Write-Host "    ID: $($service.service.id)" -ForegroundColor Gray
        Write-Host "    Tipo: $($service.service.type)" -ForegroundColor Gray
        Write-Host "    URL: $($service.service.serviceDetails.url)" -ForegroundColor Gray
        Write-Host ""
        
        $index++
    }
    
    # Solicitar seleção
    Write-Host "`n❓ Qual serviço deseja usar? (digite o número): " -ForegroundColor Yellow -NoNewline
    $selection = Read-Host
    
    if ([int]$selection -lt 1 -or [int]$selection -gt $services.Count) {
        Write-Host "❌ Seleção inválida." -ForegroundColor Red
        exit 1
    }
    
    $selectedService = $services[[int]$selection - 1]
    
    Write-Host "`n✅ Serviço selecionado: $($selectedService.name)`n" -ForegroundColor Green
    
    # Obter detalhes do serviço incluindo deploy hook
    Write-Host "🔍 Buscando Deploy Hook...`n" -ForegroundColor Cyan
    
    $serviceDetails = Invoke-RestMethod -Uri "https://api.render.com/v1/services/$($selectedService.id)" -Headers $headers -Method Get
    
    # Construir Deploy Hook URL
    $deployHookUrl = "https://api.render.com/deploy/$($selectedService.id)?key=$($serviceDetails.service.serviceDetails.deployHookId)"
    
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "🎯 RENDER DEPLOY HOOK URL" -ForegroundColor Green
    Write-Host "═══════════════════════════════════════════════════════════`n" -ForegroundColor Cyan
    
    Write-Host $deployHookUrl -ForegroundColor White
    
    Write-Host "`n═══════════════════════════════════════════════════════════`n" -ForegroundColor Cyan
    
    # Copiar para clipboard (se disponível)
    try {
        Set-Clipboard -Value $deployHookUrl
        Write-Host "✅ URL copiada para a área de transferência!`n" -ForegroundColor Green
    } catch {
        Write-Host "⚠️  Não foi possível copiar automaticamente. Copie manualmente acima.`n" -ForegroundColor Yellow
    }
    
    # Instruções
    Write-Host "📝 PRÓXIMOS PASSOS:" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════`n" -ForegroundColor Cyan
    
    Write-Host "1. Acesse:" -ForegroundColor Yellow
    Write-Host "   https://github.com/diegoluanfs/Heimdall/settings/secrets/actions`n" -ForegroundColor Gray
    
    Write-Host "2. Clique em 'New repository secret'`n" -ForegroundColor Yellow
    
    Write-Host "3. Configure:" -ForegroundColor Yellow
    Write-Host "   Name:  RENDER_DEPLOY_HOOK_URL" -ForegroundColor Gray
    Write-Host "   Value: [Cole a URL acima]`n" -ForegroundColor Gray
    
    Write-Host "4. Salve e faça um commit para testar:" -ForegroundColor Yellow
    Write-Host "   git commit -m `"test: trigger Render deploy`"" -ForegroundColor Gray
    Write-Host "   git push origin main`n" -ForegroundColor Gray
    
    Write-Host "═══════════════════════════════════════════════════════════`n" -ForegroundColor Cyan
    
    Write-Host "🎉 Configuração concluída!" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Erro ao conectar com a API da Render:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host "`nVerifique:" -ForegroundColor Yellow
    Write-Host "- Se a API Key está correta" -ForegroundColor Gray
    Write-Host "- Se você tem permissões na conta Render" -ForegroundColor Gray
    Write-Host "- Sua conexão com a internet`n" -ForegroundColor Gray
    exit 1
}
