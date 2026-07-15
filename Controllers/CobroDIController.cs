using ApiBanPlaz.models.CobroDI;
using ApiBanPlaz.models.CobroDl;
using ApiBanPlaz.Servicios.CobroDl;
using ApiBanPlaz.Servicios.General;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;


[ApiController]
[Route("v1/cce/debinm")]
public class CobroDIController : ControllerBase
{
    private readonly IProcCobroDIService _IProcCobroDIService;
    private readonly CobroDIService _CobroDIService;
    private readonly IConfiguration _config;
    private readonly ILogger<CobroDIController> _logger;

    public CobroDIController(IConfiguration config,  CobroDIService cobroDIService, IProcCobroDIService IProcCobroDIService, ILogger<CobroDIController> logger)
    {
        _CobroDIService = cobroDIService;
        _IProcCobroDIService = IProcCobroDIService;
        _logger = logger;
    }

    [HttpPost("CobroDI")]
    [ProducesResponseType(typeof(CobroDIResp), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> CobroDI(CancellationToken ct)
    {
        string reqCobroDIRaw;
        using (var reader = new StreamReader(Request.Body, System.Text.Encoding.UTF8))
        {
            reqCobroDIRaw = await reader.ReadToEndAsync(ct);
        }

        if (string.IsNullOrWhiteSpace(reqCobroDIRaw))
            return BadRequest("El cuerpo de la petición no puede estar vacío.");

        CobroDIReq? reqCobroDI;
        try
        {
            reqCobroDI = JsonConvert.DeserializeObject<CobroDIReq>(reqCobroDIRaw);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON inválido recibido en /CobroDI");
            return BadRequest("Cuerpo de petición inválido: JSON mal formado.");
        }

        if (reqCobroDI == null)
            return BadRequest("Cuerpo de petición inválido.");

        if (!TryValReq(reqCobroDI, out var errVal))
            return BadRequest(errVal);

        try
        {
            var resultado = await _IProcCobroDIService.ProcCobroDIAsync(reqCobroDI, reqCobroDIRaw, ct);
            return Ok(resultado);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de comunicación con Banco Plaza en /CobroDI");
            return StatusCode(StatusCodes.Status502BadGateway,
                "No se pudo comunicar con el servicio del banco.");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Timeout al llamar a Banco Plaza en /CobroDI");
            return StatusCode(StatusCodes.Status504GatewayTimeout,
                "Tiempo de espera agotado al contactar al banco.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("La petición /CobroDI fue cancelada por el cliente.");
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado procesando /CobroDI");
            return StatusCode(StatusCodes.Status500InternalServerError,
                "Ocurrió un error inesperado procesando la solicitud.");
        }
    }

    private static bool TryValReq(CobroDIReq req, out string? err)
    {
        if (string.IsNullOrWhiteSpace(req.Moneda))
        {
            err = "El campo 'Moneda' es obligatorio.";
            return false;
        }
        else if (string.IsNullOrWhiteSpace(req.Token_p))
        {
            err = "El campo 'Token_p' es obligatorio.";
            return false;
        }
       else  if (req.Monto <= 0)
        {
            err = "El campo 'Monto' debe ser mayor a cero.";
            return false;
        }

        err = null;
        return true;
    }

}
