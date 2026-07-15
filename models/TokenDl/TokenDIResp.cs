using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.TokenDl
{
    public class TokenDlResp
    {
        public int IdTokenDI { get; set; }
        public bool GuardadoRespuestaOk { get; set; }
        public string CodigoRespuesta { get; set; } = string.Empty;
        public string DescripcionCliente { get; set; } = string.Empty;
        public string DescripcionSistema { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
    }
}
