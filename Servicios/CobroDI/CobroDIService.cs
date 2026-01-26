using ApiBanPlaz.models.Entities;
using ApiBanPlaz.models.CobroDl;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using static TokenDIController;

namespace ApiBanPlaz.Servicios.CobroDl
{


    public class CobroDIService
    {
        private readonly BanPlazDbContext _context;

        public CobroDIService(BanPlazDbContext context)
        {
            _context = context;
        }

        public async Task<int> GrdCobroDIAsync(
            string prmMoneda,
            string prmCanal,
            string prmTvalidacion_p,
            string prmIdentificacion_p,
            string prmCuenta_cobrador,
            string prmCuenta_pagador,
            string prmTelefono_pagador,
            string prmCod_banco_p,
            string prmNombre_p,
            decimal prmMonto,
            string prmConcepto,
            string prmToken_p,
            string prmDireccion_ip,
            string prmReferencia_c,
            string prmCadReq)
        {

            var sql = @"
        EXEC spGrdCobroDIReq
            @prmMoneda,
            @prmCanal,
            @prmTvalidacion_p,
            @prmIdentificacion_p,
            @prmCuenta_cobrador,
            @prmCuenta_pagador,
            @prmTelefono_pagador,
            @prmCod_banco_p,
            @prmNombre_p,
            @prmMonto,
            @prmConcepto,
            @prmToken_p,
            @prmDireccion_ip,
            @prmReferencia_c,
            @prmCadReq";

            var id = await _context.Database
         .SqlQueryRaw<int>(
             sql,
             new SqlParameter("@prmMoneda", prmMoneda),
             new SqlParameter("@prmCanal", prmCanal),
             new SqlParameter("@prmTvalidacion_p", prmTvalidacion_p),
             new SqlParameter("@prmIdentificacion_p", prmIdentificacion_p),
             new SqlParameter("@prmCuenta_cobrador", prmCuenta_cobrador),
             new SqlParameter("@prmCuenta_pagador", prmCuenta_pagador),
             new SqlParameter("@prmTelefono_pagador", prmTelefono_pagador),
             new SqlParameter("@prmCod_banco_p", prmCod_banco_p),
             new SqlParameter("@prmNombre_p", prmNombre_p),
             new SqlParameter("@prmMonto", prmMonto),
             new SqlParameter("@prmConcepto", prmConcepto),
             new SqlParameter("@prmToken_p", prmToken_p),
             new SqlParameter("@prmDireccion_ip", prmDireccion_ip),
             new SqlParameter("@prmReferencia_c", prmReferencia_c),
             new SqlParameter("@prmCadReq", prmCadReq)
         )
         .ToListAsync();

            return id.First();
        }

        public async Task<bool> GrdCobroDIRespAsync(
            int prmIdTokenDI,
            string prmCodigoRespuesta,
            string prmDescripcionCliente,
            string prmDescripcionSistema,
            DateTime prmFechaHora,
            string prmReferencia_c,
            string prmEndtoend,
            string prmCadResp)
        {
            try
            {
                var sql = @"
            EXEC spGrdCobroDIResp
                @prmIdTokenDI,
                @prmCodigoRespuesta,
                @prmDescripcionCliente,
                @prmDescripcionSistema,
                @prmFechaHora,
                @prmReferencia_c,
                @prmEndtoend,
                @prmCadResp";

                var rows = await _context.Database.ExecuteSqlRawAsync(
                    sql,
                    new SqlParameter("@prmIdTokenDI", prmIdTokenDI),
                    new SqlParameter("@prmCodigoRespuesta", prmCodigoRespuesta),
                    new SqlParameter("@prmDescripcionCliente", prmDescripcionCliente),
                    new SqlParameter("@prmDescripcionSistema", prmDescripcionSistema),
                    new SqlParameter("@prmFechaHora", prmFechaHora),
                    new SqlParameter("@prmReferencia_c", prmReferencia_c),
                    new SqlParameter("@prmEndtoend", prmEndtoend),
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
