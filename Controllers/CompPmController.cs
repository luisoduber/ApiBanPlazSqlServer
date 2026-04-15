using ApiBanPlaz.models.CobroDl;
using ApiBanPlaz.models.CompPm;
using ApiBanPlaz.models.ConsultaLiq;
using ApiBanPlaz.Servicios.CobroDl;
using ApiBanPlaz.Servicios.CompPm;
using ApiBanPlaz.Servicios.General;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("v1/pagos")]
public class CompPmController : ControllerBase
{
    private readonly NonceService _nonceService;
    private readonly CredApiRsService _credApiRsService;

    private readonly CompPmService _CompPmService;
    private readonly IConfiguration _config;
    string urlBan = "";
    int idCompPm = 0;

    CompPmReq _CompPmReq = new CompPmReq();
    CompPmResp _CompPmResp = new CompPmResp();
    CompPmPag _CompPmPag = new CompPmPag();
    CompPm _CompPm = new CompPm();
    public CompPmController(IConfiguration config, NonceService nonceService,
                        CredApiRsService credApiRsService, CompPmService CompPmService)
    {
        _nonceService = nonceService;
        _credApiRsService = credApiRsService;
        _config = config;
        urlBan = _config["urlBan"].ToString();
        _CompPmService = CompPmService;
    }

