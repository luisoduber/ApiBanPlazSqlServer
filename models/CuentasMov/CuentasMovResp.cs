using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.CuentasMov
{
    public class CuentasMovResp
    {
        public string CodigoRespuesta { get; set; }
        public string DescripcionCliente { get; set; }
        public string DescripcionSistema { get; set; }
        public DateTime FechaHora { get; set; }
        public int CantMov { get; set; }

    }
}
