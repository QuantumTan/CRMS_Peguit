using Microsoft.EntityFrameworkCore;
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
        var scsb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(masterConnection)
        {
            InitialCatalog = $"CRMS_Tenant_{tenantId}"
        };
        tenantConnection = scsb.ConnectionString;
    }
    catch
    {
        // fallback
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
// CONTROLLERS / OPENAPI
// ==========================================================

builder.Services.AddControllers();

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
// MASTER DATABASE ENDPOINTS (LAB 4)
// ==========================================================

app.MapPost(
    "/devices",
    async (
        Device device,
        MasterCrmsDbContext db) =>
    {
        db.Devices.Add(device);
        await db.SaveChangesAsync();
        return Results.Created($"/devices/{device.DeviceId}", device);
    }
);

app.MapPost(
    "/company-databases",
    async (
        CompanyDatabase companyDatabase,
        MasterCrmsDbContext db) =>
    {
        db.CompanyDatabases.Add(companyDatabase);
        await db.SaveChangesAsync();
        return Results.Created(
            $"/company-databases/{companyDatabase.CompanyDatabaseId}",
            companyDatabase
        );
    }
);

// ==========================================================
// DYNAMIC TENANT DATABASE RESOLUTION ENDPOINTS (LAB 4)
// ==========================================================

app.MapGet(
    "/test-tenant/{companyId:int}",
    async (
        int companyId,
        ITenantDbContextFactory tenantFactory) =>
    {
        await using var tenantDb = await tenantFactory.CreateAsync(companyId);
        var customerCount = await tenantDb.Customers.CountAsync();
        var propertyCount = await tenantDb.Properties.CountAsync();
        var leadCount = await tenantDb.Leads.CountAsync();
        var dealCount = await tenantDb.Deals.CountAsync();
        return Results.Ok(new
        {
            companyId,
            customerCount,
            propertyCount,
            leadCount,
            dealCount
        });
    }
);

app.MapPost(
    "/tenant/{companyId:int}/customers",
    async (
        int companyId,
        Customer customer,
        ITenantDbContextFactory tenantFactory) =>
    {
        await using var tenantDb = await tenantFactory.CreateAsync(companyId);
        if (customer.PersonId <= 0 && customer.Person == null)
        {
            customer.Person = new CRMS_Peguit.domain.entities.Person
            {
                FirstName = customer.FirstName,
                MiddleName = customer.MiddleName,
                LastName = customer.LastName,
                Suffix = customer.Suffix,
                Email = customer.Email,
                Phone = customer.Phone
            };
        }
        if (customer.CreatedByUserId <= 0) customer.CreatedByUserId = 1;
        customer.CreatedAt = DateTime.UtcNow;
        tenantDb.Customers.Add(customer);
        await tenantDb.SaveChangesAsync();
        return Results.Created(
            $"/tenant/{companyId}/customers/{customer.CustomerId}",
            customer
        );
    }
);

app.MapGet(
    "/tenant/{companyId:int}/customers",
    async (
        int companyId,
        ITenantDbContextFactory tenantFactory) =>
    {
        await using var tenantDb = await tenantFactory.CreateAsync(companyId);
        var customers = await tenantDb.Customers
            .AsNoTracking()
            .OrderBy(x => x.CustomerId)
            .ToListAsync();
        return Results.Ok(customers);
    }
);

app.MapPost(
    "/tenant/{companyId:int}/properties",
    async (
        int companyId,
        Property property,
        ITenantDbContextFactory tenantFactory) =>
    {
        await using var tenantDb = await tenantFactory.CreateAsync(companyId);
        if (property.CreatedByUserId <= 0) property.CreatedByUserId = 1;
        property.CreatedAt = DateTime.UtcNow;
        tenantDb.Properties.Add(property);
        await tenantDb.SaveChangesAsync();
        return Results.Created(
            $"/tenant/{companyId}/properties/{property.PropertyId}",
            property
        );
    }
);

app.MapGet(
    "/tenant/{companyId:int}/properties",
    async (
        int companyId,
        ITenantDbContextFactory tenantFactory) =>
    {
        await using var tenantDb = await tenantFactory.CreateAsync(companyId);
        var properties = await tenantDb.Properties
            .AsNoTracking()
            .OrderBy(x => x.PropertyId)
            .ToListAsync();
        return Results.Ok(properties);
    }
);

app.MapPost(
    "/tenant/{companyId:int}/leads",
    async (
        int companyId,
        Lead lead,
        ITenantDbContextFactory tenantFactory) =>
    {
        await using var tenantDb = await tenantFactory.CreateAsync(companyId);
        if (lead.PersonId <= 0 && lead.Person == null)
        {
            lead.Person = new CRMS_Peguit.domain.entities.Person
            {
                FirstName = lead.FirstName,
                MiddleName = lead.MiddleName,
                LastName = lead.LastName,
                Suffix = lead.Suffix,
                Email = lead.Email,
                Phone = lead.Phone
            };
        }
        if (lead.CreatedByUserId <= 0) lead.CreatedByUserId = 1;
        lead.CreatedAt = DateTime.UtcNow;
        tenantDb.Leads.Add(lead);
        await tenantDb.SaveChangesAsync();
        return Results.Created(
            $"/tenant/{companyId}/leads/{lead.LeadId}",
            lead
        );
    }
);

app.MapGet(
    "/tenant/{companyId:int}/leads",
    async (
        int companyId,
        ITenantDbContextFactory tenantFactory) =>
    {
        await using var tenantDb = await tenantFactory.CreateAsync(companyId);
        var leads = await tenantDb.Leads
            .AsNoTracking()
            .OrderBy(x => x.LeadId)
            .ToListAsync();
        return Results.Ok(leads);
    }
);

app.MapPost(
    "/tenant/{companyId:int}/deals",
    async (
        int companyId,
        Deal deal,
        ITenantDbContextFactory tenantFactory) =>
    {
        await using var tenantDb = await tenantFactory.CreateAsync(companyId);
        if (deal.CreatedByUserId <= 0) deal.CreatedByUserId = 1;
        deal.CreatedAt = DateTime.UtcNow;
        tenantDb.Deals.Add(deal);
        await tenantDb.SaveChangesAsync();
        return Results.Created(
            $"/tenant/{companyId}/deals/{deal.DealId}",
            deal
        );
    }
);

app.MapGet(
    "/tenant/{companyId:int}/deals",
    async (
        int companyId,
        ITenantDbContextFactory tenantFactory) =>
    {
        await using var tenantDb = await tenantFactory.CreateAsync(companyId);
        var deals = await tenantDb.Deals
            .AsNoTracking()
            .OrderBy(x => x.DealId)
            .ToListAsync();
        return Results.Ok(deals);
    }
);

app.MapPost(
    "/tenant/{companyId:int}/activities",
    async (
        int companyId,
        Activity activity,
        ITenantDbContextFactory tenantFactory) =>
    {
        await using var tenantDb = await tenantFactory.CreateAsync(companyId);
        if (activity.LoggedByAgentId <= 0) activity.LoggedByAgentId = 1;
        if (activity.ActivityDate == default)
        {
            activity.ActivityDate = DateTime.UtcNow;
        }
        tenantDb.Activities.Add(activity);
        await tenantDb.SaveChangesAsync();
        return Results.Created(
            $"/tenant/{companyId}/activities/{activity.ActivityId}",
            activity
        );
    }
);

app.MapGet(
    "/tenant/{companyId:int}/activities",
    async (
        int companyId,
        ITenantDbContextFactory tenantFactory) =>
    {
        await using var tenantDb = await tenantFactory.CreateAsync(companyId);
        var activities = await tenantDb.Activities
            .AsNoTracking()
            .OrderBy(x => x.ActivityId)
            .ToListAsync();
        return Results.Ok(activities);
    }
);

// ==========================================================
// START API
// ==========================================================

app.Run();