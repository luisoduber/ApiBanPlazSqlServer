using ApiBanPlaz.models.ConsultaLiq;
using ApiBanPlaz.Servicios.ConsultaLiq;
using ApiBanPlaz.Servicios.General;
using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("v1/cce")]
public class ConsultaLiqController : ControllerBase
{
    private readonly NonceService _nonceService;
    private readonly CredApiRsService _credApiRsService;

    private readonly ConsultaLiqService _ConsultaLiqService;
    private readonly IConfiguration _config;
    string urlBan = "";
    int idConsultaLiq = 0;

    ConsultaLiqReq _ConsultaLiqReq = new ConsultaLiqReq();
    ConsultaLiqResp _ConsultaLiqResp = new ConsultaLiqResp();
    ConsultaLiq _ConsultaLiq = new ConsultaLiq();
    public ConsultaLiqController(IConfiguration config, NonceService nonceService,
                               CredApiRsService credApiRsService, ConsultaLiqService ConsultaLiqService)
    {
        _nonceService = nonceService;
        _credApiRsService = credApiRsService;
        _config = config;
        urlBan = _config["urlBan"].ToString();
        _ConsultaLiqService = ConsultaLiqService;
    }

    [HttpGet("consultaLiq/{id}")]
    public async Task<IActionResult> ConsultaLiq(string id,
        [FromQuery] string cuenta,
        [FromQuery] string referencia,
        [FromQuery] decimal monto,
        [FromQuery] string fecha,
        [FromQuery] string canal)

    {
        string qryStringLiq = "";
        string nonce = await _nonceService.ObtNonce();
        var cred = await _credApiRsService.ObtCredApi();
        if (cred == null) return NotFound();

        string path = "v1/cce/consultaLiq";
        string apiSignature = ApiSignatureGen.Generar(
            path,
            nonce,
            cred.apiKeySecret
        );

        qryStringLiq =
                       "?cuenta=" + cuenta +
                       "&referencia=" + referencia +
                       "&monto=" + monto.ToString().Replace(",", ".") +
                       "&fecha=" + fecha +
                       "&canal=" + canal.ToString();

        _ConsultaLiqResp = await SolConsultaLiq(id, qryStringLiq, cred.ApiKey, apiSignature, nonce);
        _ConsultaLiqReq.Id = id;
        _ConsultaLiqReq.Cuenta = cuenta;
        _ConsultaLiqReq.Referencia = referencia;
        _ConsultaLiqReq.Monto = Convert.ToDecimal(monto);
        _ConsultaLiqReq.fecha = fecha;
        _ConsultaLiqReq.canal = canal;

        _ConsultaLiq.IdConsultaLiq = await _ConsultaLiqService.GrdConsultaLiq(
             _ConsultaLiqReq.Id,
             _ConsultaLiqReq.Cuenta,
             _ConsultaLiqReq.Referencia,
             _ConsultaLiqReq.Monto,
             _ConsultaLiqReq.fecha,
             _ConsultaLiqReq.canal,
            qryStringLiq
            );

        string jsonConsultaLiqResp = JsonConvert.SerializeObject(_ConsultaLiqResp);
        bool rsValConsultarDIResp = await _ConsultaLiqService.GrdConsultaLiqResp(
            _ConsultaLiq.IdConsultaLiq,
            _ConsultaLiqResp.CodigoRespuesta,
            _ConsultaLiqResp.DescripcionCliente,
            _ConsultaLiqResp.DescripcionSistema,
            _ConsultaLiqResp.FechaHora,
            jsonConsultaLiqResp);

        return Ok(new
        {
            _ConsultaLiq.IdConsultaLiq,
            rsValConsultarDIResp,
            _ConsultaLiqResp.CodigoRespuesta,
            _ConsultaLiqResp.DescripcionCliente,
            _ConsultaLiqResp.DescripcionSistema,
            _ConsultaLiqResp.FechaHora

        });
    }

    public async Task<ConsultaLiqResp> SolConsultaLiq(string prmId, string prmQryString,
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
            client.BaseAddress = new Uri(urlBan);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("api-key", prmApiKey);
            client.DefaultRequestHeaders.Add("api-signature", prmApiSignature);
            client.DefaultRequestHeaders.Add("nonce", prmNonce);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Debug.WriteLine($"{urlBan}v1/cce/consultaLiq/{prmId}{prmQryString}");
            using (var Res = await client.GetAsync($"v1/cce/consultaLiq/{prmId}{prmQryString}"))
            {

                if (Res.Headers.TryGetValues("codigoRespuesta", out var values)) { codigoRespuesta = values.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("descripcionCliente", out var values1)) { descripcionCliente = values1.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("descripcionSistema", out var values2)) { descripcionSistema = values2.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("fechaHora", out var values3)) { fechaHora = values3.FirstOrDefault(); }

                _ConsultaLiqResp.CodigoRespuesta = codigoRespuesta;
                _ConsultaLiqResp.DescripcionCliente = descripcionCliente;
                _ConsultaLiqResp.DescripcionSistema = descripcionSistema;
                _ConsultaLiqResp.FechaHora = DateTime.Parse(fechaHora);

                // rsDat = await Res.Content.ReadAsStringAsync();
                // _ConsultarDIResp = JsonConvert.DeserializeObject<ConsultarDIResp>(rsDat);
            }
        }
        return _ConsultaLiqResp;
    }

    public static class ApiKeyGen
    {
        public static string GenApiKey()
        {
            byte[] bytes = new byte[16];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            return Convert.ToHexString(bytes).ToLower();
        }
    }

    public static class ApiKeySecretGen
    {
        public static string GenKeySecret(int bytes = 16)
        {
            var buffer = new byte[bytes];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(buffer);

            return Convert.ToHexString(buffer).ToLower();
        }
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
