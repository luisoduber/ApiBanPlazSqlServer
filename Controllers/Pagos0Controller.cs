using ApiBanPlaz.models.Pagos0;
using ApiBanPlaz.Servicios.Pagos0;
using ApiBanPlaz.Servicios.General;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("/v1/cce")]
public class Pagos0Controller : ControllerBase
{
    private readonly NonceService _nonceService;
    private readonly CredApiRsService _credApiRsService;

    private readonly Pagos0Service _Pagos0Service;
    private readonly IConfiguration _config;
    string urlBan = "";

    Pagos0Resp _Pagos0Resp = new Pagos0Resp();
    Pagos0 _Pagos0 = new Pagos0();
    public Pagos0Controller(IConfiguration config, NonceService nonceService,
                        CredApiRsService credApiRsService, Pagos0Service Pagos0Service)
    {
        _nonceService = nonceService;
        _credApiRsService = credApiRsService;
        _config = config;
        urlBan = _config["urlBan"].ToString();
        _Pagos0Service = Pagos0Service;
    }

    [HttpPost("Pagos0/{prmRif}")]
    public async Task<IActionResult> Pagos0Rif(string prmRif)
    {
        string reqPagos0= "";
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            reqPagos0 = await reader.ReadToEndAsync();
        }

        var _ReqPagos0 = JsonConvert.DeserializeObject<Pagos0Req>(reqPagos0);
        if (_ReqPagos0 == null) return BadRequest("Cuerpo de petición inválido.");

        string nonce = await _nonceService.ObtNonce();
        var cred = await _credApiRsService.ObtCredApi();
        if (cred == null) return NotFound();

        string path = "/v1/cce/pagoO";
        string apiSignature = ApiSignatureGen.Generar(
            path,
            nonce,
            reqPagos0,
            cred.apiKeySecret
        );

        _Pagos0Resp = await ProcPagos0Rif(prmRif, reqPagos0, cred.ApiKey, apiSignature, nonce);
        _Pagos0.IdPagos0 = await _Pagos0Service.spGrdPagos0Req(
             _ReqPagos0.Moneda,
             _ReqPagos0.Canal,
             _ReqPagos0.Tipo_cce,
             _ReqPagos0.Tipo_proposito,
             _ReqPagos0.Tipo_instrumento_b,
             _ReqPagos0.Identificacion_o,
             _ReqPagos0.Identificacion_b,
             _ReqPagos0.Cuenta_origen,
             _ReqPagos0.Cuenta_destino,
             _ReqPagos0.Telefono,
             _ReqPagos0.Correo,
             _ReqPagos0.Cod_banco_d,
             _ReqPagos0.Cod_banco_a,
             _ReqPagos0.Nombre_d,
             _ReqPagos0.Nombre_a,
             _ReqPagos0.Monto,
             _ReqPagos0.Concepto,
             _ReqPagos0.Direccion_ip,
             _ReqPagos0.Referencia,
             _ReqPagos0.Fecha_hora,
             reqPagos0);

        string jsonPagosP2pResp = JsonConvert.SerializeObject(_Pagos0Resp);
        bool rsValPagosP2pResp = await _Pagos0Service.spGrdPagos0Resp(
            _Pagos0.IdPagos0,
            _Pagos0Resp.CodigoRespuesta,
            _Pagos0Resp.DescripcionCliente,
            _Pagos0Resp.DescripcionSistema,
            _Pagos0Resp.FechaHora,
            _Pagos0Resp.NumeroReferencia,
            jsonPagosP2pResp);

