using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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
        connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.Trim('/')};Username={userInfo[0]};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
    catch (Exception ex)
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Program");
        logger.LogWarning(ex, "Erro ao processar connection string, usando original");
    }
}

// Configurar DB Context com PostgreSQL
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("DATABASE_URL não configurada. Configure a variável de ambiente DATABASE_URL no Render.");
}

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

// Adiciona controllers
builder.Services.AddControllers();

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
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Erro ao aplicar migrações do banco de dados");
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
