using System.Security.Cryptography;
using System.Text;

namespace ApiBanPlaz.Servicios.General
{
    public interface IApiSigService
    {
        string Generar(string path, string nonce, string body, string secret);
    }

    /// <summary>
    /// Antes era una clase estática anidada dentro del controlador.
    /// Se extrae como servicio inyectable para poder mockearla en pruebas unitarias.
    /// </summary>
    public class ApiSigService : IApiSigService
    {
        public string Generar(string path, string nonce, string body, string secret)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            ArgumentException.ThrowIfNullOrWhiteSpace(secret);

            string signatureRaw = $"/{path}{nonce}{body}";
            byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
            byte[] messageBytes = Encoding.UTF8.GetBytes(signatureRaw);

            using var hmac = new HMACSHA384(keyBytes);
            byte[] hashBytes = hmac.ComputeHash(messageBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }

    /// <summary>
    /// Utilidades para generación de credenciales (antes clases estáticas anidadas en el controlador).
    /// </summary>
    public static class ApiCredencialesGen
    {
        public static string GenApiKey()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(16);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        public static string GenKeySecret(int bytes = 16)
        {
            byte[] buffer = RandomNumberGenerator.GetBytes(bytes);
            return Convert.ToHexString(buffer).ToLowerInvariant();
        }
    }
}
