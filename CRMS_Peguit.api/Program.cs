using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.api;

var builder = WebApplication.CreateBuilder(args);

var masterConnection =
    Environment.GetEnvironmentVariable("CRMS_CONNECTION")
    ?? builder.Configuration.GetConnectionString("MasterCrms");

builder.Services.AddDbContext<MasterCrmsDbContext>(options =>
    options.UseSqlServer(masterConnection));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantResolver, HttpTenantResolver>();

builder.Services.AddScoped<RealEstateDbContext>(serviceProvider =>
{
    var tenantResolver = serviceProvider.GetRequiredService<ITenantResolver>();
    var tenantId = tenantResolver.GetTenantId();

    var options = new DbContextOptionsBuilder<RealEstateDbContext>()
        .UseSqlServer(masterConnection)
        .Options;

    return new RealEstateDbContext(options, tenantId);
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapPost("/companies", async (Company company, MasterCrmsDbContext db) =>
{
    db.Companies.Add(company);
    await db.SaveChangesAsync();

    return Results.Created($"/companies/{company.CompanyId}", company);
});

app.Run();