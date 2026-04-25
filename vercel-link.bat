@echo off
echo ================================
echo   Vercel Link - Obter IDs
echo ================================
echo.

echo [1/3] Fazendo login na Vercel...
echo (Uma janela do navegador vai abrir)
npx vercel login

echo.
echo [2/3] Linkando projeto...
echo.
echo Escolha as opcoes:
echo   - Set up and deploy? Y
echo   - Which scope? Selecione sua conta
echo   - Link to existing project? Y (se existir) ou N (criar novo)
echo   - Project name? heimdall
echo.
npx vercel link

echo.
echo [3/3] Mostrando IDs do projeto...
echo.
if exist .vercel\project.json (
    type .vercel\project.json
    echo.
    echo ================================
    echo   COPIE ESTES VALORES:
    echo ================================
    echo.
    echo Abra o arquivo .vercel\project.json
    echo Copie orgId e projectId
    echo Atualize os secrets no GitHub!
    echo.
) else (
    echo Arquivo .vercel\project.json nao encontrado!
    echo O link falhou ou foi cancelado.
)

pause
