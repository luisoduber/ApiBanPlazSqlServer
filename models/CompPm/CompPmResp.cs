namespace ApiBanPlaz.models.CompPm
{
    public class CompPmResp
    {
        public string CodigoRespuesta { get; set; }
        public string DescripcionCliente { get; set; }
        public string DescripcionSistema { get; set; }
        public DateTime FechaHora { get; set; }
        public int cantidadPagos { get; set; }
    }
}
