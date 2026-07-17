using ApiBanPlaz.models.Operaciones;
using ApiBanPlaz.Servicios.General;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace ApiBanPlaz.Servicios.Operaciones
{
    public interface IProcOperacionesService
    {
        Task<OperacionesResp> ProcesarOperacionesAsync(
            OperacionesReq req, string reqRawJson, string prmRif, CancellationToken ct);
    }

    public class ProcOperacionesService : IProcOperacionesService
    {
        private const string PathOperaciones = "v0/cuentas/operaciones";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly NonceService _nonceService;
        private readonly CredApiRsService _credApiRsService;
        private readonly OperacionesService _operacionesService;
        private readonly IApiSigService _apiSigService;
        private readonly ILogger<ProcOperacionesService> _logger;

        public ProcOperacionesService(
            IHttpClientFactory httpClientFactory,
            NonceService nonceService,
            CredApiRsService credApiRsService,
            OperacionesService operacionesService,
            IApiSigService apiSigService,
            ILogger<ProcOperacionesService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _nonceService = nonceService;
            _credApiRsService = credApiRsService;
            _operacionesService = operacionesService;
            _apiSigService = apiSigService;
            _logger = logger;
        }

        public async Task<OperacionesResp> ProcesarOperacionesAsync(
            OperacionesReq req, string reqRawJson, string prmRif, CancellationToken ct)
        {
            string nonce = await _nonceService.ObtNonce();
            var cred = await _credApiRsService.ObtCredApi();
            if (cred == null)
                throw new InvalidOperationException("No se encontraron credenciales de API configuradas.");
            string apiSignature = _apiSigService.Generar(PathOperaciones, nonce, reqRawJson, cred.apiKeySecret);

            var (resumen, movimientos) = await CnBanAsync(prmRif, reqRawJson, cred.ApiKey, apiSignature, nonce, ct);
            decimal montoMinimo = req.MontoMinimo ?? 0;
            decimal montoMaximo = req.MontoMaximo ?? 0;

            int idOperaciones = await _operacionesService.GrdOperacionesReq(
                prmRif, req.Cuenta, req.Moneda, req.TPago, req.Naturaleza, req.FechaInicio, req.FechaFin,
                req.Canal, req.Id, req.Banco, req.Referencia, montoMinimo, montoMaximo, req.Direccion_ip,
                reqRawJson);

            string jsonResumen = JsonConvert.SerializeObject(resumen);
            bool guardadoRespuestaOk = await _operacionesService.GrdOperacionesResp(
                idOperaciones,
                resumen.CodigoRespuesta,
                resumen.DescripcionCliente,
                resumen.DescripcionSistema,
                resumen.FechaHora,
                resumen.CantMovimientos,
                jsonResumen);

            if (!guardadoRespuestaOk)
            {
                _logger.LogError(
                    "No se pudo persistir el resumen de Operaciones para IdOperaciones={IdOperaciones}. Respuesta banco: {Respuesta}",
                    idOperaciones, jsonResumen);
            }

            bool guardadoMovimientosOk = true;
            foreach (var movimiento in movimientos)
            {
                string jsonMovimientoIndividual = JsonConvert.SerializeObject(movimiento);
                bool okMovimiento = await _operacionesService.GrdOperacionesMov(
                    idOperaciones,
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
                        "No se pudo persistir un movimiento individual de Operaciones para IdOperaciones={IdOperaciones}. Movimiento: {Movimiento}",
                        idOperaciones, jsonMovimientoIndividual);
                }
            }

            return new OperacionesResp
            {
                CantMovimientos = resumen.CantMovimientos,
                CodigoRespuesta = resumen.CodigoRespuesta,
                DescripcionCliente = resumen.DescripcionCliente,
                DescripcionSistema = resumen.DescripcionSistema,
                FechaHora = resumen.FechaHora
            };
        }

        private async Task<(OperacionesResp resumen, List<MovimientoOpe> movimientos)> CnBanAsync(
            string prmRif, string prmJson, string prmApiKey, string prmApiSignature, string prmNonce,
            CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("BanPlaz");
            string urlRelativa = $"{PathOperaciones}/{Uri.EscapeDataString(prmRif)}";

            using var content = new StringContent(prmJson, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, urlRelativa)
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

            OperacionesMov movimientosResp;
            if (string.IsNullOrWhiteSpace(bodyRaw))
            {
                _logger.LogInformation(
                    "Banco Plaza respondió sin body en {Path} (status {StatusCode}). Se asume 0 movimientos.",
                    urlRelativa, (int)res.StatusCode);
                movimientosResp = new OperacionesMov();
            }
            else
            {
                try
                {
                    movimientosResp = JsonConvert.DeserializeObject<OperacionesMov>(bodyRaw) ?? new OperacionesMov();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex,
                        "Body de Banco Plaza no es JSON válido, se asume 0 movimientos. Body: {Body}",
                        bodyRaw);
                    movimientosResp = new OperacionesMov();
                }
            }
            movimientosResp.movimientos ??= new List<MovimientoOpe>();

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

            var resumen = new OperacionesResp
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

