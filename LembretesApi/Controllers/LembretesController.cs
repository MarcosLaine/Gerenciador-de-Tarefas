using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LembretesApi.Data;
using LembretesApi.Models;
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
                        l.Data,
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
        public async Task<ActionResult<Lembrete>> Create(Lembrete lembrete)
        {
            try
            {
                var usuarioId = ObterUsuarioId();
                
                lembrete.UsuarioId = usuarioId;
                lembrete.DataCriacao = DateTime.UtcNow;
                lembrete.Usuario = null; // Evita referência circular

                _context.Lembretes.Add(lembrete);
                await _context.SaveChangesAsync();

                // Retorna apenas os dados necessários (sem Usuario)
                return CreatedAtAction(nameof(Get), new { id = lembrete.Id }, new 
                {
                    lembrete.Id,
                    lembrete.Nome,
                    lembrete.Data,
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
