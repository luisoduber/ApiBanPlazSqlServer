namespace ApiBanPlaz.models.Operaciones
{
    public class OperacionesResp
    {
        public int CantMovimientos { get; set; }
        public string CodigoRespuesta { get; set; } = string.Empty;
        public string DescripcionCliente { get; set; } = string.Empty;
        public string DescripcionSistema { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
    }
}
