using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.infrastructure.Seeding;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.api;

var builder = WebApplication.CreateBuilder(args);

// ==========================================================
// DATABASE CONNECTION
// ==========================================================

var masterConnection =
    Environment.GetEnvironmentVariable("CRMS_CONNECTION")
    ?? builder.Configuration.GetConnectionString("MasterCrms");

if (string.IsNullOrWhiteSpace(masterConnection))
{
    throw new InvalidOperationException(
        "Database connection string 'MasterCrms' was not found."
    );
}

// ==========================================================
// MASTER DATABASE
// ==========================================================

builder.Services.AddDbContext<MasterCrmsDbContext>(options =>
    options.UseSqlServer(masterConnection)
);

// ==========================================================
// HTTP CONTEXT / TENANT RESOLVER
// ==========================================================

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ITenantResolver, HttpTenantResolver>();

// ==========================================================
// TENANT DATABASE CONTEXT
// ==========================================================

builder.Services.AddScoped<RealEstateDbContext>(serviceProvider =>
{
    var tenantResolver =
        serviceProvider.GetRequiredService<ITenantResolver>();

    var tenantId =
        tenantResolver.GetTenantId();

    var options =
        new DbContextOptionsBuilder<RealEstateDbContext>()
            .UseSqlServer(masterConnection)
            .Options;

    return new RealEstateDbContext(
        options,
        tenantId
    );
});

// ==========================================================
// CONTROLLERS / OPENAPI
// ==========================================================

builder.Services.AddControllers();

builder.Services.AddOpenApi();

// ==========================================================
// BUILD APPLICATION
// ==========================================================

var app = builder.Build();

// ==========================================================
// DATABASE SEEDING
// ==========================================================
//
// There is no HTTP request during application startup,
// therefore HttpTenantResolver cannot determine TenantId.
//
// We explicitly seed TenantId = 1 here.
//

using (var scope = app.Services.CreateScope())
{
    var options =
        new DbContextOptionsBuilder<RealEstateDbContext>()
            .UseSqlServer(masterConnection)
            .Options;

    await using var db =
        new RealEstateDbContext(
            options,
            tenantId: 1
        );

    await DbSeeder.SeedTestUsersAsync(
        db,
        tenantId: 1
    );
}

// ==========================================================
// DEVELOPMENT OPENAPI
// ==========================================================

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ==========================================================
// MIDDLEWARE
// ==========================================================

app.UseHttpsRedirection();

app.UseAuthorization();

// ==========================================================
// CONTROLLERS
// ==========================================================

app.MapControllers();

// ==========================================================
// CREATE COMPANY ENDPOINT
// ==========================================================

app.MapPost(
    "/companies",
    async (
        Company company,
        MasterCrmsDbContext db) =>
    {
        db.Companies.Add(company);

        await db.SaveChangesAsync();

        return Results.Created(
            $"/companies/{company.CompanyId}",
            company
        );
    }
);

// ==========================================================
// START API
// ==========================================================

app.Run();