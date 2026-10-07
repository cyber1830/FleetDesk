using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FleetDesk.Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FleetDesk.Api.Services;

public class JwtOptions
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "FleetDesk";
    public string Audience { get; set; } = "FleetDeskClients";
    public int ExpiryMinutes { get; set; } = 60;
}

public class TokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _opts = options.Value;

    public (string Token, DateTime ExpiresAtUtc) Create(User user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_opts.ExpiryMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.Key));
        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
