using ApiBanPlaz.Servicios.CobroDl;
using ApiBanPlaz.Servicios.General;
using ApiBanPlaz.Servicios.TokenDl;
using ApiBanPlaz.Servicios.ConsultarDl;
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
