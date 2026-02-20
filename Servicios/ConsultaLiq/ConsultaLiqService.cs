using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ApiBanPlaz.Servicios.ConsultaLiq
{


    public class ConsultaLiqService
    {
        private readonly BanPlazDbContext _context;

        public ConsultaLiqService(BanPlazDbContext context)
        {
            _context = context;
        }

        public async Task<int> GrdConsultaLiq(
            string prmId,
            string prmCuenta,
            string prmReferencia,
            decimal prmMonto,
            string prmfecha,
            string prmCanal,
            string prmCadReq)

        {
            var sql = @"
            EXEC spGrdConsultarDIReq
            @prmId,
            @prmCuenta,
            @prmReferencia,
            @prmMonto,
            @prmfecha,
            @prmCanal,
            @prmCadReq";

            var id = await _context.Database
         .SqlQueryRaw<int>(
             sql,
             new SqlParameter("@prmId", prmId),
             new SqlParameter("@prmCuenta", prmCuenta),
             new SqlParameter("@prmReferencia", prmReferencia),
             new SqlParameter("@prmMonto", prmMonto),
             new SqlParameter("@prmfecha", prmfecha),
             new SqlParameter("@prmCanal", prmCanal),
             new SqlParameter("@prmCadReq", prmCadReq)
         )
         .ToListAsync();

            return id.First();
        }

        public async Task<bool> GrdConsultaLiqResp(
            int prmIdConsultaLiq,
            string prmCodigoRespuesta,
            string prmDescripcionCliente,
            string prmDescripcionSistema,
            DateTime prmFechaHora,
            string prmCadResp)
        {
            try
            {
                var sql = @"
            EXEC spGrdConsultaLiqResp
                @prmIdConsultaLiq,
                @prmCodigoRespuesta,
                @prmDescripcionCliente,
                @prmDescripcionSistema,
                @prmFechaHora,
                @prmCadResp";

                var rows = await _context.Database.ExecuteSqlRawAsync(
                    sql,
                    new SqlParameter("@prmIdConsultaLiq", prmIdConsultaLiq),
                    new SqlParameter("@prmCodigoRespuesta", prmCodigoRespuesta),
                    new SqlParameter("@prmDescripcionCliente", prmDescripcionCliente),
                    new SqlParameter("@prmDescripcionSistema", prmDescripcionSistema),
                    new SqlParameter("@prmFechaHora", prmFechaHora),
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
