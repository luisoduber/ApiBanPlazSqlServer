using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.TokenDl
{
    [Keyless]
    public class CredApiRs
    {
        public string ApiKey { get; set; }
        public string apiKeySecret { get; set; }

    }
}
