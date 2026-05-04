using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.CuentasMov
{
    public class Movimiento
    {
        public string fecha { get; set; }
        public string hora { get; set; }
        public string referencia { get; set; }
        public string concepto { get; set; }
        public string tipo { get; set; }
        public string naturaleza { get; set; }
        public decimal monto { get; set; }


    }

    public class CuentasListMov
    {
        public string numero { get; set; }
        public string fechaApertura { get; set; }
        public string tipoCuenta { get; set; }
        public string estatus { get; set; }
        public string moneda { get; set; }
        public decimal saldoDisponible { get; set; }
        public List<Movimiento> movimientos { get; set; }
    }

}
