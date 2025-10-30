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
            var usuarioId = ObterUsuarioId();
            
            var lembretes = await _context.Lembretes
                .Where(l => l.UsuarioId == usuarioId)
                .OrderBy(l => l.Data)
                .ToListAsync();

            return Ok(lembretes);
        }

        [HttpPost]
        public async Task<ActionResult<Lembrete>> Create(Lembrete lembrete)
        {
            var usuarioId = ObterUsuarioId();
            
            lembrete.UsuarioId = usuarioId;
            lembrete.DataCriacao = DateTime.UtcNow;

            _context.Lembretes.Add(lembrete);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = lembrete.Id }, lembrete);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
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
    }
}
