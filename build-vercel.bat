@echo off
REM Script de build do Blazor WebAssembly para Vercel (Windows)

echo Building Heimdall Blazor WebAssembly...

cd src\Heimdall.Web

dotnet publish -c Release -o ..\..\vercel-output\wwwroot

xcopy /E /I /Y ..\..\vercel-output\wwwroot\wwwroot ..\..\vercel-output\

echo Build completed! Output in vercel-output\
