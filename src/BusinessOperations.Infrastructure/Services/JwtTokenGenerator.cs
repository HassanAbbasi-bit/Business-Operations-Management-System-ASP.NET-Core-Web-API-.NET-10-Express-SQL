using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BusinessOperations.Application.Interfaces;
using BusinessOperations.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BusinessOperations.Infrastructure.Services;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAtUtc) GenerateToken(User user, Employee? employee = null)
    {
        string secretKey = _configuration["JwtSettings:Secret"]
            ?? "BusinessOperationsDefaultDevelopmentSecretKey_AtLeast32CharsLong!";
        string issuer = _configuration["JwtSettings:Issuer"] ?? "BusinessOperationsApi";
        string audience = _configuration["JwtSettings:Audience"] ?? "BusinessOperationsClient";
        int expirationMinutes = int.TryParse(_configuration["JwtSettings:ExpirationMinutes"], out int exp) ? exp : 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (employee != null)
        {
            claims.Add(new Claim("employeeId", employee.Id.ToString()));
            claims.Add(new Claim("fullName", employee.FullName));
            claims.Add(new Claim("departmentId", employee.DepartmentId.ToString()));
        }

        DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(expirationMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAtUtc,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        string tokenString = tokenHandler.WriteToken(securityToken);

        return (tokenString, expiresAtUtc);
    }
}
