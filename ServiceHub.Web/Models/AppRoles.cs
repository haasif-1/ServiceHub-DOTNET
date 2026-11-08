namespace ServiceHub.Web.Models;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";
    public const string ServiceProvider = "ServiceProvider";
    public const string LegacyProvider = "Provider";

    public static readonly string[] All = [Admin, Customer, ServiceProvider];

    public static readonly string[] SelfServiceRoles = [Customer, ServiceProvider];
}
