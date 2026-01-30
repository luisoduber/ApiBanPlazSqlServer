using ApiBanPlaz.models.Entities;
using ApiBanPlaz.models.TokenDl;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ApiBanPlaz.Servicios.ConsultarDl
{


    public class ConsultarDlService
    {
        private readonly BanPlazDbContext _context;

        public ConsultarDlService(BanPlazDbContext context)
        {
            _context = context;
        }

        public async Task<int> GrdConsultarDIAsync(
            string prmId,
            string prmCuenta_pagador,
            string prmEndtoend,
            string prmReferencia_c,
            decimal prmMonto,
            string prmCanal,
            string prmCadReq)
        {
            var sql = @"
            EXEC spGrdConsultarDIReq
            @prmId,
            @prmCuenta_pagador,
            @prmEndtoend,
            @prmReferencia_c,
            @prmMonto,
            @prmCanal,
            @prmCadReq";

            var id = await _context.Database
         .SqlQueryRaw<int>(
             sql,
             new SqlParameter("@prmId", prmId),
             new SqlParameter("@prmCuenta_pagador", prmCuenta_pagador),
             new SqlParameter("@prmEndtoend", prmEndtoend),
             new SqlParameter("@prmReferencia_c", prmReferencia_c),
             new SqlParameter("@prmMonto", prmMonto),
             new SqlParameter("@prmCanal", prmCanal),
             new SqlParameter("@prmCadReq", prmCadReq)
         )
         .ToListAsync();

            return id.First();
        }

        public async Task<bool> GrdConsultarDIRespAsync(
            int prmIdConsultarDI,
            string prmCodigoRespuesta,
            string prmDescripcionCliente,
            string prmDescripcionSistema,
            DateTime prmFechaHora, 
            string prmCadResp)
        {
            try
            {
                var sql = @"
            EXEC spGrdConsultarDIResp
                @prmIdConsultarDI,
                @prmCodigoRespuesta,
                @prmDescripcionCliente,
                @prmDescripcionSistema,
                @prmFechaHora,
                @prmCadResp";

                var rows = await _context.Database.ExecuteSqlRawAsync(
                    sql,
                    new SqlParameter("@prmIdConsultarDI", prmIdConsultarDI),
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
