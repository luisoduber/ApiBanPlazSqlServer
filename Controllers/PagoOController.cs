using ApiBanPlaz.models.PagoO;
using ApiBanPlaz.Servicios.PagoO;
using ApiBanPlaz.Servicios.General;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("/v1/cce")]
public class PagoOController : ControllerBase
{
    private readonly NonceService _nonceService;
    private readonly CredApiRsService _credApiRsService;

    private readonly PagoOService _PagoOService;
    private readonly IConfiguration _config;
    string urlBan = "";

    PagoOResp _PagoOResp = new PagoOResp();
    PagoO _PagoO = new PagoO();
    public PagoOController(IConfiguration config, NonceService nonceService,
                        CredApiRsService credApiRsService, PagoOService Pagos0Service)
    {
        _nonceService = nonceService;
        _credApiRsService = credApiRsService;
        _config = config;
        urlBan = _config["urlBan"].ToString();
        _PagoOService = Pagos0Service;
    }

    [HttpPost("PagoO/{prmRif}")]
    public async Task<IActionResult> PagoORif(string prmRif)
    {
        string reqPagoO= "";
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            reqPagoO = await reader.ReadToEndAsync();
        }

        var _ReqPagoO = JsonConvert.DeserializeObject<PagoOReq>(reqPagoO);
        if (_ReqPagoO == null) return BadRequest("Cuerpo de petición inválido.");

        string nonce = await _nonceService.ObtNonce();
        var cred = await _credApiRsService.ObtCredApi();
        if (cred == null) return NotFound();

        string path = "v1/cce/pagoO";
        string apiSignature = ApiSignatureGen.Generar(
            path,
            nonce,
            reqPagoO,
            cred.apiKeySecret
        );

        if (string.IsNullOrWhiteSpace(_ReqPagoO.Referencia)) {_ReqPagoO.Referencia = "";}
        if (string.IsNullOrWhiteSpace(_ReqPagoO.Fecha_hora.ToString())) { _ReqPagoO.Fecha_hora = DateTime.Now; }

        _PagoOResp = await ProcPagoORif(prmRif, reqPagoO, cred.ApiKey, apiSignature, nonce);
        _PagoO.IdPagoO = await _PagoOService.spGrdPagoOReq(
             _ReqPagoO.Moneda,
             _ReqPagoO.Canal,
             _ReqPagoO.Tipo_cce,
             _ReqPagoO.Tipo_proposito,
             _ReqPagoO.Tipo_instrumento_b,
             _ReqPagoO.Identificacion_o,
             _ReqPagoO.Identificacion_b,
             _ReqPagoO.Cuenta_origen,
             _ReqPagoO.Cuenta_destino,
             _ReqPagoO.Telefono,
             _ReqPagoO.Correo,
             _ReqPagoO.Cod_banco_d,
             _ReqPagoO.Cod_banco_a,
             _ReqPagoO.Nombre_d,
             _ReqPagoO.Nombre_a,
             _ReqPagoO.Monto,
             _ReqPagoO.Concepto,
             _ReqPagoO.Direccion_ip,
             _ReqPagoO.Referencia,
             _ReqPagoO.Fecha_hora,
             reqPagoO);

        string jsonPagosP2pResp = JsonConvert.SerializeObject(_PagoOResp);
        bool rsValPagosP2pResp = await _PagoOService.spGrdPagoOResp(
            _PagoO.IdPagoO,
            _PagoOResp.CodigoRespuesta,
            _PagoOResp.DescripcionCliente,
            _PagoOResp.DescripcionSistema,
            _PagoOResp.FechaHora,
            _PagoOResp.NumeroReferencia,
            jsonPagosP2pResp);

