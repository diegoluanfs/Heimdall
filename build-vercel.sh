#!/bin/bash

# Script de build do Blazor WebAssembly para Vercel

echo "🔨 Building Heimdall Blazor WebAssembly..."

# Navegar para o projeto Web
cd src/Heimdall.Web

# Publicar em modo Release
dotnet publish -c Release -o ../../vercel-output/wwwroot

# Copiar wwwroot para a raiz do output
cp -r ../../vercel-output/wwwroot/* ../../vercel-output/

echo "✅ Build completed! Output in vercel-output/"
