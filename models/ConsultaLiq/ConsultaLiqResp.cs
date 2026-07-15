namespace ApiBanPlaz.models.ConsultaLiq
{
    public class ConsultaLiqResp
    {
        public string CodigoRespuesta { get; set; } = string.Empty;
        public string DescripcionCliente { get; set; } = string.Empty;
        public string DescripcionSistema { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
    }
}
