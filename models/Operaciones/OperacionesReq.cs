using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.Operaciones
{
    public class OperacionesReq
    {
        [StringLength(12)]
        public string Rif_cliente { get; set; }

        [StringLength(20)]
        public string Cuenta { get; set; }

        [StringLength(4)]
        public string Moneda { get; set; }

        [StringLength(4)]
        public string TPago { get; set; }

        [StringLength(2)]
        public string Naturaleza { get; set; }

        [StringLength(10)]
        public string FechaInicio { get; set; }

        [StringLength(10)]
        public string FechaFin { get; set; }

        [StringLength(10)]
        public string Canal { get; set; }

        [StringLength(10)]
        public string Id { get; set; }

        [StringLength(4)]
        public string Banco { get; set; }

        [StringLength(12)]
        public string Referencia { get; set; }
        public decimal MontoMinimo { get; set; }
        public decimal MontoMaximo { get; set; }

        [StringLength(20)]
        public string Direccion_ip { get; set; }

    }
}
