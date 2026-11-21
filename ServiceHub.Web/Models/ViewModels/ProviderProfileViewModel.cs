using System.ComponentModel.DataAnnotations;

namespace ServiceHub.Web.Models.ViewModels;

public sealed class ProviderProfileViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(32)]
    [Display(Name = "Phone number")]
    public string PhoneNumber { get; set; } = string.Empty;

    public string Role { get; set; } = "ServiceProvider";

    public DateTime CreatedAtUtc { get; set; }

    public int TotalServicesOffered { get; set; }

    public int ActiveServicesCount { get; set; }
}
