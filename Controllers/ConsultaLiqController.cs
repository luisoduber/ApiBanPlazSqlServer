using ApiBanPlaz.models.ConsultaLiq;
using ApiBanPlaz.Servicios.ConsultaLiq;
using ApiBanPlaz.Servicios.ConsultarDl;
using ApiBanPlaz.Servicios.General;
using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("v1/cce")]
public class ConsultaLiqController : ControllerBase
{
    private readonly IProcConsultaLiqService _ProcConsultaLiqService;
    private readonly ILogger<ConsultaLiqController> _logger;

    public ConsultaLiqController(
        IProcConsultaLiqService ProcConsultaLiqService,
        ILogger<ConsultaLiqController> logger)
    {
        _ProcConsultaLiqService = ProcConsultaLiqService;
        _logger = logger;
    }

    [HttpGet("consultaLiq/{id}")]
    [ProducesResponseType(typeof(ConsultaLiqResp), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> ConsultaLiq(
        string id,
        [FromQuery] string cuenta,
        [FromQuery] string referencia,
        [FromQuery] decimal monto,
        [FromQuery] string fecha,
        [FromQuery] string canal,
        CancellationToken ct)
    {
        var req = new ConsultaLiqReq
        {
            Id = id,
            Cuenta = cuenta,
            Referencia = referencia,
            Monto = monto,
            Fecha = fecha,
            Canal = canal
        };

        if (!TryVal(req, out var errVal))
            return BadRequest(errVal);

        try
        {
            var resultado = await _ProcConsultaLiqService.ProcConsultaLiqAsync(req, ct);
            return Ok(resultado);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de comunicación con Banco Plaza en /consultaLiq");
            return StatusCode(StatusCodes.Status502BadGateway,
                "No se pudo comunicar con el servicio del banco.");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Timeout al llamar a Banco Plaza en /consultaLiq");
            return StatusCode(StatusCodes.Status504GatewayTimeout,
                "Tiempo de espera agotado al contactar al banco.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("La petición /consultaLiq fue cancelada por el cliente.");
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado procesando /consultaLiq");
            return StatusCode(StatusCodes.Status500InternalServerError,
                "Ocurrió un error inesperado procesando la solicitud.");
        }
    }

    private static bool TryVal(ConsultaLiqReq req, out string? err)
    {
        if (string.IsNullOrWhiteSpace(req.Id))
        {
            err = "El parámetro 'id' es obligatorio.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(req.Cuenta))
        {
            err = "El parámetro 'cuenta' es obligatorio.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(req.Referencia))
        {
            err = "El parámetro 'referencia' es obligatorio.";
            return false;
        }
        if (req.Monto <= 0)
        {
            err = "El parámetro 'monto' debe ser mayor a cero.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(req.Fecha))
        {
            err = "El parámetro 'fecha' es obligatorio.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(req.Canal))
        {
            err = "El parámetro 'canal' es obligatorio.";
            return false;
        }

        err = null;
        return true;
    }
}
