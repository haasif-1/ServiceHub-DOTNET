using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microting.EntityFrameworkCore.MySql;
using ServiceHub.Web.Data;
using ServiceHub.Web.Models;
using ServiceHub.Web.Options;
using ServiceHub.Web.Services;

namespace ServiceHub.Web.Extensions;

public static class IdentityServiceExtensions
{
    public static IServiceCollection AddServiceHubIdentity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<JwtTokenService>();
        services.AddSingleton<IAppEmailSender, LoggingEmailSender>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var useInMemory = configuration.GetValue("UseInMemoryDatabase", false);

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (useInMemory)
            {
                options.UseInMemoryDatabase("ServiceHub");
                return;
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Set ConnectionStrings:DefaultConnection to a MySQL connection string, or set UseInMemoryDatabase=true for local tests.");
            }

            var serverVersion = configuration["MySql:ServerVersion"];
            options.UseMySql(
                connectionString,
                string.IsNullOrWhiteSpace(serverVersion)
                    ? new MySqlServerVersion(new Version(8, 0, 30))
                    : ServerVersion.Parse(serverVersion));
        });

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
        });

        var authentication = services.AddAuthentication();
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        if (!string.IsNullOrWhiteSpace(jwtOptions.Key) && jwtOptions.Key.Length >= 32)
        {
            authentication.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });
        }

        var googleClientId = configuration["Authentication:Google:ClientId"];
        var googleClientSecret = configuration["Authentication:Google:ClientSecret"];
        if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
        {
            authentication.AddGoogle(options =>
            {
                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;
                options.SignInScheme = IdentityConstants.ExternalScheme;
            });
        }

        return services;
    }
}
