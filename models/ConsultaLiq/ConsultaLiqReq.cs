using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.ConsultaLiq
{
    public class ConsultaLiqReq
    {
        [Required]
        public string Id { get; set; }

        [Required]
        public string Cuenta { get; set; }

        [Required]
        [StringLength(8, MinimumLength = 8)]
        public string Referencia { get; set; }

        [Required]
        public decimal Monto { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string fecha { get; set; }

        [Required]
        public string canal { get; set; }

    }
}
