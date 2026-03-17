using ApiBanPlaz.models.CompPm;
using ApiBanPlaz.models.Operacion;
using ApiBanPlaz.models.Operacion;
using ApiBanPlaz.Servicios.CompPm;
using ApiBanPlaz.Servicios.General;
using ApiBanPlaz.Servicios.Operacion;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("v0/cuentas")]
public class OperacionController : ControllerBase
{
    private readonly NonceService _nonceService;
    private readonly CredApiRsService _credApiRsService;

    private readonly OperacionService _OperacionService;
    private readonly IConfiguration _config;
    string urlBan = "";
    int idOperacion = 0;

    OpeMovimientos _OpeMovimientos = new OpeMovimientos();
    OperacionResp _OperacionResp = new OperacionResp();
    Operacion _Operacion = new Operacion();
    public OperacionController(IConfiguration config, NonceService nonceService,
                        CredApiRsService credApiRsService, OperacionService OperacionService)
    {
        _nonceService = nonceService;
        _credApiRsService = credApiRsService;
        _config = config;
        urlBan = _config["urlBan"].ToString();
        _OperacionService = OperacionService;
    }

    [HttpPost("Operacion")]
    public async Task<IActionResult> Operacion()
    {
        string reqOperacion = "";
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            reqOperacion = await reader.ReadToEndAsync();
        }

        var _ReqOperacion = JsonConvert.DeserializeObject<OperacionReq>(reqOperacion);
        if (_ReqOperacion == null) return BadRequest("Cuerpo de petición inválido.");

        string nonce = await _nonceService.ObtNonce();
        var cred = await _credApiRsService.ObtCredApi();
        if (cred == null) return NotFound();

        string path = "v0/cuentas/Operacion";
        string apiSignature = ApiSignatureGen.Generar(
            path,
            nonce,
            reqOperacion,
            cred.apiKeySecret
        );

        _OperacionResp = await SolOperacion(reqOperacion, cred.ApiKey, apiSignature, nonce);
        _Operacion.idOperacion = await _OperacionService.GrdOperacionReq(
            _ReqOperacion.Cuenta,
            _ReqOperacion.Moneda,
            _ReqOperacion.Banco,
            _ReqOperacion.TPago,
            _ReqOperacion.Naturaleza,
            _ReqOperacion.prmReferencia,
            _ReqOperacion.FechaInicio,
            _ReqOperacion.FechaFin,
            _ReqOperacion.Monto,
            _ReqOperacion.canal,
            _ReqOperacion.Id,
            _ReqOperacion.Direccion_ip,
             reqOperacion
            );


       string jsonOperacionResp = JsonConvert.SerializeObject(_OperacionResp);
        bool rsValOperacionResp = await _OperacionService.GrdOperacionResp(
            _Operacion.idOperacion,
            _OperacionResp.CodigoRespuesta,
            _OperacionResp.DescripcionCliente,
            _OperacionResp.DescripcionSistema,
            _OperacionResp.FechaHora,
            _OperacionResp.CantMovimientos,
            jsonOperacionResp);

        bool rsValOpeMovimientos = false;
        if (_OpeMovimientos.movimientos.Count > 0)
        {
            foreach (var rsDat in _OpeMovimientos.movimientos)
            {
                string jsonOpeMovimientos = JsonConvert.SerializeObject(_OpeMovimientos.movimientos);
                rsValOpeMovimientos = await _OperacionService.GrdOpeMovimientos
                (
                     _Operacion.idOperacion,
                     rsDat.Fecha,
                     rsDat.Hora,
                     rsDat.Referencia,
                     rsDat.Concepto,
                     rsDat.Tipo,
                     rsDat.Naturaleza,
                     rsDat.Monto,
                    jsonOpeMovimientos
                );
            }
        }

        return Ok(new
        {
            //reqOperacion,
            _Operacion.idOperacion,
            rsValOperacionResp,
            rsValOpeMovimientos,
            _OperacionResp.CantMovimientos,
            _OperacionResp.CodigoRespuesta,
            _OperacionResp.DescripcionCliente,
            _OperacionResp.DescripcionSistema,
            _OperacionResp.FechaHora

        });
    }

    public async Task<OperacionResp> SolOperacion(string prmJson, string prmApiKey,
                                         string prmApiSignature, string prmNonce)
    {
        string rsDat = "";
        string codigoRespuesta = "";
        string descripcionCliente = "";
        string descripcionSistema = "";
        string fechaHora = "";
        string referencia_c = "";
        string endtoend = "";

        using (var client = new HttpClient())
        {
            var content = new StringContent(prmJson, Encoding.UTF8, "application/json");
            client.BaseAddress = new Uri(urlBan);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("api-key", prmApiKey);
            client.DefaultRequestHeaders.Add("api-signature", prmApiSignature);
            client.DefaultRequestHeaders.Add("nonce", prmNonce);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using (var Res = await client.PostAsync("/v0/cuentas/operacion", content))
            {
                if (Res.Headers.TryGetValues("codigoRespuesta", out var values)) { codigoRespuesta = values.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("descripcionCliente", out var values1)) { descripcionCliente = values1.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("descripcionSistema", out var values2)) { descripcionSistema = values2.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("fechaHora", out var values3)) { fechaHora = values3.FirstOrDefault(); }

                rsDat = await Res.Content.ReadAsStringAsync();
                _OperacionResp = JsonConvert.DeserializeObject<OperacionResp>(rsDat);

                _OperacionResp.CodigoRespuesta = codigoRespuesta;
                _OperacionResp.DescripcionCliente = descripcionCliente;
                _OperacionResp.DescripcionSistema = descripcionSistema;
                _OperacionResp.FechaHora = DateTime.Parse(fechaHora);
            }
        }
        return _OperacionResp;
    }

    public static class ApiSignatureGen
    {
        public static string Generar(string path, string nonce, string body, string secret)
        {
            string signatureRaw = $"/{path}{nonce}{body}";
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
