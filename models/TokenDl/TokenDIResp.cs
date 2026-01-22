using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.TokenDl
{
    public class TokenDIResp
    {
        public string CodigoRespuesta { get; set; }
        public string DescripcionCliente { get; set; }
        public string DescripcionSistema { get; set; }
        public DateTime FechaHora { get; set; }
    }
}
