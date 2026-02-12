using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.PagosP2p
{
    public class PagosP2pReq
    {
        public string Banco { get; set; }
        public string IdBeneficiario { get; set; }
        public string Telefono { get; set; }
        public Decimal Monto{ get; set; }
        public string Motivo { get; set; }
        public string Canal{ get; set; }
        public string? IdExterno { get; set; }
        public string? Cuenta{ get; set; }
        public string? TelefonoAfiliado { get; set; }
        public string? Moneda { get; set; }
        public string? Sucursal { get; set; }
        public string? Cajero { get; set; }
        public string? Caja { get; set; }
        public string? IpCliente { get; set; }
        public string? Longitud { get; set; }
        public string? Latitud { get; set; }
        public string? Precision { get; set; }
    }
}
