using ApiBanPlaz.models.CuentasMov;
using ApiBanPlaz.Servicios.CuentasMov;
using Microsoft.AspNetCore.Mvc;

namespace ApiBanPlaz.Controllers
{
    [ApiController]
    [Route("v0/")]
    public class CuentasMovController : ControllerBase
    {
        private readonly IProcCuentasMovService _IProcCuentasMovService;
        private readonly ILogger<CuentasMovController> _logger;

        public CuentasMovController(
            IProcCuentasMovService IProcCuentasMovService,
            ILogger<CuentasMovController> logger)
        {
            _IProcCuentasMovService = IProcCuentasMovService;
            _logger = logger;
        }

        [HttpGet("cuentasMov/{cuenta}/movimientos")]
        [ProducesResponseType(typeof(CuentasMovResp), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
        public Task<IActionResult> CuentasMov(
            string cuenta,
            [FromQuery] string moneda,
            [FromQuery] string fechaInicio,
            [FromQuery] string fechafin,
            CancellationToken ct)
        {
            // Un solo segmento de cuenta: se escapa individualmente.
            string cuentaPathSegment = Uri.EscapeDataString(cuenta ?? string.Empty);
            return ProcesarAsync(cuenta, moneda, fechaInicio, fechafin, cuentaPathSegment, ct);
        }

        [HttpGet("cuentasMov/{id}/{cuenta}/movimientos")]
        [ProducesResponseType(typeof(CuentasMovResp), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
        public Task<IActionResult> CuentasMovConId(
            string id,
            string cuenta,
            [FromQuery] string moneda,
            [FromQuery] string fechaInicio,
            [FromQuery] string fechafin,
            CancellationToken ct)
        {
            string cuentaCompuesta = $"{id}/{cuenta}";
            string cuentaPathSegment = $"{Uri.EscapeDataString(id ?? string.Empty)}/{Uri.EscapeDataString(cuenta ?? string.Empty)}";
            return ProcesarAsync(cuentaCompuesta, moneda, fechaInicio, fechafin, cuentaPathSegment, ct);
        }
        private async Task<IActionResult> ProcesarAsync(
            string cuentaParaGuardar, string moneda, string fechaInicio, string fechafin,
            string cuentaPathSegment, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(cuentaPathSegment))
                return BadRequest("El parámetro 'cuenta' es obligatorio.");
            if (string.IsNullOrWhiteSpace(moneda))
                return BadRequest("El parámetro 'moneda' es obligatorio.");
            if (string.IsNullOrWhiteSpace(fechaInicio))
                return BadRequest("El parámetro 'fechaInicio' es obligatorio.");
            if (string.IsNullOrWhiteSpace(fechafin))
                return BadRequest("El parámetro 'fechafin' es obligatorio.");

            var req = new CuentasMovReq
            {
                Cuenta = cuentaParaGuardar,
                Referencia = string.Empty,
                Moneda = moneda,
                FechaInicio = fechaInicio,
                FechaFin = fechafin,
                Tipo = string.Empty,
                MontoMinimo = 0,
                MontoMaximo = 0,
                Concepto = string.Empty
            };

            try
            {
                var resultado = await _IProcCuentasMovService.ProcesarCuentasMovAsync(req, cuentaPathSegment, ct);
                return Ok(resultado);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de comunicación con Banco Plaza en /cuentasMov/{Cuenta}/movimientos (tras los reintentos configurados)", cuentaParaGuardar);
                return StatusCode(StatusCodes.Status502BadGateway,
                    "No se pudo comunicar con el servicio del banco.");
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Timeout al llamar a Banco Plaza en /cuentasMov/{Cuenta}/movimientos", cuentaParaGuardar);
                return StatusCode(StatusCodes.Status504GatewayTimeout,
                    "Tiempo de espera agotado al contactar al banco.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("La petición /cuentasMov/{Cuenta}/movimientos fue cancelada por el cliente.", cuentaParaGuardar);
                return StatusCode(499);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Parámetro inválido en /cuentasMov/{Cuenta}/movimientos", cuentaParaGuardar);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado procesando /cuentasMov/{Cuenta}/movimientos", cuentaParaGuardar);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Ocurrió un error inesperado procesando la solicitud.");
            }
        }
    }
}
