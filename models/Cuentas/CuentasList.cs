namespace ApiBanPlaz.models.Cuentas
{
    public class Cuenta
    {
        public string numero { get; set; }
        public string fechaApertura { get; set; }
        public string tipoCuenta { get; set; }
        public string estatus { get; set; }
        public string moneda { get; set; }
        public decimal saldoDisponible { get; set; }
    }

    public class CuentasList
    {
        public int conteoCuentas { get; set; }
        public List<Cuenta> cuentas { get; set; }
    }

}
