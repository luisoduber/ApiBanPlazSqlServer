using ApiBanPlaz.models.TokenDl;
using ApiBanPlaz.Servicios.General;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace ApiBanPlaz.Servicios.TokenDl
{
    public interface IProcTokenDIService
    {
        Task<TokenDlResp> ProcTokenDIAsync(TokenDIReq req, string reqRawJson, CancellationToken ct);
    }


    public class ProcTokenDIService : IProcTokenDIService
    {
        private const string PathTokenDI = "v1/cce/debinm/tokenDI";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly NonceService _nonceService;
        private readonly CredApiRsService _credApiRsService;
        private readonly TokenDIService _tokenDIService;
        private readonly IApiSigService _apiSigService;
        private readonly ILogger<ProcTokenDIService> _logger;

        public ProcTokenDIService(
            IHttpClientFactory httpClientFactory,
            NonceService nonceService,
            CredApiRsService credApiRsService,
            TokenDIService tokenDIService,
            IApiSigService apiSigService,
            ILogger<ProcTokenDIService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _nonceService = nonceService;
            _credApiRsService = credApiRsService;
            _tokenDIService = tokenDIService;
            _apiSigService = apiSigService;
            _logger = logger;
        }

        public async Task<TokenDlResp> ProcTokenDIAsync(
            TokenDIReq req, string reqRawJson, CancellationToken ct)
        {
            string nonce = await _nonceService.ObtNonce();
            var cred = await _credApiRsService.ObtCredApi();
            if (cred == null)
                throw new InvalidOperationException("No se encontraron credenciales de API configuradas.");

            string apiSignature = _apiSigService.Generar(PathTokenDI, nonce, reqRawJson, cred.apiKeySecret);

            var bancoResp = await CnBanAsync(reqRawJson, cred.ApiKey, apiSignature, nonce, ct);
            int idTokenDI = await _tokenDIService.GrdTokenDIAsync(
                req.Moneda,
                req.Canal,
                req.Tvalidacion_p,
                req.Identificacion_p,
                req.Cuenta_cobrador,
                req.Cuenta_pagador,
                req.Telefono_pagador,
                req.Cod_banco_p,
                req.Monto,
                req.Direccion_ip,
                reqRawJson);

            string jsonBancoResp = JsonConvert.SerializeObject(bancoResp);
            bool guardadoOk = await _tokenDIService.GrdTokenDIRespAsync(
                idTokenDI,
                bancoResp.CodigoRespuesta,
                bancoResp.DescripcionCliente,
                bancoResp.DescripcionSistema,
                bancoResp.FechaHora,
                jsonBancoResp);

            if (!guardadoOk)
            {
                _logger.LogError(
                    "No se pudo persistir la respuesta del banco para IdTokenDI={IdTokenDI}. Respuesta banco: {Respuesta}",
                    idTokenDI, jsonBancoResp);
            }

            return new TokenDlResp
            {
                IdTokenDI = idTokenDI,
                GuardadoRespuestaOk = guardadoOk,
                CodigoRespuesta = bancoResp.CodigoRespuesta,
                DescripcionCliente = bancoResp.DescripcionCliente,
                DescripcionSistema = bancoResp.DescripcionSistema,
                FechaHora = bancoResp.FechaHora
            };
        }

        private async Task<TokenDlResp> CnBanAsync(
            string prmJson, string prmApiKey, string prmApiSignature, string prmNonce, CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("BanPlaz");
            using var content = new StringContent(prmJson, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, PathTokenDI)
            {
                Content = content
            };
            request.Headers.Add("api-key", prmApiKey);
            request.Headers.Add("api-signature", prmApiSignature);
            request.Headers.Add("nonce", prmNonce);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var res = await client.SendAsync(request);
            if (!res.IsSuccessStatusCode)
            {
                var bodyError = await res.Content.ReadAsStringAsync(ct);
                _logger.LogError(
                    "Banco Plaza respondió {StatusCode} en {Path}. Body: {Body}",
                    (int)res.StatusCode, PathTokenDI, bodyError);

                //throw new HttpRequestException($"Banco Plaza respondió con código {(int)res.StatusCode}.");
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

            return new TokenDlResp
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

