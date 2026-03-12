using System.Text;
using FCG.Payments.Application;
using FCG.Payments.Infrastructure;
using FCG.Payments.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var sqlConnection = builder.Configuration.GetValue<string>("SQL_CONNECTION")
    ?? "Server=localhost,1433;Database=PaymentsDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";
var serviceBusConnection = builder.Configuration.GetValue<string>("SERVICEBUS_CONNECTION");
var appInsightsConnection = builder.Configuration.GetValue<string>("APPLICATIONINSIGHTS_CONNECTION_STRING");

// Add layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    sqlConnection,
    serviceBusConnection,
    appInsightsConnection,
    "FCG.Payments.Api");

// Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Authentication (JWT Bearer) - minimal validation for dev
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = false,
            ValidateLifetime = false
        };
    });

var app = builder.Build();

// Ensure database created (for dev only)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    db.Database.EnsureCreated();
}

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health endpoints
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapGet("/ready", () => Results.Ok(new { status = "Ready" }));

app.Run();

public partial class Program { }
