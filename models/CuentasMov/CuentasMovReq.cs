using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.CuentasMov
{
    public class CuentasMovReq
    {
        [StringLength(20)]
        public string Cuenta { get; set; } = string.Empty;
        [StringLength(3)]
        public string Moneda { get; set; } = string.Empty;
        [StringLength(12)]
        public string Referencia { get; set; } = string.Empty;
        [StringLength(10)]
        public string FechaInicio { get; set; } = string.Empty;
        [StringLength(10)]
        public string FechaFin { get; set; } = string.Empty;
        [StringLength(200)]
        public string Tipo { get; set; } = string.Empty;
        public decimal MontoMinimo { get; set; }
        public decimal MontoMaximo { get; set; }
        [StringLength(200)]
        public string Concepto { get; set; } = string.Empty;

    }
}
