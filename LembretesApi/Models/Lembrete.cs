namespace LembretesApi.Models
{
    public class Lembrete
    {
        public int Id { get; set; }
        public string? Nome { get; set; }
        public string? Descricao { get; set; } // Descrição opcional
        public string? Categoria { get; set; } // Categoria opcional (Trabalho, Pessoal, Saúde, etc.)
        public DateTime Data { get; set; }
        public TimeSpan? Horario { get; set; } // Horário opcional
        public bool Concluido { get; set; } = false; // Status de conclusão
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        
        // Relacionamento com Usuário
        public string UsuarioId { get; set; } = string.Empty;
        public Usuario? Usuario { get; set; }
    }
}
