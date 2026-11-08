using System.ComponentModel.DataAnnotations;
using ServiceHub.Web.Models.ViewModels;

namespace ServiceHub.Web.Tests;

public class RegisterViewModelTests
{
    [Fact]
    public void Matching_passwords_and_email_are_valid()
    {
        var model = new RegisterViewModel
        {
            FullName = "Ayesha Khan",
            Email = "ayesha@example.com",
            PhoneNumber = "+15555550123",
            Password = "Password1",
            ConfirmPassword = "Password1",
            Role = "Customer"
        };

        var results = Validate(model);

        Assert.Empty(results);
    }

    [Fact]
    public void Mismatched_passwords_fail_validation()
    {
        var model = new RegisterViewModel
        {
            FullName = "Ayesha Khan",
            Email = "ayesha@example.com",
            PhoneNumber = "+15555550123",
            Password = "Password1",
            ConfirmPassword = "Password2",
            Role = "Customer"
        };

        var results = Validate(model);

        Assert.Contains(results, result => result.ErrorMessage?.Contains("do not match") == true);
    }

    [Fact]
    public void Admin_role_is_not_valid_for_public_registration()
    {
        var model = new RegisterViewModel
        {
            FullName = "Ayesha Khan",
            Email = "ayesha@example.com",
            PhoneNumber = "+15555550123",
            Password = "Password1",
            ConfirmPassword = "Password1",
            Role = "Admin"
        };

        var results = Validate(model);

        Assert.Contains(results, result => result.ErrorMessage?.Contains("Choose Customer or Service Provider.") == true);
    }

    private static List<ValidationResult> Validate(object model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }
}
