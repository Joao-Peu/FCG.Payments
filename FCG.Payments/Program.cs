using System.Text;
using FCG.Payments.Shared;
using FCG.Payments.Shared.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var connectionString = builder.Configuration.GetValue<string>("SQL_CONNECTION") ?? "Server=localhost,1433;Database=PaymentsDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";
var useInMemoryBus = builder.Configuration.GetValue<bool?>("USE_INMEMORY_BUS") ?? true;

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext
builder.Services.AddDbContext<PaymentsDbContext>(opts =>
    opts.UseSqlServer(connectionString));

// Messaging
if (useInMemoryBus)
{
    builder.Services.AddSingleton<IMessagePublisher, InMemoryMessagePublisher>();
    builder.Services.AddSingleton<IMessageSubscriber, InMemoryMessagePublisher>();
}
else
{
    // Real Service Bus implementation placeholder
    builder.Services.AddSingleton<IMessagePublisher, ServiceBusMessagePublisher>();
    builder.Services.AddSingleton<IMessageSubscriber, ServiceBusMessagePublisher>();
}

// Payment service
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Authentication (JWT Bearer) - minimal validation
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
