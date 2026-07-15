using ApiBanPlaz.models.ConsultaLiq;
using ApiBanPlaz.models.ConsultaLiq;
using ApiBanPlaz.Servicios.General;
using System.Globalization;
using System.Net.Http.Headers;

namespace ApiBanPlaz.Servicios.ConsultaLiq
{
    public interface IProcConsultaLiqService
    {
        Task<ConsultaLiqResp> ProcConsultaLiqAsync(ConsultaLiqReq req, CancellationToken ct);
    }

    public class ProcConsultaLiqService : IProcConsultaLiqService
    {
        private const string PathConsultaLiq = "v1/cce/consultaLiq";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly NonceService _nonceService;
        private readonly CredApiRsService _credApiRsService;
        private readonly ConsultaLiqService _consultaLiqService;
        private readonly IApiSigService _apiSigService;
        private readonly ILogger<ProcConsultaLiqService> _logger;

        public ProcConsultaLiqService(
            IHttpClientFactory httpClientFactory,
            NonceService nonceService,
            CredApiRsService credApiRsService,
            ConsultaLiqService consultaLiqService,
            IApiSigService apiSigService,
            ILogger<ProcConsultaLiqService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _nonceService = nonceService;
            _credApiRsService = credApiRsService;
            _consultaLiqService = consultaLiqService;
            _apiSigService = apiSigService;
            _logger = logger;
        }

        public async Task<ConsultaLiqResp> ProcConsultaLiqAsync(ConsultaLiqReq req, CancellationToken ct)
        {
            string nonce = await _nonceService.ObtNonce();
            var cred = await _credApiRsService.ObtCredApi();
            if (cred == null)
                throw new InvalidOperationException("No se encontraron credenciales de API configuradas.");

            string apiSignature = _apiSigService.Generar(PathConsultaLiq, nonce, string.Empty, cred.apiKeySecret);
            string queryString = ArmarQueryString(req);
            var bancoResp = await CnBanAsync(req.Id, queryString, cred.ApiKey, apiSignature, nonce, ct);

            int idConsultaLiq = await _consultaLiqService.GrdConsultaLiq(
                req.Id,
                req.Cuenta,
                req.Referencia,
                req.Monto,
                req.Fecha,
                req.Canal,
                queryString);

            string jsonBancoResp = System.Text.Json.JsonSerializer.Serialize(bancoResp);
            bool guardadoOk = await _consultaLiqService.GrdConsultaLiqResp(
                idConsultaLiq,
                bancoResp.CodigoRespuesta,
                bancoResp.DescripcionCliente,
                bancoResp.DescripcionSistema,
                bancoResp.FechaHora,
                jsonBancoResp);

            if (!guardadoOk)
            {
                _logger.LogError(
                    "No se pudo persistir la respuesta del banco para IdConsultaLiq={IdConsultaLiq}. Respuesta banco: {Respuesta}",
                    idConsultaLiq, jsonBancoResp);
            }

            return new ConsultaLiqResp
            {
                CodigoRespuesta = bancoResp.CodigoRespuesta,
                DescripcionCliente = bancoResp.DescripcionCliente,
                DescripcionSistema = bancoResp.DescripcionSistema,
                FechaHora = bancoResp.FechaHora
            };
        }
        private static string ArmarQueryString(ConsultaLiqReq req)
        {
            var query = new Dictionary<string, string>
            {
                ["cuenta"] = req.Cuenta,
                ["referencia"] = req.Referencia,
                ["monto"] = req.Monto.ToString(CultureInfo.InvariantCulture),
                ["fecha"] = req.Fecha,
                ["canal"] = req.Canal
            };

            return "?" + string.Join("&", query.Select(kv =>
                $"{kv.Key}={Uri.EscapeDataString(kv.Value ?? string.Empty)}"));
        }

        private async Task<ConsultaLiqResp> CnBanAsync(string prmId, string prmQueryString, 
            string prmApiKey, string prmApiSignature, 
            string prmNonce,CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("BanPlaz");
            string urlRelativa = $"{PathConsultaLiq}/{Uri.EscapeDataString(prmId)}{prmQueryString}";
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
                    (int)res.StatusCode, PathConsultaLiq, bodyError);

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

            return new ConsultaLiqResp
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
