using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.Operacion
{
    public class OperacionReq
    {
        public string Cuenta { get; set; } = string.Empty;
        public string Moneda { get; set; } = string.Empty;
        public string Banco { get; set; } = string.Empty;
        public string TPago { get; set; } = string.Empty;
        public string Naturaleza { get; set; } = string.Empty;
        public string Referencia { get; set; } = string.Empty;
        public string FechaInicio { get; set; } = string.Empty;
        public string FechaFin { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string Canal { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Direccion_ip { get; set; } = string.Empty;
    }
}
