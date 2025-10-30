namespace LembretesApi.Models
{
    public class Lembrete
    {
        public int Id { get; set; }
        public string? Nome { get; set; }
        public DateTime Data { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        
        // Relacionamento com Usuário
        public string UsuarioId { get; set; } = string.Empty;
        public Usuario? Usuario { get; set; }
    }
}
