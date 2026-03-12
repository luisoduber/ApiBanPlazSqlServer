using System;

namespace ApiBanPlaz.models.CompPm
{
    public class CompPmPag
    {
        public int IdCompPm { get; set; }
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
}
