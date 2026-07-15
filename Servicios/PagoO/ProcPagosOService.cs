using ApiBanPlaz.models.PagoO;
using ApiBanPlaz.Servicios.General;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace ApiBanPlaz.Servicios.PagoO
{
    public interface IProcPagosOService
    {
        Task<PagoOResp> ProcesarPagoOAsync(
            PagoOReq req, string reqRawJson, string? rif, CancellationToken ct);
    }

    public class ProcPagosOService : IProcPagosOService
    {
        private const string PathPagoO = "v1/cce/pagoO";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly NonceService _nonceService;
        private readonly CredApiRsService _credApiRsService;
        private readonly PagoOService _pagoOService;
        private readonly IApiSigService _apiSigService;
        private readonly ILogger<ProcPagosOService> _logger;

        public ProcPagosOService(
            IHttpClientFactory httpClientFactory,
            NonceService nonceService,
            CredApiRsService credApiRsService,
            PagoOService pagoOService,
            IApiSigService apiSigService,
            ILogger<ProcPagosOService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _nonceService = nonceService;
            _credApiRsService = credApiRsService;
            _pagoOService = pagoOService;
            _apiSigService = apiSigService;
            _logger = logger;
        }
        public async Task<PagoOResp> ProcesarPagoOAsync(
            PagoOReq req, string reqRawJson, string? rif, CancellationToken ct)
        {
            string nonce = await _nonceService.ObtNonce();
            var cred = await _credApiRsService.ObtCredApi();
            if (cred == null)
                throw new InvalidOperationException("No se encontraron credenciales de API configuradas.");

            string apiSignature = _apiSigService.Generar(PathPagoO, nonce, reqRawJson, cred.apiKeySecret);
            var bancoResp = await CnBanAsync(reqRawJson, cred.ApiKey, apiSignature, nonce, rif, ct);
            DateTime fechaHora = req.Fecha_hora ?? DateTime.UtcNow;

            int idPagoO = await _pagoOService.spGrdPagoOReq(
                req.Moneda,
                req.Canal,
                req.Tipo_cce,
                req.Tipo_proposito,
                req.Tipo_instrumento_b,
                req.Identificacion_o,
                req.Identificacion_b,
                req.Cuenta_origen,
                req.Cuenta_destino,
                req.Telefono,
                req.Correo,
                req.Cod_banco_d,
                req.Cod_banco_a,
                req.Nombre_d,
                req.Nombre_a,
                req.Monto,
                req.Concepto,
                req.Direccion_ip,
                req.Referencia,
                fechaHora,
                reqRawJson);

            string jsonBancoResp = JsonConvert.SerializeObject(bancoResp);
            bool guardadoOk = await _pagoOService.spGrdPagoOResp(
                idPagoO,
                bancoResp.CodigoRespuesta,
                bancoResp.DescripcionCliente,
                bancoResp.DescripcionSistema,
                bancoResp.FechaHora,
                bancoResp.NumeroReferencia,
                jsonBancoResp);

            if (!guardadoOk)
            {
                _logger.LogError(
                    "No se pudo persistir la respuesta del banco para IdPagoO={IdPagoO}. Respuesta banco: {Respuesta}",
                    idPagoO, jsonBancoResp);
            }

            return new PagoOResp
            {
                NumeroReferencia = bancoResp.NumeroReferencia,
                CodigoRespuesta = bancoResp.CodigoRespuesta,
                DescripcionCliente = bancoResp.DescripcionCliente,
                DescripcionSistema = bancoResp.DescripcionSistema,
                FechaHora = bancoResp.FechaHora
            };
        }

        private async Task<PagoOResp> CnBanAsync(
            string prmJson, string prmApiKey, string prmApiSignature, string prmNonce,
            string? rif, CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("BanPlaz");

            string urlRelativa = string.IsNullOrWhiteSpace(rif)
                ? PathPagoO
                : $"{PathPagoO}/{Uri.EscapeDataString(rif)}";

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

            PagoOResp bancoResp;
            if (string.IsNullOrWhiteSpace(bodyRaw))
            {
                _logger.LogInformation(
                    "Banco Plaza respondió sin body en {Path} (status {StatusCode}). Se usa solo la información de los headers.",
                    urlRelativa, (int)res.StatusCode);
                bancoResp = new PagoOResp();
            }
            else
            {
                try
                {
                    bancoResp = JsonConvert.DeserializeObject<PagoOResp>(bodyRaw) ?? new PagoOResp();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex,
                        "Body de Banco Plaza no es JSON válido, se ignora y se continúa solo con headers. Body: {Body}",
                        bodyRaw);
                    bancoResp = new PagoOResp();
                }
            }

            bancoResp.CodigoRespuesta = codigoRespuesta;
            bancoResp.DescripcionCliente = ObtenerHeader(res.Headers, "descripcionCliente");
            bancoResp.DescripcionSistema = ObtenerHeader(res.Headers, "descripcionSistema");

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
            bancoResp.FechaHora = fechaHora;
            return bancoResp;
        }
        private static string ObtenerHeader(HttpResponseHeaders headers, string nombre)
        {
            return headers.TryGetValues(nombre, out var values)
                ? values.FirstOrDefault() ?? string.Empty
                : string.Empty;
        }
    }
}
