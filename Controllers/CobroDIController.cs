using ApiBanPlaz.models.CobroDI;
using ApiBanPlaz.models.CobroDl;
using ApiBanPlaz.models.Entities;
using ApiBanPlaz.Servicios.CobroDl;
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
[Route("v1/cce/debinm")]
public class CobroDIController : ControllerBase
{
    private readonly NonceService _nonceService;
    private readonly CredApiRsService _credApiRsService;

    private readonly CobroDIService _CobroDIService;
    private readonly IConfiguration _config;
    string urlBan = "";
    int idCobroDI = 0;

    CobroDIResp _CobroDIResp = new CobroDIResp();
    CobroDI _CobroDI = new CobroDI();
    public CobroDIController(IConfiguration config, NonceService nonceService,
                        CredApiRsService credApiRsService, CobroDIService cobroDIService)
    {
        _nonceService = nonceService;
        _credApiRsService = credApiRsService;
        _config = config;
        urlBan = _config["urlBan"].ToString();
        _CobroDIService = cobroDIService;
    }

    [HttpPost("CobroDI")]
    public async Task<IActionResult> CobroDI()
    {
        // 1. Leer el body como string "crudo"
        string reqCobroDI="";
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            reqCobroDI = await reader.ReadToEndAsync();
        }

        var _ReqCobroDI = JsonConvert.DeserializeObject<CobroDIReq>(reqCobroDI);
        if (_ReqCobroDI == null) return BadRequest("Cuerpo de petición inválido.");

        string nonce = await _nonceService.ObtNonce();
        var cred = await _credApiRsService.ObtCredApi();
        if (cred == null) return NotFound();

        string path = "v1/cce/debinm/cobroDI";
        string apiSignature = ApiSignatureGen.Generar(
            path,
            nonce,
           reqCobroDI,
            cred.apiKeySecret
        );

        _CobroDIResp = await SolTokenDI(reqCobroDI, cred.ApiKey,apiSignature, nonce);
        //return Ok(new { nonce,cred.ApiKey,cred.apiKeySecret,apiSignature});

        _CobroDI.IdCobroDI = await _CobroDIService.GrdCobroDIAsync(
            _ReqCobroDI.Moneda,
            _ReqCobroDI.Canal,
            _ReqCobroDI.Tvalidacion_p,
            _ReqCobroDI.Identificacion_p,
            _ReqCobroDI.Cuenta_cobrador,
            _ReqCobroDI.Cuenta_pagador,
            _ReqCobroDI.Telefono_pagador,
            _ReqCobroDI.Cod_banco_p,
            _ReqCobroDI.Nombre_p,
            _ReqCobroDI.Monto,
            _ReqCobroDI.Concepto,
            _ReqCobroDI.Direccion_ip,
            reqCobroDI
            );

        string jsonCobroDIResp = JsonConvert.SerializeObject(_CobroDIResp);
        bool rsValCobroDIResp = await _CobroDIService.GrdCobroDIRespAsync(
            _CobroDI.IdCobroDI,
            _CobroDIResp.CodigoRespuesta,
            _CobroDIResp.DescripcionCliente,
            _CobroDIResp.DescripcionSistema,
            _CobroDIResp.FechaHora,
            _CobroDIResp.Referencia_c,
            _CobroDIResp.Endtoend,
            jsonCobroDIResp);

        return Ok(new
        {
            _CobroDI.IdCobroDI,
             rsValCobroDIResp,
            _CobroDIResp.CodigoRespuesta,
            _CobroDIResp.DescripcionCliente,
            _CobroDIResp.DescripcionSistema,
            _CobroDIResp.FechaHora

        });
    }

    public async Task<CobroDIResp> SolTokenDI(string prmJson, string prmApiKey, 
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

            using (var Res = await client.PostAsync("v1/cce/debinm/cobroDI", content))
            {
                if (Res.Headers.TryGetValues("codigoRespuesta", out var values)) { codigoRespuesta= values.FirstOrDefault();  Debug.WriteLine(values.FirstOrDefault()); }
                if (Res.Headers.TryGetValues("descripcionCliente", out var values1)) { descripcionCliente = values1.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("descripcionSistema", out var values2)) { descripcionSistema = values2.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("fechaHora", out var values3)) { fechaHora = values3.FirstOrDefault(); }
             

                //Debug.WriteLine("codigoRespuest: "+codigoRespuesta);
                //Debug.WriteLine("descripcionCliente: " + descripcionCliente);
                //Debug.WriteLine("descripcionSistema : " + descripcionSistema);
                //Debug.WriteLine("fechaHora : " + fechaHora);
                //Debug.WriteLine("urlBan: " + urlBan + "/v1/cce/debinm/cobroDI");

                _CobroDIResp.CodigoRespuesta = codigoRespuesta;
                _CobroDIResp.DescripcionCliente = descripcionCliente;
                _CobroDIResp.DescripcionSistema = descripcionSistema;
                _CobroDIResp.FechaHora =DateTime.Parse(fechaHora);
               _CobroDIResp.Referencia_c ="";
                _CobroDIResp.Endtoend = "";

                // rsDat = await Res.Content.ReadAsStringAsync();
                // _CobroDIResp = JsonConvert.DeserializeObject<CobroDIResp>(rsDat);
            }
        }
        return _CobroDIResp;
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
