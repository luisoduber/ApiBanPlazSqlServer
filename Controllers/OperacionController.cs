using ApiBanPlaz.models.Operacion;
using ApiBanPlaz.Servicios.Operacion;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

[ApiController]
[Route("v0/cuentas")]
public class OperacionController : ControllerBase
{
    private readonly IProcOperacionService _IProcOperacionService;
    private readonly ILogger<OperacionController> _logger;

    public OperacionController(
        IProcOperacionService IProcOperacionService,
        ILogger<OperacionController> logger)
    {
        _IProcOperacionService = IProcOperacionService;
        _logger = logger;
    }

    [HttpPost("Operacion")]
    [ProducesResponseType(typeof(OperacionResp), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> Operacion(CancellationToken ct)
    {
        string reqOperacionRaw;
        using (var reader = new StreamReader(Request.Body, System.Text.Encoding.UTF8))
        {
            reqOperacionRaw = await reader.ReadToEndAsync(ct);
        }

        if (string.IsNullOrWhiteSpace(reqOperacionRaw))
            return BadRequest("El cuerpo de la petición no puede estar vacío.");

        OperacionReq? reqOperacion;
        try
        {
            reqOperacion = JsonConvert.DeserializeObject<OperacionReq>(reqOperacionRaw);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON inválido recibido en /Operacion");
            return BadRequest("Cuerpo de petición inválido: JSON mal formado.");
        }

        if (reqOperacion == null)
            return BadRequest("Cuerpo de petición inválido.");

        if (!TryVal(reqOperacion, out var errVal))
            return BadRequest(errVal);

        try
        {
            var resultado = await _IProcOperacionService.ProcesarOperacionAsync(reqOperacion, reqOperacionRaw, ct);
            return Ok(resultado);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de comunicación con Banco Plaza en /Operacion (tras los reintentos configurados)");
            return StatusCode(StatusCodes.Status502BadGateway,
                "No se pudo comunicar con el servicio del banco.");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Timeout al llamar a Banco Plaza en /Operacion");
            return StatusCode(StatusCodes.Status504GatewayTimeout,
                "Tiempo de espera agotado al contactar al banco.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("La petición /Operacion fue cancelada por el cliente.");
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado procesando /Operacion");
            return StatusCode(StatusCodes.Status500InternalServerError,
                "Ocurrió un error inesperado procesando la solicitud.");
        }
    }

    private static bool TryVal(OperacionReq req, out string? err)
    {
        if (string.IsNullOrWhiteSpace(req.Cuenta))
        {
            err = "El campo 'Cuenta' es obligatorio.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(req.Moneda))
        {
            err = "El campo 'Moneda' es obligatorio.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(req.Id))
        {
            err = "El campo 'Id' es obligatorio.";
            return false;
        }

        err = null;
        return true;
    }
}