        return Ok(new
        {
            _Pagos0.IdPagos0,
            rsValPagosP2pResp,
            _Pagos0Resp.NumeroReferencia,
            _Pagos0Resp.CodigoRespuesta,
            _Pagos0Resp.DescripcionCliente,
            _Pagos0Resp.DescripcionSistema,
            _Pagos0Resp.FechaHora
        });
    }


    [HttpPost("Pagos0")]
    public async Task<IActionResult> Pagos0()
    {
        string reqPagos0 = "";
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            reqPagos0 = await reader.ReadToEndAsync();
        }

        var _ReqPagos0 = JsonConvert.DeserializeObject<Pagos0Req>(reqPagos0);
        if (_ReqPagos0 == null) return BadRequest("Cuerpo de petición inválido.");

        string nonce = await _nonceService.ObtNonce();
        var cred = await _credApiRsService.ObtCredApi();
        if (cred == null) return NotFound();

        string path = "/v1/cce/pagoO";
        string apiSignature = ApiSignatureGen.Generar(
            path,
            nonce,
            reqPagos0,
            cred.apiKeySecret
        );

        _Pagos0Resp = await ProcPagos0(reqPagos0, cred.ApiKey, apiSignature, nonce);

        _Pagos0.IdPagos0 = await _Pagos0Service.spGrdPagos0Req(
                     _ReqPagos0.Moneda,
                     _ReqPagos0.Canal,
                     _ReqPagos0.Tipo_cce,
                     _ReqPagos0.Tipo_proposito,
                     _ReqPagos0.Tipo_instrumento_b,
                     _ReqPagos0.Identificacion_o,
                     _ReqPagos0.Identificacion_b,
                     _ReqPagos0.Cuenta_origen,
                     _ReqPagos0.Cuenta_destino,
                     _ReqPagos0.Telefono,
                     _ReqPagos0.Correo,
                     _ReqPagos0.Cod_banco_d,
                     _ReqPagos0.Cod_banco_a,
                     _ReqPagos0.Nombre_d,
                     _ReqPagos0.Nombre_a,
                     _ReqPagos0.Monto,
                     _ReqPagos0.Concepto,
                     _ReqPagos0.Direccion_ip,
                     _ReqPagos0.Referencia,
                     _ReqPagos0.Fecha_hora,
                     reqPagos0);

        string jsonPagos0Resp = JsonConvert.SerializeObject(_Pagos0Resp);
        bool rsValPagos0Resp = await _Pagos0Service.spGrdPagos0Resp(
            _Pagos0.IdPagos0,
            _Pagos0Resp.CodigoRespuesta,
            _Pagos0Resp.DescripcionCliente,
            _Pagos0Resp.DescripcionSistema,
            _Pagos0Resp.FechaHora,
            _Pagos0Resp.NumeroReferencia,
            jsonPagos0Resp);

        return Ok(new
        {
            _Pagos0.IdPagos0,
            rsValPagos0Resp,
            _Pagos0Resp.NumeroReferencia,
            _Pagos0Resp.CodigoRespuesta,
            _Pagos0Resp.DescripcionCliente,
            _Pagos0Resp.DescripcionSistema,
            _Pagos0Resp.FechaHora
        });
    }

    public async Task<Pagos0Resp> ProcPagos0(string prmJson, string prmApiKey,
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

            Debug.WriteLine("api-key:", prmApiKey);
            Debug.WriteLine("api-signature:", prmApiSignature);
            Debug.WriteLine("nonce: AAAA ", prmNonce);
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
                   // _Pagos0Resp = JsonConvert.DeserializeObject<Pagos0Resp>(rsDat);
                }
                else { _Pagos0Resp.NumeroReferencia = ""; }

                _Pagos0Resp.CodigoRespuesta = codigoRespuesta;
                _Pagos0Resp.DescripcionCliente = descripcionCliente;
                _Pagos0Resp.DescripcionSistema = descripcionSistema;
                _Pagos0Resp.FechaHora = DateTime.Parse(fechaHora);
            }
        }
        return _Pagos0Resp;
    }


    public async Task<Pagos0Resp> ProcPagos0Rif(string prmRif, string prmJson,
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
                    _Pagos0Resp = JsonConvert.DeserializeObject<Pagos0Resp>(rsDat);
                }
                else { _Pagos0Resp.NumeroReferencia = ""; }

                _Pagos0Resp.CodigoRespuesta = codigoRespuesta;
                _Pagos0Resp.DescripcionCliente = descripcionCliente;
                _Pagos0Resp.DescripcionSistema = descripcionSistema;
                _Pagos0Resp.FechaHora = DateTime.Parse(fechaHora);
            }
        }
        return _Pagos0Resp;
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

