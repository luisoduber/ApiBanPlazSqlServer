using ApiBanPlaz.models.CobroDl;
using ApiBanPlaz.Servicios.General;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace ApiBanPlaz.Servicios.CobroDl
{
    public interface IProcCobroDIService
    {
        Task<CobroDIResp> ProcCobroDIAsync(CobroDIReq req, string reqRawJson, CancellationToken ct);
    }

    public class ProcCobroDIService : IProcCobroDIService
    {
        private const string PathCobroDI = "v1/cce/debinm/cobroDI";
        private const string PathCobroDIUrl = "v1/cce/debinm/cobroDI";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly NonceService _nonceService;
        private readonly CredApiRsService _credApiRsService;
        private readonly CobroDIService _cobroDIService;
        private readonly IApiSigService _apiSigService;
        private readonly ILogger<ProcCobroDIService> _logger;

        public ProcCobroDIService(
            IHttpClientFactory httpClientFactory,
            NonceService nonceService,
            CredApiRsService credApiRsService,
            CobroDIService cobroDIService,
            IApiSigService apiSignatureService,
            ILogger<ProcCobroDIService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _nonceService = nonceService;
            _credApiRsService = credApiRsService;
            _cobroDIService = cobroDIService;
            _apiSigService = apiSignatureService;
            _logger = logger;
        }

        public async Task<CobroDIResp> ProcCobroDIAsync(
            CobroDIReq req, string reqRawJson, CancellationToken ct)
        {
            string nonce = await _nonceService.ObtNonce();
            var cred = await _credApiRsService.ObtCredApi();
            if (cred == null)
                throw new InvalidOperationException("No se encontraron credenciales de API configuradas.");

            string apiSignature = _apiSigService.Generar(PathCobroDI, nonce, reqRawJson, cred.apiKeySecret);
            var bancoResp = await CnBanAsync(reqRawJson, cred.ApiKey, apiSignature, nonce, ct);

            int idCobroDI = await _cobroDIService.GrdCobroDIAsync(
                req.Moneda,
                req.Canal,
                req.Tvalidacion_p,
                req.Identificacion_p,
                req.Cuenta_cobrador,
                req.Cuenta_pagador,
                req.Telefono_pagador,
                req.Cod_banco_p,
                req.Nombre_p,
                req.Monto,
                req.Concepto,
                req.Token_p,
                req.Direccion_ip,
                req.Referencia_c,
                reqRawJson);

            string jsonBancoResp = JsonConvert.SerializeObject(bancoResp);
            bool guardadoOk = await _cobroDIService.GrdCobroDIRespAsync(
                idCobroDI,
                bancoResp.CodigoRespuesta,
                bancoResp.DescripcionCliente,
                bancoResp.DescripcionSistema,
                bancoResp.FechaHora,
                bancoResp.Referencia_c,
                bancoResp.Endtoend,
                jsonBancoResp);

            if (!guardadoOk)
            {
                _logger.LogError(
                    "No se pudo persistir la respuesta del banco para IdCobroDI={IdCobroDI}. Respuesta banco: {Respuesta}",
                    idCobroDI, jsonBancoResp);
            }

            return new CobroDIResp
            {
                Referencia_c = bancoResp.Referencia_c,
                Endtoend = bancoResp.Endtoend,
                CodigoRespuesta = bancoResp.CodigoRespuesta,
                DescripcionCliente = bancoResp.DescripcionCliente,
                DescripcionSistema = bancoResp.DescripcionSistema,
                FechaHora = bancoResp.FechaHora
            };
        }

        private async Task<CobroDIResp> CnBanAsync( string prmJson,string prmApiKey,
                       string prmApiSignature,string prmNonce,CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("BanPlaz");
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("api-key", prmApiKey);
            client.DefaultRequestHeaders.Add("api-signature", prmApiSignature);
            client.DefaultRequestHeaders.Add("nonce", prmNonce);

            using var content = new StringContent( prmJson,Encoding.UTF8,"application/json");
            using var res = await client.PostAsync(PathCobroDIUrl,content,ct);
            string bodyRaw = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Banco Plaza respondió {StatusCode}. Body: {Body}",
                    (int)res.StatusCode,
                    bodyRaw);

                throw new HttpRequestException(
                    $"Banco Plaza respondió con código {(int)res.StatusCode}.");
            }

            CobroDIResp rsDat;
            if (string.IsNullOrWhiteSpace(bodyRaw)){ rsDat = new CobroDIResp();}
            else
            {
                try
                {
                    rsDat = JsonConvert.DeserializeObject<CobroDIResp>(bodyRaw)
                                ?? new CobroDIResp();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "El body recibido de Banco Plaza no es un JSON válido. Body: {Body}",
                        bodyRaw);

                    rsDat = new CobroDIResp();
                }
            }

            rsDat.CodigoRespuesta = ObtenerHeader(res.Headers, "codigoRespuesta");
            rsDat.DescripcionCliente = ObtenerHeader(res.Headers, "descripcionCliente");
            rsDat.DescripcionSistema = ObtenerHeader(res.Headers, "descripcionSistema");

            var fechaHoraHeader = ObtenerHeader(res.Headers, "fechaHora");

            if (!DateTime.TryParse(
                    fechaHoraHeader,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var fechaHora))
            {
                fechaHora = DateTime.UtcNow;
            }

            rsDat.FechaHora = fechaHora;
            return rsDat;
        }

        private static string ObtenerHeader(HttpResponseHeaders headers, string nombre)
        {
            return headers.TryGetValues(nombre, out var values)
                ? values.FirstOrDefault() ?? string.Empty
                : string.Empty;
        }
    }
}

