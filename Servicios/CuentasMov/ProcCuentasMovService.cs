using ApiBanPlaz.models.CuentasMov;
using ApiBanPlaz.models.Operacion;
using ApiBanPlaz.Servicios.General;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http.Headers;

namespace ApiBanPlaz.Servicios.CuentasMov
{
    public interface IProcCuentasMovService
    {
        Task<CuentasMovResp> ProcesarCuentasMovAsync(
            CuentasMovReq req, string cuentaPathSegment, CancellationToken ct);
    }

    public class ProcCuentasMovService : IProcCuentasMovService
    {

        private const string PathFirma = "v0/cuentas";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly NonceService _nonceService;
        private readonly CredApiRsService _credApiRsService;
        private readonly CuentasMovService _cuentasMovService;
        private readonly IApiSigService _apiSigService;
        private readonly ILogger<ProcCuentasMovService> _logger;

        public ProcCuentasMovService(
            IHttpClientFactory httpClientFactory,
            NonceService nonceService,
            CredApiRsService credApiRsService,
            CuentasMovService cuentasMovService,
            IApiSigService apiSigService,
            ILogger<ProcCuentasMovService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _nonceService = nonceService;
            _credApiRsService = credApiRsService;
            _cuentasMovService = cuentasMovService;
            _apiSigService = apiSigService;
            _logger = logger;
        }

        public async Task<CuentasMovResp> ProcesarCuentasMovAsync(
            CuentasMovReq req, string cuentaPathSegment, CancellationToken ct)
        {
            string nonce = await _nonceService.ObtNonce();
            var cred = await _credApiRsService.ObtCredApi();
            if (cred == null)
                throw new InvalidOperationException("No se encontraron credenciales de API configuradas.");

            string apiSignature = _apiSigService.Generar(PathFirma, nonce, string.Empty, cred.apiKeySecret);
            string queryString = ArmarQueryString(req);

            var (resumen, cuentaInfo, movimientos) = await ConsultarBancoAsync(
                cuentaPathSegment, queryString, cred.ApiKey, apiSignature, nonce, ct);

            int idCuentasMov = await _cuentasMovService.GrdCuentasMovReq(
                req.Cuenta, req.Moneda, req.Referencia, req.FechaInicio, req.FechaFin,
                req.Tipo, req.MontoMinimo, req.MontoMaximo, req.Concepto, queryString);

            string jsonResumen = JsonConvert.SerializeObject(resumen);
            bool guardadoRespuestaOk = await _cuentasMovService.GrdCuentMovResp(
                idCuentasMov,
                resumen.CodigoRespuesta,
                resumen.DescripcionCliente,
                resumen.DescripcionSistema,
                resumen.FechaHora,
                resumen.CantMov,
                cuentaInfo.Numero,
                cuentaInfo.FechaApertura,
                cuentaInfo.TipoCuenta,
                cuentaInfo.Estatus,
                cuentaInfo.Moneda,
                cuentaInfo.SaldoDisponible,
                jsonResumen);

            if (!guardadoRespuestaOk)
            {
                _logger.LogError(
                    "No se pudo persistir el resumen de CuentasMov para IdCuentasMov={IdCuentasMov}. Respuesta banco: {Respuesta}",
                    idCuentasMov, jsonResumen);
            }

            bool guardadoMovimientosOk = true;
            foreach (var movimiento in movimientos)
            {
                string jsonMovimientoIndividual = JsonConvert.SerializeObject(movimiento);
                bool okMovimiento = await _cuentasMovService.GrdCuentListMov(
                    idCuentasMov,
                    cuentaInfo.Numero,
                    movimiento.Fecha,
                    movimiento.Hora,
                    movimiento.Referencia,
                    movimiento.Concepto,
                    movimiento.Tipo,
                    movimiento.Naturaleza,
                    movimiento.Monto,
                    jsonMovimientoIndividual);

                if (!okMovimiento)
                {
                    guardadoMovimientosOk = false;
                    _logger.LogError(
                        "No se pudo persistir un movimiento individual de CuentasMov para IdCuentasMov={IdCuentasMov}. Movimiento: {Movimiento}",
                        idCuentasMov, jsonMovimientoIndividual);
                }
            }

            return new CuentasMovResp
            {
                CantMov = resumen.CantMov,
                CodigoRespuesta = resumen.CodigoRespuesta,
                DescripcionCliente = resumen.DescripcionCliente,
                DescripcionSistema = resumen.DescripcionSistema,
                FechaHora = resumen.FechaHora
            };
        }

