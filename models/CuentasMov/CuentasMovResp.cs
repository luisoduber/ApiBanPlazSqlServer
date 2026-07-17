using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.CuentasMov
{
    public class CuentasMovResp
    {
        public string CodigoRespuesta { get; set; } = string.Empty;
        public string DescripcionCliente { get; set; } = string.Empty;
        public string DescripcionSistema { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public int CantMov { get; set; }

    }
}
