using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ServiceHub.Web.Tests;

public class ServiceHubWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseInMemoryDatabase"] = "true",
                ["InitialAdmin:Email"] = null,
                ["InitialAdmin:Password"] = null,
                ["Jwt:Key"] = "development-jwt-signing-key-32chars!!",
                ["Jwt:Issuer"] = "ServiceHub",
                ["Jwt:Audience"] = "ServiceHub"
            });
        });
    }
}
