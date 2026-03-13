using ApiBanPlaz.models.CobroDI;
using ApiBanPlaz.models.ConsultarDl;
using ApiBanPlaz.models.Entities;
using ApiBanPlaz.models.General;
using ApiBanPlaz.models.PagosP2p;
using ApiBanPlaz.models.PagoO;
using ApiBanPlaz.models.ConsultaLiq;
using ApiBanPlaz.models.TokenDl;
using ApiBanPlaz.models.CompPm;
using ApiBanPlaz.models.Operacion;
using ApiBanPlaz.models.Operaciones;
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
        modelBuilder.Entity<ConsultarDI>().HasNoKey();
        modelBuilder.Entity<TokenDI>().HasNoKey();
        modelBuilder.Entity<CobroDI>().HasNoKey();
        modelBuilder.Entity<PagosP2p>().HasNoKey();
        modelBuilder.Entity<PagoO>().HasNoKey();
        modelBuilder.Entity<ConsultaLiq>().HasNoKey();
        modelBuilder.Entity<CompPm>().HasNoKey();
        modelBuilder.Entity<Operacion>().HasNoKey();
        modelBuilder.Entity<Operaciones>().HasNoKey();
    }

    // DbSet vacío solo para ejecutar SP
   // public DbSet<DebinResult> DebinResults { get; set; }

}
