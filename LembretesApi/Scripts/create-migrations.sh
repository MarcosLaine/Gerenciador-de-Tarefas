#!/bin/bash
# Script para criar migrations do Entity Framework
# Execute: bash Scripts/create-migrations.sh

echo "🔧 Criando migrations do Entity Framework..."

# Vai para a pasta da API
cd LembretesApi || exit

# Remove migrations antigas (se existirem)
if [ -d "Migrations" ]; then
    echo "📦 Removendo migrations antigas..."
    rm -rf Migrations
fi

# Cria a migration inicial
echo "✨ Criando migration inicial..."
dotnet ef migrations add InitialCreate --project LembretesApi.csproj

if [ $? -eq 0 ]; then
    echo "✅ Migration criada com sucesso!"
    echo "📝 Arquivos criados em: LembretesApi/Migrations/"
    echo ""
    echo "Próximos passos:"
    echo "1. Commit as migrations: git add LembretesApi/Migrations && git commit -m 'Add EF migrations'"
    echo "2. Push para o repositório"
    echo "3. No Render, as migrations serão aplicadas automaticamente no startup"
else
    echo "❌ Erro ao criar migration. Verifique se o dotnet ef tools está instalado:"
    echo "   dotnet tool install --global dotnet-ef"
fi

