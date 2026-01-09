using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Linq;
using System.Text;
using LembretesApi.Data;
using LembretesApi.Models;
using LembretesApi.Services;
using DotNetEnv;

// Carregar variáveis de ambiente do arquivo .env (apenas em desenvolvimento)
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" && 
    File.Exists(".env"))
{
    Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

// Configurar PostgreSQL
// PRIORIDADE: Variável de ambiente primeiro (para Render), depois appsettings.json
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// Se vier do Neon.tech ou Render (formato postgresql:// ou postgres://...)
if (!string.IsNullOrEmpty(connectionString) && (connectionString.StartsWith("postgresql://") || connectionString.StartsWith("postgres://")))
{
    try
    {
        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':');
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        
        // Usar porta padrão 5432 se não especificada na URL
        var dbPort = uri.Port == -1 ? 5432 : uri.Port;
        
        // Extrair parâmetros de query (sslmode, channel_binding, etc.)
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(uri.Query))
        {
            var query = uri.Query.TrimStart('?');
            var pairs = query.Split('&');
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    var paramKey = keyValue[0];
                    var value = Uri.UnescapeDataString(keyValue[1]);
                    
                    // Converter parâmetros de query para formato Npgsql
                    switch (paramKey.ToLower())
                    {
                        case "sslmode":
                            queryParams.Add($"SSL Mode={value}");
                            break;
                        case "channel_binding":
                            queryParams.Add($"Channel Binding={value}");
                            break;
                        default:
                            queryParams.Add($"{paramKey}={value}");
                            break;
                    }
                }
            }
        }
        
        // Se não tiver SSL Mode na query, adicionar padrão
        if (!queryParams.Any(p => p.StartsWith("SSL Mode", StringComparison.OrdinalIgnoreCase)))
        {
            queryParams.Add("SSL Mode=Require");
            queryParams.Add("Trust Server Certificate=true");
        }
        
        var queryString = string.Join(";", queryParams);
        connectionString = $"Host={uri.Host};Port={dbPort};Database={uri.AbsolutePath.Trim('/')};Username={userInfo[0]};Password={password};{queryString}";
    }
    catch (Exception ex)
    {
        // Log simples usando Console (antes do logger estar configurado)
        Console.WriteLine($"⚠️ Erro ao processar connection string: {ex.Message}");
        // Não usar a original se falhou, vai lançar exceção depois
        connectionString = null;
    }
}

// Configurar DB Context com PostgreSQL
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("DATABASE_URL não configurada. Configure a variável de ambiente DATABASE_URL ou a connection string no appsettings.json.");
}

// Configurar Npgsql para aceitar datas sem timezone (converte automaticamente para UTC)
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Configurar DbContext com PostgreSQL (Neon.tech)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

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
builder.Services.AddScoped<TimezoneService>();
builder.Services.AddScoped<PushNotificationService>();
builder.Services.AddHostedService<ReminderNotificationBackgroundService>();

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
            logger.LogInformation("ℹ️ Nenhuma migration pendente. Verificando se tabelas existem...");
            
            // Verifica se o banco existe e tem tabelas
            bool tablesExist = false;
            try
            {
                if (context.Database.CanConnect())
                {
                    // Verifica se a tabela AspNetUsers existe (tabela principal do Identity)
                    var connection = context.Database.GetDbConnection();
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        connection.Open();
                    }
                    
                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                        SELECT EXISTS (
                            SELECT 1 FROM information_schema.tables 
                            WHERE table_schema = 'public' 
                            AND table_name = 'AspNetUsers'
                        );
                    ";
                    var result = command.ExecuteScalar();
                    tablesExist = result != null && Convert.ToBoolean(result);
                    
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }
            }
            catch (Exception checkEx)
            {
                logger.LogWarning(checkEx, "⚠️ Erro ao verificar tabelas: {Error}", checkEx.Message);
            }
            
            if (!tablesExist)
            {
                logger.LogWarning("⚠️ Tabelas não existem. Criando banco de dados...");
                try
                {
                    var created = context.Database.EnsureCreated();
                    if (created)
                    {
                        logger.LogInformation("✅ Banco criado com sucesso via EnsureCreated()!");
                    }
                    else
                    {
                        logger.LogInformation("ℹ️ EnsureCreated retornou false - tabelas podem já existir");
                    }
                }
                catch (Exception createEx)
                {
                    logger.LogError(createEx, "❌ Erro ao criar banco: {Error}", createEx.Message);
                    throw;
                }
            }
            else
            {
                logger.LogInformation("✅ Tabelas existem. Verificando se coluna Timezone existe...");
                
                // Verificar e adicionar coluna Timezone se não existir
                try
                {
                    var connection = context.Database.GetDbConnection();
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        connection.Open();
                    }
                    
                    using var checkColumnCommand = connection.CreateCommand();
                    checkColumnCommand.CommandText = @"
                        SELECT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_schema = 'public' 
                            AND table_name = 'AspNetUsers'
                            AND column_name = 'Timezone'
                        );
                    ";
                    var columnExists = Convert.ToBoolean(checkColumnCommand.ExecuteScalar());
                    
                    if (!columnExists)
                    {
                        logger.LogInformation("📝 Adicionando coluna Timezone à tabela AspNetUsers...");
                        using var addColumnCommand = connection.CreateCommand();
                        addColumnCommand.CommandText = @"
                            ALTER TABLE ""AspNetUsers""
                            ADD COLUMN ""Timezone"" TEXT NOT NULL DEFAULT 'America/Sao_Paulo';
                        ";
                        addColumnCommand.ExecuteNonQuery();
                        logger.LogInformation("✅ Coluna Timezone adicionada com sucesso!");
                    }
                    else
                    {
                        logger.LogInformation("✅ Coluna Timezone já existe.");
                    }
                    
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }
                catch (Exception columnEx)
                {
                    logger.LogWarning(columnEx, "⚠️ Erro ao verificar/adicionar coluna Timezone: {Error}", columnEx.Message);
                }
            }
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

// Middleware de tratamento de erros para retornar JSON
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        var errorResponse = new
        {
            message = "Erro interno do servidor",
            error = exception?.Message,
            details = app.Environment.IsDevelopment() ? exception?.ToString() : null
        };

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));
    });
});

// CORS deve vir antes de Authentication e Authorization
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Configurar porta para Render (usa $PORT se disponível, senão 5285)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5285";
app.Run($"http://0.0.0.0:{port}");
