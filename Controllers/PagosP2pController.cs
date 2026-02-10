using ApiBanPlaz.models.PagosP2p;
using ApiBanPlaz.models.Entities;
using ApiBanPlaz.Servicios.PagosP2p;
using ApiBanPlaz.Servicios.General;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;

[ApiController]
[Route("/v1/pagos")]
public class PagosP2pController : ControllerBase
{
    private readonly NonceService _nonceService;
    private readonly CredApiRsService _credApiRsService;

    private readonly PagosP2pService _PagosP2pService;
    private readonly IConfiguration _config;
    string urlBan = "";

    PagosP2pResp _PagosP2pResp = new PagosP2pResp();
    PagosP2p _PagosP2p = new PagosP2p();
    public PagosP2pController(IConfiguration config, NonceService nonceService,
                        CredApiRsService credApiRsService, PagosP2pService PagosP2pService)
    {
        _nonceService = nonceService;
        _credApiRsService = credApiRsService;
        _config = config;
        urlBan = _config["urlBan"].ToString();
        _PagosP2pService = PagosP2pService;
    }

    [HttpPost("PagosP2p")]
    public async Task<IActionResult> PagosP2p()
    {
        string reqPagosP2p = "";
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            reqPagosP2p = await reader.ReadToEndAsync();
        }

        var _ReqPagosP2p = JsonConvert.DeserializeObject<PagosP2pReq>(reqPagosP2p);
        if (_ReqPagosP2p == null) return BadRequest("Cuerpo de petición inválido.");

        string nonce = await _nonceService.ObtNonce();
        var cred = await _credApiRsService.ObtCredApi();
        if (cred == null) return NotFound();

        string path = "/v1/pagos/p2p";
        string apiSignature = ApiSignatureGen.Generar(
            path,
            nonce,
            reqPagosP2p,
            cred.apiKeySecret
        );

        _PagosP2pResp = await ProcPagosP2p(reqPagosP2p, cred.ApiKey, apiSignature, nonce);
        _PagosP2p.IdPagosP2p = await _PagosP2pService.spGrdPagosP2pReq(
             _ReqPagosP2p.Banco,
             _ReqPagosP2p.IdBeneficiario,
             _ReqPagosP2p.Telefono,
             _ReqPagosP2p.Monto,
             _ReqPagosP2p.Motivo,
             _ReqPagosP2p.Canal,
             _ReqPagosP2p.IdExterno,
             _ReqPagosP2p.Cuenta,
             _ReqPagosP2p.TelefonoAfiliado,
             _ReqPagosP2p.Moneda,
             _ReqPagosP2p.Sucursal,
             _ReqPagosP2p.Cajero,
             _ReqPagosP2p.Caja,
             _ReqPagosP2p.IpCliente,
             _ReqPagosP2p.Longitud,
             _ReqPagosP2p.Latitud,
             _ReqPagosP2p.Precision,
             reqPagosP2p);

    string jsonPagosP2pResp = JsonConvert.SerializeObject(_PagosP2pResp);
        bool rsValPagosP2pResp = await _PagosP2pService.spGrdPagosP2pResp(
            _PagosP2p.IdPagosP2p,
            _PagosP2pResp.CodigoRespuesta,
            _PagosP2pResp.DescripcionCliente,
            _PagosP2pResp.DescripcionSistema,
            _PagosP2pResp.FechaHora,
            _PagosP2pResp.NumeroReferencia,
            jsonPagosP2pResp);

        return Ok(new
        {
            //reqCobroDI,
            _PagosP2p.IdPagosP2p,
             rsValPagosP2pResp,
            _PagosP2pResp.NumeroReferencia,
            _PagosP2pResp.CodigoRespuesta,
            _PagosP2pResp.DescripcionCliente,
            _PagosP2pResp.DescripcionSistema,
            _PagosP2pResp.FechaHora
        });
    }

    public async Task<PagosP2pResp> ProcPagosP2p(string prmJson, string prmApiKey,
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


            Debug.WriteLine(urlBan +"v1/pagos/p2p");
            using (var Res = await client.PostAsync("v1/pagos/p2p", content))
            {
                if (Res.Headers.TryGetValues("codigoRespuesta", out var values)) { codigoRespuesta = values.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("descripcionCliente", out var values1)) { descripcionCliente = values1.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("descripcionSistema", out var values2)) { descripcionSistema = values2.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("fechaHora", out var values3)) { fechaHora = values3.FirstOrDefault(); }

                rsDat = await Res.Content.ReadAsStringAsync();
                _PagosP2pResp = JsonConvert.DeserializeObject<PagosP2pResp>(rsDat);

                _PagosP2pResp.CodigoRespuesta = codigoRespuesta;
                _PagosP2pResp.DescripcionCliente = descripcionCliente;
                _PagosP2pResp.DescripcionSistema = descripcionSistema;
                _PagosP2pResp.FechaHora = DateTime.Parse(fechaHora);
            }
        }
        return _PagosP2pResp;
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
