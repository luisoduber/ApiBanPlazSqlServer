using ApiBanPlaz.models.PagosP2p;
using ApiBanPlaz.Servicios.PagosP2p;
using ApiBanPlaz.Servicios.General;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("/v1/pagos")]
public class PagosP2pController : ControllerBase
{
    private readonly IProcPagosP2pService _ProcPagosP2pService;
    private readonly ILogger<PagosP2pController> _logger;

    public PagosP2pController(
        IProcPagosP2pService ProcPagosP2pService,
        ILogger<PagosP2pController> logger)
    {
        _ProcPagosP2pService = ProcPagosP2pService;
        _logger = logger;
    }

    [HttpPost("PagosP2p/{prmRif}")]
    [ProducesResponseType(typeof(PagosP2pResp), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public Task<IActionResult> PagosP2pRif(string prmRif, CancellationToken ct)
        => ProcesarAsync(rif: prmRif, ct);

    [HttpPost("PagosP2p")]
    [ProducesResponseType(typeof(PagosP2pResp), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public Task<IActionResult> PagosP2p(CancellationToken ct)
        => ProcesarAsync(rif: null, ct);

    /// <summary>
    /// Lógica común a ambos endpoints (antes duplicada en PagosP2pRif y PagosP2p
    /// dentro del controlador original). Solo cambia si se le pasa un RIF o no.
    /// </summary>
    private async Task<IActionResult> ProcesarAsync(string? rif, CancellationToken ct)
    {
        string reqPagosP2pRaw;
        using (var reader = new StreamReader(Request.Body, System.Text.Encoding.UTF8))
        {
            reqPagosP2pRaw = await reader.ReadToEndAsync(ct);
        }

        if (string.IsNullOrWhiteSpace(reqPagosP2pRaw))
            return BadRequest("El cuerpo de la petición no puede estar vacío.");

        PagosP2pReq? reqPagosP2p;
        try
        {
            reqPagosP2p = JsonConvert.DeserializeObject<PagosP2pReq>(reqPagosP2pRaw);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON inválido recibido en /PagosP2p");
            return BadRequest("Cuerpo de petición inválido: JSON mal formado.");
        }

        if (reqPagosP2p == null)
            return BadRequest("Cuerpo de petición inválido.");

        if (!TryVal(reqPagosP2p, out var errVal))
            return BadRequest(errVal);

        try
        {
            var resultado = await _ProcPagosP2pService.ProcPagosP2pAsync(reqPagosP2p, reqPagosP2pRaw, rif, ct);
            return Ok(resultado);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de comunicación con Banco Plaza en /PagosP2p (tras los reintentos configurados)");
            return StatusCode(StatusCodes.Status502BadGateway,
                "No se pudo comunicar con el servicio del banco.");
        }

        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Timeout al llamar a Banco Plaza en /PagosP2p");
            return StatusCode(StatusCodes.Status504GatewayTimeout,
                "Tiempo de espera agotado al contactar al banco.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("La petición /PagosP2p fue cancelada por el cliente.");
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado procesando /PagosP2p");
            return StatusCode(StatusCodes.Status500InternalServerError,
                "Ocurrió un error inesperado procesando la solicitud.");
        }
    }

    private static bool TryVal(PagosP2pReq req, out string? err)
    {
        if (string.IsNullOrWhiteSpace(req.Banco))
        {
            err = "El campo 'Banco' es obligatorio.";
            return false;
        }
        else if (string.IsNullOrWhiteSpace(req.Telefono))
        {
            err = "El campo 'Telefono' es obligatorio.";
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
}


