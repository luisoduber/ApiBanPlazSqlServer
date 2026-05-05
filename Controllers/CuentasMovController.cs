using ApiBanPlaz.models.Cuentas;
using ApiBanPlaz.models.CuentasMov;
using ApiBanPlaz.Servicios.CuentasMov;
using ApiBanPlaz.Servicios.General;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace ApiBanPlaz.Controllers
{
    [ApiController]
    [Route("v0/")]
    public class CuentasMovController : Controller
    {
        private readonly NonceService _nonceService;
        private readonly CredApiRsService _credApiRsService;

        private readonly CuentasMovService _CuentasMovService;
        private readonly IConfiguration _config;
        string urlBan = "";
        int idCuent = 0;

        CuentasListMov _CuentasListMov = new CuentasListMov();
        CuentasMovResp _CuentasMovResp = new CuentasMovResp();
        CuentasMovReq _CuentasMovReq = new CuentasMovReq();
        CuentasMov _CuentasMov = new CuentasMov();

        public CuentasMovController(IConfiguration config, NonceService nonceService,
                            CredApiRsService credApiRsService, CuentasMovService CuentasMovService)
        {
            _nonceService = nonceService;
            _credApiRsService = credApiRsService;
            _config = config;
            urlBan = _config["urlBan"].ToString();
            _CuentasMovService = CuentasMovService;
        }

        [HttpGet("v0/cuentasMov/{id}/{cuenta}/movimientos")]

        public async Task<IActionResult> Cuentas(string id, string cuenta, 
        [FromQuery] string moneda,
        [FromQuery] string fechaInicio,
        [FromQuery] string fechafin)
        {
            string nonce = await _nonceService.ObtNonce();
            var cred = await _credApiRsService.ObtCredApi();
            if (cred == null) return NotFound();

            bool rsvalidId = string.IsNullOrWhiteSpace(id);
            bool rsvalidCuent = string.IsNullOrWhiteSpace(cuenta);




            if ((rsvalidId == false) && (rsvalidCuent == false))
            {
                return BadRequest("El ID o la Cuenta no pueden estar vacíos.");
            }
            if (id.Length) && (rsvalidCuent == false))
            {
            }


                string qryStringListMov = "";
            string path = "v0/cuentas";
            string apiSignature = ApiSignatureGen.Generar(
                path,
                nonce,
                cred.apiKeySecret
            );

            qryStringListMov =
                "?moneda=" + moneda +
                "&fechaInicio=" + fechaInicio +
                "&fechafin=" + fechafin;

            _CuentasMovResp = await SolCuentas(id, qryStringListMov, cred.ApiKey, apiSignature, nonce);
            _CuentasMov.idCuent = await _CuentasMovService.GrdCuentasMovReq
            (
                _CuentasMovReq.Cuenta,
                _CuentasMovReq.Moneda,
                _CuentasMovReq.prmReferencia,
                _CuentasMovReq.FechaInicio,
                _CuentasMovReq.FechaFin,
                _CuentasMovReq.Tipo,
                _CuentasMovReq.MontoMinimo,
                _CuentasMovReq.MontoMaximo,
                _CuentasMovReq.Concepto,
                ""
             );

            string jsonCuentasMovResp = JsonConvert.SerializeObject(_CuentasMovResp);
            bool rsValCuentasMovResp = await _CuentasMovService.GrdCuentMovResp
            (
                _CuentasMov.idCuent,
                _CuentasMovResp.CodigoRespuesta,
                _CuentasMovResp.DescripcionCliente,
                _CuentasMovResp.DescripcionSistema,
                _CuentasMovResp.FechaHora,
                _CuentasMovResp.CantMov,
                _CuentasListMov.numero,
                _CuentasListMov.fechaApertura,
                _CuentasListMov.tipoCuenta,
                _CuentasListMov.estatus,
                _CuentasListMov.moneda,
                _CuentasListMov.saldoDisponible,
                jsonCuentasMovResp
            );

        bool rsValCuentasMovList = false;

                if ((_CuentasListMov != null) && (_CuentasListMov.movimientos != null))
                {
                    foreach (var rsDat in _CuentasListMov.movimientos)
                    {
                        string jsonCuentMovList = JsonConvert.SerializeObject(_CuentasListMov.movimientos);
                        rsValCuentasMovList = await _CuentasMovService.GrdCuentListMov
                        (
                             _CuentasMov.idCuent,
                             _CuentasListMov.numero,
                             rsDat.fecha,
                             rsDat.hora,
                             rsDat.referencia,
                             rsDat.concepto,
                             rsDat.tipo,
                             rsDat.naturaleza,
                              rsDat.monto,
                            jsonCuentMovList
                        );
                    }
                }
            

            
            return Ok(new
            {
                //reqOperacion,
                _CuentasMov.idCuent,
                rsValCuentasMovResp,
                rsValCuentasMovList,
                _CuentasMovResp.CantMov,
                _CuentasMovResp.CodigoRespuesta,
                _CuentasMovResp.DescripcionCliente,
                _CuentasMovResp.DescripcionSistema,
                _CuentasMovResp.FechaHora
            });
        }

        public async Task<CuentasMovResp> SolCuentas(string prmId, string prmQryStringListMov, 
                                                    string prmApiKey,string prmApiSignature, 
                                                    string prmNonce)
        {
            string rsDat = "", prmPath="";
            string codigoRespuesta = "";
            string descripcionCliente = "";
            string descripcionSistema = "";
            string fechaHora = "";
            string referencia_c = "";
            string endtoend = "";

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(urlBan);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("api-key", prmApiKey);
                client.DefaultRequestHeaders.Add("api-signature", prmApiSignature);
                client.DefaultRequestHeaders.Add("nonce", prmNonce);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (prmId == "default") { prmPath = $"v0/cuentas/{prmId}/movimientos?{prmQryStringListMov}"; }
                else { prmPath = $"v0/cuentas/{prmId}"; }

                using (var Res = await client.GetAsync(prmPath))
                {
                    if (Res.Headers.TryGetValues("codigoRespuesta", out var values)) { codigoRespuesta = values.FirstOrDefault(); }
                    if (Res.Headers.TryGetValues("descripcionCliente", out var values1)) { descripcionCliente = values1.FirstOrDefault(); }
                    if (Res.Headers.TryGetValues("descripcionSistema", out var values2)) { descripcionSistema = values2.FirstOrDefault(); }
                    if (Res.Headers.TryGetValues("fechaHora", out var values3)) { fechaHora = values3.FirstOrDefault(); }

                    rsDat = await Res.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(rsDat))
                    {
                        _CuentasListMov = JsonConvert.DeserializeObject<CuentasListMov>(rsDat);
                        if ((_CuentasListMov != null) && (_CuentasListMov.movimientos != null))
                        {
                            _CuentasMovResp.CantMov = _CuentasListMov.movimientos.Count;
                        }

                    }
                    _CuentasMovResp.CodigoRespuesta = codigoRespuesta;
                    _CuentasMovResp.DescripcionCliente = descripcionCliente;
                    _CuentasMovResp.DescripcionSistema = descripcionSistema;
                    _CuentasMovResp.FechaHora = DateTime.Parse(fechaHora);
                }
            }
            return _CuentasMovResp;
        }

        public static class ApiSignatureGen
        {
            public static string Generar(string path, string nonce, string secret)
            {
                string signatureRaw = $"/{path}{nonce}";
                byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
                byte[] messageBytes = Encoding.UTF8.GetBytes(signatureRaw);

                using (var hmac = new HMACSHA384(keyBytes))
                {
                    byte[] hashBytes = hmac.ComputeHash(messageBytes);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }
            }
        }
    }
}
