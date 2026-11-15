namespace ServiceHub.Web.Models.ViewModels;

public sealed class ServiceProviderDashboardViewModel
{
    public string FullName { get; set; } = string.Empty;

    public IReadOnlyList<ProviderServiceViewModel> Services { get; set; } = [];
}
