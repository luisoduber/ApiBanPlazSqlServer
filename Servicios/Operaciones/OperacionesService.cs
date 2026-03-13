using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ApiBanPlaz.Servicios.Operaciones
{
    public class OperacionesService
    {
        private readonly BanPlazDbContext _context;
        public OperacionesService(BanPlazDbContext context)
        {
            _context = context;
        }

        public async Task<int> GrdOperacionesReq(
           string prmRif_cliente,
           string prmCuenta,
           string prmMoneda,
           string prmTPago,
           string prmNaturaleza,
           string prmFechaInicio,
           string prmFechaFin,
           string prmCanal,
           string prmId,
           string prmBanco,
           string prmReferencia,
           decimal prmMontoMinimo,
           decimal prmMontoMaximo,
           string prmDireccion_ip,
           string prmCadReq)

        {
            var sql = @"
            EXEC spGrdOperacionesReq
            @prmRif_cliente,
            @prmCuenta,
            @prmMoneda,
            @prmTPago,
            @prmNaturaleza,
            @prmFechaInicio,
            @prmFechaFin,
            @prmCanal,
            @prmId,
            @prmBanco,
            @prmReferencia,
            @prmMontoMinimo,
            @prmMontoMaximo,
            @prmDireccion_ip,
            @prmCadReq";

            var id = await _context.Database
         .SqlQueryRaw<int>(
             sql,
             new SqlParameter("@prmRif_cliente", prmRif_cliente),
             new SqlParameter("@prmCuenta", prmCuenta),
             new SqlParameter("@prmMoneda", prmMoneda),
             new SqlParameter("@prmTPago", prmTPago),
             new SqlParameter("@prmNaturaleza", prmNaturaleza),
             new SqlParameter("@prmFechaInicio", prmFechaInicio),
             new SqlParameter("@prmFechaFin", prmFechaFin),
             new SqlParameter("@prmCanal", prmCanal),
             new SqlParameter("@prmId", prmId),
             new SqlParameter("@prmBanco", prmBanco),
             new SqlParameter("@prmReferencia", prmReferencia),
             new SqlParameter("@prmMontoMinimo", prmMontoMinimo),
             new SqlParameter("@prmMontoMaximo", prmMontoMaximo),
             new SqlParameter("@prmDireccion_ip", prmDireccion_ip),
             new SqlParameter("@prmCadReq", prmCadReq)
         )
         .ToListAsync();

            return id.First();
        }

        public async Task<bool> GrdOperacionesResp(
            int prmIdOperaciones,
            string prmCodigoRespuesta,
            string prmDescripcionCliente,
            string prmDescripcionSistema,
            DateTime prmFechaHora,
            int prmCantMovimientos,
            string prmCadResp)
        {
            try
            {
                var sql = @"
                EXEC spGrdOperacionesResp
                @prmIdOperaciones, 
                @prmCodigoRespuesta,
                @prmDescripcionCliente, 
                @prmDescripcionSistema,
                @prmFechaHora, 
                @prmCantMovimientos, 
                @prmCadResp";

                var rows = await _context.Database.ExecuteSqlRawAsync(
                    sql,
                    new SqlParameter("@prmIdOperaciones", prmIdOperaciones),
                    new SqlParameter("@prmCodigoRespuesta", prmCodigoRespuesta),
                    new SqlParameter("@prmDescripcionCliente", prmDescripcionCliente),
                    new SqlParameter("@prmDescripcionSistema", prmDescripcionSistema),
                    new SqlParameter("@prmFechaHora", prmFechaHora),
                    new SqlParameter("@prmCantMovimientos", prmCantMovimientos),
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
        public async Task<bool> GrdOperacionesMov(
            int prmIdOperaciones,
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
                EXEC spGrdOperacionesMov
                @prmIdOperaciones,
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
                    new SqlParameter("@prmIdOperaciones", prmIdOperaciones),
                    new SqlParameter("@prmFecha", prmFecha),
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
