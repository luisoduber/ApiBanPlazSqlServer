using ApiBanPlaz.models.Operaciones;
using ApiBanPlaz.Servicios.Operaciones;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;


[ApiController]
[Route("v0/cuentas")]
public class OperacionesController : ControllerBase
{
    private readonly IProcOperacionesService _IProcOperacionesService;
    private readonly ILogger<OperacionesController> _logger;

    public OperacionesController(
        IProcOperacionesService IProcOperacionesService,
        ILogger<OperacionesController> logger)
    {
        _IProcOperacionesService = IProcOperacionesService;
        _logger = logger;
    }

    [HttpPost("operaciones/{prmRif}")]
    [ProducesResponseType(typeof(OperacionesResp), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> Operaciones(string prmRif, CancellationToken ct)
    {
        string reqOperacionesRaw;
        using (var reader = new StreamReader(Request.Body, System.Text.Encoding.UTF8))
        {
            reqOperacionesRaw = await reader.ReadToEndAsync(ct);
        }

        if (string.IsNullOrWhiteSpace(reqOperacionesRaw))
            return BadRequest("El cuerpo de la petición no puede estar vacío.");

        if (string.IsNullOrWhiteSpace(prmRif))
            return BadRequest("El parámetro 'prmRif' es obligatorio.");

        OperacionesReq? reqOperaciones;
        try
        {
            reqOperaciones = JsonConvert.DeserializeObject<OperacionesReq>(reqOperacionesRaw);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON inválido recibido en /operaciones/{Rif}", prmRif);
            return BadRequest("Cuerpo de petición inválido: JSON mal formado.");
        }

        if (reqOperaciones == null)
            return BadRequest("Cuerpo de petición inválido.");

        if (!TryVal(reqOperaciones, out var errVal))
            return BadRequest(errVal);

        try
        {
            var resultado = await _IProcOperacionesService.ProcesarOperacionesAsync(reqOperaciones, reqOperacionesRaw, prmRif, ct);
            return Ok(resultado);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de comunicación con Banco Plaza en /operaciones/{Rif} (tras los reintentos configurados)", prmRif);
            return StatusCode(StatusCodes.Status502BadGateway,
                "No se pudo comunicar con el servicio del banco.");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Timeout al llamar a Banco Plaza en /operaciones/{Rif}", prmRif);
            return StatusCode(StatusCodes.Status504GatewayTimeout,
                "Tiempo de espera agotado al contactar al banco.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("La petición /operaciones/{Rif} fue cancelada por el cliente.", prmRif);
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado procesando /operaciones/{Rif}", prmRif);
            return StatusCode(StatusCodes.Status500InternalServerError,
                "Ocurrió un error inesperado procesando la solicitud.");
        }
    }

    private static bool TryVal(OperacionesReq req, out string? err)
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
