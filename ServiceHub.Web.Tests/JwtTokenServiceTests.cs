using Microsoft.Extensions.Options;
using ServiceHub.Web.Models;
using ServiceHub.Web.Options;
using ServiceHub.Web.Services;

namespace ServiceHub.Web.Tests;

public class JwtTokenServiceTests
{
    [Fact]
    public void IsConfigured_is_false_when_key_is_short()
    {
        var service = CreateService("short-key");

        Assert.False(service.IsConfigured);
        Assert.Throws<InvalidOperationException>(() =>
            service.CreateToken(CreateUser(), ["Customer"]));
    }

    [Fact]
    public void CreateToken_includes_email_and_role_claims()
    {
        var service = CreateService("development-jwt-signing-key-32chars!!");
        var token = service.CreateToken(CreateUser(), ["Customer"]);

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(3, token.Split('.').Length);
    }

    private static JwtTokenService CreateService(string key)
    {
        return new JwtTokenService(Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "ServiceHub",
            Audience = "ServiceHub",
            ExpiryMinutes = 60,
            Key = key
        }));
    }

    private static ApplicationUser CreateUser()
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "student@example.com",
            UserName = "student@example.com",
            FullName = "Test Student"
        };
    }
}
