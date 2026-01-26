using ApiBanPlaz.models.Entities;
using ApiBanPlaz.models.TokenDl;
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

[ApiController]
[Route("v1/cce/debinm")]
public class TokenDIController : ControllerBase
{
    private readonly NonceService _nonceService;
    private readonly CredApiRsService _credApiRsService;

    private readonly TokenDIService _TokenDIService;
    private readonly IConfiguration _config;
    string urlBan = "";
    int idTokenDI = 0;

    TokenDIResp _TokenDIResp = new TokenDIResp();
    TokenDI _TokenDI = new TokenDI();
    public TokenDIController(IConfiguration config, NonceService nonceService, 
                        CredApiRsService credApiRsService, TokenDIService tokenDIService)
    {
        _nonceService = nonceService;
        _credApiRsService = credApiRsService;
        _config = config;
        urlBan = _config["urlBan"].ToString();
        _TokenDIService = tokenDIService;
    }

    [HttpPost("tokenDI")]
    public async Task<IActionResult> TokenDI()
    {
        // 1. Leer el body como string "crudo"
        string reqTokeDI="";
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            reqTokeDI = await reader.ReadToEndAsync();
        }

        var _ReqTokeDI = JsonConvert.DeserializeObject<TokenDIReq>(reqTokeDI);
        if (_ReqTokeDI == null) return BadRequest("Cuerpo de petición inválido.");

        string nonce = await _nonceService.ObtNonce();
        var cred = await _credApiRsService.ObtCredApi();
        if (cred == null) return NotFound();

        string path = "v1/cce/debinm/tokenDI";
        string apiSignature = ApiSignatureGen.Generar(
            path,
            nonce,
            reqTokeDI,
            cred.apiKeySecret
        );

        _TokenDIResp = await SolTokenDI(reqTokeDI, cred.ApiKey,apiSignature, nonce);
        //return Ok(new { nonce,cred.ApiKey,cred.apiKeySecret,apiSignature});

        _TokenDI.IdTokenDI= await _TokenDIService.GrdTokenDIAsync(
            _ReqTokeDI.Moneda,
            _ReqTokeDI.Canal,
            _ReqTokeDI.Tvalidacion_p,
            _ReqTokeDI.Identificacion_p,
            _ReqTokeDI.Cuenta_cobrador,
            _ReqTokeDI.Cuenta_pagador,
            _ReqTokeDI.Telefono_pagador,
            _ReqTokeDI.Cod_banco_p,
            _ReqTokeDI.Monto,
            _ReqTokeDI.Direccion_ip,
            reqTokeDI
            );

        string jsonTokenDIResp = JsonConvert.SerializeObject(_TokenDIResp);
        bool rsValTokDIResp = await _TokenDIService.GrdTokenDIRespAsync(
            _TokenDI.IdTokenDI,
            _TokenDIResp.CodigoRespuesta,
            _TokenDIResp.DescripcionCliente,
            _TokenDIResp.DescripcionSistema,
            _TokenDIResp.FechaHora,
            jsonTokenDIResp);

        return Ok(new
        {
            _TokenDI.IdTokenDI,
             rsValTokDIResp,
            _TokenDIResp.CodigoRespuesta,
            _TokenDIResp.DescripcionCliente,
            _TokenDIResp.DescripcionSistema,
            _TokenDIResp.FechaHora

        });
    }

    public async Task<TokenDIResp> SolTokenDI(string prmJson, string prmApiKey, 
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

            using (var Res = await client.PostAsync("v1/cce/debinm/tokenDI", content))
            {
                if (Res.Headers.TryGetValues("codigoRespuesta", out var values)) { codigoRespuesta= values.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("descripcionCliente", out var values1)) { descripcionCliente = values1.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("descripcionSistema", out var values2)) { descripcionSistema = values2.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("fechaHora", out var values3)) { fechaHora = values3.FirstOrDefault(); }

                //Debug.WriteLine("codigoRespuest: "+codigoRespuesta);
                //Debug.WriteLine("descripcionCliente: " + descripcionCliente);
                //Debug.WriteLine("descripcionSistema : " + descripcionSistema);
                //Debug.WriteLine("fechaHora : " + fechaHora);
                //Debug.WriteLine("urlBan: " + urlBan + "v1/cce/debinm/tokenDI");

                _TokenDIResp.CodigoRespuesta = codigoRespuesta;
                _TokenDIResp.DescripcionCliente = descripcionCliente;
                _TokenDIResp.DescripcionSistema = descripcionSistema;
                _TokenDIResp.FechaHora =DateTime.Parse(fechaHora); 

                // rsDat = await Res.Content.ReadAsStringAsync();
                // _TokenDIResp = JsonConvert.DeserializeObject<TokenDIResp>(rsDat);
            }
        }
        return _TokenDIResp;
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
