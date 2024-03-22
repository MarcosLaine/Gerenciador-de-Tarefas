using Microsoft.AspNetCore.Mvc;
using LembretesApi.Models;
using System.Collections.Generic;
using System.Linq;

namespace LembretesApi.Controllers
{
    [ApiController] // Define que a classe é um controller da API
    [Route("api/[controller]")] // Define a rota base para as requisições deste controller
    public class LembretesController : ControllerBase // Herda de ControllerBase para funcionalidades básicas de um controller
    {
        private static List<Lembrete> lembretes = new List<Lembrete>(); // Lista estática para armazenar os lembretes
        private static int nextId = 1; // Variável estática para gerar o próximo ID

        [HttpGet] // Define que este método responde a requisições HTTP GET
        public ActionResult<IEnumerable<Lembrete>> Get() // Retorna todos os lembretes
        {
            return lembretes; // Retorna a lista de lembretes
        }

        [HttpPost] // Define que este método responde a requisições HTTP POST
        public ActionResult<Lembrete> Create(Lembrete lembrete) // Cria um novo lembrete
        {
            lembrete.Id = nextId++; // Define o ID do lembrete como o próximo ID disponível e incrementa o próximo ID
            lembretes.Add(lembrete); // Adiciona o lembrete à lista de lembretes
            return CreatedAtAction(nameof(Create), new { id = lembrete.Id }, lembrete); // Retorna um código 201 (Created) com o lembrete criado
        }

        [HttpDelete("{id}")] // Define que este método responde a requisições HTTP DELETE com um parâmetro ID na rota
        public IActionResult Delete(int id) // Deleta um lembrete pelo ID
        {
            var lembrete = lembretes.FirstOrDefault(x => x.Id == id); // Busca o lembrete na lista pelo ID
            if (lembrete == null)
            {
                return NotFound(); // Retorna 404 (Not Found) se o lembrete não for encontrado
            }
            lembretes.Remove(lembrete); // Remove o lembrete da lista
            return NoContent(); // Retorna 204 (No Content) indicando que a operação foi bem-sucedida
        }
    }
}
