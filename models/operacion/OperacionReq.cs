using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.Operacion
{
    public class OperacionReq
    {
        [StringLength(20)]
        public string Cuenta { get; set; }

        [StringLength(4)]
        public string Moneda { get; set; }

        [StringLength(4)]
        public string Banco { get; set; }
       
        [StringLength(4)]
        public string TPago { get; set; }

        [StringLength(2)]
        public string Naturaleza { get; set; }

        [StringLength(12)]
        public string prmReferencia { get; set; }

        [StringLength(10)]
        public string FechaInicio { get; set; }

        [StringLength(10)]
        public string FechaFin { get; set; }
        public decimal Monto { get; set; }

        [StringLength(2)]
        public string canal { get; set; }

        [StringLength(10)]
        public string Id { get; set; }

        [StringLength(20)]
        public string Direccion_ip { get; set; }
    }
}
