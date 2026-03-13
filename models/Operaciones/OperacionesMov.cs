namespace ApiBanPlaz.models.Operaciones
{
    public class OperacionesMov
    {
        public int IdOperaciones { get; set; }
        public string Fecha { get; set; }
        public string Hora { get; set; }
        public string Referencia { get; set; }
        public string Concepto { get; set; }
        public string Tipo { get; set; }
        public string Naturaleza { get; set; }
        public decimal Monto { get; set; }
    }
}
