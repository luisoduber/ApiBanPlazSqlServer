using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.CompPm
{
    public class CompPmReq
    {
        public string id { get; set; }
        public string canal { get; set; }
        public string acc { get; set; }
        public string fi { get; set; }
        public string ff { get; set; }
        public string tlf { get; set; }
        public string? tlfa { get; set; }
        public string? horaIni { get; set; }
        public string? horaFin { get; set; }
    }
}
