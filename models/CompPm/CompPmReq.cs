using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.CompPm
{
    public class CompPmReq
    {
        public string Id { get; set; } = string.Empty;
        public string Canal { get; set; } = string.Empty;
        public string? Acc { get; set; }
        public string? Fi { get; set; }
        public string? Ff { get; set; }
        public string? Tlf { get; set; }
        public string? Tlfa { get; set; } = string.Empty;
        public string? HoraIni { get; set; }
        public string? HoraFin { get; set; }
    }
}
