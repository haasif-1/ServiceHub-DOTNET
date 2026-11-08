using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceHub.Web.Data;
using ServiceHub.Web.Models;

namespace ServiceHub.Web.Tests;

public class AccountPageTests : IClassFixture<ServiceHubWebApplicationFactory>
{
    private readonly ServiceHubWebApplicationFactory _factory;

    public AccountPageTests(ServiceHubWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_page_returns_success()
    {
        using var client = CreateClient();
        var response = await client.GetAsync("/Account/Login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Log in", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Startup_seeds_exactly_the_supported_roles()
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var roles = roleManager.Roles
            .Select(role => role.Name ?? string.Empty)
            .ToArray();

        Assert.Equal(
            new[] { AppRoles.Admin, AppRoles.Customer, AppRoles.ServiceProvider }.OrderBy(role => role, StringComparer.Ordinal),
            roles.OrderBy(role => role, StringComparer.Ordinal));
    }

    [Fact]
    public async Task Configured_admin_is_seeded_idempotently_and_can_log_in()
    {
        var adminEmail = $"admin-{Guid.NewGuid():N}@example.com";
        var adminPassword = $"{Guid.NewGuid():N}Aa1!";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InitialAdmin:Email"] = adminEmail,
                ["InitialAdmin:Password"] = adminPassword
            })
            .Build();

        await IdentityDataSeeder.SeedRolesAndAdminAsync(_factory.Services, configuration);
        await IdentityDataSeeder.SeedRolesAndAdminAsync(_factory.Services, configuration);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync(adminEmail);

        Assert.NotNull(admin);
        Assert.True(await userManager.IsInRoleAsync(admin, AppRoles.Admin));
        Assert.True(await userManager.CheckPasswordAsync(admin, adminPassword));
        Assert.Equal(1, userManager.Users.Count(user => user.Email == adminEmail));

        using var client = CreateClient();
        var token = await GetAntiforgeryTokenAsync(client, "/Account/Login");
        using var loginContent = CreateLoginContent(adminEmail, adminPassword, token, "https://example.com");
        var loginResponse = await client.PostAsync("/Account/Login", loginContent);

        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        Assert.Equal("/Admin/Dashboard", loginResponse.Headers.Location?.OriginalString);

        var dashboardResponse = await client.GetAsync("/Admin/Dashboard");
        Assert.Equal(HttpStatusCode.OK, dashboardResponse.StatusCode);
    }

