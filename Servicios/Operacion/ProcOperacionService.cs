using ApiBanPlaz.models.Operacion;
using ApiBanPlaz.Servicios.General;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace ApiBanPlaz.Servicios.Operacion
{
    public interface IProcOperacionService
    {
        Task<OperacionResp> ProcesarOperacionAsync(OperacionReq req, string reqRawJson, CancellationToken ct);
    }
    public class ProcOperacionService : IProcOperacionService
    {
        private const string PathOperacion = "v0/cuentas/operacion";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly NonceService _nonceService;
        private readonly CredApiRsService _credApiRsService;
        private readonly OperacionService _operacionService;
        private readonly IApiSigService _apiSigService;
        private readonly ILogger<ProcOperacionService> _logger;

        public ProcOperacionService(
            IHttpClientFactory httpClientFactory,
            NonceService nonceService,
            CredApiRsService credApiRsService,
            OperacionService operacionService,
            IApiSigService apiSigService,
            ILogger<ProcOperacionService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _nonceService = nonceService;
            _credApiRsService = credApiRsService;
            _operacionService = operacionService;
            _apiSigService = apiSigService;
            _logger = logger;
        }

        public async Task<OperacionResp> ProcesarOperacionAsync(
            OperacionReq req, string reqRawJson, CancellationToken ct)
        {
            string nonce = await _nonceService.ObtNonce();

            var cred = await _credApiRsService.ObtCredApi();
            if (cred == null)
                throw new InvalidOperationException("No se encontraron credenciales de API configuradas.");

            string apiSignature = _apiSigService.Generar(PathOperacion, nonce, reqRawJson, cred.apiKeySecret);
            var (resumen, movimientos) = await CnBanAsync(reqRawJson, cred.ApiKey, apiSignature, nonce, ct);

            int idOperacion = await _operacionService.GrdOperacionReq(
                req.Cuenta, req.Moneda, req.Banco, req.TPago, req.Naturaleza, req.Referencia,
                req.FechaInicio, req.FechaFin, req.Monto, req.Canal, req.Id, req.Direccion_ip,
                reqRawJson);

            string jsonResumen = JsonConvert.SerializeObject(resumen);
            bool guardadoRespuestaOk = await _operacionService.GrdOperacionResp(
                idOperacion,
                resumen.CodigoRespuesta,
                resumen.DescripcionCliente,
                resumen.DescripcionSistema,
                resumen.FechaHora,
                resumen.CantMovimientos,
                jsonResumen);

            if (!guardadoRespuestaOk)
            {
                _logger.LogError(
                    "No se pudo persistir el resumen de Operacion para IdOperacion={IdOperacion}. Respuesta banco: {Respuesta}",
                    idOperacion, jsonResumen);
            }

            bool guardadoMovimientosOk = true;
            foreach (var movimiento in movimientos)
            {
                string jsonMovimientoIndividual = JsonConvert.SerializeObject(movimiento);
                bool okMovimiento = await _operacionService.GrdOpeMovimientos(
                    idOperacion,
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
                        "No se pudo persistir un movimiento individual de Operacion para IdOperacion={IdOperacion}. Movimiento: {Movimiento}",
                        idOperacion, jsonMovimientoIndividual);
                }
            }

            return new OperacionResp
            {
                CantMovimientos = resumen.CantMovimientos,
                CodigoRespuesta = resumen.CodigoRespuesta,
                DescripcionCliente = resumen.DescripcionCliente,
                DescripcionSistema = resumen.DescripcionSistema,
                FechaHora = resumen.FechaHora
            };
        }

        private async Task<(OperacionResp resumen, List<Movimiento> movimientos)> CnBanAsync(
            string prmJson, string prmApiKey, string prmApiSignature, string prmNonce, CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("BanPlaz");

            using var content = new StringContent(prmJson, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, PathOperacion)
            {
                Content = content
            };
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
                    PathOperacion, (int)res.StatusCode);
            }

            if (!res.IsSuccessStatusCode && string.IsNullOrWhiteSpace(codigoRespuesta))
            {
                _logger.LogError(
                    "Banco Plaza respondió {StatusCode} en {Path} sin datos de negocio utilizables. Body: {Body}",
                    (int)res.StatusCode, PathOperacion, bodyRaw);

                throw new HttpRequestException(
                    $"Banco Plaza respondió con código {(int)res.StatusCode}.");
            }

            OpeMovimientos movimientosResp;
            if (string.IsNullOrWhiteSpace(bodyRaw))
            {
                _logger.LogInformation(
                    "Banco Plaza respondió sin body en {Path} (status {StatusCode}). Se asume 0 movimientos.",
                    PathOperacion, (int)res.StatusCode);
                movimientosResp = new OpeMovimientos();
            }
            else
            {
                try
                {
                    movimientosResp = JsonConvert.DeserializeObject<OpeMovimientos>(bodyRaw) ?? new OpeMovimientos();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex,
                        "Body de Banco Plaza no es JSON válido, se asume 0 movimientos. Body: {Body}",
                        bodyRaw);
                    movimientosResp = new OpeMovimientos();
                }
            }
            movimientosResp.movimientos ??= new List<Movimiento>();

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

            var resumen = new OperacionResp
            {
                CodigoRespuesta = codigoRespuesta,
                DescripcionCliente = ObtenerHeader(res.Headers, "descripcionCliente"),
                DescripcionSistema = ObtenerHeader(res.Headers, "descripcionSistema"),
                FechaHora = fechaHora,
                CantMovimientos = movimientosResp.movimientos.Count
            };

            return (resumen, movimientosResp.movimientos);
        }

        private static string ObtenerHeader(HttpResponseHeaders headers, string nombre)
        {
            return headers.TryGetValues(nombre, out var values)
                ? values.FirstOrDefault() ?? string.Empty
                : string.Empty;
        }
    }
}

