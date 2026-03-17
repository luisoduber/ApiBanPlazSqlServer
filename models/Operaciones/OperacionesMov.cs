using ApiBanPlaz.models.Operaciones;

namespace ApiBanPlaz.models.Operaciones
{
    public class MovimientoOpe
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

public class OperacionesMov
{
    public List<MovimientoOpe> movimientos { get; set; }
}