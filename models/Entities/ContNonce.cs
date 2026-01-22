using Microsoft.EntityFrameworkCore;

namespace ApiBanPlaz.models.Entities
{
    [Keyless]
    public class ContNonce
    {
        public string UltNonce { get; set; }
    }
}
