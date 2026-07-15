using ApiBanPlaz.models.ConsultarDl;
using ApiBanPlaz.models.Entities;
using ApiBanPlaz.Servicios.ConsultarDl;
using ApiBanPlaz.Servicios.General;
using ApiBanPlaz.Servicios.TokenDl;
using Azure;
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
public class ConsultarDIController : ControllerBase
{
    private readonly IProcConsultarDlService _ProcConsultarDlService;
    private readonly ConsultarDlService _ConsultarDIService;
    private readonly IConfiguration _config;
    private readonly ILogger<ConsultarDIController> _logger;
    public ConsultarDIController(IConfiguration config, ConsultarDlService ConsultarDIService, 
        IProcConsultarDlService ProcConsultarDlService, ILogger<ConsultarDIController> logger)
    {
        _config = config;
        _ConsultarDIService = ConsultarDIService;
        _ProcConsultarDlService = ProcConsultarDlService;
        _logger = logger;
    }
   
    [HttpGet("consultarDI/{id}")]
    [ProducesResponseType(typeof(ConsultarDlResp), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> ConsultarDI(string id,
        [FromQuery] string cuenta_cobrador,
        [FromQuery] string endtoend,
        [FromQuery] string referencia_c,
        [FromQuery] decimal monto,
        [FromQuery] string canal,
            CancellationToken ct)

    {
        var req = new ConsultarDlReq
        {
            Id = id,
            cuenta_cobrador = cuenta_cobrador,
            endtoend = endtoend,
            referencia_c = referencia_c,
            monto = monto,
            canal = canal
        };

        if (!TryVal(req, out var errVal))
            return BadRequest(errVal);

        try
        {
            var resultado = await _ProcConsultarDlService.ProcesarConsultarDIAsync(req, ct);
            return Ok(resultado);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de comunicación con Banco Plaza en /consultarDI (tras los reintentos configurados)");
            return StatusCode(StatusCodes.Status502BadGateway,
                "No se pudo comunicar con el servicio del banco.");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Timeout al llamar a Banco Plaza en /consultarDI");
            return StatusCode(StatusCodes.Status504GatewayTimeout,
                "Tiempo de espera agotado al contactar al banco.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("La petición /consultarDI fue cancelada por el cliente.");
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado procesando /consultarDI");
            return StatusCode(StatusCodes.Status500InternalServerError,
                "Ocurrió un error inesperado procesando la solicitud.");
        }
    }

    private static bool TryVal(ConsultarDlReq req, out string? err)
    {
        if (string.IsNullOrWhiteSpace(req.Id))
        {
            err = "El parámetro 'id' es obligatorio.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(req.cuenta_cobrador))
        {
            err = "El parámetro 'cuenta_cobrador' es obligatorio.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(req.endtoend))
        {
            err = "El parámetro 'endtoend' es obligatorio.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(req.referencia_c))
        {
            err = "El parámetro 'referencia_c' es obligatorio.";
            return false;
        }
        if (req.monto <= 0)
        {
            err = "El parámetro 'monto' debe ser mayor a cero.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(req.canal))
        {
            err = "El parámetro 'canal' es obligatorio.";
            return false;
        }

        err = null;
        return true;
    }


}
