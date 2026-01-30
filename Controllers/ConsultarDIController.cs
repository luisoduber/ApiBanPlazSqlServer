using ApiBanPlaz.models.Entities;
using ApiBanPlaz.models.ConsultarDl;
using ApiBanPlaz.Servicios.General;
using ApiBanPlaz.Servicios.TokenDl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using ApiBanPlaz.Servicios.ConsultarDl;

[ApiController]
[Route("v1/cce/debinm")]
public class ConsultarDIController : ControllerBase
{
    private readonly NonceService _nonceService;
    private readonly CredApiRsService _credApiRsService;

    private readonly ConsultarDlService _ConsultarDIService;
    private readonly IConfiguration _config;
    string urlBan = "";
    int idConsultarDI = 0;

    ConsultarDlResp _ConsultarDIResp = new ConsultarDlResp();
    ConsultarDI _ConsultarDI = new ConsultarDI();
    public ConsultarDIController(IConfiguration config, NonceService nonceService, 
                        CredApiRsService credApiRsService, ConsultarDlService ConsultarDIService)
    {
        _nonceService = nonceService;
        _credApiRsService = credApiRsService;
        _config = config;
        urlBan = _config["urlBan"].ToString();
        _ConsultarDIService = ConsultarDIService;
    }

    [HttpPost("ConsultarDl")]
    public async Task<IActionResult> ConsultarDI()
    {
        // 1. Leer el body como string "crudo"
        string reqConsultarDI = "";
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            reqConsultarDI = await reader.ReadToEndAsync();
        }

        var _ReqConsultarDI = JsonConvert.DeserializeObject<ConsultarDlReq>(reqConsultarDI);
        if (_ReqConsultarDI == null) return BadRequest("Cuerpo de petición inválido.");

        string nonce = await _nonceService.ObtNonce();
        var cred = await _credApiRsService.ObtCredApi();
        if (cred == null) return NotFound();

        string path = "v1/cce/debinm/ConsultarDI";
        string apiSignature = ApiSignatureGen.Generar(
            path,
            nonce,
            reqConsultarDI,
            cred.apiKeySecret
        );

        _ConsultarDIResp = await SolConsultarDI(reqConsultarDI, cred.ApiKey,apiSignature, nonce);
        //return Ok(new { nonce,cred.ApiKey,cred.apiKeySecret,apiSignature});

        _ConsultarDI.IdConsultarDI= await _ConsultarDIService.GrdConsultarDIAsync(
            _ReqConsultarDI.Id,
            _ReqConsultarDI.cuenta_cobrador,
            _ReqConsultarDI.endtoend,
            _ReqConsultarDI.referencia_c,
            _ReqConsultarDI.Monto,
            _ReqConsultarDI.canal,
            reqConsultarDI
            );

        string jsonConsultarDIResp = JsonConvert.SerializeObject(_ConsultarDIResp);
        bool rsValConsultarDIResp = await _ConsultarDIService.GrdConsultarDIRespAsync(
            _ConsultarDI.IdConsultarDI,
            _ConsultarDIResp.CodigoRespuesta,
            _ConsultarDIResp.DescripcionCliente,
            _ConsultarDIResp.DescripcionSistema,
            _ConsultarDIResp.FechaHora,
            jsonConsultarDIResp);

        return Ok(new
        {
            _ConsultarDI.IdConsultarDI,
            rsValConsultarDIResp,
            _ConsultarDIResp.CodigoRespuesta,
            _ConsultarDIResp.DescripcionCliente,
            _ConsultarDIResp.DescripcionSistema,
            _ConsultarDIResp.FechaHora

        });
    }

    public async Task<ConsultarDlResp> SolConsultarDI(string prmJson, string prmApiKey, 
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

            using (var Res = await client.PostAsync("v1/cce/debinm/ConsultarDI", content))
            {
                if (Res.Headers.TryGetValues("codigoRespuesta", out var values)) { codigoRespuesta= values.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("descripcionCliente", out var values1)) { descripcionCliente = values1.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("descripcionSistema", out var values2)) { descripcionSistema = values2.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("fechaHora", out var values3)) { fechaHora = values3.FirstOrDefault(); }

                //Debug.WriteLine("codigoRespuest: "+codigoRespuesta);
                //Debug.WriteLine("descripcionCliente: " + descripcionCliente);
                //Debug.WriteLine("descripcionSistema : " + descripcionSistema);
                //Debug.WriteLine("fechaHora : " + fechaHora);
                //Debug.WriteLine("urlBan: " + urlBan + "v1/cce/debinm/ConsultarDI");

                _ConsultarDIResp.CodigoRespuesta = codigoRespuesta;
                _ConsultarDIResp.DescripcionCliente = descripcionCliente;
                _ConsultarDIResp.DescripcionSistema = descripcionSistema;
                _ConsultarDIResp.FechaHora =DateTime.Parse(fechaHora); 

                // rsDat = await Res.Content.ReadAsStringAsync();
                // _ConsultarDIResp = JsonConvert.DeserializeObject<ConsultarDIResp>(rsDat);
            }
        }
        return _ConsultarDIResp;
    }


    public static class ApiKeyGen
    {
        public static string GenApiKey()
        {
            // 16 bytes = 32 caracteres hex
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

            return Convert.ToHexString(buffer).ToLower(); //  32
        }
    }

public static class ApiSignatureGen
{
    public static string Generar(string path, string nonce, string body, string secret)
    {
        // 1. Recrear la cadena de firma exactamente como en el JS de Postman:
        // let signature = `/${apiPath}${nonce}${body}`;
        // Asegúrate de que 'path' no tenga la '/' inicial al pasarlo, o ajusta aquí:
        string signatureRaw = $"/{path}{nonce}{body}";

        // 2. Convertir a bytes usando UTF-8
        byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
        byte[] messageBytes = Encoding.UTF8.GetBytes(signatureRaw);

        // 3. Calcular HMAC SHA384
        using (var hmac = new HMACSHA384(keyBytes))
        {
            byte[] hashBytes = hmac.ComputeHash(messageBytes);

            // 4. Convertir a Hexadecimal (minúsculas como hace CryptoJS por defecto)
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}

}
