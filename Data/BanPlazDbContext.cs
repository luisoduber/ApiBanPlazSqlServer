using ApiBanPlaz.models.Entities;
using ApiBanPlaz.models.Responses;
using ApiBanPlaz.models.TokenDl;
using Microsoft.EntityFrameworkCore;

public class BanPlazDbContext : DbContext
{
    public BanPlazDbContext(DbContextOptions<BanPlazDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ContNonce>().HasNoKey();
        modelBuilder.Entity<CredApiRs>().HasNoKey();
        modelBuilder.Entity<TokenDI>().HasNoKey();
    }


    // DbSet vacío solo para ejecutar SP
   // public DbSet<DebinResult> DebinResults { get; set; }

}
