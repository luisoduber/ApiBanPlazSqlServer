using ApiBanPlaz.models.CompPm;
using ApiBanPlaz.Servicios.CompPm;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

[ApiController]
[Route("v1/pagos")]
public class CompPmController : ControllerBase
{
    private readonly IProcCompPmService _IProcCompPmService;
    private readonly ILogger<CompPmController> _logger;

    public CompPmController(
        IProcCompPmService IProcCompPmService,
        ILogger<CompPmController> logger)
    {
        _IProcCompPmService = IProcCompPmService;
        _logger = logger;
    }

    [HttpGet("p2p/{id}")]
    [ProducesResponseType(typeof(CompPmResp), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> p2pId(
        string id,
        [FromQuery] string canal,
        [FromQuery] string? acc,
        [FromQuery] string? fi,
        [FromQuery] string? ff,
        [FromQuery] string? tlf,
        [FromQuery] string? tlfa,
        [FromQuery] string? horaIni,
        [FromQuery] string? horaFin,
        CancellationToken ct)
    {

        tlfa = tlfa ?? "";
        horaIni = horaIni ?? "";
        horaFin = horaFin ?? "";
        var req = new CompPmReq
        {
            Id = id,
            Canal = canal,
            Acc = acc,
            Fi = fi,
            Ff = ff,
            Tlf = tlf,
            Tlfa = tlfa,
            HoraIni = horaIni,
            HoraFin = horaFin
        };

        if (!TryVal(req, out var errVal))
            return BadRequest(errVal);

        try
        {
            var resultado = await _IProcCompPmService.ProcesarCompPmAsync(req, ct);
            return Ok(resultado);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de comunicación con Banco Plaza en /p2p/{Id}", id);
            return StatusCode(StatusCodes.Status502BadGateway,
                "No se pudo comunicar con el servicio del banco.");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Timeout al llamar a Banco Plaza en /p2p/{Id}", id);
            return StatusCode(StatusCodes.Status504GatewayTimeout,
                "Tiempo de espera agotado al contactar al banco.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("La petición /p2p/{Id} fue cancelada por el cliente.", id);
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado procesando /p2p/{Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                "Ocurrió un error inesperado procesando la solicitud.");
        }
    }

    private static bool TryVal(CompPmReq req, out string? err)
    {
        if (string.IsNullOrWhiteSpace(req.Id))
        {
            err = "El parámetro 'id' es obligatorio.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(req.Canal))
        {
            err = "El parámetro 'canal' es obligatorio.";
            return false;
        }

        if (!string.IsNullOrEmpty(req.Fi) && !DateTime.TryParse(req.Fi, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            err = "El parámetro 'fi' no es una fecha válida.";
            return false;
        }
        if (!string.IsNullOrEmpty(req.Ff) && !DateTime.TryParse(req.Ff, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            err = "El parámetro 'ff' no es una fecha válida.";
            return false;
        }

        err = null;
        return true;
    }
}

