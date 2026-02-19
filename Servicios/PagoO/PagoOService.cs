using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ApiBanPlaz.Servicios.PagoO
{
    public class PagoOService
    {
        private readonly BanPlazDbContext _context;

        public PagoOService(BanPlazDbContext context)
        {
            _context = context;
        }

        public async Task<int> spGrdPagoOReq(
        string prmMoneda,
        string prmCanal,
        string prmTipo_cce,
        string prmTipo_proposito,
        string prmTipo_instrumento_b,
        string prmIdentificacion_o,
        string prmIdentificacion_b,
        string prmCuenta_origen,
        string prmCuenta_destino,
        string prmTelefono,
        string prmCorreo,
        string prmCod_banco_d,
        string prmCod_banco_a,
        string prmNombre_d,
        string prmNombre_a,
        decimal prmMonto,
        string prmConcepto,
        string prmDireccion_ip,
        string prmReferencia,
        DateTime prmFecha_hora,
        string prmCadReq)
        {
            var sql = @"
        EXEC spGrdPagos0Req
            @prmMoneda,
            @prmCanal,
            @prmTipo_cce,
            @prmTipo_proposito,
            @prmTipo_instrumento_b,
            @prmIdentificacion_o,
            @prmIdentificacion_b,
            @prmCuenta_origen,
            @prmCuenta_destino,
            @prmTelefono,
            @prmCorreo,
            @prmCod_banco_d,
            @prmCod_banco_a,
            @prmNombre_d,
            @prmNombre_a,
            @prmMonto,
            @prmConcepto,
            @prmDireccion_ip,
            @prmReferencia,
            @prmFecha_hora,
            @prmCadReq";

            var id = await _context.Database
         .SqlQueryRaw<int>(
             sql,
             new SqlParameter("@prmMoneda", prmMoneda),
             new SqlParameter("@prmCanal", prmCanal),
             new SqlParameter("@prmTipo_cce", prmTipo_cce),
             new SqlParameter("@prmTipo_proposito", prmTipo_proposito),
             new SqlParameter("@prmTipo_instrumento_b", prmTipo_instrumento_b),
             new SqlParameter("@prmIdentificacion_o", prmIdentificacion_o),
             new SqlParameter("@prmIdentificacion_b", prmIdentificacion_b),
             new SqlParameter("@prmCuenta_origen", prmCuenta_origen),
             new SqlParameter("@prmCuenta_destino", prmCuenta_destino),
             new SqlParameter("@prmMoneda", prmMoneda),
             new SqlParameter("@prmTelefono", prmTelefono),
             new SqlParameter("@prmCorreo", prmCorreo),
             new SqlParameter("@prmCod_banco_d", prmCod_banco_d),
             new SqlParameter("@prmCod_banco_a", prmCod_banco_a),
             new SqlParameter("@prmNombre_d", prmNombre_d),
             new SqlParameter("@prmNombre_a", prmNombre_a),
             new SqlParameter("@prmMonto", prmMonto),
             new SqlParameter("@prmConcepto", prmConcepto),
             new SqlParameter("@prmDireccion_ip", prmDireccion_ip),
             new SqlParameter("@prmReferencia", prmReferencia),
             new SqlParameter("@prmFecha_hora", prmFecha_hora),
             new SqlParameter("@prmCadReq", prmCadReq))
         .ToListAsync();
            return id.First();
        }

        public async Task<bool> spGrdPagoOResp(
               int prmIdPagoO,
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
            EXEC spGrdPagos0Resp
               @prmIdPagos0,
               @prmCodigoRespuesta,
               @prmDescripcionCliente,
               @prmDescripcionSistema,
               @prmFechaHora,
               @prmNumeroReferencia,
               @prmCadResp";

                var rows = await _context.Database.ExecuteSqlRawAsync(
                    sql,
                    new SqlParameter("@prmIdPagos0", prmIdPagoO),
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