        private static string ArmarQueryString(CuentasMovReq req)
        {
            var pares = new List<string>
            {
                $"moneda={Uri.EscapeDataString(req.Moneda ?? string.Empty)}",
                $"fechaInicio={Uri.EscapeDataString(req.FechaInicio ?? string.Empty)}",
                $"fechafin={Uri.EscapeDataString(req.FechaFin ?? string.Empty)}"
            };
            return "?" + string.Join("&", pares);
        }

        private async Task<(CuentasMovResp resumen, CuentasListMov cuentaInfo, List<MovimientoCuent> movimientos)>
            ConsultarBancoAsync(
                string cuentaPathSegment, string queryString, string prmApiKey, string prmApiSignature,
                string prmNonce, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(cuentaPathSegment))
                throw new ArgumentException("El segmento de cuenta no puede estar vacío.", nameof(cuentaPathSegment));

            var client = _httpClientFactory.CreateClient("BanPlaz");
            string urlRelativa = $"v0/cuentas/{cuentaPathSegment}/movimientos{queryString}";

            using var request = new HttpRequestMessage(HttpMethod.Get, urlRelativa);
            request.Headers.Add("api-key", prmApiKey);
            request.Headers.Add("api-signature", prmApiSignature);
            request.Headers.Add("nonce", prmNonce);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var res = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            string codigoRespuesta = ObtenerHeader(res.Headers, "codigoRespuesta");
            string bodyRaw = string.Empty;
            try
            {
                using var ctsBody = CancellationTokenSource.CreateLinkedTokenSource(ct);
                ctsBody.CancelAfter(TimeSpan.FromSeconds(5));
                bodyRaw = await res.Content.ReadAsStringAsync(ctsBody.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "La lectura del body de Banco Plaza en {Path} no terminó en 5s (status {StatusCode}). " +
                    "Se continúa solo con la información de los headers.",
                    urlRelativa, (int)res.StatusCode);
            }

            if (!res.IsSuccessStatusCode && string.IsNullOrWhiteSpace(codigoRespuesta))
            {
                _logger.LogError(
                    "Banco Plaza respondió {StatusCode} en {Path} sin datos de negocio utilizables. Body: {Body}",
                    (int)res.StatusCode, urlRelativa, bodyRaw);

                throw new HttpRequestException(
                    $"Banco Plaza respondió con código {(int)res.StatusCode}.");
            }

            CuentasListMov cuentaInfo;
            if (string.IsNullOrWhiteSpace(bodyRaw))
            {
                _logger.LogInformation(
                    "Banco Plaza respondió sin body en {Path} (status {StatusCode}). Se asume 0 movimientos.",
                    urlRelativa, (int)res.StatusCode);
                cuentaInfo = new CuentasListMov();
            }
            else
            {
                try
                {
                    cuentaInfo = JsonConvert.DeserializeObject<CuentasListMov>(bodyRaw) ?? new CuentasListMov();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex,
                        "Body de Banco Plaza no es JSON válido, se asume 0 movimientos. Body: {Body}",
                        bodyRaw);
                    cuentaInfo = new CuentasListMov();
                }
            }
            cuentaInfo.Movimientos ??= new List<MovimientoCuent>();

            string fechaHoraRaw = ObtenerHeader(res.Headers, "fechaHora");
            if (!DateTime.TryParse(
                    fechaHoraRaw,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var fechaHora))
            {
                _logger.LogWarning(
                    "No se pudo parsear el header 'fechaHora' ('{FechaHoraRaw}'). Se usa la hora actual del servidor.",
                    fechaHoraRaw);
                fechaHora = DateTime.UtcNow;
            }

            var resumen = new CuentasMovResp
            {
                CodigoRespuesta = codigoRespuesta,
                DescripcionCliente = ObtenerHeader(res.Headers, "descripcionCliente"),
                DescripcionSistema = ObtenerHeader(res.Headers, "descripcionSistema"),
                FechaHora = fechaHora,
                CantMov = cuentaInfo.Movimientos.Count
            };

            return (resumen, cuentaInfo, cuentaInfo.Movimientos);
        }

        private static string ObtenerHeader(HttpResponseHeaders headers, string nombre)
        {
            return headers.TryGetValues(nombre, out var values)
                ? values.FirstOrDefault() ?? string.Empty
                : string.Empty;
        }
    }
}
