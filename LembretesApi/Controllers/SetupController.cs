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

                // Tenta garantir que o banco existe
                var canConnect = _context.Database.CanConnect();
                _logger.LogInformation("Pode conectar ao banco: {CanConnect}", canConnect);

                if (!canConnect)
                {
                    _logger.LogInformation("Criando banco de dados...");
                    var created = _context.Database.EnsureCreated();
                    if (created)
                    {
                        _logger.LogInformation("✅ Banco criado com sucesso!");
                        return Ok(new { message = "Banco de dados criado com sucesso!", created = true });
                    }
                }

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
                    _logger.LogWarning(migrateEx, "Não foi possível aplicar migrations, tentando EnsureCreated...");
                    
                    // Se não houver migrations, cria via EnsureCreated
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
                        _logger.LogInformation("ℹ️ Banco já existe e tabelas já criadas");
                        return Ok(new { 
                            message = "Banco de dados já existe e está pronto!", 
                            alreadyExists = true 
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao criar banco: {Error}", ex.Message);
                return StatusCode(500, new { 
                    message = "Erro ao criar banco de dados", 
                    error = ex.Message 
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

                // Verifica se a tabela AspNetUsers existe
                var tableExists = false;
                try
                {
                    var result = await _context.Database.ExecuteSqlRawAsync(
                        "SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'AspNetUsers'"
                    );
                    // Se não lançar exceção, a tabela existe
                    tableExists = true;
                }
                catch
                {
                    tableExists = false;
                }

                return Ok(new { 
                    connected = true,
                    tablesExist = tableExists,
                    message = tableExists ? "Banco conectado e tabelas existem!" : "Banco conectado mas tabelas não existem. Execute /api/setup/create-database"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Erro ao verificar banco", 
                    error = ex.Message 
                });
            }
        }
    }
}

