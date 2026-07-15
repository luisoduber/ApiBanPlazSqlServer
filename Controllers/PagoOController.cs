using ApiBanPlaz.models.PagoO;
using ApiBanPlaz.Servicios.PagoO;
using ApiBanPlaz.Servicios.General;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;


[ApiController]
[Route("/v1/cce")]
public class PagoOController : ControllerBase
{
    private readonly IProcPagosOService _ProcPagosOService;
    private readonly ILogger<PagoOController> _logger;

    public PagoOController(
        IProcPagosOService ProcPagosOService,
        ILogger<PagoOController> logger)
    {
        _ProcPagosOService = ProcPagosOService;
        _logger = logger;
    }

    [HttpPost("PagoO/{prmRif}")]
    [ProducesResponseType(typeof(PagoOResp), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public Task<IActionResult> PagoORif(string prmRif, CancellationToken ct)
        => ProcesarAsync(rif: prmRif, ct);

    [HttpPost("PagoO")]
    [ProducesResponseType(typeof(PagoOResp), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public Task<IActionResult> PagoO(CancellationToken ct)
        => ProcesarAsync(rif: null, ct);

    /// <summary>
    /// Lógica común a ambos endpoints (antes duplicada en PagoORif y PagoO
    /// dentro del controlador original). Solo cambia si se le pasa un RIF o no.
    /// </summary>
    private async Task<IActionResult> ProcesarAsync(string? rif, CancellationToken ct)
    {
        string reqPagoORaw;
        using (var reader = new StreamReader(Request.Body, System.Text.Encoding.UTF8))
        {
            reqPagoORaw = await reader.ReadToEndAsync(ct);
        }

        if (string.IsNullOrWhiteSpace(reqPagoORaw))
            return BadRequest("El cuerpo de la petición no puede estar vacío.");

        PagoOReq? reqPagoO;
        try
        {
            reqPagoO = JsonConvert.DeserializeObject<PagoOReq>(reqPagoORaw);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON inválido recibido en /PagoO");
            return BadRequest("Cuerpo de petición inválido: JSON mal formado.");
        }

        if (reqPagoO == null)
            return BadRequest("Cuerpo de petición inválido.");

        if (!TryVal(reqPagoO, out var errVal))
            return BadRequest(errVal);

        try
        {
            var resultado = await _ProcPagosOService.ProcesarPagoOAsync(reqPagoO, reqPagoORaw, rif, ct);
            return Ok(resultado);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de comunicación con Banco Plaza en /PagoO (tras los reintentos configurados)");
            return StatusCode(StatusCodes.Status502BadGateway,
                "No se pudo comunicar con el servicio del banco.");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Timeout al llamar a Banco Plaza en /PagoO");
            return StatusCode(StatusCodes.Status504GatewayTimeout,
                "Tiempo de espera agotado al contactar al banco.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("La petición /PagoO fue cancelada por el cliente.");
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado procesando /PagoO");
            return StatusCode(StatusCodes.Status500InternalServerError,
                "Ocurrió un error inesperado procesando la solicitud.");
        }
    }

    private static bool TryVal(PagoOReq req, out string? err)
    {
        if (string.IsNullOrWhiteSpace(req.Moneda))
        {
            err = "El campo 'Moneda' es obligatorio.";
            return false;
        }
        else if (string.IsNullOrWhiteSpace(req.Cuenta_origen))
        {
            err = "El campo 'Cuenta_origen' es obligatorio.";
            return false;
        }
       else  if (string.IsNullOrWhiteSpace(req.Cuenta_destino))
        {
            err = "El campo 'Cuenta_destino' es obligatorio.";
            return false;
        }
        else if (req.Monto <= 0)
        {
            err = "El campo 'Monto' debe ser mayor a cero.";
            return false;
        }
        // Agrega aquí el resto de validaciones de negocio necesarias...

        err = null;
        return true;
    }
}

