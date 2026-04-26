#!/usr/bin/env pwsh
# Test script for local Heimdall API login

Write-Host "🔧 Testing Heimdall Local Development Environment" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$API_BASE_URL = "http://localhost:5231"
$ADMIN_EMAIL = "admin@heimdall.com"
$ADMIN_PASSWORD = "Admin@123!Dev"
$AUDIENCE = "heimdall-api"

# Test 1: Health Check
Write-Host "1️⃣ Testing Health Endpoint..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$API_BASE_URL/health" -Method GET -TimeoutSec 5
    Write-Host "   ✅ Health: $($health.status)" -ForegroundColor Green
    Write-Host "   📅 Environment: $($health.environment)" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "   ❌ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   💡 Make sure the API is running on port 5231" -ForegroundColor Yellow
    Write-Host "   Run: cd src/Heimdall.Api && dotnet run" -ForegroundColor Yellow
    exit 1
}

# Test 2: Login
Write-Host "2️⃣ Testing Login Endpoint..." -ForegroundColor Yellow
$loginBody = @{
    email = $ADMIN_EMAIL
    password = $ADMIN_PASSWORD
    audience = $AUDIENCE
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod `
        -Uri "$API_BASE_URL/api/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody `
        -TimeoutSec 10
    
    Write-Host "   ✅ Login successful!" -ForegroundColor Green
    Write-Host "   🔑 Access Token: $($loginResponse.accessToken.Substring(0,50))..." -ForegroundColor Green
    Write-Host "   ⏱️  Expires In: $($loginResponse.expiresIn) seconds" -ForegroundColor Green
    Write-Host "   🔄 Refresh Token: $($loginResponse.refreshToken.Substring(0,50))..." -ForegroundColor Green
    Write-Host ""
    
    # Save tokens for refresh test
    $script:accessToken = $loginResponse.accessToken
    $script:refreshToken = $loginResponse.refreshToken
    
} catch {
    Write-Host "   ❌ Login failed!" -ForegroundColor Red
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "   Status Code: $statusCode" -ForegroundColor Red
        
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $errorDetails = $reader.ReadToEnd()
        $reader.Close()
        Write-Host "   Error Details: $errorDetails" -ForegroundColor Red
    } else {
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "   💡 Troubleshooting:" -ForegroundColor Yellow
    Write-Host "   - Check credentials in appsettings.Development.json" -ForegroundColor Yellow
    Write-Host "   - Verify JWT keys are in PKCS#8 format (BEGIN PRIVATE KEY)" -ForegroundColor Yellow
    Write-Host "   - Review logs for detailed error messages" -ForegroundColor Yellow
    exit 1
}

# Test 3: Refresh Token
Write-Host "3️⃣ Testing Refresh Token Endpoint..." -ForegroundColor Yellow
$refreshBody = @{
    refreshToken = $script:refreshToken
    audience = $AUDIENCE
} | ConvertTo-Json

try {
    $refreshResponse = Invoke-RestMethod `
        -Uri "$API_BASE_URL/api/refresh" `
        -Method POST `
        -ContentType "application/json" `
        -Body $refreshBody `
        -TimeoutSec 10
    
    Write-Host "   ✅ Token refresh successful!" -ForegroundColor Green
    Write-Host "   🔑 New Access Token: $($refreshResponse.accessToken.Substring(0,50))..." -ForegroundColor Green
    Write-Host "   🔄 New Refresh Token: $($refreshResponse.refreshToken.Substring(0,50))..." -ForegroundColor Green
    Write-Host ""
    
} catch {
    Write-Host "   ❌ Refresh failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
}

# Summary
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "✅ All critical tests passed!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Configuration Summary:" -ForegroundColor Cyan
Write-Host "   API URL: $API_BASE_URL" -ForegroundColor White
Write-Host "   Admin Email: $ADMIN_EMAIL" -ForegroundColor White
Write-Host "   Admin Password: $ADMIN_PASSWORD" -ForegroundColor White
Write-Host "   Audience: $AUDIENCE" -ForegroundColor White
Write-Host ""
Write-Host "📚 Next Steps:" -ForegroundColor Cyan
Write-Host "   1. Test the Blazor frontend: cd src/Heimdall.Web && dotnet run" -ForegroundColor White
Write-Host "   2. Open browser: http://localhost:5173" -ForegroundColor White
Write-Host "   3. See full guide: LOCAL-TESTING.md" -ForegroundColor White
Write-Host ""
