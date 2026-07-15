using ApiBanPlaz.models.PagosP2p;
using ApiBanPlaz.Servicios.General;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace ApiBanPlaz.Servicios.PagosP2p
{
    public interface IProcPagosP2pService
    {
        Task<PagosP2pResp> ProcPagosP2pAsync(
            PagosP2pReq req, string reqRawJson, string? rif, CancellationToken ct);
    }

    public class ProcPagosP2pService : IProcPagosP2pService
    {

        private const string PathPagosP2p = "v1/pagos/p2p";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly NonceService _nonceService;
        private readonly CredApiRsService _credApiRsService;
        private readonly PagosP2pService _pagosP2pService;
        private readonly IApiSigService _apiSigService;
        private readonly ILogger<ProcPagosP2pService> _logger;

        public ProcPagosP2pService(
            IHttpClientFactory httpClientFactory,
            NonceService nonceService,
            CredApiRsService credApiRsService,
            PagosP2pService pagosP2pService,
            IApiSigService apiSigService,
            ILogger<ProcPagosP2pService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _nonceService = nonceService;
            _credApiRsService = credApiRsService;
            _pagosP2pService = pagosP2pService;
            _apiSigService = apiSigService;
            _logger = logger;
        }

        public async Task<PagosP2pResp> ProcPagosP2pAsync(
            PagosP2pReq req, string reqRawJson, string? rif, CancellationToken ct)
        {
            string nonce = await _nonceService.ObtNonce();
            var cred = await _credApiRsService.ObtCredApi();
            if (cred == null)
                throw new InvalidOperationException("No se encontraron credenciales de API configuradas.");

            string apiSignature = _apiSigService.Generar(PathPagosP2p, nonce, reqRawJson, cred.apiKeySecret);
            var bancoResp = await CnBanAsync(reqRawJson, cred.ApiKey, apiSignature, nonce, rif, ct);

            int idPagosP2p = await _pagosP2pService.spGrdPagosP2pReq(
                req.Banco,
                req.IdBeneficiario,
                req.Telefono,
                req.Monto,
                req.Motivo,
                req.Canal,
                req.IdExterno,
                req.Cuenta,
                req.TelefonoAfiliado,
                req.Moneda,
                req.Sucursal,
                req.Cajero,
                req.Caja,
                req.IpCliente,
                req.Longitud,
                req.Latitud,
                req.Precision,
                reqRawJson);

            string jsonBancoResp = JsonConvert.SerializeObject(bancoResp);
            bool guardadoOk = await _pagosP2pService.spGrdPagosP2pResp(
                idPagosP2p,
                bancoResp.CodigoRespuesta,
                bancoResp.DescripcionCliente,
                bancoResp.DescripcionSistema,
                bancoResp.FechaHora,
                bancoResp.NumeroReferencia,
                jsonBancoResp);

            if (!guardadoOk)
            {
                _logger.LogError(
                    "No se pudo persistir la respuesta del banco para IdPagosP2p={IdPagosP2p}. Respuesta banco: {Respuesta}",
                    idPagosP2p, jsonBancoResp);
            }

            return new PagosP2pResp
            {
                NumeroReferencia = bancoResp.NumeroReferencia,
                CodigoRespuesta = bancoResp.CodigoRespuesta,
                DescripcionCliente = bancoResp.DescripcionCliente,
                DescripcionSistema = bancoResp.DescripcionSistema,
                FechaHora = bancoResp.FechaHora
            };
        }

        private async Task<PagosP2pResp> CnBanAsync(
            string prmJson, string prmApiKey, string prmApiSignature, string prmNonce,
            string? rif, CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("BanPlaz");
            string urlRelativa = string.IsNullOrWhiteSpace(rif)
                ? PathPagosP2p
                : $"{PathPagosP2p}/{Uri.EscapeDataString(rif)}";

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

            PagosP2pResp bancoResp;
            if (string.IsNullOrWhiteSpace(bodyRaw))
            {
                _logger.LogInformation(
                    "Banco Plaza respondió sin body en {Path} (status {StatusCode}). Se usa solo la información de los headers.",
                    urlRelativa, (int)res.StatusCode);
                bancoResp = new PagosP2pResp();
            }
            else
            {
                try
                {
                    bancoResp = JsonConvert.DeserializeObject<PagosP2pResp>(bodyRaw) ?? new PagosP2pResp();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex,
                        "Body de Banco Plaza no es JSON válido, se ignora y se continúa solo con headers. Body: {Body}",
                        bodyRaw);
                    bancoResp = new PagosP2pResp();
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

