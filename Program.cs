using ApiBanPlaz.Servicios.CobroDl;
using ApiBanPlaz.Servicios.ConsultarDl;
using ApiBanPlaz.Servicios.General;
using ApiBanPlaz.Servicios.PagoO;
using ApiBanPlaz.Servicios.PagosP2p;
using ApiBanPlaz.Servicios.TokenDl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
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
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
