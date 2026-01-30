using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ApiBanPlaz.models.General
{
    [Keyless]
    public class CredApiRs
    {
        public string ApiKey { get; set; }
        public string apiKeySecret { get; set; }

    }
}
