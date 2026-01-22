
using ApiBanPlaz.models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiBanPlaz.Servicios.General
{
    public class NonceService
    {
        private readonly BanPlazDbContext _context;

        public NonceService(BanPlazDbContext context)
        {
            _context = context;
        }
        public Task<string> ObtNonce()
        {
            var nonce = _context.Database
                .SqlQueryRaw<string>("EXEC spGrdContNonce")
                .AsEnumerable()
                .First();

            return Task.FromResult(nonce);
        }
    }
}