    [Theory]
    [InlineData("/Dashboard")]
    [InlineData("/Admin/Dashboard")]
    [InlineData("/Customer/Dashboard")]
    [InlineData("/ServiceProvider/Dashboard")]
    public async Task Dashboard_redirects_anonymous_users_to_login(string path)
    {
        using var client = CreateClient();
        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Customer", "/Customer/Dashboard", "/Admin/Dashboard", "Customer")]
    [InlineData("ServiceProvider", "/ServiceProvider/Dashboard", "/Customer/Dashboard", "ServiceProvider")]
    public async Task Signup_assigns_requested_role_and_protects_other_dashboards(
        string role,
        string expectedDashboard,
        string forbiddenDashboard,
        string expectedRole)
    {
        using var client = CreateClient();
        var token = await GetAntiforgeryTokenAsync(client, "/Account/Register");
        var email = $"user-{Guid.NewGuid():N}@example.com";
        using var content = new FormUrlEncodedContent(
        [
            new("FullName", "Taylor Service"),
            new("Email", email),
            new("PhoneNumber", "+15555550123"),
            new("Password", "Password1"),
            new("ConfirmPassword", "Password1"),
            new("Role", role),
            new("__RequestVerificationToken", token)
        ]);

        var signupResponse = await client.PostAsync("/Account/Register", content);
        Assert.Equal(HttpStatusCode.Redirect, signupResponse.StatusCode);
        Assert.Equal(expectedDashboard, signupResponse.Headers.Location?.OriginalString);

        var dashboardResponse = await client.GetAsync(expectedDashboard);
        Assert.Equal(HttpStatusCode.OK, dashboardResponse.StatusCode);
        var dashboardHtml = await dashboardResponse.Content.ReadAsStringAsync();
        Assert.Contains("Taylor Service", dashboardHtml);
        Assert.Contains(expectedRole, dashboardHtml);

        var authenticatedLoginResponse = await client.GetAsync("/Account/Login");
        Assert.Equal(HttpStatusCode.Redirect, authenticatedLoginResponse.StatusCode);
        Assert.Equal(expectedDashboard, authenticatedLoginResponse.Headers.Location?.OriginalString);

        var initialLogoutToken = await GetAntiforgeryTokenAsync(client, expectedDashboard);
        using var initialLogoutContent = new FormUrlEncodedContent(
        [
            new("__RequestVerificationToken", initialLogoutToken)
        ]);
        var initialLogoutResponse = await client.PostAsync("/Account/Logout", initialLogoutContent);
        Assert.Equal(HttpStatusCode.Redirect, initialLogoutResponse.StatusCode);

        var loginToken = await GetAntiforgeryTokenAsync(client, "/Account/Login");
        using var invalidLoginContent = CreateLoginContent(email, "WrongPassword1", loginToken, "https://example.com");
        var invalidLoginResponse = await client.PostAsync("/Account/Login", invalidLoginContent);
        Assert.Equal(HttpStatusCode.OK, invalidLoginResponse.StatusCode);
        var invalidLoginHtml = await invalidLoginResponse.Content.ReadAsStringAsync();
        Assert.Contains("Invalid email or password.", invalidLoginHtml);

        var validLoginToken = await GetAntiforgeryTokenAsync(client, "/Account/Login");
        using var validLoginContent = CreateLoginContent(email, "Password1", validLoginToken, "https://example.com");
        var loginResponse = await client.PostAsync("/Account/Login", validLoginContent);
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        Assert.Equal(expectedDashboard, loginResponse.Headers.Location?.OriginalString);

        var forbiddenResponse = await client.GetAsync(forbiddenDashboard);
        Assert.Equal(HttpStatusCode.Redirect, forbiddenResponse.StatusCode);
        Assert.Equal("/Account/AccessDenied", forbiddenResponse.Headers.Location?.AbsolutePath);

        var logoutToken = await GetAntiforgeryTokenAsync(client, expectedDashboard);
        using var logoutContent = new FormUrlEncodedContent(
        [
            new("__RequestVerificationToken", logoutToken)
        ]);
        var logoutResponse = await client.PostAsync("/Account/Logout", logoutContent);
        Assert.Equal(HttpStatusCode.Redirect, logoutResponse.StatusCode);

        var anonymousResponse = await client.GetAsync(expectedDashboard);
        Assert.Equal(HttpStatusCode.Redirect, anonymousResponse.StatusCode);
        Assert.Contains("/Account/Login", anonymousResponse.Headers.Location?.OriginalString ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Public_signup_rejects_admin_role()
    {
        using var client = CreateClient();
        var token = await GetAntiforgeryTokenAsync(client, "/Account/Register");
        using var content = new FormUrlEncodedContent(
        [
            new("FullName", "Unauthorized Admin"),
            new("Email", $"user-{Guid.NewGuid():N}@example.com"),
            new("PhoneNumber", "+15555550123"),
            new("Password", "Password1"),
            new("ConfirmPassword", "Password1"),
            new("Role", "Admin"),
            new("__RequestVerificationToken", token)
        ]);

        var response = await client.PostAsync("/Account/Register", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Choose Customer or Service Provider.", html);
    }

    [Fact]
    public async Task Jwt_token_endpoint_rejects_unknown_users()
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/token", new
        {
            email = "missing@example.com",
            password = "Password1"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.CultureInvariant);

        Assert.True(match.Success, "The form did not contain an antiforgery token.");
        return match.Groups[1].Value;
    }

    private static FormUrlEncodedContent CreateLoginContent(
        string email,
        string password,
        string antiforgeryToken,
        string returnUrl)
    {
        return new FormUrlEncodedContent(
        [
            new("Email", email),
            new("Password", password),
            new("RememberMe", "false"),
            new("ReturnUrl", returnUrl),
            new("__RequestVerificationToken", antiforgeryToken)
        ]);
    }
}
