using System;

namespace ApiBanPlaz.models.CompPm
{
    public class pago
    {
        public string Accion { get; set; }
        public string Banco { get; set; }
        public string TelefonoCliente { get; set; }
        public string TelefonoAfiliado { get; set; }
        public decimal Monto { get; set; }
        public string Fecha { get; set; }
        public string Hora { get; set; }
        public string Referencia { get; set; }
        public string Motivo { get; set; }
    }


    public class CompPmPag
    {
        public int cantidadPagos { get; set; }
        public List<pago> pagos { get; set; }
    }
}
