using ApiBanPlaz.Servicios.CobroDl;
using ApiBanPlaz.Servicios.ConsultarDl;
using ApiBanPlaz.Servicios.ConsultaLiq;
using ApiBanPlaz.Servicios.General;
using ApiBanPlaz.Servicios.PagoO;
using ApiBanPlaz.Servicios.PagosP2p;
using ApiBanPlaz.Servicios.TokenDl;
using ApiBanPlaz.Servicios.CompPm;
using ApiBanPlaz.Servicios.Operacion;
using ApiBanPlaz.Servicios.Operaciones;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<BanPlazDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("CadCnSqlServer")
    );
});

builder.Services.AddScoped<CredApiRsService>();
builder.Services.AddScoped<NonceService>();
builder.Services.AddScoped<TokenDIService>();
builder.Services.AddScoped<CobroDIService>();
builder.Services.AddScoped<ConsultarDlService>();
builder.Services.AddScoped<PagosP2pService>();
builder.Services.AddScoped<PagoOService>();
builder.Services.AddScoped<ConsultaLiqService>();
builder.Services.AddScoped<CompPmService>();
builder.Services.AddScoped<OperacionService>();
builder.Services.AddScoped<OperacionesService>();
var app = builder.Build();

if (app.Environment.IsDevelopment()) { app.MapOpenApi(); }
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
