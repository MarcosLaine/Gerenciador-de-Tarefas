using Microsoft.AspNetCore.Identity;

namespace LembretesApi.Models
{
    public class Usuario : IdentityUser
    {
        public string? Nome { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        
        // Relacionamento com Lembretes
        public ICollection<Lembrete> Lembretes { get; set; } = new List<Lembrete>();
    }
}

