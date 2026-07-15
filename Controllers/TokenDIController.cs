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
    private readonly IProcTokenDIService _IProcTokenDIService;
    private readonly ILogger<TokenDIController> _logger;
    private readonly TokenDIService _TokenDIService;
    private readonly IConfiguration _config;
    string urlBan = "";
    int idTokenDI = 0;

    TokenDlResp _TokenDIResp = new TokenDlResp();
    TokenDI _TokenDI = new TokenDI();
    public TokenDIController(IConfiguration config, TokenDIService tokenDIService, IProcTokenDIService IProcTokenDIService,ILogger<TokenDIController> logger)
    {
        _config = config;
        _TokenDIService = tokenDIService;
        _IProcTokenDIService = IProcTokenDIService;
        _logger = logger;
    }

    [HttpPost("tokenDI")]
    [ProducesResponseType(typeof(TokenDlResp), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> TokenDI(CancellationToken ct)
    {
        string reqTokeDIRaw;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8)) {reqTokeDIRaw = await reader.ReadToEndAsync(ct); }

        if (string.IsNullOrWhiteSpace(reqTokeDIRaw))
            return BadRequest("El cuerpo de la petición no puede estar vacío.");

        TokenDIReq? reqTokeDI;
        try
        {
            reqTokeDI = JsonConvert.DeserializeObject<TokenDIReq>(reqTokeDIRaw);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON inválido recibido en /tokenDI");
            return BadRequest("Cuerpo de petición inválido: JSON mal formado.");
        }



        if (reqTokeDI == null)
            return BadRequest("Cuerpo de petición inválido.");

        if (!TryValReq(reqTokeDI, out var errVal))
            return BadRequest(errVal);

        try
        {
            var resultado = await _IProcTokenDIService.ProcTokenDIAsync(reqTokeDI, reqTokeDIRaw, ct);
            return Ok(resultado);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de comunicación con Banco Plaza en /tokenDI");
            return StatusCode(StatusCodes.Status502BadGateway,
                "No se pudo comunicar con el servicio del banco.");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            // TaskCanceledException lanzada por timeout del HttpClient (no por el cliente cancelando)
            _logger.LogWarning(ex, "Timeout al llamar a Banco Plaza en /tokenDI");
            return StatusCode(StatusCodes.Status504GatewayTimeout,
                "Tiempo de espera agotado al contactar al banco.");
        }
        catch (OperationCanceledException)
        {
            // El propio cliente HTTP canceló la petición: no hay nada que responder.
            _logger.LogInformation("La petición /tokenDI fue cancelada por el cliente.");
            return StatusCode(499); // Client Closed Request (convención informal)
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado procesando /tokenDI");
            return StatusCode(StatusCodes.Status500InternalServerError,
                "Ocurrió un error inesperado procesando la solicitud.");
        }
    }

    private static bool TryValReq(TokenDIReq req, out string? err)
    {
        if (string.IsNullOrWhiteSpace(req.Moneda))
        {
            err = "El campo 'Moneda' es obligatorio.";
            return false;
        }
        else if (req.Monto <= 0)
        {
            err = "El campo 'Monto' debe ser mayor a cero.";
            return false;
        }

        err = null;
        return true;
    }

    

    public async Task<TokenDlResp> SolTokenDI(string prmJson, string prmApiKey, 
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

}
