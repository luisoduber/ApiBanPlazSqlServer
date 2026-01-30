using ApiBanPlaz.models.Entities;
using ApiBanPlaz.models.Responses;
using ApiBanPlaz.models.TokenDl;
using ApiBanPlaz.models.CobroDI;
using ApiBanPlaz.models.ConsultarDl;
using Microsoft.EntityFrameworkCore;
using ApiBanPlaz.models.General;

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
        modelBuilder.Entity<ConsultarDI>().HasNoKey();
        modelBuilder.Entity<TokenDI>().HasNoKey();
        modelBuilder.Entity<CobroDI>().HasNoKey();
    }


    // DbSet vacío solo para ejecutar SP
   // public DbSet<DebinResult> DebinResults { get; set; }

}