    [HttpGet("p2p/{id}")]
    public async Task<IActionResult> p2pId(string id,
        [FromQuery] string canal,
        [FromQuery] string? acc,
        [FromQuery] string? fi,
        [FromQuery] string? ff,
        [FromQuery] string? tlf,
        [FromQuery] string? tlfa,
        [FromQuery] string? horaIni,
        [FromQuery] string? horaFin
        )
    {
        string qryStringCompPm = "";
        string nonce = await _nonceService.ObtNonce();
        var cred = await _credApiRsService.ObtCredApi();
        if (cred == null) return NotFound();

        string path = "v1/pagos/p2p";
        string apiSignature = ApiSignatureGen.Generar(
            path,
            nonce,
            cred.apiKeySecret
        );

        if (string.IsNullOrEmpty(canal)) { canal = ""; } else { qryStringCompPm = "?canal=" + canal; }
        if (string.IsNullOrEmpty(acc)) { acc = ""; } else { qryStringCompPm += "&acc=" + acc; }
        if (string.IsNullOrEmpty(fi))  { fi = ""; } else { qryStringCompPm += "&fi=" + Convert.ToDateTime(fi).ToString("yyyy-MM-dd"); }
        if (string.IsNullOrEmpty(ff)) { ff = ""; } else  { qryStringCompPm += "&ff=" + Convert.ToDateTime(ff).ToString("yyyy-MM-dd"); } 
        if (string.IsNullOrEmpty(tlf)) { tlf = ""; } else { qryStringCompPm += "&tlf=" + tlf; }
        if (string.IsNullOrEmpty(tlfa)) { tlfa = ""; } else { qryStringCompPm += "&tlfa=" + tlfa; }
        if (string.IsNullOrEmpty(horaIni)) { horaIni = ""; } else { qryStringCompPm += "&horaIni=" + horaIni; }
        if (string.IsNullOrEmpty(horaFin)) { horaFin = ""; } else { qryStringCompPm += "&horaFin=" + horaFin; }

        _CompPmReq.id = id;
        _CompPmReq.canal = canal;
        _CompPmReq.acc = acc;
        _CompPmReq.fi = fi;
        _CompPmReq.ff = ff;
        _CompPmReq.tlf = tlf;
        _CompPmReq.tlfa = tlfa;
        _CompPmReq.horaIni=horaIni;
        _CompPmReq.horaFin=horaFin;

        Debug.WriteLine(fi.ToString()+" "+ ff.ToString());

        _CompPmResp = await SolCompPm(id, qryStringCompPm, cred.ApiKey, apiSignature, nonce);
        _CompPm.idCompPm = await _CompPmService.GrdCompPmReq(
        _CompPmReq.id,
        _CompPmReq.canal,
        _CompPmReq.acc,
        _CompPmReq.fi,
        _CompPmReq.ff,
        _CompPmReq.tlf,
        _CompPmReq.tlfa,
        _CompPmReq.horaIni,
        _CompPmReq.horaFin,
        qryStringCompPm
            );

        string jsonCompPmResp = JsonConvert.SerializeObject(_CompPmResp);
        bool rsValCompPmResp = await _CompPmService.GrdCompPmResp(
        _CompPm.idCompPm,
        _CompPmResp.CodigoRespuesta,
        _CompPmResp.DescripcionCliente,
        _CompPmResp.DescripcionSistema,
        _CompPmResp.FechaHora,
        _CompPmResp.cantidadPagos,
        jsonCompPmResp);

        bool rsValCompPmPag = false;

        Debug.WriteLine("cantidad de pagos: "+ _CompPmPag.pagos.Count);
        if (_CompPmPag.pagos.Count > 0)
        {
            Debug.WriteLine("entro pagos: " + _CompPmPag.pagos.Count);
            foreach (var rsDat in _CompPmPag.pagos)
            {
                Debug.WriteLine("foreach pagos: " + _CompPmPag.pagos.Count);
                string jsonCompPmPag = JsonConvert.SerializeObject(_CompPmPag);
                rsValCompPmPag = await _CompPmService.GrdCompPmPag
                (
                    _CompPm.idCompPm,
                     rsDat.Accion,
                     rsDat.Banco,
                     rsDat.TelefonoCliente,
                     rsDat.TelefonoAfiliado,
                     rsDat.Monto,
                     rsDat.Origen,
                     rsDat.Fecha,
                     rsDat.Hora,
                     rsDat.Referencia,
                     rsDat.Concepto,
                     rsDat.cedulaB,
                    jsonCompPmPag
                );
            }
        }

        return Ok(new
        {
            //reqCompPm,
            _CompPm.idCompPm,
            rsValCompPmResp,
            rsValCompPmPag,
            _CompPmResp.cantidadPagos,
            _CompPmResp.CodigoRespuesta,
            _CompPmResp.DescripcionCliente,
            _CompPmResp.DescripcionSistema,
            _CompPmResp.FechaHora

        });
    }
    public async Task<CompPmResp> SolCompPm(string prmId, string prmQryString,
                                                     string prmApiKey, string prmApiSignature,
                                                     string prmNonce)
    {
        string rsDat = "";
        string codigoRespuesta = "";
        string descripcionCliente = "";
        string descripcionSistema = "";
        string fechaHora = "";

        using (var client = new HttpClient())
        {
            client.BaseAddress = new Uri(urlBan);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("api-key", prmApiKey);
            client.DefaultRequestHeaders.Add("api-signature", prmApiSignature);
            client.DefaultRequestHeaders.Add("nonce", prmNonce);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Debug.WriteLine($"{urlBan}v1/pagos/p2p/{prmId}{prmQryString}");
            using (var Res = await client.GetAsync($"{urlBan}v1/pagos/p2p/{prmId}{prmQryString}"))
            {

                if (Res.Headers.TryGetValues("codigoRespuesta", out var values)) { codigoRespuesta = values.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("descripcionCliente", out var values1)) { descripcionCliente = values1.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("descripcionSistema", out var values2)) { descripcionSistema = values2.FirstOrDefault(); }
                if (Res.Headers.TryGetValues("fechaHora", out var values3)) { fechaHora = values3.FirstOrDefault(); }

                 rsDat = await Res.Content.ReadAsStringAsync();
                _CompPmPag = JsonConvert.DeserializeObject<CompPmPag>(rsDat);

                _CompPmResp.CodigoRespuesta = codigoRespuesta;
                _CompPmResp.DescripcionCliente = descripcionCliente;
                _CompPmResp.DescripcionSistema = descripcionSistema;
                _CompPmResp.FechaHora = DateTime.Parse(fechaHora);
                _CompPmResp.cantidadPagos = _CompPmPag.cantidadPagos;
            }
        }
        return _CompPmResp;
    }

    public static class ApiSignatureGen
    {
        public static string Generar(string path, string nonce, string secret)
        {
            string signatureRaw = $"/{path}{nonce}";
            byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
            byte[] messageBytes = Encoding.UTF8.GetBytes(signatureRaw);

            using (var hmac = new HMACSHA384(keyBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(messageBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }

}

