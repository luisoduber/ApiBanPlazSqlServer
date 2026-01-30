using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.ConsultarDl
{
    public class ConsultarDlReq
    {
        [Required]
        public string Id { get; set; }

        [Required]
        public string cuenta_cobrador  { get; set; }

        [Required]
        public string endtoend { get; set; } 

        [Required]
        [StringLength(12, MinimumLength = 12)]
        public string referencia_c { get; set; }

        [Required]
        public decimal Monto { get; set; }
        [Required]
        public string? canal { get; set; }

    }
}
