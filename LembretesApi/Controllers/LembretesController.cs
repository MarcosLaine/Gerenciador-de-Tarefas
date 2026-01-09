using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LembretesApi.Data;
using LembretesApi.Models;
using LembretesApi.DTOs;
using LembretesApi.Services;
using System.Security.Claims;

namespace LembretesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requer autenticação para todos os endpoints
    public class LembretesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly TimezoneService _timezoneService;

        public LembretesController(AppDbContext context, UserManager<Usuario> userManager, TimezoneService timezoneService)
        {
            _context = context;
            _userManager = userManager;
            _timezoneService = timezoneService;
        }

        private string ObterUsuarioId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) 
                ?? throw new UnauthorizedAccessException("Usuário não autenticado");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Lembrete>>> Get()
        {
            try
            {
                var usuarioId = ObterUsuarioId();
                
                var lembretes = await _context.Lembretes
                    .Where(l => l.UsuarioId == usuarioId)
                    .OrderBy(l => l.Data)
                    .Select(l => new
                    {
                        l.Id,
                        l.Nome,
                        l.Descricao,
                        l.Categoria,
                        l.Data,
                        l.Horario,
                        l.Concluido,
                        l.Recorrencia,
                        l.DataCriacao,
                        l.UsuarioId
                    })
                    .ToListAsync();

                return Ok(lembretes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Erro ao buscar lembretes", 
                    error = ex.Message 
                });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Lembrete>> Create(CreateLembreteDto dto)
        {
            try
            {
                var usuarioId = ObterUsuarioId();
                
                // Buscar usuário para obter timezone
                var usuario = await _userManager.FindByIdAsync(usuarioId);
                var userTimezone = usuario?.Timezone ?? "America/Sao_Paulo";
                
                // Converter horário de string para TimeSpan se fornecido
                TimeSpan? horarioTimeSpan = null;
                if (!string.IsNullOrEmpty(dto.Horario))
                {
                    // Aceita formatos HH:mm ou HH:mm:ss
                    var horarioStr = dto.Horario;
                    if (!horarioStr.Contains(':'))
                    {
                        return BadRequest(new { message = "Formato de horário inválido. Use o formato HH:mm" });
                    }
                    
                    // Se for HH:mm, adiciona :00 para tornar HH:mm:ss
                    var parts = horarioStr.Split(':');
                    if (parts.Length == 2)
                    {
                        horarioStr = $"{horarioStr}:00";
                    }
                    
                    if (TimeSpan.TryParse(horarioStr, out var timeSpan))
                    {
                        horarioTimeSpan = timeSpan;
                    }
                    else
                    {
                        return BadRequest(new { message = "Formato de horário inválido. Use o formato HH:mm" });
                    }
                }
                
                // Processar data: extrair apenas a parte da data (sem hora) e aplicar o horário se existir
                var dataBase = dto.Data.Date; // Pega apenas a data (sem hora)
                DateTime dataProcessada;
                
                // Se tem horário, combina data + horário
                if (horarioTimeSpan.HasValue)
                {
                    // Combinar data com horário especificado no timezone do usuário
                    var dataComHorario = dataBase.Add(horarioTimeSpan.Value);
                    
                    // Criar DateTime no timezone do usuário
                    var localDate = new DateTime(
                        dataComHorario.Year,
                        dataComHorario.Month,
                        dataComHorario.Day,
                        dataComHorario.Hour,
                        dataComHorario.Minute,
                        dataComHorario.Second,
                        DateTimeKind.Unspecified
                    );
                    
                    // Converter do timezone do usuário para UTC
                    dataProcessada = _timezoneService.ConvertToUtc(localDate, userTimezone);
                }
                else
                {
                    // Sem horário: usar meio-dia no timezone do usuário
                    var localDate = new DateTime(
                        dataBase.Year,
                        dataBase.Month,
                        dataBase.Day,
                        12, 0, 0, // Meio-dia no timezone do usuário
                        DateTimeKind.Unspecified
                    );
                    
                    // Converter do timezone do usuário para UTC
                    dataProcessada = _timezoneService.ConvertToUtc(localDate, userTimezone);
                }
                
                // Função auxiliar para criar um lembrete
                Lembrete CriarLembrete(DateTime data)
                {
                    return new Lembrete
                    {
                        Nome = dto.Nome,
                        Descricao = dto.Descricao,
                        Categoria = dto.Categoria,
                        Data = data,
                        Horario = horarioTimeSpan,
                        Recorrencia = dto.Recorrencia,
                        UsuarioId = usuarioId,
                        DataCriacao = DateTime.UtcNow
                    };
                }

                // Lista para armazenar todos os lembretes criados
                var lembretesCriados = new List<Lembrete>();

                // Se não há recorrência, criar apenas um lembrete
                if (string.IsNullOrEmpty(dto.Recorrencia))
                {
                    var lembrete = CriarLembrete(dataProcessada);
                    _context.Lembretes.Add(lembrete);
                    lembretesCriados.Add(lembrete);
                }
                else
                {
                    // Calcular datas baseado na recorrência
                    var datas = new List<DateTime>();
                    // Para cálculos mensais/anuais, usar a data processada convertida para local
                    // Para manter a mesma hora/dia ao longo dos meses
                    var dataLocal = dataProcessada.ToLocalTime();

                    switch (dto.Recorrencia.ToLower())
                    {
                        case "diario":
                            // Próximos 15 dias
                            for (int i = 0; i < 15; i++)
                            {
                                var novaData = dataLocal.AddDays(i);
                                // Se tem horário, usar o horário especificado
                                if (horarioTimeSpan.HasValue)
                                {
                                    novaData = new DateTime(
                                        novaData.Year,
                                        novaData.Month,
                                        novaData.Day,
                                        horarioTimeSpan.Value.Hours,
                                        horarioTimeSpan.Value.Minutes,
                                        0,
                                        DateTimeKind.Local
                                    );
                                }
                                else
                                {
                                    // Sem horário: usar meio-dia
                                    novaData = new DateTime(
                                        novaData.Year,
                                        novaData.Month,
                                        novaData.Day,
                                        12, 0, 0,
                                        DateTimeKind.Local
                                    );
                                }
                                datas.Add(novaData.ToUniversalTime());
                            }
                            break;

                        case "semanal":
                            // Próximas 4 semanas
                            for (int i = 0; i < 4; i++)
                            {
                                var novaData = dataLocal.AddDays(i * 7);
                                if (horarioTimeSpan.HasValue)
                                {
                                    novaData = new DateTime(
                                        novaData.Year,
                                        novaData.Month,
                                        novaData.Day,
                                        horarioTimeSpan.Value.Hours,
                                        horarioTimeSpan.Value.Minutes,
                                        0,
                                        DateTimeKind.Local
                                    );
                                }
                                else
                                {
                                    // Sem horário: usar meio-dia
                                    novaData = new DateTime(
                                        novaData.Year,
                                        novaData.Month,
                                        novaData.Day,
                                        12, 0, 0,
                                        DateTimeKind.Local
                                    );
                                }
                                datas.Add(novaData.ToUniversalTime());
                            }
                            break;

                        case "mensal":
                            // Próximos 3 meses (incluindo o mês atual)
                            for (int i = 0; i < 3; i++)
                            {
                                // Adicionar meses primeiro, preservando dia e hora
                                var novaData = dataLocal.AddMonths(i);
                                
                                // Preservar horário especificado ou usar meio-dia
                                if (horarioTimeSpan.HasValue)
                                {
                                    // Aplicar horário após AddMonths para garantir consistência
                                    novaData = new DateTime(
                                        novaData.Year,
                                        novaData.Month,
                                        novaData.Day,
                                        horarioTimeSpan.Value.Hours,
                                        horarioTimeSpan.Value.Minutes,
                                        0,
                                        DateTimeKind.Local
                                    );
                                }
                                else
                                {
                                    // Se não tem horário, garantir que está no meio-dia
                                    novaData = new DateTime(
                                        novaData.Year,
                                        novaData.Month,
                                        novaData.Day,
                                        12, 0, 0,
                                        DateTimeKind.Local
                                    );
                                }
                                
                                datas.Add(novaData.ToUniversalTime());
                            }
                            break;

                        case "anual":
                            // Próximos 2 anos (incluindo o ano atual)
                            for (int i = 0; i < 2; i++)
                            {
                                // Adicionar anos, preservando dia e hora
                                var novaData = dataLocal.AddYears(i);
                                
                                // Preservar horário especificado ou usar meio-dia
                                if (horarioTimeSpan.HasValue)
                                {
                                    // Aplicar horário após AddYears para garantir consistência
                                    novaData = new DateTime(
                                        novaData.Year,
                                        novaData.Month,
                                        novaData.Day,
                                        horarioTimeSpan.Value.Hours,
                                        horarioTimeSpan.Value.Minutes,
                                        0,
                                        DateTimeKind.Local
                                    );
                                }
                                else
                                {
                                    // Se não tem horário, garantir meio-dia
                                    novaData = new DateTime(
                                        novaData.Year,
                                        novaData.Month,
                                        novaData.Day,
                                        12, 0, 0,
                                        DateTimeKind.Local
                                    );
                                }
                                
                                datas.Add(novaData.ToUniversalTime());
                            }
                            break;

                        default:
                            return BadRequest(new { message = "Recorrência inválida. Use: diario, semanal, mensal ou anual" });
                    }

                    // Criar um lembrete para cada data
                    foreach (var data in datas)
                    {
                        var lembrete = CriarLembrete(data);
                        _context.Lembretes.Add(lembrete);
                        lembretesCriados.Add(lembrete);
                    }
                    
                    // Debug: verificar quantos lembretes foram criados
                    // System.Diagnostics.Debug.WriteLine($"Criados {lembretesCriados.Count} lembretes para recorrência {dto.Recorrencia}");
                }

                await _context.SaveChangesAsync();
                
                // Após salvar, verificar se todos foram persistidos
                // var totalSalvo = await _context.Lembretes.CountAsync(l => l.UsuarioId == usuarioId && l.Nome == dto.Nome);
                // System.Diagnostics.Debug.WriteLine($"Total de lembretes salvos: {totalSalvo}");

                // Retornar todos os lembretes criados
                var lembretesRetorno = lembretesCriados.Select(l => new
                {
                    l.Id,
                    l.Nome,
                    l.Descricao,
                    l.Categoria,
                    l.Data,
                    l.Horario,
                    l.Concluido,
                    l.Recorrencia,
                    l.DataCriacao,
                    l.UsuarioId
                }).ToList();

                // Se criou apenas um, retorna CreatedAtAction
                if (lembretesRetorno.Count == 1)
                {
                    return CreatedAtAction(nameof(Get), new { id = lembretesRetorno.First().Id }, lembretesRetorno.First());
                }

                // Se criou vários, retorna Ok com a lista
                return Ok(lembretesRetorno);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Erro ao criar lembrete", 
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Lembrete>> Update(int id, UpdateLembreteDto dto)
        {
            try
            {
                var usuarioId = ObterUsuarioId();
                
                var lembrete = await _context.Lembretes
                    .FirstOrDefaultAsync(l => l.Id == id && l.UsuarioId == usuarioId);

                if (lembrete == null)
                {
                    return NotFound(new { message = "Lembrete não encontrado" });
                }

                // Buscar usuário para obter timezone
                var usuario = await _userManager.FindByIdAsync(usuarioId);
                var userTimezone = usuario?.Timezone ?? "America/Sao_Paulo";

                // Converter horário de string para TimeSpan se fornecido (mesmo processo do Create)
                TimeSpan? horarioTimeSpanUpdate = null;
                if (!string.IsNullOrEmpty(dto.Horario))
                {
                    var horarioStr = dto.Horario;
                    if (!horarioStr.Contains(':'))
                    {
                        return BadRequest(new { message = "Formato de horário inválido. Use o formato HH:mm" });
                    }
                    
                    var parts = horarioStr.Split(':');
                    if (parts.Length == 2)
                    {
                        horarioStr = $"{horarioStr}:00";
                    }
                    
                    if (TimeSpan.TryParse(horarioStr, out var timeSpan))
                    {
                        horarioTimeSpanUpdate = timeSpan;
                    }
                    else
                    {
                        return BadRequest(new { message = "Formato de horário inválido. Use o formato HH:mm" });
                    }
                }

                // Processar data: extrair apenas a parte da data e aplicar o horário se existir (mesmo do Create)
                var dataBaseUpdate = dto.Data.Date;
                DateTime dataProcessadaUpdate;
                
                if (horarioTimeSpanUpdate.HasValue)
                {
                    // Combinar data com horário especificado no timezone do usuário
                    var dataComHorario = dataBaseUpdate.Add(horarioTimeSpanUpdate.Value);
                    
                    // Criar DateTime no timezone do usuário
                    var localDate = new DateTime(
                        dataComHorario.Year,
                        dataComHorario.Month,
                        dataComHorario.Day,
                        dataComHorario.Hour,
                        dataComHorario.Minute,
                        dataComHorario.Second,
                        DateTimeKind.Unspecified
                    );
                    
                    // Converter do timezone do usuário para UTC
                    dataProcessadaUpdate = _timezoneService.ConvertToUtc(localDate, userTimezone);
                }
                else
                {
                    // Sem horário: usar meio-dia no timezone do usuário
                    var localDate = new DateTime(
                        dataBaseUpdate.Year,
                        dataBaseUpdate.Month,
                        dataBaseUpdate.Day,
                        12, 0, 0, // Meio-dia no timezone do usuário
                        DateTimeKind.Unspecified
                    );
                    
                    // Converter do timezone do usuário para UTC
                    dataProcessadaUpdate = _timezoneService.ConvertToUtc(localDate, userTimezone);
                }

                // Atualizar campos
                lembrete.Nome = dto.Nome;
                lembrete.Descricao = dto.Descricao;
                lembrete.Categoria = dto.Categoria;
                lembrete.Data = dataProcessadaUpdate;
                lembrete.Horario = horarioTimeSpanUpdate;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    lembrete.Id,
                    lembrete.Nome,
                    lembrete.Descricao,
                    lembrete.Categoria,
                    lembrete.Data,
                    lembrete.Horario,
                    lembrete.Concluido,
                    lembrete.DataCriacao,
                    lembrete.UsuarioId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Erro ao atualizar lembrete", 
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        [HttpPatch("{id}/concluir")]
        public async Task<ActionResult<Lembrete>> MarcarComoConcluido(int id)
        {
            try
            {
                var usuarioId = ObterUsuarioId();
                
                var lembrete = await _context.Lembretes
                    .FirstOrDefaultAsync(l => l.Id == id && l.UsuarioId == usuarioId);

                if (lembrete == null)
                {
                    return NotFound(new { message = "Lembrete não encontrado" });
                }

                lembrete.Concluido = true;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    lembrete.Id,
                    lembrete.Nome,
                    lembrete.Descricao,
                    lembrete.Categoria,
                    lembrete.Data,
                    lembrete.Horario,
                    lembrete.Concluido,
                    lembrete.Recorrencia,
                    lembrete.DataCriacao,
                    lembrete.UsuarioId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Erro ao marcar lembrete como concluído", 
                    error = ex.Message 
                });
            }
        }

        [HttpPatch("{id}/desmarcar")]
        public async Task<ActionResult<Lembrete>> DesmarcarComoConcluido(int id)
        {
            try
            {
                var usuarioId = ObterUsuarioId();
                
                var lembrete = await _context.Lembretes
                    .FirstOrDefaultAsync(l => l.Id == id && l.UsuarioId == usuarioId);

                if (lembrete == null)
                {
                    return NotFound(new { message = "Lembrete não encontrado" });
                }

                lembrete.Concluido = false;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    lembrete.Id,
                    lembrete.Nome,
                    lembrete.Descricao,
                    lembrete.Categoria,
                    lembrete.Data,
                    lembrete.Horario,
                    lembrete.Concluido,
                    lembrete.Recorrencia,
                    lembrete.DataCriacao,
                    lembrete.UsuarioId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Erro ao desmarcar lembrete", 
                    error = ex.Message 
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var usuarioId = ObterUsuarioId();
                
                var lembrete = await _context.Lembretes
                    .FirstOrDefaultAsync(l => l.Id == id && l.UsuarioId == usuarioId);

                if (lembrete == null)
                {
                    return NotFound(new { message = "Lembrete não encontrado" });
                }

                _context.Lembretes.Remove(lembrete);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Erro ao deletar lembrete", 
                    error = ex.Message 
                });
            }
        }
    }
}
