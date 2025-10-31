using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Linq;
using System.Text;
using LembretesApi.Data;
using LembretesApi.Models;
using LembretesApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurar PostgreSQL
// PRIORIDADE: Variável de ambiente primeiro (para Render), depois appsettings.json
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// Se vier do Render (formato postgresql:// ou postgres://...)
if (!string.IsNullOrEmpty(connectionString) && (connectionString.StartsWith("postgresql://") || connectionString.StartsWith("postgres://")))
{
    try
    {
        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':');
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        
        // Usar porta padrão 5432 se não especificada na URL
        var dbPort = uri.Port == -1 ? 5432 : uri.Port;
        
        connectionString = $"Host={uri.Host};Port={dbPort};Database={uri.AbsolutePath.Trim('/')};Username={userInfo[0]};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
    catch (Exception ex)
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Program");
        logger.LogWarning(ex, "Erro ao processar connection string: {Error}", ex.Message);
        // Não usar a original se falhou, vai lançar exceção depois
        connectionString = null;
    }
}

// Configurar DB Context com PostgreSQL
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("DATABASE_URL não configurada. Configure a variável de ambiente DATABASE_URL no Render.");
}

// Configurar Npgsql para aceitar datas sem timezone (converte automaticamente para UTC)
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configurar Identity
builder.Services.AddIdentity<Usuario, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Configurar JWT (prioridade: env var, depois config, depois default)
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") 
    ?? Environment.GetEnvironmentVariable("Jwt__Key")
    ?? builder.Configuration["Jwt:Key"] 
    ?? "ChaveSecretaSuperSeguraParaDesenvolvimento123!@#";
var key = Encoding.ASCII.GetBytes(jwtKey);

var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? Environment.GetEnvironmentVariable("Jwt__Issuer")
    ?? builder.Configuration["Jwt:Issuer"]
    ?? "LembretesApi";

var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? Environment.GetEnvironmentVariable("Jwt__Audience")
    ?? builder.Configuration["Jwt:Audience"]
    ?? "LembretesApp";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        ClockSkew = TimeSpan.Zero
    };
});

// Registrar serviços
builder.Services.AddScoped<TokenService>();

