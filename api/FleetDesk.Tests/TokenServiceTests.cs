using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FleetDesk.Api.Models;
using FleetDesk.Api.Services;
using Microsoft.Extensions.Options;

namespace FleetDesk.Tests;

public class TokenServiceTests
{
    private static TokenService NewService() => new(Options.Create(new JwtOptions
    {
        Key = "unit-test-key-unit-test-key-0123456789",
        Issuer = "FleetDesk",
        Audience = "FleetDeskClients",
        ExpiryMinutes = 30
    }));

    [Fact]
    public void Create_embeds_email_role_and_expiry()
    {
        var user = new User { Id = 7, Email = "dispatch@example.com", Role = UserRole.Dispatcher };

        var (token, expires) = NewService().Create(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("dispatch@example.com", parsed.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Dispatcher", parsed.Claims.First(c => c.Type == ClaimTypes.Role).Value);
        Assert.Equal("FleetDesk", parsed.Issuer);
        Assert.True(expires > DateTime.UtcNow.AddMinutes(29));
    }
}
