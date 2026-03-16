using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ApiBanPlaz.Servicios.CompPm
{
    public class CompPmService
    {
        private readonly BanPlazDbContext _context;
        public CompPmService(BanPlazDbContext context)
        {
            _context = context;
        }

        public async Task<int> GrdCompPmReq(
           string prmId,
           string prmCanal,
           int prmAcc,
           DateTime prmfi,
           DateTime prmFf,
           string prmTlf,
           string prmTlfa,
           string prmHoraIni,
           string prmHoraFin,
           string prmCadReq)

        {
            var sql = @"
            EXEC spGrdCompPmReq
            @prmId, 
            @prmCanal, 
            @prmAcc, 
            @prmfi,
            @prmFf, 
            @prmTlf,
            @prmTlfa,
            @prmHoraIni,
            @prmHoraFin,
            @prmCadReq";

            var id = await _context.Database
         .SqlQueryRaw<int>(
             sql,
             new SqlParameter("@prmId", prmId),
             new SqlParameter("@prmCanal", prmCanal),
             new SqlParameter("@prmAcc", prmAcc),
             new SqlParameter("@prmfi", prmfi),
             new SqlParameter("@prmFf", prmFf),
             new SqlParameter("@prmTlf", prmTlf),
             new SqlParameter("@prmTlfa", prmTlfa),
             new SqlParameter("@prmHoraIni", prmHoraIni),
             new SqlParameter("@prmHoraFin", prmHoraFin),
             new SqlParameter("@prmCadReq", prmCadReq)
         )
         .ToListAsync();

            return id.First();
        }

        public async Task<bool> GrdCompPmResp(
            int prmIdCompPm,
            string prmCodigoRespuesta,
            string prmDescripcionCliente,
            string prmDescripcionSistema,
            DateTime prmFechaHora,
            int prmCantidadPagos,
            string prmCadResp)
        {
            try
            {
                var sql = @"
            EXEC spGrdCompPmResp
                @prmIdCompPm, 
                @prmCodigoRespuesta,
                @prmDescripcionCliente, 
                @prmDescripcionSistema,
                @prmFechaHora, 
                @prmCantidadPagos, 
                @prmCadResp";

                var rows = await _context.Database.ExecuteSqlRawAsync(
                    sql,
                    new SqlParameter("@prmIdCompPm", prmIdCompPm),
                    new SqlParameter("@prmCodigoRespuesta", prmCodigoRespuesta),
                    new SqlParameter("@prmDescripcionCliente", prmDescripcionCliente),
                    new SqlParameter("@prmDescripcionSistema", prmDescripcionSistema),
                    new SqlParameter("@prmFechaHora", prmFechaHora),
                     new SqlParameter("@prmCantidadPagos", prmCantidadPagos),
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
        public async Task<bool> GrdCompPmPag(
            int prmIdCompPm,
            string prmAccion,
            string prmBanco,
            string prmTelefonoCliente,
            string prmTelefonoAfiliado,
            decimal prmMonto,
            string prmFecha,
            string prmHora,
            string prmReferencia,
            string prmMotivo,
            string prmCadResp)

        {
            try
            {
                var sql = @"
            EXEC spGrdCompPmPag
                @prmIdCompPm,
                @prmAccion,
                @prmBanco,
                @prmTelefonoCliente,
                @prmTelefonoAfiliado,
                @prmMonto,
                @prmFecha,
                @prmHora,
                @prmReferencia,
                @prmMotivo, 
                @prmCadResp";

                var rows = await _context.Database.ExecuteSqlRawAsync(
                    sql,
                    new SqlParameter("@prmIdCompPm", prmIdCompPm),
                    new SqlParameter("@prmAccion", prmAccion),
                    new SqlParameter("@prmBanco", prmBanco),
                    new SqlParameter("@prmTelefonoCliente", prmTelefonoCliente),
                    new SqlParameter("@prmTelefonoAfiliado", prmTelefonoAfiliado),
                    new SqlParameter("@prmMonto", prmMonto),
                    new SqlParameter("@prmFecha", prmFecha),
                    new SqlParameter("@prmHora", prmHora),
                    new SqlParameter("@prmReferencia", prmReferencia),
                    new SqlParameter("@prmMotivo", prmMotivo),
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


