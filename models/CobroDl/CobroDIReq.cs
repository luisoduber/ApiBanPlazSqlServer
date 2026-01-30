using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.CobroDl
{
    public class CobroDIReq
    {
        [Required]
        [StringLength(3)]
        public string Moneda { get; set; }

        [Required]
        public string Canal { get; set; }

        [Required]
        public string Tvalidacion_p { get; set; } 

        [Required]
        public string Identificacion_p { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 20)]
        public string Cuenta_cobrador { get; set; }

        public string? Cuenta_pagador { get; set; }

        [StringLength(11)]
        public string? Telefono_pagador { get; set; }

        [Required]
        [StringLength(4, MinimumLength = 4)]
        public string Cod_banco_p { get; set; }

        [Required]
        public string Nombre_p { get; set; }
        
        [Required]
        public decimal Monto { get; set; }

        [Required]
        public string Concepto { get; set; }

        [Required]
        public string? Token_p { get; set; }
        [Required]
        public string? Direccion_ip { get; set; }
        [Required]
        public string? Referencia_c { get; set; }
    }
}
