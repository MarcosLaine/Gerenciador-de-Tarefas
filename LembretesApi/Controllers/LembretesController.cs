using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LembretesApi.Data;
using LembretesApi.Models;
using LembretesApi.DTOs;
using System.Security.Claims;

namespace LembretesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requer autenticação para todos os endpoints
    public class LembretesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LembretesController(AppDbContext context)
        {
            _context = context;
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
                        l.Data,
                        l.Horario,
                        l.Concluido,
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
                
                // Criar objeto Lembrete
                var lembrete = new Lembrete
                {
                    Nome = dto.Nome,
                    Descricao = dto.Descricao,
                    Data = dto.Data,
                    UsuarioId = usuarioId,
                    DataCriacao = DateTime.UtcNow
                };
                
                // Converter horário de string para TimeSpan se fornecido
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
                        lembrete.Horario = timeSpan;
                    }
                    else
                    {
                        return BadRequest(new { message = "Formato de horário inválido. Use o formato HH:mm" });
                    }
                }
                
                // Tratar data corretamente para evitar problemas de timezone
                // Se a data tem apenas data (sem hora específica), tratar como data local
                if (lembrete.Data.Kind == DateTimeKind.Unspecified)
                {
                    // Se a hora é 00:00:00, provavelmente é apenas uma data, então usar meio-dia local
                    if (lembrete.Data.Hour == 0 && lembrete.Data.Minute == 0 && lembrete.Data.Second == 0)
                    {
                        // Criar nova data com a mesma data mas tratando como local
                        var localDate = new DateTime(
                            lembrete.Data.Year,
                            lembrete.Data.Month,
                            lembrete.Data.Day,
                            12, 0, 0, // Usar meio-dia para evitar problemas de timezone
                            DateTimeKind.Local
                        );
                        lembrete.Data = localDate.ToUniversalTime();
                    }
                    else
                    {
                        // Se tem horário, tratar como UTC
                        lembrete.Data = DateTime.SpecifyKind(lembrete.Data, DateTimeKind.Utc);
                    }
                }
                else if (lembrete.Data.Kind == DateTimeKind.Local)
                {
                    lembrete.Data = lembrete.Data.ToUniversalTime();
                }

                _context.Lembretes.Add(lembrete);
                await _context.SaveChangesAsync();

                // Retorna apenas os dados necessários (sem Usuario)
                return CreatedAtAction(nameof(Get), new { id = lembrete.Id }, new 
                {
                    lembrete.Id,
                    lembrete.Nome,
                    lembrete.Descricao,
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

                // Atualizar campos
                lembrete.Nome = dto.Nome;
                lembrete.Descricao = dto.Descricao;
                lembrete.Data = dto.Data;

                // Converter horário de string para TimeSpan se fornecido
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
                        lembrete.Horario = timeSpan;
                    }
                    else
                    {
                        return BadRequest(new { message = "Formato de horário inválido. Use o formato HH:mm" });
                    }
                }
                else
                {
                    lembrete.Horario = null;
                }

                // Tratar data corretamente para evitar problemas de timezone (mesmo tratamento do Create)
                if (lembrete.Data.Kind == DateTimeKind.Unspecified)
                {
                    if (lembrete.Data.Hour == 0 && lembrete.Data.Minute == 0 && lembrete.Data.Second == 0)
                    {
                        var localDate = new DateTime(
                            lembrete.Data.Year,
                            lembrete.Data.Month,
                            lembrete.Data.Day,
                            12, 0, 0,
                            DateTimeKind.Local
                        );
                        lembrete.Data = localDate.ToUniversalTime();
                    }
                    else
                    {
                        lembrete.Data = DateTime.SpecifyKind(lembrete.Data, DateTimeKind.Utc);
                    }
                }
                else if (lembrete.Data.Kind == DateTimeKind.Local)
                {
                    lembrete.Data = lembrete.Data.ToUniversalTime();
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    lembrete.Id,
                    lembrete.Nome,
                    lembrete.Descricao,
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
