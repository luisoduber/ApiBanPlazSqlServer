namespace ApiBanPlaz.models.PagoO
{
    public class PagoOResp
    {
        public string CodigoRespuesta { get; set; } = string.Empty;
        public string DescripcionCliente { get; set; } = string.Empty;
        public string DescripcionSistema { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public string NumeroReferencia { get; set; } = string.Empty;
    }
}
