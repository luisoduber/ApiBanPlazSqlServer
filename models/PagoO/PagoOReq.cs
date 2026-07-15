namespace ApiBanPlaz.models.PagoO
{
    public class PagoOReq
    {
        public string Moneda { get; set; } = string.Empty;
        public string Canal { get; set; } = string.Empty;
        public string Tipo_cce { get; set; } = string.Empty;
        public string Tipo_proposito { get; set; } = string.Empty;
        public string Tipo_instrumento_b { get; set; } = string.Empty;
        public string Identificacion_o { get; set; } = string.Empty;
        public string Identificacion_b { get; set; } = string.Empty;
        public string Cuenta_origen { get; set; } = string.Empty;
        public string Cuenta_destino { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Cod_banco_d { get; set; } = string.Empty;
        public string Cod_banco_a { get; set; } = string.Empty;
        public string Nombre_d { get; set; } = string.Empty;
        public string Nombre_a { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public string Direccion_ip { get; set; } = string.Empty;
        public string Referencia { get; set; } = string.Empty;
        public DateTime? Fecha_hora { get; set; }
    }
}
