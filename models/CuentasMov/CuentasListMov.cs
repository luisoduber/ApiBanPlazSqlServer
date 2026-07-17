using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.CuentasMov
{
    public class MovimientoCuent
    {
        public string Fecha { get; set; } = string.Empty;
        public string Hora { get; set; } = string.Empty;
        public string Referencia { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Naturaleza { get; set; } = string.Empty;
        public decimal Monto { get; set; }


    }

    public class CuentasListMov
    {
        public string Numero { get; set; } = string.Empty;
        public string FechaApertura { get; set; } = string.Empty;
        public string TipoCuenta { get; set; } = string.Empty;
        public string Estatus { get; set; } = string.Empty;
        public string Moneda { get; set; } = string.Empty;
        public decimal SaldoDisponible { get; set; }
        public List<MovimientoCuent> Movimientos { get; set; } = new();
    }

}
