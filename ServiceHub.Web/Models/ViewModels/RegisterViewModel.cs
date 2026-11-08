using System.ComponentModel.DataAnnotations;

namespace ServiceHub.Web.Models.ViewModels;

public class RegisterViewModel
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(32)]
    [Display(Name = "Phone number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must be at least {2} characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Customer|ServiceProvider)$", ErrorMessage = "Choose Customer or Service Provider.")]
    [Display(Name = "I want to")]
    public string Role { get; set; } = AppRoles.Customer;
}
