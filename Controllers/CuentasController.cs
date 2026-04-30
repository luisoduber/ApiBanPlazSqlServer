using ApiBanPlaz.models.Cuentas;
using ApiBanPlaz.Servicios.Cuentas;
using ApiBanPlaz.Servicios.General;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace ApiBanPlaz.Controllers
{
    [ApiController]
    [Route("v0/")]
    public class CuentasController : Controller
    {
        private readonly NonceService _nonceService;
        private readonly CredApiRsService _credApiRsService;

        private readonly CuentasService _CuentasService;
        private readonly IConfiguration _config;
        string urlBan = "";
        int idCuent = 0;

        CuentasList _CuentasList = new CuentasList();
        CuentasResp _CuentasResp = new CuentasResp();
        CuentasReq _CuentasReq = new CuentasReq();
        Cuentas _Cuentas = new Cuentas();
        Cuenta _Cuenta = new Cuenta();
        public CuentasController(IConfiguration config, NonceService nonceService,
                            CredApiRsService credApiRsService, CuentasService CuentasService)
        {
            _nonceService = nonceService;
            _credApiRsService = credApiRsService;
            _config = config;
            urlBan = _config["urlBan"].ToString();
            _CuentasService = CuentasService;
        }

        [HttpGet("Cuentas/{id}/{cuenta}")]

        public async Task<IActionResult> Cuentas(string id, string cuenta)
        {
            string nonce = await _nonceService.ObtNonce();
            var cred = await _credApiRsService.ObtCredApi();
            if (cred == null) return NotFound();

            string path = "v0/cuentas";
            string apiSignature = ApiSignatureGen.Generar(
                path,
                nonce,
                cred.apiKeySecret
            );

            if (id == "default") { _CuentasReq.Rif_cliente = "J00408109263"; _CuentasReq.Cuenta = ""; }
            if ((id.Length == 12) && (cuenta.Length == 20)) { _CuentasReq.Rif_cliente = id; _CuentasReq.Cuenta = cuenta; }
            if (id.Length == 12) { _CuentasReq.Rif_cliente = id; _CuentasReq.Cuenta = ""; }
            if (id.Length == 20) { _CuentasReq.Rif_cliente = "" ; _CuentasReq.Cuenta = id; }

            _CuentasReq.Telefono = "";
            _CuentasReq.Moneda = "";

            _CuentasResp = await SolCuentas(id, cred.ApiKey, apiSignature, nonce);
            _Cuentas.idCuent = await _CuentasService.GrdCuentasReq
            (
                _CuentasReq.Rif_cliente,
                _CuentasReq.Cuenta,
                _CuentasReq.Telefono,
                _CuentasReq.Moneda,
                 ""
             );

            string jsonCuentasResp = JsonConvert.SerializeObject(_CuentasResp);
            bool rsValCuentasResp = await _CuentasService.GrdCuentasResp
            (
                _Cuentas.idCuent,
                _CuentasResp.CodigoRespuesta,
                _CuentasResp.DescripcionCliente,
                _CuentasResp.DescripcionSistema,
                _CuentasResp.FechaHora,
                _CuentasResp.conteoCuentas,
                jsonCuentasResp
            );

            bool rsValCuentasList = false;

            if ((id == "default"))
            {
                if ((_CuentasList != null) && (_CuentasList.cuentas != null))
                {
                    foreach (var rsDat in _CuentasList.cuentas)
                    {
                        string jsonCuentList = JsonConvert.SerializeObject(_CuentasList.cuentas);
                        rsValCuentasList = await _CuentasService.GrdCuentasList
                        (
                             _Cuentas.idCuent,
                             rsDat.numero,
                             rsDat.fechaApertura,
                             rsDat.tipoCuenta,
                             rsDat.estatus,
                             rsDat.moneda,
                             rsDat.saldoDisponible,
                            jsonCuentList
                        );
                    }
                }
            }

            else if ((id.Length == 12) && (cuenta.Length == 20))
                {
                Debug.WriteLine($"cuenta: {_Cuenta}");
                string jsonCuentList = JsonConvert.SerializeObject(_Cuenta);
                rsValCuentasList = await _CuentasService.GrdCuentasList
                (
                     _Cuentas.idCuent,
                     _Cuenta.numero,
                     _Cuenta.fechaApertura,
                     _Cuenta.tipoCuenta,
                     _Cuenta.estatus,
                     _Cuenta.moneda,
                     _Cuenta.saldoDisponible,
                    jsonCuentList
                );
                Debug.WriteLine($"jsonCuentList: {_Cuentas.idCuent}   {jsonCuentList}");
            }
            if ((id.Length == 12 ))
            {
                if ((_CuentasList != null) && (_CuentasList.cuentas != null))
                {
                    foreach (var rsDat in _CuentasList.cuentas)
                    {
                        string jsonCuentList = JsonConvert.SerializeObject(_CuentasList.cuentas);
                        rsValCuentasList = await _CuentasService.GrdCuentasList
                        (
                             _Cuentas.idCuent,
                             rsDat.numero,
                             rsDat.fechaApertura,
                             rsDat.tipoCuenta,
                             rsDat.estatus,
                             rsDat.moneda,
                             rsDat.saldoDisponible,
                            jsonCuentList
                        );
                    }
                }
            }

            else if (id.Length == 20)
            {
                Debug.WriteLine($"cuenta: {_Cuenta}");
                string jsonCuentList = JsonConvert.SerializeObject(_Cuenta);
                rsValCuentasList = await _CuentasService.GrdCuentasList
                (
                     _Cuentas.idCuent,
                     _Cuenta.numero,
                     _Cuenta.fechaApertura,
                     _Cuenta.tipoCuenta,
                     _Cuenta.estatus,
                     _Cuenta.moneda,
                     _Cuenta.saldoDisponible,
                    jsonCuentList
                );
                Debug.WriteLine($"jsonCuentList: {_Cuentas.idCuent}   {jsonCuentList}");
            }
            
            return Ok(new
            {
                //reqOperacion,
                _Cuentas.idCuent,
                rsValCuentasResp,
                rsValCuentasList,
                _CuentasResp.conteoCuentas,
                _CuentasResp.CodigoRespuesta,
                _CuentasResp.DescripcionCliente,
                _CuentasResp.DescripcionSistema,
                _CuentasResp.FechaHora

            });
        }

        public async Task<CuentasResp> SolCuentas(string prmId, string prmApiKey,
                                        string prmApiSignature, string prmNonce)
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

                if (prmId == "default") { prmPath = "v0/cuentas/default";}
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
                        if ((prmId == "default") || (prmId.Length == 12))
                        {
                            _CuentasList = JsonConvert.DeserializeObject<CuentasList>(rsDat);
                            if ((_CuentasList != null) && (_CuentasList.cuentas != null))
                            {
                                _CuentasResp.conteoCuentas = _CuentasList.conteoCuentas;
                            }
                        }
                        else if (prmId.Length == 20)
                        {
                            _Cuenta = JsonConvert.DeserializeObject<Cuenta>(rsDat);
                            _CuentasResp.conteoCuentas =0;
                        }
                    }
                    
                    _CuentasResp.CodigoRespuesta = codigoRespuesta;
                    _CuentasResp.DescripcionCliente = descripcionCliente;
                    _CuentasResp.DescripcionSistema = descripcionSistema;
                    _CuentasResp.FechaHora = DateTime.Parse(fechaHora);
                }
            }
            return _CuentasResp;
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
