namespace ApiBanPlaz.models.Pagos0
{
    public class Pagos0Req
    {
        public string Moneda { get; set; }
        public string Canal { get; set; }
        public string Tipo_cce { get; set; }
        public string Tipo_proposito { get; set; }
        public string Tipo_instrumento_b { get; set; }
        public string Identificacion_o { get; set; }
        public string Identificacion_b { get; set; }
        public string Cuenta_origen { get; set; }
        public string Cuenta_destino { get; set; }
        public string Telefono { get; set; }
        public string Correo { get; set; }
        public string Cod_banco_d { get; set; }
        public string Cod_banco_a { get; set; }
        public string Nombre_d { get; set; }
        public string Nombre_a { get; set; }
        public decimal Monto { get; set; }
        public string Concepto { get; set; }
        public string Direccion_ip { get; set; }
        public string Referencia { get; set; }
        public DateTime Fecha_hora { get; set; }
    }
}
