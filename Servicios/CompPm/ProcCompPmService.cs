using ApiBanPlaz.models.CompPm;
using ApiBanPlaz.Servicios.General;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace ApiBanPlaz.Servicios.CompPm
{
    public interface IProcCompPmService
    {
        Task<CompPmResp> ProcesarCompPmAsync(CompPmReq req, CancellationToken ct);
    }

    public class ProcCompPmService : IProcCompPmService
    {
        private const string PathCompPm = "v1/pagos/p2p";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly NonceService _nonceService;
        private readonly CredApiRsService _credApiRsService;
        private readonly CompPmService _compPmService;
        private readonly IApiSigService _apiSigService;
        private readonly ILogger<ProcCompPmService> _logger;

        public ProcCompPmService(
            IHttpClientFactory httpClientFactory,
            NonceService nonceService,
            CredApiRsService credApiRsService,
            CompPmService compPmService,
            IApiSigService apiSigService,
            ILogger<ProcCompPmService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _nonceService = nonceService;
            _credApiRsService = credApiRsService;
            _compPmService = compPmService;
            _apiSigService = apiSigService;
            _logger = logger;
        }

        public async Task<CompPmResp> ProcesarCompPmAsync(CompPmReq req, CancellationToken ct)
        {
            string nonce = await _nonceService.ObtNonce();
            var cred = await _credApiRsService.ObtCredApi();
            if (cred == null)
                throw new InvalidOperationException("No se encontraron credenciales de API configuradas.");

            string apiSignature = _apiSigService.Generar(PathCompPm, nonce, string.Empty, cred.apiKeySecret);

            string queryString = ArmarQueryString(req);
            var (resumen, pagos) = await CnBanAsync(req.Id, queryString, cred.ApiKey, apiSignature, nonce, ct);

            int idCompPm = await _compPmService.GrdCompPmReq(
                req.Id, req.Canal, req.Acc, req.Fi, req.Ff, req.Tlf, req.Tlfa, req.HoraIni, req.HoraFin,
                queryString);

            string jsonResumen = JsonConvert.SerializeObject(resumen);
            bool guardadoRespuestaOk = await _compPmService.GrdCompPmResp(
                idCompPm,
                resumen.CodigoRespuesta,
                resumen.DescripcionCliente,
                resumen.DescripcionSistema,
                resumen.FechaHora,
                resumen.CantidadPagos,
                jsonResumen);

            if (!guardadoRespuestaOk)
            {
                _logger.LogError(
                    "No se pudo persistir el resumen de CompPm para IdCompPm={IdCompPm}. Respuesta banco: {Respuesta}",
                    idCompPm, jsonResumen);
            }


            bool guardadoPagosOk = true;
            foreach (var pago in pagos)
            {
                string jsonPagoIndividual = JsonConvert.SerializeObject(pago);
                bool okPago = await _compPmService.GrdCompPmPag(
                    idCompPm,
                    pago.Accion,
                    pago.Banco,
                    pago.TelefonoCliente,
                    pago.TelefonoAfiliado,
                    pago.Monto,
                    pago.Origen,
                    pago.Fecha,
                    pago.Hora,
                    pago.Referencia,
                    pago.Concepto,
                    pago.CedulaB,
                    jsonPagoIndividual);

                if (!okPago)
                {
                    guardadoPagosOk = false;
                    _logger.LogError(
                        "No se pudo persistir un pago individual de CompPm para IdCompPm={IdCompPm}. Pago: {Pago}",
                        idCompPm, jsonPagoIndividual);
                }
            }

            return new CompPmResp
            {
                CantidadPagos = resumen.CantidadPagos,
                CodigoRespuesta = resumen.CodigoRespuesta,
                DescripcionCliente = resumen.DescripcionCliente,
                DescripcionSistema = resumen.DescripcionSistema,
                FechaHora = resumen.FechaHora
            };
        }
        private static string ArmarQueryString(CompPmReq req)
        {
            var pares = new List<string>();

            void AgregarSiTieneValor(string clave, string? valor)
            {
                if (!string.IsNullOrEmpty(valor))
                    pares.Add($"{clave}={Uri.EscapeDataString(valor)}");
            }

            AgregarSiTieneValor("canal", req.Canal);
            AgregarSiTieneValor("acc", req.Acc);
            AgregarSiTieneValor("fi", FormatearFecha(req.Fi));
            AgregarSiTieneValor("ff", FormatearFecha(req.Ff));
            AgregarSiTieneValor("tlf", req.Tlf);
            AgregarSiTieneValor("tlfa", req.Tlfa);
            AgregarSiTieneValor("horaIni", req.HoraIni);
            AgregarSiTieneValor("horaFin", req.HoraFin);

            return pares.Count == 0 ? string.Empty : "?" + string.Join("&", pares);
        }

        private static string? FormatearFecha(string? fecha)
        {
            if (string.IsNullOrEmpty(fecha))
                return null;

            return DateTime.TryParse(fecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                ? dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : fecha; // ya debería haber sido validado antes; esto es una red de seguridad
        }

        private async Task<(CompPmResp resumen, List<Pago> pagos)> CnBanAsync(
            string prmId, string prmQueryString, string prmApiKey, string prmApiSignature, string prmNonce,
            CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("BanPlaz");
            string urlRelativa = $"{PathCompPm}/{Uri.EscapeDataString(prmId)}{prmQueryString}";

            /* using var request = new HttpRequestMessage(HttpMethod.Get, urlRelativa);
             request.Headers.Add("api-key", prmApiKey);
             request.Headers.Add("api-signature", prmApiSignature);
             request.Headers.Add("nonce", prmNonce);
             request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

             using var res = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);*/


            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("api-key", prmApiKey);
            client.DefaultRequestHeaders.Add("api-signature", prmApiSignature);
            client.DefaultRequestHeaders.Add("nonce", prmNonce);

            using var res = await client.GetAsync(urlRelativa, ct);
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

            CompPmPag pagosResp;
            if (string.IsNullOrWhiteSpace(bodyRaw))
            {
                _logger.LogInformation(
                    "Banco Plaza respondió sin body en {Path} (status {StatusCode}). Se asume 0 pagos.",
                    urlRelativa, (int)res.StatusCode);
                pagosResp = new CompPmPag();
            }
            else
            {
                try
                {
                    pagosResp = JsonConvert.DeserializeObject<CompPmPag>(bodyRaw) ?? new CompPmPag();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex,
                        "Body de Banco Plaza no es JSON válido, se asume 0 pagos. Body: {Body}",
                        bodyRaw);
                    pagosResp = new CompPmPag();
                }
            }
            pagosResp.Pagos ??= new List<Pago>();

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

            var resumen = new CompPmResp
            {
                CodigoRespuesta = codigoRespuesta,
                DescripcionCliente = ObtenerHeader(res.Headers, "descripcionCliente"),
                DescripcionSistema = ObtenerHeader(res.Headers, "descripcionSistema"),
                FechaHora = fechaHora,
                CantidadPagos = pagosResp.CantidadPagos
            };

            return (resumen, pagosResp.Pagos);
        }

        private static string ObtenerHeader(HttpResponseHeaders headers, string nombre)
        {
            return headers.TryGetValues(nombre, out var values)
                ? values.FirstOrDefault() ?? string.Empty
                : string.Empty;
        }
    }
}
