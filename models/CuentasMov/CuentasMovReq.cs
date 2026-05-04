using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.CuentasMov
{
    public class CuentasMovReq
    {
        [StringLength(20)]
        public string Cuenta { get; set; }
        [StringLength(3)]
        public string Moneda { get; set; }
        [StringLength(12)]
        public string prmReferencia { get; set; }
        [StringLength(10)]
        public string FechaInicio { get; set; }
        [StringLength(10)]
        public string FechaFin { get; set; }
        [StringLength(200)]
        public string Tipo { get; set; }
        public decimal MontoMinimo { get; set; }
        public decimal MontoMaximo { get; set; }
        [StringLength(200)]
        public string Concepto { get; set; }

    }
}
