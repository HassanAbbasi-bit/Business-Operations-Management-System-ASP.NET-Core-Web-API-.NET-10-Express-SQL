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
var jwtSecret = builder.Configuration["JwtSettings:Secret"]
    ?? "BusinessOperations_Very_Secure_Development_Secret_Key_At_Least_32_Chars!";
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
