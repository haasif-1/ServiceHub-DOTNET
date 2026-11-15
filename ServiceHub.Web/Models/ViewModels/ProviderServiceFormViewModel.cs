using System.ComponentModel.DataAnnotations;

namespace ServiceHub.Web.Models.ViewModels;

public sealed class ProviderServiceFormViewModel
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    [Display(Name = "Service name")]
    public string ServiceName { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0", "99999999.99")]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [Range(1, 1440)]
    [Display(Name = "Estimated duration (minutes)")]
    public int EstimatedDurationMinutes { get; set; } = 60;

    [Display(Name = "Available for booking")]
    public bool IsActive { get; set; } = true;
}
