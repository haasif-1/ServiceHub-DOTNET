using System.ComponentModel.DataAnnotations;
using ServiceHub.Web.Models.ViewModels;

namespace ServiceHub.Web.Tests;

public class ProviderProfileViewModelTests
{
    [Fact]
    public void Valid_profile_passes_validation()
    {
        var model = new ProviderProfileViewModel
        {
            FullName = "Jane Specialist",
            PhoneNumber = "+15555550199",
            Email = "provider@example.com"
        };

        var results = Validate(model);

        Assert.Empty(results);
    }

    [Fact]
    public void Missing_or_short_name_fails_validation()
    {
        var model = new ProviderProfileViewModel
        {
            FullName = "A",
            PhoneNumber = "+15555550199"
        };

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(model.FullName)));
    }

    [Fact]
    public void Missing_phone_number_fails_validation()
    {
        var model = new ProviderProfileViewModel
        {
            FullName = "Valid Provider",
            PhoneNumber = ""
        };

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(model.PhoneNumber)));
    }

    private static List<ValidationResult> Validate(ProviderProfileViewModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
