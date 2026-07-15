using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.ConsultaLiq
{
    public class ConsultaLiqReq
    {
        public string Id { get; set; } = string.Empty;
        public string Cuenta { get; set; } = string.Empty;
        public string Referencia { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string Fecha { get; set; } = string.Empty;
        public string Canal { get; set; } = string.Empty;
    }
}
