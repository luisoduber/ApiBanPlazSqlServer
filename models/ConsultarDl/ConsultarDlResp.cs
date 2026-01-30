using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.ConsultarDl
{
    public class ConsultarDlResp
    {
        public string CodigoRespuesta { get; set; }
        public string DescripcionCliente { get; set; }
        public string DescripcionSistema { get; set; }
        public DateTime FechaHora { get; set; }
    }
}
