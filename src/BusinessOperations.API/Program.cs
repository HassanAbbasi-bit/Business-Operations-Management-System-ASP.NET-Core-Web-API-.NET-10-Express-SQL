using System.Text;
using BusinessOperations.API.Middleware;
using BusinessOperations.Application;
using BusinessOperations.Domain.Enums;
using BusinessOperations.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Configure JWT Authentication
const string DevelopmentFallbackSecret = "BusinessOperations_Very_Secure_Development_Secret_Key_At_Least_32_Chars!";
var configuredSecret = builder.Configuration["JwtSettings:Secret"];

if (builder.Environment.IsProduction())
{
    if (string.IsNullOrWhiteSpace(configuredSecret) ||
        configuredSecret.Length < 32 ||
        configuredSecret.Contains("Development_Secret_Key", StringComparison.OrdinalIgnoreCase) ||
        configuredSecret.Contains("placeholder", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(configuredSecret, DevelopmentFallbackSecret, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            "CRITICAL SECURITY CONFIGURATION ERROR: A valid production JWT signing key (at least 32 characters) must be configured in Production. " +
            "Development placeholder keys are strictly rejected in Production. Configure 'JwtSettings:Secret' via environment variables or a secret vault.");
    }
}

var jwtSecret = configuredSecret;
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "CRITICAL SECURITY CONFIGURATION ERROR: JWT signing key is not configured. Fallback development secret is only permitted in Development environment.");
    }
    jwtSecret = DevelopmentFallbackSecret;
}

var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "BusinessOperationsApi";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "BusinessOperationsClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole(UserRole.Admin.ToString()));
    options.AddPolicy("RequireManager", policy => policy.RequireRole(UserRole.Admin.ToString(), UserRole.Manager.ToString()));
    options.AddPolicy("RequireEmployee", policy => policy.RequireRole(UserRole.Admin.ToString(), UserRole.Manager.ToString(), UserRole.Employee.ToString()));
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Global Exception Handling Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed demonstration data in Development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<BusinessOperations.Infrastructure.Data.ApplicationDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<BusinessOperations.Application.Interfaces.IPasswordHasher>();
    await BusinessOperations.Infrastructure.Data.DatabaseSeeder.SeedAsync(dbContext, passwordHasher);
}

app.Run();

public partial class Program { }
