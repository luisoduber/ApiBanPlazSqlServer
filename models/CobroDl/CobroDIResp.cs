using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.CobroDl
{
    public class CobroDIResp
    {
        public string CodigoRespuesta { get; set; }
        public string DescripcionCliente { get; set; }
        public string DescripcionSistema { get; set; }
        public DateTime FechaHora { get; set; }
        public string Referencia_c { get; set; } = string.Empty;
        public string Endtoend { get; set; } = string.Empty;

    }
}
