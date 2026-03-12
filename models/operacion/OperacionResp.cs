namespace ApiBanPlaz.models.operacion
{
    public class OperacionResp
    {
        public string CodigoRespuesta { get; set; }
        public string DescripcionCliente { get; set; }
        public string DescripcionSistema { get; set; }
        public DateTime FechaHora { get; set; }
        public int CantMovimientos { get; set; }

    }
}
