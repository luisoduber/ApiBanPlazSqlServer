using ApiBanPlaz.models.Entities;
using ApiBanPlaz.models.PagosP2p;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using static TokenDIController;

namespace ApiBanPlaz.Servicios.PagosP2p
{
    public class PagosP2pService
    {
        private readonly BanPlazDbContext _context;

        public PagosP2pService(BanPlazDbContext context)
        {
            _context = context;
        }

        public async Task<int> spGrdPagosP2pReq(
            string prmBanco,
            string prmIdBeneficiario,
            string prmTelefono,
            decimal prmMonto,
            string prmMotivo,
            string prmCanal,
            string prmIdExterno,
            string prmCuenta,
            string prmTelefonoAfiliado,
            string prmMoneda,
            string prmSucursal,
            string prmCajero,
            string prmCaja,
            string prmIpCliente,
            string prmLongitud,
            string prmLatitud,
            string prmPrecision,
            string prmCadReq)
        {
            var sql = @"
        EXEC spGrdPagosP2pReq
            @prmBanco,
            @prmIdBeneficiario,
            @prmTelefono,
            @prmMonto,
            @prmMotivo,
            @prmCanal,
            @prmIdExterno,
            @prmCuenta,
            @prmTelefonoAfiliado,
            @prmMoneda,
            @prmSucursal,
            @prmCajero,
            @prmCaja,
            @prmIpCliente,
            @prmLongitud,
            @prmLatitud,
            @prmPrecision,
            @prmCadReq";

            var id = await _context.Database
         .SqlQueryRaw<int>(
             sql,
             new SqlParameter("@prmBanco", prmBanco),
             new SqlParameter("@prmIdBeneficiario", prmIdBeneficiario),
             new SqlParameter("@prmTelefono", prmTelefono),
             new SqlParameter("@prmMonto", prmMonto),
             new SqlParameter("@prmMotivo", prmMotivo),
             new SqlParameter("@prmCanal", prmCanal),
             new SqlParameter("@prmIdExterno", prmIdExterno),
             new SqlParameter("@prmCuenta", prmCuenta),
             new SqlParameter("@prmMoneda", prmMoneda),
             new SqlParameter("@prmSucursal", prmSucursal),
             new SqlParameter("@prmCajero", prmCajero),
             new SqlParameter("@prmCaja", prmCaja),
             new SqlParameter("@prmIpCliente", prmIpCliente),
             new SqlParameter("@prmLongitud", prmLongitud),
             new SqlParameter("@prmLatitud", prmLatitud),
             new SqlParameter("@prmPrecision", prmPrecision),
             new SqlParameter("@prmCadReq", prmCadReq)
         )
         .ToListAsync();
          return id.First();
        }

        public async Task<bool> spGrdPagosP2pResp(
               int prmIdPagosP2p,
               string prmCodigoRespuesta,
               string prmDescripcionCliente,
               string prmDescripcionSistema,
               DateTime prmFechaHora,
               string prmNumeroReferencia,
               string prmCadResp)
        {
            try
            {
                var sql = @"
            EXEC spGrdPagosP2pResp
               @prmIdPagosP2p,
               @prmCodigoRespuesta,
               @prmDescripcionCliente,
               @prmDescripcionSistema,
               @prmFechaHora,
               @prmNumeroReferencia,
               @prmCadResp";

                var rows = await _context.Database.ExecuteSqlRawAsync(
                    sql,
                    new SqlParameter("@prmIdPagosP2p", prmIdPagosP2p),
                    new SqlParameter("@prmCodigoRespuesta", prmCodigoRespuesta),
                    new SqlParameter("@prmDescripcionCliente", prmDescripcionCliente),
                    new SqlParameter("@prmDescripcionSistema", prmDescripcionSistema),
                    new SqlParameter("@prmFechaHora", prmFechaHora),
                    new SqlParameter("@prmNumeroReferencia", prmNumeroReferencia),
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

