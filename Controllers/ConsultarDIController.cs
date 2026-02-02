using ApiBanPlaz.models.ConsultarDl;
using ApiBanPlaz.models.Entities;
using ApiBanPlaz.Servicios.ConsultarDl;
using ApiBanPlaz.Servicios.General;
using ApiBanPlaz.Servicios.TokenDl;
using Azure;
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
[Route("v1/cce/debinm")]
public class ConsultarDIController : ControllerBase
{
    private readonly NonceService _nonceService;
    private readonly CredApiRsService _credApiRsService;

    private readonly ConsultarDlService _ConsultarDIService;
    private readonly IConfiguration _config;
    string urlBan = "";
    int idConsultarDI = 0;
    ConsultarDlReq _ConsultarDlReq = new ConsultarDlReq();

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
   
    [HttpGet("consultarDI/{id}")]
    public async Task<IActionResult> ConsultarDI(string id,
        [FromQuery] string cuenta_cobrador,
        [FromQuery] string endtoend,
        [FromQuery] string referencia_c,
        [FromQuery] decimal monto,
        [FromQuery] string canal)

    {
        string queryStringDI= "";
        

        string nonce = await _nonceService.ObtNonce();
        var cred = await _credApiRsService.ObtCredApi();
        if (cred == null) return NotFound();

        string path = "v1/cce/debinm/consultarDI";


        string apiSignature = ApiSignatureGen.Generar(
            path,
            nonce,
            cred.apiKeySecret
        );

        queryStringDI =
                       "?cuenta_cobrador=" + cuenta_cobrador +
                       "&endtoend=" + endtoend +
                       "&referencia_c=" + referencia_c +
                       "&monto=" + monto.ToString().Replace(",",".") +
                       "&canal=" + canal.ToString();

        _ConsultarDIResp = await SolConsultarDI(id, queryStringDI, cred.ApiKey,apiSignature, nonce);
        _ConsultarDlReq.Id = id;
        _ConsultarDlReq.cuenta_cobrador = cuenta_cobrador;
        _ConsultarDlReq.endtoend = endtoend;
        _ConsultarDlReq.referencia_c = referencia_c;
        _ConsultarDlReq.Monto = monto;
        _ConsultarDlReq.canal= canal;

        _ConsultarDI.IdConsultarDI= await _ConsultarDIService.GrdConsultarDIAsync(
             _ConsultarDlReq.Id,
             _ConsultarDlReq.cuenta_cobrador,
             _ConsultarDlReq.endtoend,
             _ConsultarDlReq.referencia_c,
             _ConsultarDlReq.Monto,
             _ConsultarDlReq.canal,
            queryStringDI
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

    public async Task<ConsultarDlResp> SolConsultarDI(string prmId, string prmQryString, 
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


            Debug.WriteLine($"prmApiKey: {prmApiKey}");
            Debug.WriteLine($"prmApiSignature: {prmApiSignature}");
            Debug.WriteLine($"prmNonce: {prmNonce}");

            Debug.WriteLine($"{urlBan}v1/cce/debinm/consultarDI/{prmId}{prmQryString}");
            using (var Res = await client.GetAsync($"v1/cce/debinm/consultarDI/{prmId}{prmQryString}"))
            {

                Debug.WriteLine("respuesta: "+Res.IsSuccessStatusCode);
                if (Res.Headers.TryGetValues("codigoRespuesta", out var values)) { codigoRespuesta= values.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("descripcionCliente", out var values1)) { descripcionCliente = values1.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("descripcionSistema", out var values2)) { descripcionSistema = values2.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("fechaHora", out var values3)) { fechaHora = values3.FirstOrDefault(); }

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
