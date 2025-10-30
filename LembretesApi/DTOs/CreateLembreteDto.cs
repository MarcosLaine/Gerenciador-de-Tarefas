using System.ComponentModel.DataAnnotations;

namespace LembretesApi.DTOs
{
    public class CreateLembreteDto
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Data é obrigatória")]
        public DateTime Data { get; set; }
        
        public string? Horario { get; set; } // Recebe como string no formato "HH:mm" ou "HH:mm:ss"
    }
}

