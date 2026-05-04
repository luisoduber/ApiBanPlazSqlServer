using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ApiBanPlaz.Servicios.CuentasMov
{
    public class CuentasMovService
    {
        private readonly BanPlazDbContext _context;
        public CuentasMovService(BanPlazDbContext context)
        {
            _context = context;
        }

        public async Task<int> GrdCuentasMovReq(
           string prmCuenta,
           string prmMoneda,
           string prmReferencia,
           string prmFechaInicio,
           string prmFechaFin,
           string prmTipo,
           decimal prmMontoMinimo,
           decimal prmMontoMaximo,
           string prmConcepto,
           string prmCadReq)
        {
            var sql = @"
            EXEC spGrdCuentasMovReq
            @prmCuenta,
            @prmMoneda,
            @prmReferencia,
            @prmFechaInicio,
            @prmFechaFin,
            @prmTipo,
            @prmMontoMinimo,
            @prmMontoMaximo,
            @prmConcepto,
            @prmCadReq";

            var id = await _context.Database
         .SqlQueryRaw<int>(
             sql,
             new SqlParameter("@prmCuenta", prmCuenta),
             new SqlParameter("@prmMoneda", prmMoneda),
             new SqlParameter("@prmReferencia", prmReferencia),
             new SqlParameter("@prmFechaInicio", prmFechaInicio),
             new SqlParameter("@prmFechaFin", prmFechaFin),
             new SqlParameter("@prmTipo", prmTipo),
             new SqlParameter("@prmMontoMinimo", prmMontoMinimo),
             new SqlParameter("@prmMontoMaximo", prmMontoMaximo),
             new SqlParameter("@prmConcepto", prmConcepto),
             new SqlParameter("@prmCadReq", prmCadReq)
         )
         .ToListAsync();
            return id.First();
        }

        public async Task<bool> GrdCuentMovResp(
            int prmIdCuent,
            string prmCodigoRespuesta,
            string prmDescripcionCliente,
            string prmDescripcionSistema,
            DateTime prmFechaHora,
            int prmCantMov,
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
                EXEC spGrdCuentMovResp
                @prmIdCuent,
                @prmCodigoRespuesta,
                @prmDescripcionCliente,
                @prmDescripcionSistema,
                @prmFechaHora,
                @prmCantMov,
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
                    new SqlParameter("@prmCodigoRespuesta", prmCodigoRespuesta),
                    new SqlParameter("@prmDescripcionCliente", prmDescripcionCliente),
                    new SqlParameter("@prmDescripcionSistema", prmDescripcionSistema),
                    new SqlParameter("@prmFechaHora", prmFechaHora),
                    new SqlParameter("@prmCantMov", prmCantMov),
                    new SqlParameter("@prmNumero", prmNumero),
                    new SqlParameter("@prmFechaApertura", prmFechaApertura),
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
        public async Task<bool> GrdCuentListMov(
            int prmIdCuent,
            string prmNumero,
            string prmFecha,
            string prmHora,
            string prmReferencia,
            string prmConcepto,
            string prmTipo,
            string prmNaturaleza,
            decimal prmMonto,
            string prmCadResp)
        {
            try
            {
                var sql = @"
                EXEC spGrdCuentListMov
                @prmIdCuent,
                @prmnumero,
                @prmFecha,
                @prmHora,
                @prmReferencia,
                @prmConcepto,
                @prmTipo,
                @prmNaturaleza,
                @prmMonto,
                @prmCadResp";

                var rows = await _context.Database.ExecuteSqlRawAsync(
                    sql,
                    new SqlParameter("@prmIdCuent", prmIdCuent),
                    new SqlParameter("@prmNumero", prmNumero),
                    new SqlParameter("prmFecha", prmFecha),
                    new SqlParameter("@prmHora", prmHora),
                    new SqlParameter("@prmReferencia", prmReferencia),
                    new SqlParameter("@prmConcepto", prmConcepto),
                    new SqlParameter("@prmTipo", prmTipo),
                    new SqlParameter("@prmNaturaleza", prmNaturaleza),
                    new SqlParameter("@prmMonto", prmMonto),
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
