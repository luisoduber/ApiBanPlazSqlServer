namespace ApiBanPlaz.models.Operacion
{
    public class Movimiento
    {
        public string Fecha { get; set; }
        public string Hora { get; set; }
        public string Referencia { get; set; }
        public string Concepto { get; set; }
        public string Tipo { get; set; }
        public string Naturaleza { get; set; }
        public decimal Monto { get; set; }
    }
    public class OpeMovimientos
    {
        public List<Movimiento> movimientos { get; set; }
    }

}
