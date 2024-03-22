using Microsoft.AspNetCore.Mvc;
using LembretesApi.Models;
using System.Collections.Generic;
using System.Linq;

namespace LembretesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LembretesController : ControllerBase
    {
        private static List<Lembrete> lembretes = new List<Lembrete>();
        private static int nextId = 1;

        [HttpGet]
        public ActionResult<IEnumerable<Lembrete>> Get()
        {
            return lembretes;
        }

        [HttpPost]
        public ActionResult<Lembrete> Create(Lembrete lembrete)
        {
            lembrete.Id = nextId++;
            lembretes.Add(lembrete);
            return CreatedAtAction(nameof(Create), new { id = lembrete.Id }, lembrete);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var lembrete = lembretes.FirstOrDefault(x => x.Id == id);
            if (lembrete == null)
            {
                return NotFound();
            }
            lembretes.Remove(lembrete);
            return NoContent();
        }
    }
}