        return Ok(new
        {
            _PagoO.IdPagoO,
            rsValPagosP2pResp,
            _PagoOResp.NumeroReferencia,
            _PagoOResp.CodigoRespuesta,
            _PagoOResp.DescripcionCliente,
            _PagoOResp.DescripcionSistema,
            _PagoOResp.FechaHora
        });
    }


    [HttpPost("PagoO")]
    public async Task<IActionResult> PagoO()
    {
        string reqPagoO = "";
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            reqPagoO = await reader.ReadToEndAsync();
        }

        var _ReqPagoO = JsonConvert.DeserializeObject<PagoOReq>(reqPagoO);
        if (_ReqPagoO == null) return BadRequest("Cuerpo de petición inválido.");

        string nonce = await _nonceService.ObtNonce();
        var cred = await _credApiRsService.ObtCredApi();
        if (cred == null) return NotFound();

        string path = "v1/cce/pagoO";
        string apiSignature = ApiSignatureGen.Generar(
            path,
            nonce,
            reqPagoO,
            cred.apiKeySecret
        );

        if (string.IsNullOrWhiteSpace(_ReqPagoO.Referencia)){_ReqPagoO.Referencia = "";}
        if (string.IsNullOrWhiteSpace(_ReqPagoO.Fecha_hora.ToString())) { _ReqPagoO.Fecha_hora = DateTime.Now; }

        _PagoOResp = await ProcPagoO(reqPagoO, cred.ApiKey, apiSignature, nonce);
        _PagoO.IdPagoO = await _PagoOService.spGrdPagoOReq(
                     _ReqPagoO.Moneda,
                     _ReqPagoO.Canal,
                     _ReqPagoO.Tipo_cce,
                     _ReqPagoO.Tipo_proposito,
                     _ReqPagoO.Tipo_instrumento_b,
                     _ReqPagoO.Identificacion_o,
                     _ReqPagoO.Identificacion_b,
                     _ReqPagoO.Cuenta_origen,
                     _ReqPagoO.Cuenta_destino,
                     _ReqPagoO.Telefono,
                     _ReqPagoO.Correo,
                     _ReqPagoO.Cod_banco_d,
                     _ReqPagoO.Cod_banco_a,
                     _ReqPagoO.Nombre_d,
                     _ReqPagoO.Nombre_a,
                     _ReqPagoO.Monto,
                     _ReqPagoO.Concepto,
                     _ReqPagoO.Direccion_ip,
                     _ReqPagoO.Referencia,
                     _ReqPagoO.Fecha_hora,
                     reqPagoO);

        string jsonPagos0Resp = JsonConvert.SerializeObject(_PagoOResp);
        bool rsValPagos0Resp = await _PagoOService.spGrdPagoOResp(
            _PagoO.IdPagoO,
            _PagoOResp.CodigoRespuesta,
            _PagoOResp.DescripcionCliente,
            _PagoOResp.DescripcionSistema,
            _PagoOResp.FechaHora,
            _PagoOResp.NumeroReferencia,
            jsonPagos0Resp);

        return Ok(new
        {
            _PagoO.IdPagoO,
            rsValPagos0Resp,
            _PagoOResp.NumeroReferencia,
            _PagoOResp.CodigoRespuesta,
            _PagoOResp.DescripcionCliente,
            _PagoOResp.DescripcionSistema,
            _PagoOResp.FechaHora
        });
    }

    public async Task<PagoOResp> ProcPagoO(string prmJson, string prmApiKey,
                                         string prmApiSignature, string prmNonce)
    {
        string rsDat = "";
        string codigoRespuesta = "";
        string descripcionCliente = "";
        string descripcionSistema = "";
        string fechaHora = "";

        using (var client = new HttpClient())
        {
            var content = new StringContent(prmJson, Encoding.UTF8, "application/json");
            client.BaseAddress = new Uri(urlBan);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("api-key", prmApiKey);
            client.DefaultRequestHeaders.Add("api-signature", prmApiSignature);
            client.DefaultRequestHeaders.Add("nonce", prmNonce);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using (var Res = await client.PostAsync("v1/cce/pagoO", content))
            {
                if (Res.Headers.TryGetValues("codigoRespuesta", out var values)) { codigoRespuesta = values.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("descripcionCliente", out var values1)) { descripcionCliente = values1.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("descripcionSistema", out var values2)) { descripcionSistema = values2.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("fechaHora", out var values3)) { fechaHora = values3.FirstOrDefault(); }

                rsDat = await Res.Content.ReadAsStringAsync();
                Debug.WriteLine(" rsDat : S", rsDat);
                if (!string.IsNullOrEmpty(rsDat))
                {
                  _PagoOResp = JsonConvert.DeserializeObject<PagoOResp>(rsDat);
                }
                else { _PagoOResp.NumeroReferencia = ""; }

                _PagoOResp.CodigoRespuesta = codigoRespuesta;
                _PagoOResp.DescripcionCliente = descripcionCliente;
                _PagoOResp.DescripcionSistema = descripcionSistema;
                _PagoOResp.FechaHora = DateTime.Parse(fechaHora);
            }
        }
        return _PagoOResp;
    }


    public async Task<PagoOResp> ProcPagoORif(string prmRif, string prmJson,
                                                   string prmApiKey, string prmApiSignature,
                                                   string prmNonce)
    {
        string rsDat = "";
        string codigoRespuesta = "";
        string descripcionCliente = "";
        string descripcionSistema = "";
        string fechaHora = "";

        using (var client = new HttpClient())
        {
            var content = new StringContent(prmJson, Encoding.UTF8, "application/json");
            client.BaseAddress = new Uri(urlBan);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("api-key", prmApiKey);
            client.DefaultRequestHeaders.Add("api-signature", prmApiSignature);
            client.DefaultRequestHeaders.Add("nonce", prmNonce);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using (var Res = await client.PostAsync("v1/cce/pagoO/" + prmRif, content))
            {
                if (Res.Headers.TryGetValues("codigoRespuesta", out var values)) { codigoRespuesta = values.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("descripcionCliente", out var values1)) { descripcionCliente = values1.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("descripcionSistema", out var values2)) { descripcionSistema = values2.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("fechaHora", out var values3)) { fechaHora = values3.FirstOrDefault(); }

                rsDat = await Res.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(rsDat))
                {
                    _PagoOResp = JsonConvert.DeserializeObject<PagoOResp>(rsDat);
                }
                else { _PagoOResp.NumeroReferencia = ""; }

                _PagoOResp.CodigoRespuesta = codigoRespuesta;
                _PagoOResp.DescripcionCliente = descripcionCliente;
                _PagoOResp.DescripcionSistema = descripcionSistema;
                _PagoOResp.FechaHora = DateTime.Parse(fechaHora);
            }
        }
        return _PagoOResp;
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

