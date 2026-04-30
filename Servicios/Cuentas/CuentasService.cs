using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ApiBanPlaz.Servicios.Cuentas
{
    public class CuentasService
    {
        private readonly BanPlazDbContext _context;
        public CuentasService(BanPlazDbContext context)
        {
            _context = context;
        }

        public async Task<int> GrdCuentasReq(
           string prmRif_cliente,
           string prmCuenta,
           string prmTelefono,
           string prmMoneda,
           string prmCadReq)

        {
            var sql = @"
            EXEC spGrdCuentasReq
            @prmRif_cliente,
            @prmCuenta,
            @prmTelefono,
            @prmMoneda,
            @prmCadReq";

            var id = await _context.Database
         .SqlQueryRaw<int>(
             sql,
             new SqlParameter("@prmRif_cliente", prmRif_cliente),
             new SqlParameter("@prmCuenta", prmCuenta),
             new SqlParameter("@prmTelefono", prmTelefono),
             new SqlParameter("@prmMoneda", prmMoneda),
             new SqlParameter("@prmCadReq", prmCadReq)
         )
         .ToListAsync();
          return id.First();
        }

        public async Task<bool> GrdCuentasResp(
            int prmIdCuent,
            string prmCodigoRespuesta,
            string prmDescripcionCliente,
            string prmDescripcionSistema,
            DateTime prmFechaHora,
            int prmConteoCuentas,
            string prmCadResp)


        {
            try
            {
                var sql = @"
                EXEC spGrdCuentResp
                @prmIdCuent, 
                @prmCodigoRespuesta,
                @prmDescripcionCliente, 
                @prmDescripcionSistema,
                @prmFechaHora, 
                @prmConteoCuentas, 
                @prmCadResp";

                var rows = await _context.Database.ExecuteSqlRawAsync(
                    sql,
                    new SqlParameter("@prmIdCuent", prmIdCuent),
                    new SqlParameter("@prmCodigoRespuesta", prmCodigoRespuesta),
                    new SqlParameter("@prmDescripcionCliente", prmDescripcionCliente),
                    new SqlParameter("@prmDescripcionSistema", prmDescripcionSistema),
                    new SqlParameter("@prmFechaHora", prmFechaHora),
                    new SqlParameter("@prmConteoCuentas", prmConteoCuentas),
                    new SqlParameter("@prmCadResp", prmCadResp)
                );

                return rows > 0 ? true : false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: " + ex.Message);
                return false;
            }
        }
        public async Task<bool> GrdCuentasList(
            int prmIdCuent,
            string prmNumero,
            string prmFechaApertura,
            string prmTipoCuenta,
            string prmEstatus,
            string prmMoneda,
            decimal prmSaldoDisponible,
            string prmCadResp)

        {
            try
            {
                var sql = @"
                EXEC spGrdCuentList
                @prmIdCuent,
                @prmNumero,
                @prmFechaApertura,
                @prmTipoCuenta,
                @prmEstatus,
                @prmMoneda,
                @prmSaldoDisponible,
                @prmCadResp";

                var rows = await _context.Database.ExecuteSqlRawAsync(
                    sql,
                    new SqlParameter("@prmIdCuent", prmIdCuent),
                    new SqlParameter("@prmNumero", prmNumero),
                    new SqlParameter("prmFechaApertura", prmFechaApertura),
                    new SqlParameter("@prmTipoCuenta", prmTipoCuenta),
                    new SqlParameter("@prmEstatus", prmEstatus),
                    new SqlParameter("@prmMoneda", prmMoneda),
                    new SqlParameter("@prmSaldoDisponible", prmSaldoDisponible),
                    new SqlParameter("@prmCadResp", prmCadResp)
                );

                return rows > 0 ? true : false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: " + ex.Message);
                return false;
            }
        }
    }
}
