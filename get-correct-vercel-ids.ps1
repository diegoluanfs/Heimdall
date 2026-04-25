$token = "vcp_2k46T7go9gsFh36AX4JsPqVR5Zk30ab05YiAckRKfnY1I8mm134dRGbq"

Write-Host "================================" -ForegroundColor Cyan
Write-Host "  Vercel - Obter IDs Corretos" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

$headers = @{
    "Authorization" = "Bearer $token"
}

try {
    # 1. Obter informações do usuário
    Write-Host "[1/3] Buscando informacoes do usuario..." -ForegroundColor Yellow
    $user = Invoke-RestMethod -Uri "https://api.vercel.com/v2/user" -Headers $headers
    
    Write-Host "  Usuario: $($user.user.username)" -ForegroundColor Green
    Write-Host "  Email: $($user.user.email)" -ForegroundColor Green
    Write-Host "  User ID: $($user.user.uid)" -ForegroundColor Green
    Write-Host ""
    
    # 2. Obter lista de projetos
    Write-Host "[2/3] Buscando projeto 'heimdall'..." -ForegroundColor Yellow
    $projects = Invoke-RestMethod -Uri "https://api.vercel.com/v9/projects" -Headers $headers
    
    $heimdallProject = $projects.projects | Where-Object { $_.name -eq "heimdall" }
    
    if ($heimdallProject) {
        Write-Host "  Projeto encontrado!" -ForegroundColor Green
        Write-Host ""
        
        # 3. Mostrar IDs corretos
        Write-Host "[3/3] IDS CORRETOS PARA O GITHUB:" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "================================" -ForegroundColor Cyan
        Write-Host "  COPIE ESTES VALORES:" -ForegroundColor Cyan
        Write-Host "================================" -ForegroundColor Cyan
        Write-Host ""
        
        Write-Host "VERCEL_ORG_ID (accountId):" -ForegroundColor White
        Write-Host "  $($heimdallProject.accountId)" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "VERCEL_PROJECT_ID:" -ForegroundColor White
        Write-Host "  $($heimdallProject.id)" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "VERCEL_TOKEN (ja configurado):" -ForegroundColor White
        Write-Host "  $token" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "================================" -ForegroundColor Cyan
        Write-Host "  DETALHES DO PROJETO:" -ForegroundColor Cyan
        Write-Host "================================" -ForegroundColor Cyan
        Write-Host "  Nome: $($heimdallProject.name)" -ForegroundColor White
        Write-Host "  Framework: $($heimdallProject.framework)" -ForegroundColor White
        if ($heimdallProject.targets.production.alias) {
            Write-Host "  URL: https://$($heimdallProject.targets.production.alias[0])" -ForegroundColor White
        }
        Write-Host ""
        
        Write-Host "================================" -ForegroundColor Yellow
        Write-Host "  PROXIMOS PASSOS:" -ForegroundColor Yellow
        Write-Host "================================" -ForegroundColor Yellow
        Write-Host "1. Acesse: https://github.com/diegoluanfs/Heimdall/settings/secrets/actions" -ForegroundColor White
        Write-Host "2. Atualize VERCEL_ORG_ID com: $($heimdallProject.accountId)" -ForegroundColor White
        Write-Host "3. Atualize VERCEL_PROJECT_ID com: $($heimdallProject.id)" -ForegroundColor White
        Write-Host "4. Faca um novo commit para testar" -ForegroundColor White
        Write-Host ""
        
    } else {
        Write-Host "  Projeto 'heimdall' NAO encontrado!" -ForegroundColor Red
        Write-Host ""
        Write-Host "Projetos disponiveis:" -ForegroundColor Yellow
        foreach ($p in $projects.projects) {
            Write-Host "  - $($p.name) (ID: $($p.id))" -ForegroundColor White
        }
    }
    
} catch {
    Write-Host ""
    Write-Host "Erro ao buscar informacoes: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Possiveis causas:" -ForegroundColor Yellow
    Write-Host "1. Token invalido ou expirado" -ForegroundColor White
    Write-Host "2. Token sem permissoes suficientes" -ForegroundColor White
    Write-Host "3. Problema de conexao com a API da Vercel" -ForegroundColor White
    Write-Host ""
}

Write-Host ""
Read-Host "Pressione Enter para fechar"
