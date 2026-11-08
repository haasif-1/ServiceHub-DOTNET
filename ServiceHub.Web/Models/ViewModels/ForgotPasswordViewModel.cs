using System.ComponentModel.DataAnnotations;

namespace ServiceHub.Web.Models.ViewModels;

public class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
