using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LembretesApi.Data;

namespace LembretesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SetupController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SetupController> _logger;

        public SetupController(AppDbContext context, ILogger<SetupController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("create-database")]
        public async Task<IActionResult> CreateDatabase()
        {
            try
            {
                _logger.LogInformation("=== INICIANDO CRIAÇÃO DO BANCO DE DADOS VIA ENDPOINT ===");

                var canConnect = _context.Database.CanConnect();
                _logger.LogInformation("Pode conectar ao banco: {CanConnect}", canConnect);

                // Tenta aplicar migrations primeiro
                try
                {
                    _logger.LogInformation("Tentando aplicar migrations...");
                    _context.Database.Migrate();
                    _logger.LogInformation("✅ Migrations aplicadas!");
                    return Ok(new { message = "Migrations aplicadas com sucesso!", migrated = true });
                }
                catch (Exception migrateEx)
                {
                    _logger.LogWarning(migrateEx, "Não foi possível aplicar migrations: {Error}", migrateEx.Message);
                    
                    // Se não houver migrations, cria via EnsureCreated
                    _logger.LogInformation("Tentando criar via EnsureCreated()...");
                    var created = _context.Database.EnsureCreated();
                    
                    if (created)
                    {
                        _logger.LogInformation("✅ Banco criado via EnsureCreated!");
                        return Ok(new { 
                            message = "Banco de dados criado com sucesso via EnsureCreated!", 
                            created = true 
                        });
                    }
                    else
                    {
                        _logger.LogInformation("ℹ️ EnsureCreated retornou false - tabelas podem já existir");
                        return Ok(new { 
                            message = "EnsureCreated retornou false. Tabelas podem já existir. Verifique com /api/setup/check-database", 
                            created = false 
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao criar banco: {Error}", ex.Message);
                return StatusCode(500, new { 
                    message = "Erro ao criar banco de dados", 
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet("check-database")]
        public async Task<IActionResult> CheckDatabase()
        {
            try
            {
                var canConnect = _context.Database.CanConnect();
                
                if (!canConnect)
                {
                    return Ok(new { 
                        connected = false, 
                        message = "Não é possível conectar ao banco de dados" 
                    });
                }

                // Lista TODAS as tabelas do banco
                var tables = new List<string>();
                try
                {
                    using var connection = _context.Database.GetDbConnection();
                    await connection.OpenAsync();
                    
                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                        SELECT table_name 
                        FROM information_schema.tables 
                        WHERE table_schema = 'public' 
                        ORDER BY table_name;
                    ";
                    
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        tables.Add(reader.GetString(0));
                    }
                }
                catch (Exception ex)
                {
                    return Ok(new { 
                        connected = true,
                        tablesExist = false,
                        error = ex.Message,
                        message = $"Erro ao listar tabelas: {ex.Message}"
                    });
                }

                var aspNetUsersExists = tables.Contains("AspNetUsers");
                var aspNetRolesExists = tables.Contains("AspNetRoles");
                var lembretesExists = tables.Contains("Lembretes");

                // Tenta fazer uma query REAL na tabela AspNetUsers
                var canQueryAspNetUsers = false;
                string queryError = null;
                try
                {
                    var count = await _context.Database.ExecuteSqlRawAsync(
                        "SELECT COUNT(*) FROM \"AspNetUsers\""
                    );
                    canQueryAspNetUsers = true;
                }
                catch (Exception queryEx)
                {
                    canQueryAspNetUsers = false;
                    queryError = queryEx.Message;
                }

                return Ok(new { 
                    connected = true,
                    tablesExist = aspNetUsersExists && aspNetRolesExists,
                    canQueryAspNetUsers = canQueryAspNetUsers,
                    tables = tables,
                    aspNetUsersExists = aspNetUsersExists,
                    aspNetRolesExists = aspNetRolesExists,
                    lembretesExists = lembretesExists,
                    queryError = queryError,
                    message = aspNetUsersExists && canQueryAspNetUsers
                        ? "✅ Banco conectado e tabelas funcionando!" 
                        : aspNetUsersExists && !canQueryAspNetUsers
                        ? $"⚠️ Tabelas existem mas não é possível fazer query. Erro: {queryError}. Execute POST /api/setup/force-recreate"
                        : "❌ Tabelas não existem. Execute POST /api/setup/create-database"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Erro ao verificar banco", 
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpPost("force-recreate")]
        public async Task<IActionResult> ForceRecreate()
        {
            try
            {
                _logger.LogInformation("=== FORÇANDO RECRIAÇÃO DO BANCO ===");

                // Primeiro tenta deletar o banco
                try
                {
                    _logger.LogInformation("Tentando deletar banco existente...");
                    await _context.Database.EnsureDeletedAsync();
                    _logger.LogInformation("✅ Banco deletado.");
                }
                catch (Exception delEx)
                {
                    _logger.LogWarning(delEx, "Não foi possível deletar banco: {Error}", delEx.Message);
                }

                // Cria tudo do zero
                _logger.LogInformation("Criando banco do zero...");
                var created = _context.Database.EnsureCreated();
                
                if (created)
                {
                    _logger.LogInformation("✅ Banco recriado com sucesso!");
                    return Ok(new { 
                        message = "Banco de dados recriado com sucesso!", 
                        created = true 
                    });
                }
                else
                {
                    return Ok(new { 
                        message = "Banco não foi recriado.", 
                        created = false 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao recriar banco: {Error}", ex.Message);
                return StatusCode(500, new { 
                    message = "Erro ao recriar banco de dados", 
                    error = ex.Message 
                });
            }
        }
    }
}
