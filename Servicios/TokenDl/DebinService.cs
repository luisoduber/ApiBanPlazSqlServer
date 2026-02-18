
using Microsoft.EntityFrameworkCore;

using ApiBanPlaz.models.TokenDl;

namespace ApiBanPlaz.Servicios.TokenDl
{
    public class DebinService
    {
        private readonly BanPlazDbContext _context;

        // 🔹 El DbContext se INYECTA, no se crea con new
        public DebinService(BanPlazDbContext context)
        {
            _context = context;
        }

      
    }
}

