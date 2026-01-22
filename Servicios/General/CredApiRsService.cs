using ApiBanPlaz.models.TokenDl;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using static DebinController;

namespace ApiBanPlaz.Servicios.General
{


    public class CredApiRsService
    {
        private readonly BanPlazDbContext _context;

        public CredApiRsService(BanPlazDbContext context)
        {
            _context = context;
        }

        public async Task<CredApiRs?> ObtCredApi()
        {
            var rsList = await _context
                .Set<CredApiRs>()
                .FromSqlRaw("EXEC spInfCredApi") 
                .AsNoTracking()
                .ToListAsync();

            return rsList.FirstOrDefault();
        }
    }

}
