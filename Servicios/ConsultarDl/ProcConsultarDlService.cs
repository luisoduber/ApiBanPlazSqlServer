using ApiBanPlaz.models.ConsultarDl;
using ApiBanPlaz.Servicios.General;
using System.Globalization;
using System.Net.Http.Headers;

namespace ApiBanPlaz.Servicios.ConsultarDl
{
    public interface IProcConsultarDlService
    {
        Task<ConsultarDlResp> ProcesarConsultarDIAsync(ConsultarDlReq req, CancellationToken ct);
    }

    public class ProcConsultarDlService : IProcConsultarDlService
    {
        private const string PathConsultarDI = "v1/cce/debinm/consultarDI";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly NonceService _nonceService;
        private readonly CredApiRsService _credApiRsService;
        private readonly ConsultarDlService _consultarDlService;
        private readonly IApiSigService _apiSigService;
        private readonly ILogger<ProcConsultarDlService> _logger;

        public ProcConsultarDlService(
            IHttpClientFactory httpClientFactory,
            NonceService nonceService,
            CredApiRsService credApiRsService,
            ConsultarDlService consultarDlService,
            IApiSigService apiSignatureService,
            ILogger<ProcConsultarDlService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _nonceService = nonceService;
            _credApiRsService = credApiRsService;
            _consultarDlService = consultarDlService;
            _apiSigService = apiSignatureService;
            _logger = logger;
        }

        public async Task<ConsultarDlResp> ProcesarConsultarDIAsync(ConsultarDlReq req, CancellationToken ct)
        {
            string nonce = await _nonceService.ObtNonce();
            var cred = await _credApiRsService.ObtCredApi();
            if (cred == null)
                throw new InvalidOperationException("No se encontraron credenciales de API configuradas.");

            string apiSignature = _apiSigService.Generar(PathConsultarDI, nonce, string.Empty, cred.apiKeySecret);
            string queryString = ArmarQueryString(req);
            var bancoResp = await ConsultarBancoAsync(req.Id, queryString, cred.ApiKey, apiSignature, nonce, ct);

            int idConsultarDI = await _consultarDlService.GrdConsultarDIAsync(
                req.Id,
                req.cuenta_cobrador,
                req.endtoend,
                req.referencia_c,
                req.monto,
                req.canal,
                queryString);

            string jsonBancoResp = System.Text.Json.JsonSerializer.Serialize(bancoResp);
            bool guardadoOk = await _consultarDlService.GrdConsultarDIRespAsync(
                idConsultarDI,
                bancoResp.CodigoRespuesta,
                bancoResp.DescripcionCliente,
                bancoResp.DescripcionSistema,
                bancoResp.FechaHora,
                jsonBancoResp);

            if (!guardadoOk)
            {
                _logger.LogError(
                    "No se pudo persistir la respuesta del banco para IdConsultarDI={IdConsultarDI}. Respuesta banco: {Respuesta}",
                    idConsultarDI, jsonBancoResp);
            }

            return new ConsultarDlResp
            {
                CodigoRespuesta = bancoResp.CodigoRespuesta,
                DescripcionCliente = bancoResp.DescripcionCliente,
                DescripcionSistema = bancoResp.DescripcionSistema,
                FechaHora = bancoResp.FechaHora
            };
        }

        private static string ArmarQueryString(ConsultarDlReq req)
        {
            var query = new Dictionary<string, string>
            {
                ["cuenta_cobrador"] = req.cuenta_cobrador,
                ["endtoend"] = req.endtoend,
                ["referencia_c"] = req.referencia_c,
                ["monto"] = req.monto.ToString(CultureInfo.InvariantCulture),
                ["canal"] = req.canal
            };

            return "?" + string.Join("&", query.Select(kv =>
                $"{kv.Key}={Uri.EscapeDataString(kv.Value ?? string.Empty)}"));
        }

        private async Task<ConsultarDlResp> ConsultarBancoAsync(
            string prmId, string prmQueryString, string prmApiKey, string prmApiSignature, string prmNonce,
            CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("BanPlaz");
            string urlRelativa = $"{PathConsultarDI}/{Uri.EscapeDataString(prmId)}{prmQueryString}";

            using var request = new HttpRequestMessage(HttpMethod.Get, urlRelativa);
            request.Headers.Add("api-key", prmApiKey);
            request.Headers.Add("api-signature", prmApiSignature);
            request.Headers.Add("nonce", prmNonce);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var res = await client.SendAsync(request, ct);

            if (!res.IsSuccessStatusCode)
            {
                var bodyError = await res.Content.ReadAsStringAsync(ct);
                _logger.LogError(
                    "Banco Plaza respondió {StatusCode} en {Path}. Body: {Body}",
                    (int)res.StatusCode, PathConsultarDI, bodyError);

                throw new HttpRequestException(
                    $"Banco Plaza respondió con código {(int)res.StatusCode}.");
            }

            string codigoRespuesta = ObtenerHeader(res.Headers, "codigoRespuesta");
            string descripcionCliente = ObtenerHeader(res.Headers, "descripcionCliente");
            string descripcionSistema = ObtenerHeader(res.Headers, "descripcionSistema");
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

            return new ConsultarDlResp
            {
                CodigoRespuesta = codigoRespuesta,
                DescripcionCliente = descripcionCliente,
                DescripcionSistema = descripcionSistema,
                FechaHora = fechaHora
            };
        }

        private static string ObtenerHeader(HttpResponseHeaders headers, string nombre)
        {
            return headers.TryGetValues(nombre, out var values)
                ? values.FirstOrDefault() ?? string.Empty
                : string.Empty;
        }
    }
}
