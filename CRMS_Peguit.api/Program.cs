using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.domain.entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MasterCrmsDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("MasterCrms")));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
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
