using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.PagosP2p
{
    public class PagosP2pReq
    {
        public string Banco { get; set; } = string.Empty;
        public string IdBeneficiario { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string Canal { get; set; } = string.Empty;
        public string IdExterno { get; set; } = string.Empty;
        public string Cuenta { get; set; } = string.Empty;
        public string TelefonoAfiliado { get; set; } = string.Empty;
        public string Moneda { get; set; } = string.Empty;
        public string Sucursal { get; set; } = string.Empty;
        public string Cajero { get; set; } = string.Empty;
        public string Caja { get; set; } = string.Empty;
        public string IpCliente { get; set; } = string.Empty;
        public string Longitud { get; set; } = string.Empty;
        public string Latitud { get; set; } = string.Empty;
        public string Precision { get; set; } = string.Empty;
    }
}
