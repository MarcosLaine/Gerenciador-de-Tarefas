# Script PowerShell para criar migrations do Entity Framework
# Execute da raiz do projeto: .\LembretesApi\Scripts\create-migrations.ps1
# OU de dentro de LembretesApi: .\Scripts\create-migrations.ps1

Write-Host "🔧 Criando migrations do Entity Framework..." -ForegroundColor Cyan

# Determina o diretório base (raiz do projeto ou LembretesApi)
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$apiPath = Split-Path -Parent $scriptPath

# Se estamos em Scripts, subimos para LembretesApi
if ($scriptPath -like "*Scripts") {
    $apiPath = $scriptPath.Replace("\Scripts", "")
}

Write-Host "📁 Diretório da API: $apiPath" -ForegroundColor Gray

# Vai para a pasta da API
Push-Location $apiPath

# Remove migrations antigas (se existirem)
if (Test-Path "Migrations") {
    Write-Host "📦 Removendo migrations antigas..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force Migrations
}

# Verifica se dotnet ef está instalado
Write-Host "🔍 Verificando dotnet-ef tools..." -ForegroundColor Cyan
$efInstalled = dotnet ef --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠️ dotnet-ef não está instalado. Instalando..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
    Write-Host "✅ dotnet-ef instalado. Pode ser necessário reiniciar o terminal." -ForegroundColor Green
}

# Cria a migration inicial
Write-Host "✨ Criando migration inicial..." -ForegroundColor Cyan
dotnet ef migrations add InitialCreate

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Migration criada com sucesso!" -ForegroundColor Green
    Write-Host "📝 Arquivos criados em: Migrations\" -ForegroundColor Green
    Write-Host ""
    Write-Host "Próximos passos:" -ForegroundColor Cyan
    Write-Host "1. Commit as migrations: git add LembretesApi/Migrations" -ForegroundColor White
    Write-Host "2. Push para o repositório" -ForegroundColor White
    Write-Host "3. No Render, as migrations serão aplicadas automaticamente no startup" -ForegroundColor White
} 
else {
    Write-Host "❌ Erro ao criar migration. Verifique os logs acima." -ForegroundColor Red
}

Pop-Location

