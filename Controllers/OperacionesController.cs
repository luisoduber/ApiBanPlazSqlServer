using ApiBanPlaz.models.CompPm;
using ApiBanPlaz.models.Operacion;
using ApiBanPlaz.models.Operaciones;
using ApiBanPlaz.models.Operaciones;
using ApiBanPlaz.Servicios.CompPm;
using ApiBanPlaz.Servicios.General;
using ApiBanPlaz.Servicios.Operaciones;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("v0/cuentas")]
public class OperacionesesController : ControllerBase
{
    private readonly NonceService _nonceService;
    private readonly CredApiRsService _credApiRsService;

    private readonly OperacionesService _OperacionesService;
    private readonly IConfiguration _config;
    string urlBan = "";
    int idOperaciones = 0;

    OperacionesMov _OperacionesMov = new OperacionesMov();
    OperacionesResp _OperacionesResp = new OperacionesResp();
    Operaciones _Operaciones = new Operaciones();
    public OperacionesesController(IConfiguration config, NonceService nonceService,
                        CredApiRsService credApiRsService, OperacionesService OperacionesService)
    {
        _nonceService = nonceService;
        _credApiRsService = credApiRsService;
        _config = config;
        urlBan = _config["urlBan"].ToString();
        _OperacionesService = OperacionesService;
    }

    [HttpPost("operaciones/{prmRif}")]
    public async Task<IActionResult> Operaciones(string prmRif)
    {
        string reqOperaciones = "";
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            reqOperaciones = await reader.ReadToEndAsync();
        }

        var _ReqOperaciones = JsonConvert.DeserializeObject<OperacionesReq>(reqOperaciones);
        if (_ReqOperaciones == null) return BadRequest("Cuerpo de petición inválido.");

        string nonce = await _nonceService.ObtNonce();
        var cred = await _credApiRsService.ObtCredApi();
        if (cred == null) return NotFound();

        string path = "v0/cuentas/operaciones";
        string apiSignature = ApiSignatureGen.Generar(
            path,
            nonce,
            reqOperaciones,
            cred.apiKeySecret
        );

        if (string.IsNullOrWhiteSpace(_ReqOperaciones.Banco)) { _ReqOperaciones.Banco = ""; }
        if (string.IsNullOrWhiteSpace(_ReqOperaciones.Referencia)) { _ReqOperaciones.Referencia = ""; }
        if (_ReqOperaciones.MontoMinimo == null) { _ReqOperaciones.MontoMinimo = 0; }
        if (_ReqOperaciones.MontoMaximo == null) { _ReqOperaciones.MontoMaximo = 0; }

        _ReqOperaciones.Rif_cliente = prmRif;
        _OperacionesResp = await SolOperaciones(prmRif, reqOperaciones, cred.ApiKey, apiSignature, nonce);
        _Operaciones.idOperaciones = await _OperacionesService.GrdOperacionesReq(
            _ReqOperaciones.Rif_cliente,
            _ReqOperaciones.Cuenta,
            _ReqOperaciones.Moneda,
            _ReqOperaciones.TPago,
            _ReqOperaciones.Naturaleza,
            _ReqOperaciones.FechaInicio,
            _ReqOperaciones.FechaFin,
            _ReqOperaciones.Canal,
            _ReqOperaciones.Id,
            _ReqOperaciones.Banco,
            _ReqOperaciones.Referencia,
            _ReqOperaciones.MontoMinimo,
            _ReqOperaciones.MontoMaximo,
            _ReqOperaciones.Direccion_ip,
            reqOperaciones
            );


    string jsonOperacionesResp = JsonConvert.SerializeObject(_OperacionesResp);
        bool rsValOperacionesResp = await _OperacionesService.GrdOperacionesResp(
            _Operaciones.idOperaciones,
            _OperacionesResp.CodigoRespuesta,
            _OperacionesResp.DescripcionCliente,
            _OperacionesResp.DescripcionSistema,
            _OperacionesResp.FechaHora,
            _OperacionesResp.CantMovimientos,
            jsonOperacionesResp);

        bool rsValOpeMovimientos = false;
        if ((_OperacionesMov != null) && (_OperacionesMov.movimientos != null))
        {
            foreach (var rsDat in _OperacionesMov.movimientos)
            {
                string jsonOperacionesMov = JsonConvert.SerializeObject(_OperacionesMov.movimientos);
                rsValOpeMovimientos = await _OperacionesService.GrdOperacionesMov
                (
                     _Operaciones.idOperaciones,
                     rsDat.Fecha,
                     rsDat.Hora,
                     rsDat.Referencia,
                     rsDat.Concepto,
                     rsDat.Tipo,
                     rsDat.Naturaleza,
                     rsDat.Monto,
                    jsonOperacionesMov
                );
            }
        }

        return Ok(new
        {
            //reqOperaciones,
            _Operaciones.idOperaciones,
            rsValOperacionesResp,
            rsValOpeMovimientos,
            _OperacionesResp.CantMovimientos,
            _OperacionesResp.CodigoRespuesta,
            _OperacionesResp.DescripcionCliente,
            _OperacionesResp.DescripcionSistema,
            _OperacionesResp.FechaHora

        });
    }

    public async Task<OperacionesResp> SolOperaciones(string prmRif, string prmJson, 
                                                      string prmApiKey, string prmApiSignature, 
                                                      string prmNonce)
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

            Debug.WriteLine($"{urlBan}v0/cuentas/operaciones/" + prmRif);
            using (var Res = await client.PostAsync("v0/cuentas/operaciones/"+prmRif, content))
            {
                if (Res.Headers.TryGetValues("codigoRespuesta", out var values)) { codigoRespuesta = values.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("descripcionCliente", out var values1)) { descripcionCliente = values1.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("descripcionSistema", out var values2)) { descripcionSistema = values2.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("fechaHora", out var values3)) { fechaHora = values3.FirstOrDefault(); }

                rsDat = await Res.Content.ReadAsStringAsync();
                Debug.WriteLine(rsDat);
               
                if (!string.IsNullOrWhiteSpace(rsDat))
                {
                    _OperacionesMov = JsonConvert.DeserializeObject<OperacionesMov>(rsDat);
                    if ((_OperacionesMov != null) && (_OperacionesMov.movimientos != null))
                    {
                        _OperacionesResp.CantMovimientos = _OperacionesMov.movimientos.Count;
                    }
                }

                _OperacionesResp.CodigoRespuesta = codigoRespuesta;
                _OperacionesResp.DescripcionCliente = descripcionCliente;
                _OperacionesResp.DescripcionSistema = descripcionSistema;
                _OperacionesResp.FechaHora = DateTime.Parse(fechaHora);
            }
        }
        return _OperacionesResp;
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
