using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.Services;
using CRMS_Peguit.api;

var builder = WebApplication.CreateBuilder(args);

// Load optional local overrides (gitignored) for private credentials
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// ==========================================================
// DATABASE CONNECTION
// ==========================================================

var masterConnection =
    Environment.GetEnvironmentVariable("CRMS_CONNECTION")
    ?? builder.Configuration.GetConnectionString("MasterCrms")
    ?? builder.Configuration.GetConnectionString("LocalCrms");

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
    options.UseSqlServer(masterConnection, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null))
);

// ==========================================================
// HTTP CONTEXT / TENANT RESOLVER
// ==========================================================

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ITenantResolver, HttpTenantResolver>();
builder.Services.AddScoped<ITenantDatabaseResolver, TenantDatabaseResolver>();
builder.Services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();

// ==========================================================
// TENANT DATABASE CONTEXT
// ==========================================================

builder.Services.AddScoped<RealEstateDbContext>(serviceProvider =>
{
    var tenantResolver =
        serviceProvider.GetRequiredService<ITenantResolver>();

    var tenantId =
        tenantResolver.GetTenantId();

    string tenantConnection = masterConnection;
    try
    {
        var scsb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(masterConnection);
        // Only rewrite InitialCatalog if localdb AND tenantId is a valid positive ID (> 0).
        // For shared/cloud databases (like db66713) or when tenantId <= 0 (e.g. unauthenticated login),
        // we must NEVER alter InitialCatalog to "CRMS_Tenant_0".
        if (scsb.DataSource.Contains("localdb", StringComparison.OrdinalIgnoreCase) && tenantId > 0)
        {
            scsb.InitialCatalog = $"CRMS_Tenant_{tenantId}";
            tenantConnection = scsb.ConnectionString;
        }
        else
        {
            tenantConnection = masterConnection;
        }
    }
    catch
    {
        tenantConnection = masterConnection;
    }

    var options =
        new DbContextOptionsBuilder<RealEstateDbContext>()
            .UseSqlServer(tenantConnection, sqlOptions =>
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null))
            .Options;

    return new RealEstateDbContext(
        options,
        tenantId
    );
});

// ==========================================================
// AUTHENTICATION & AUTHORIZATION
// ==========================================================

var jwtSecret = builder.Configuration["Jwt:Secret"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CRMS_Peguit";

if (string.IsNullOrWhiteSpace(jwtSecret) ||
    jwtSecret.Length < 32 ||
    jwtSecret.Equals("REPLACE-WITH-A-LONG-RANDOM-STRING-AT-LEAST-32-CHARS", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "Jwt:Secret is missing, shorter than 32 characters, or still configured with the default placeholder. " +
        "Configure a secure secret key via environment variables or appsettings.local.json."
    );
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtIssuer,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ==========================================================
// CONTROLLERS / OPENAPI
// ==========================================================

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddOpenApi();

// ==========================================================
// BUILD APPLICATION
// ==========================================================

var app = builder.Build();

// ==========================================================
// DEVELOPMENT OPENAPI
// ==========================================================

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ==========================================================
// MIDDLEWARE PIPELINE
// ==========================================================

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// ==========================================================
// CONTROLLERS
// ==========================================================

app.MapControllers();

// ==========================================================
// START API
// ==========================================================

app.Run();