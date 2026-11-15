namespace ServiceHub.Web.Models.ViewModels;

public sealed class ProviderServiceViewModel
{
    public long Id { get; set; }

    public string ServiceName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int EstimatedDurationMinutes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
