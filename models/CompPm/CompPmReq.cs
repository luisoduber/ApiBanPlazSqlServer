using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.CompPm
{
    public class CompPmReq
    {
        public string id { get; set; }

        [StringLength(2)]
        public string canal { get; set; }

        public int acc { get; set; }

        public DateTime fi { get; set; }
        public DateTime ff { get; set; }

        [StringLength(11)]
        public string tlf { get; set; }

        [StringLength(11)]
        public string tlfa { get; set; }

        [StringLength(8)]
        public string horaIni { get; set; }
        
        [StringLength(8)]
        public string horaFin { get; set; }
    }
}
