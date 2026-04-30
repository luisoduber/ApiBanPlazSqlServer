using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.Cuentas
{
    public class CuentasReq
    {
        [StringLength(12)]
        public string Rif_cliente  { get; set; }

        [StringLength(20)]
        public string Cuenta { get; set; }

        [StringLength(20)]
        public string Telefono { get; set; }

        [StringLength(3)]
        public string Moneda { get; set; }

    }
}