// Adiciona controllers com configuração JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Ignora referências circulares (Usuario -> Lembrete -> Usuario)
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        // Mantém nomes de propriedades em camelCase no JSON
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Swagger com autenticação
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Migração automática do banco de dados
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        
        logger.LogInformation("🔄 Verificando e aplicando migrations...");
        
        // Verifica se há migrations pendentes
        var pendingMigrations = context.Database.GetPendingMigrations().ToList();
        
        if (pendingMigrations.Any())
        {
            logger.LogInformation($"📦 Encontradas {pendingMigrations.Count} migrations pendentes: {string.Join(", ", pendingMigrations)}");
            
            try
            {
                context.Database.Migrate();
                logger.LogInformation("✅ Migrations aplicadas com sucesso!");
            }
            catch (Exception migrateEx)
            {
                logger.LogError(migrateEx, "❌ Erro ao aplicar migrations: {Error}", migrateEx.Message);
                throw; // Re-lança a exceção para tratamento abaixo
            }
        }
        else
        {
            logger.LogInformation("ℹ️ Nenhuma migration pendente. Banco está atualizado.");
            
            // Verifica se o banco existe e tem tabelas
            if (!context.Database.CanConnect())
            {
                logger.LogWarning("⚠️ Não é possível conectar ao banco. Tentando criar...");
                var created = context.Database.EnsureCreated();
                if (created)
                {
                    logger.LogInformation("✅ Banco criado com sucesso via EnsureCreated()!");
                }
            }
        }
        
        // Verifica e adiciona colunas se não existirem
        try
        {
            logger.LogInformation("🔍 Verificando se colunas existem...");
            
            // Verificar e adicionar Horario
            var checkHorarioSql = @"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'Lembretes' AND column_name = 'Horario'";
            
            var horarioExists = context.Database.SqlQueryRaw<string>(checkHorarioSql).Any();
            
            if (!horarioExists)
            {
                logger.LogWarning("⚠️ Coluna Horario não encontrada. Criando...");
                var addHorarioSql = @"ALTER TABLE ""Lembretes"" ADD COLUMN ""Horario"" interval NULL";
                context.Database.ExecuteSqlRaw(addHorarioSql);
                logger.LogInformation("✅ Coluna Horario adicionada com sucesso!");
            }
            
            // Verificar e adicionar Descricao
            var checkDescricaoSql = @"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'Lembretes' AND column_name = 'Descricao'";
            
            var descricaoExists = context.Database.SqlQueryRaw<string>(checkDescricaoSql).Any();
            
            if (!descricaoExists)
            {
                logger.LogWarning("⚠️ Coluna Descricao não encontrada. Criando...");
                var addDescricaoSql = @"ALTER TABLE ""Lembretes"" ADD COLUMN ""Descricao"" text NULL";
                context.Database.ExecuteSqlRaw(addDescricaoSql);
                logger.LogInformation("✅ Coluna Descricao adicionada com sucesso!");
            }
            
            // Verificar e adicionar Concluido
            var checkConcluidoSql = @"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'Lembretes' AND column_name = 'Concluido'";
            
            var concluidoExists = context.Database.SqlQueryRaw<string>(checkConcluidoSql).Any();
            
            if (!concluidoExists)
            {
                logger.LogWarning("⚠️ Coluna Concluido não encontrada. Criando...");
                var addConcluidoSql = @"ALTER TABLE ""Lembretes"" ADD COLUMN ""Concluido"" boolean NOT NULL DEFAULT false";
                context.Database.ExecuteSqlRaw(addConcluidoSql);
                logger.LogInformation("✅ Coluna Concluido adicionada com sucesso!");
            }
            
            // Verificar e adicionar Categoria
            var checkCategoriaSql = @"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'Lembretes' AND column_name = 'Categoria'";
            
            var categoriaExists = context.Database.SqlQueryRaw<string>(checkCategoriaSql).Any();
            
            if (!categoriaExists)
            {
                logger.LogWarning("⚠️ Coluna Categoria não encontrada. Criando...");
                var addCategoriaSql = @"ALTER TABLE ""Lembretes"" ADD COLUMN ""Categoria"" text NULL";
                context.Database.ExecuteSqlRaw(addCategoriaSql);
                logger.LogInformation("✅ Coluna Categoria adicionada com sucesso!");
            }
            
            // Verificar e adicionar Recorrencia
            var checkRecorrenciaSql = @"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'Lembretes' AND column_name = 'Recorrencia'";
            
            var recorrenciaExists = context.Database.SqlQueryRaw<string>(checkRecorrenciaSql).Any();
            
            if (!recorrenciaExists)
            {
                logger.LogWarning("⚠️ Coluna Recorrencia não encontrada. Criando...");
                var addRecorrenciaSql = @"ALTER TABLE ""Lembretes"" ADD COLUMN ""Recorrencia"" text NULL";
                context.Database.ExecuteSqlRaw(addRecorrenciaSql);
                logger.LogInformation("✅ Coluna Recorrencia adicionada com sucesso!");
            }
            
            logger.LogInformation("✅ Todas as colunas verificadas/criadas.");
        }
        catch (Exception colEx)
        {
            logger.LogError(colEx, "❌ Erro ao verificar/criar colunas: {Error}", colEx.Message);
            // Não lança exceção, apenas loga o erro
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ ERRO CRÍTICO ao aplicar migrations: {Error}", ex.Message);
        
        // Em produção, você pode querer falhar o startup se migrations falharem
        // Em desenvolvimento, continua mas registra o erro
        if (app.Environment.IsProduction())
        {
            // Em produção, é melhor falhar do que rodar com banco desatualizado
            logger.LogCritical("🚨 CRÍTICO: Falha ao aplicar migrations em produção. A aplicação não iniciará.");
            throw;
        }
    }
}

// Configure o pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS deve vir antes de Authentication e Authorization
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Configurar porta para Render (usa $PORT se disponível, senão 5285)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5285";
app.Run($"http://0.0.0.0:{port}");
