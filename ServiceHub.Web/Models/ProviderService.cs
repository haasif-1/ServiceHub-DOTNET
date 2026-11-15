namespace ServiceHub.Web.Models;

public sealed class ProviderService
{
    public long Id { get; set; }

    public string ProviderUserId { get; set; } = string.Empty;

    public ApplicationUser Provider { get; set; } = null!;

    public string ServiceName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int EstimatedDurationMinutes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
