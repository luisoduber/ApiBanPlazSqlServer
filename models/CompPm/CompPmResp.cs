namespace ApiBanPlaz.models.CompPm
{
    public class CompPmResp
    {
        public string CodigoRespuesta { get; set; } = string.Empty;
        public string DescripcionCliente { get; set; } = string.Empty;
        public string DescripcionSistema { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public int CantidadPagos { get; set; }
    }
}

