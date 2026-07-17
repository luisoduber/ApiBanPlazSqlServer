using ApiBanPlaz.models.Operaciones;

namespace ApiBanPlaz.models.Operaciones
{
    public class MovimientoOpe
    {
        public int IdOperaciones { get; set; } 
        public string Fecha { get; set; } = string.Empty;
        public string Hora { get; set; } = string.Empty;
        public string Referencia { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Naturaleza { get; set; } = string.Empty;
        public decimal Monto { get; set; }
    }
}

public class OperacionesMov
{
    public List<MovimientoOpe> movimientos { get; set; } = new();
}