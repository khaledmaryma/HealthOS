using LIS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using LIS.Api.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// Configure EF Core SQL Server - Multiple databases
var lisConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=BOOK-N38E1PL5F3;Database=LIS;Trusted_Connection=True;TrustServerCertificate=True;";
var hospitalDefinitionConnectionString = builder.Configuration.GetConnectionString("HospitalDefinitionConnection")
    ?? "Server=BOOK-N38E1PL5F3;Database=HospitalDefinition;Trusted_Connection=True;TrustServerCertificate=True;";
var billingConnectionString = builder.Configuration.GetConnectionString("BillingConnection")
    ?? "Server=BOOK-N38E1PL5F3;Database=Billing;Trusted_Connection=True;TrustServerCertificate=True;";
var configurationConnectionString = builder.Configuration.GetConnectionString("ConfigurationConnection")
    ?? "Server=BOOK-N38E1PL5F3;Database=Configuration;Trusted_Connection=True;TrustServerCertificate=True;";
var inventoryConnectionString = builder.Configuration.GetConnectionString("InventoryConnection")
    ?? "Server=BOOK-N38E1PL5F3;Database=Inventory;Trusted_Connection=True;TrustServerCertificate=True;";
var hisUsersConnectionString = builder.Configuration.GetConnectionString("HISUsersConnection")
    ?? "Server=BOOK-N38E1PL5F3;Database=HISUsers;Trusted_Connection=True;TrustServerCertificate=True;";

// Register DbContexts for each database
builder.Services.AddDbContext<LISDbContext>(options =>
    options.UseSqlServer(lisConnectionString));

builder.Services.AddDbContext<HospitalDefinitionDbContext>(options =>
    options.UseSqlServer(hospitalDefinitionConnectionString));

builder.Services.AddDbContext<BillingDbContext>(options =>
    options.UseSqlServer(billingConnectionString));

builder.Services.AddDbContext<ConfigurationDbContext>(options =>
    options.UseSqlServer(configurationConnectionString));

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(inventoryConnectionString));

builder.Services.AddDbContext<HISUsersDbContext>(options =>
    options.UseSqlServer(hisUsersConnectionString));

// Use SQL Server repository for LabTests
builder.Services.AddScoped<ILabTestsRepository, SqlLabTestsRepository>();

// CORS for Angular dev server
const string CorsPolicy = "AllowAngular";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins("http://localhost:4200", "http://localhost:4300")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(CorsPolicy);

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Minimal MVC to enable attribute-routed controllers
app.MapControllers();

app.Run();
