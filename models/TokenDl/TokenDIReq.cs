using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.TokenDl
{
    public class TokenDIReq
    {
        [Required]
        [StringLength(3)]
        public string Moneda { get; set; }

        [Required]
        public string Canal { get; set; }

        [Required]
        public string Tvalidacion_p { get; set; } 

        [Required]
        [StringLength(12, MinimumLength = 12)]
        public string Identificacion_p { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 20)]
        public string Cuenta_cobrador { get; set; }

        [StringLength(20, MinimumLength = 20)]
        public string? Cuenta_pagador { get; set; }

        [StringLength(11)]
        public string? Telefono_pagador { get; set; }

        [Required]
        [StringLength(4, MinimumLength = 4)]
        public string Cod_banco_p { get; set; }

        [Required]
        public decimal Monto { get; set; }
        public string? Direccion_ip { get; set; }
    }
}
