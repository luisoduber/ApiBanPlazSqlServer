using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.Operaciones
{
    public class OperacionesReq
    {
        [StringLength(12)]
        public string Rif_cliente { get; set; }

        [StringLength(20)]
        public string prmCuenta { get; set; }

        [StringLength(4)]
        public string prmMoneda { get; set; }

        [StringLength(4)]
        public string prmTPago { get; set; }

        [StringLength(2)]
        public string prmNaturaleza { get; set; }

        [StringLength(10)]
        public string prmFechaInicio { get; set; }

        [StringLength(10)]
        public string prmFechaFin { get; set; }

        [StringLength(10)]
        public string prmCanal { get; set; }

        [StringLength(10)]
        public string prmId { get; set; }

        [StringLength(4)]
        public string prmBanco { get; set; }

        [StringLength(12)]
        public string prmReferencia { get; set; }
        public decimal prmMontoMinimo { get; set; }
        public decimal prmMontoMaximo { get; set; }

        [StringLength(20)]
        public string prmDireccion_ip { get; set; }

    }
}
