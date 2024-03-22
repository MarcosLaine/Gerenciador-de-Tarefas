// Define o namespace do seu modelo de dados
namespace LembretesApi.Models
{
    // Define a classe Lembrete
    public class Lembrete
    {
        // Propriedade Id que representa o identificador único de cada lembrete
        public int Id { get; set; }

        // Propriedade Nome que representa o nome do lembrete
        // O '?' indica que essa propriedade pode ser nula
        public string? Nome { get; set; }

        // Propriedade Data que representa a data do lembrete
        public DateTime Data { get; set; }
    }
}
