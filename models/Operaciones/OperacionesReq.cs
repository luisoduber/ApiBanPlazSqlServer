using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.Operaciones
{
    public class OperacionesReq
    {
        [StringLength(12)]
        public string Rif_cliente { get; set; } = string.Empty;

        [StringLength(20)]
        public string Cuenta { get; set; } = string.Empty;

        [StringLength(4)]
        public string Moneda { get; set; } = string.Empty;

        [StringLength(4)]
        public string TPago { get; set; } = string.Empty;

        [StringLength(2)]
        public string Naturaleza { get; set; } = string.Empty;

        [StringLength(10)]
        public string FechaInicio { get; set; } = string.Empty;

        [StringLength(10)]
        public string FechaFin { get; set; } = string.Empty;

        [StringLength(10)]
        public string Canal { get; set; } = string.Empty;

        [StringLength(10)]
        public string Id { get; set; } = string.Empty;

        [StringLength(4)]
        public string Banco { get; set; } = string.Empty;

        [StringLength(12)]
        public string Referencia { get; set; } = string.Empty;
        public decimal? MontoMinimo { get; set; }
        public decimal? MontoMaximo { get; set; }

        [StringLength(20)]
        public string Direccion_ip { get; set; } = string.Empty;

    }
}


