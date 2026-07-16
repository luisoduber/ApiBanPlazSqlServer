using System;

namespace ApiBanPlaz.models.CompPm
{
    public class Pago
    {
        public string Accion { get; set; } = string.Empty;
        public string Banco { get; set; } = string.Empty;
        public string TelefonoCliente { get; set; } = string.Empty;
        public string TelefonoAfiliado { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string Origen { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public string Hora { get; set; } = string.Empty;
        public string Referencia { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;
        public string CedulaB { get; set; } = string.Empty;
    }

    /// </summary>
    public class CompPmPag
    {
        public int CantidadPagos { get; set; }
        public List<Pago> Pagos { get; set; } = new();
    }
}
