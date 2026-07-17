namespace ApiBanPlaz.models.Operacion
{
    public class Movimiento
    {
        public string Fecha { get; set; } = string.Empty;
        public string Hora { get; set; } = string.Empty;
        public string Referencia { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Naturaleza { get; set; } = string.Empty;
        public decimal Monto { get; set; }
    }
    public class OpeMovimientos
    {
        public List<Movimiento> movimientos { get; set; } = new();
    }

}
