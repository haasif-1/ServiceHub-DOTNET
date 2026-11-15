using System.ComponentModel.DataAnnotations;
using ServiceHub.Web.Models.ViewModels;

namespace ServiceHub.Web.Tests;

public class ProviderServiceFormViewModelTests
{
    [Fact]
    public void Valid_service_details_pass_server_validation()
    {
        var model = new ProviderServiceFormViewModel
        {
            ServiceName = "Home cleaning",
            Description = "A standard home cleaning service.",
            Price = 49.95m,
            EstimatedDurationMinutes = 90,
            IsActive = true
        };

        var results = Validate(model);

        Assert.Empty(results);
    }

    [Fact]
    public void Invalid_service_details_fail_server_validation()
    {
        var model = new ProviderServiceFormViewModel
        {
            ServiceName = " ",
            Price = -0.01m,
            EstimatedDurationMinutes = 0,
            Description = new string('x', 2001)
        };

        var results = Validate(model);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.ServiceName)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.Price)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.EstimatedDurationMinutes)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.Description)));
    }

    private static List<ValidationResult> Validate(ProviderServiceFormViewModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
