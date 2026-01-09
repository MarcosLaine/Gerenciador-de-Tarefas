namespace LembretesApi.Models
{
    public class PushSubscription
    {
        public int Id { get; set; }
        public string UsuarioId { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string P256dh { get; set; } = string.Empty; // Chave pública
        public string Auth { get; set; } = string.Empty; // Chave de autenticação
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        
        // Relacionamento com Usuario
        public Usuario? Usuario { get; set; }
    }
}
