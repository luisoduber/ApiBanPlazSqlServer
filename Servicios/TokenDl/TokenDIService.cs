using ApiBanPlaz.models.Entities;
using ApiBanPlaz.models.TokenDl;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using static DebinController;

namespace ApiBanPlaz.Servicios.TokenDl
{


    public class TokenDIService
    {
        private readonly BanPlazDbContext _context;

        public TokenDIService(BanPlazDbContext context)
        {
            _context = context;
        }

        public async Task<int> GrdTokenDIAsync(
            string prmMoneda,
            string prmCanal,
            string prmTvalidacion_p,
            string prmIdentificacion_p,
            string prmCuenta_cobrador,
            string prmCuenta_pagador,
            string prmTelefono_pagador,
            string prmCod_banco_p,
            decimal prmMonto,
            string prmDireccion_ip,
            string prmCadReq)
        {
            var sql = @"
        EXEC spGrdTokenDIReq
            @prmMoneda,
            @prmCanal,
            @prmTvalidacion_p,
            @prmIdentificacion_p,
            @prmCuenta_cobrador,
            @prmCuenta_pagador,
            @prmTelefono_pagador,
            @prmCod_banco_p,
            @prmMonto,
            @prmDireccion_ip,
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
             new SqlParameter("@prmMonto", prmMonto),
             new SqlParameter("@prmDireccion_ip", prmDireccion_ip),
             new SqlParameter("@prmCadReq", prmCadReq)
         )
         .ToListAsync();

            return id.First();
        }

        public async Task<bool> GrdTokenDIRespAsync(
            int prmIdTokenDI,
            string prmCodigoRespuesta,
            string prmDescripcionCliente,
            string prmDescripcionSistema,
            DateTime prmFechaHora, 
            string prmCadResp)
        {
            try
            {
                var sql = @"
            EXEC spGrdTokenDIResp
                @prmIdTokenDI,
                @prmCodigoRespuesta,
                @prmDescripcionCliente,
                @prmDescripcionSistema,
                @prmFechaHora,
                @prmCadResp";

                var rows = await _context.Database.ExecuteSqlRawAsync(
                    sql,
                    new SqlParameter("@prmIdTokenDI", prmIdTokenDI),
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
