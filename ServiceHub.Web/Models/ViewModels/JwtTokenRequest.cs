using System.ComponentModel.DataAnnotations;

namespace ServiceHub.Web.Models.ViewModels;

public class JwtTokenRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
