@echo off
echo ================================
echo   Heimdall - Deploy Vercel
echo ================================
echo.

echo [1/4] Verificando Vercel CLI...
where vercel >nul 2>&1
if %errorlevel% neq 0 (
    echo Vercel CLI nao encontrado. Instalando...
    npm install -g vercel
)

echo.
echo [2/4] Navegando para pasta de deploy...
cd /d "%~dp0vercel-deploy\wwwroot"

echo.
echo [3/4] Fazendo login na Vercel...
echo (Uma janela do navegador vai abrir)
vercel login

echo.
echo [4/4] Fazendo deploy para producao...
vercel --prod

echo.
echo ================================
echo   Deploy Concluido!
echo ================================
pause
