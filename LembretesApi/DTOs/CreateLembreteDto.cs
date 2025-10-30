using System.ComponentModel.DataAnnotations;

namespace LembretesApi.DTOs
{
    public class CreateLembreteDto
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        public string Nome { get; set; } = string.Empty;

        public string? Descricao { get; set; } // Descrição opcional

        [Required(ErrorMessage = "Data é obrigatória")]
        public DateTime Data { get; set; }
        
        public string? Horario { get; set; } // Recebe como string no formato "HH:mm" ou "HH:mm:ss"
    }

    public class UpdateLembreteDto
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        public string Nome { get; set; } = string.Empty;

        public string? Descricao { get; set; }

        [Required(ErrorMessage = "Data é obrigatória")]
        public DateTime Data { get; set; }
        
        public string? Horario { get; set; }
    }
}

