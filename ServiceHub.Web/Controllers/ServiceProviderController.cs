using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using ServiceHub.Web.Models;
using ServiceHub.Web.Models.ViewModels;
using ServiceHub.Web.Services;

namespace ServiceHub.Web.Controllers;

[Authorize(Roles = AppRoles.ServiceProvider)]
[Route("ServiceProvider")]
public sealed class ServiceProviderController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ProviderServicesService _providerServices;
    private readonly ILogger<ServiceProviderController> _logger;

    public ServiceProviderController(
        UserManager<ApplicationUser> userManager,
        ProviderServicesService providerServices,
        ILogger<ServiceProviderController> logger)
    {
        _userManager = userManager;
        _providerServices = providerServices;
        _logger = logger;
    }

    [HttpGet("Dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        var providerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (user is null || string.IsNullOrWhiteSpace(providerUserId))
        {
            return Challenge();
        }

        IReadOnlyList<ProviderServiceViewModel> services = [];
        try
        {
            services = await _providerServices.GetForProviderAsync(providerUserId, cancellationToken);
        }
        catch (MySqlException exception)
        {
            _logger.LogError(exception, "Could not load services for the authenticated provider.");
            TempData["ErrorMessage"] = "Your services could not be loaded. Please try again.";
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError(exception, "Provider service database configuration is unavailable.");
            TempData["ErrorMessage"] = "Service storage is not configured. Contact the administrator.";
        }

        return View("Dashboard", new ServiceProviderDashboardViewModel
        {
            FullName = user.FullName ?? user.Email ?? "there",
            Services = services
        });
    }

    [HttpGet("Services")]
    public Task<IActionResult> Services(CancellationToken cancellationToken)
    {
        return ExecuteDatabaseActionAsync(async () =>
        {
            var services = await _providerServices.GetForProviderAsync(
                RequireProviderUserId(),
                cancellationToken);
            return View("Services", services);
        }, "list services");
    }

    [HttpGet("Services/Create")]
    public IActionResult CreateService()
    {
        return View(new ProviderServiceFormViewModel());
    }

    [HttpPost("Services/Create")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateService(
        ProviderServiceFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Task.FromResult<IActionResult>(View(model));
        }

        return ExecuteDatabaseActionAsync(async () =>
        {
            await _providerServices.CreateAsync(
                RequireProviderUserId(),
                model,
                cancellationToken);
            TempData["SuccessMessage"] = "Your service was added.";
            return RedirectToAction(nameof(Services));
        }, "create service");
    }

    [HttpGet("Services/{id:long}")]
    public Task<IActionResult> ServiceDetails(long id, CancellationToken cancellationToken)
    {
        return ExecuteDatabaseActionAsync(async () =>
        {
            var service = await _providerServices.GetByIdForProviderAsync(
                id,
                RequireProviderUserId(),
                cancellationToken);
            return service is null ? NotFound() : View("Details", service);
        }, "view service details");
    }

    [HttpGet("Services/{id:long}/Edit")]
    public Task<IActionResult> EditService(long id, CancellationToken cancellationToken)
    {
        return ExecuteDatabaseActionAsync(async () =>
        {
            var service = await _providerServices.GetByIdForProviderAsync(
                id,
                RequireProviderUserId(),
                cancellationToken);
            if (service is null)
            {
                return NotFound();
            }

            return View("EditService", new ProviderServiceFormViewModel
            {
                ServiceName = service.ServiceName,
                Description = service.Description,
                Price = service.Price,
                EstimatedDurationMinutes = service.EstimatedDurationMinutes,
                IsActive = service.IsActive
            });
        }, "load service for editing");
    }

    [HttpPost("Services/{id:long}/Edit")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> EditService(
        long id,
        ProviderServiceFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Task.FromResult<IActionResult>(View("EditService", model));
        }

        return ExecuteDatabaseActionAsync(async () =>
        {
            var updated = await _providerServices.UpdateForProviderAsync(
                id,
                RequireProviderUserId(),
                model,
                cancellationToken);
            if (!updated)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = "Your service was updated.";
            return RedirectToAction(nameof(Services));
        }, "update service");
    }

    [HttpGet("Services/{id:long}/Delete")]
    public Task<IActionResult> DeleteService(long id, CancellationToken cancellationToken)
    {
        return ExecuteDatabaseActionAsync(async () =>
        {
            var service = await _providerServices.GetByIdForProviderAsync(
                id,
                RequireProviderUserId(),
                cancellationToken);
            return service is null ? NotFound() : View("DeleteService", service);
        }, "load service for deletion");
    }

    [HttpPost("Services/{id:long}/Delete")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ConfirmDeleteService(long id, CancellationToken cancellationToken)
    {
        return ExecuteDatabaseActionAsync(async () =>
        {
            var deleted = await _providerServices.DeleteForProviderAsync(
                id,
                RequireProviderUserId(),
                cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = "Your service was deleted.";
            return RedirectToAction(nameof(Services));
        }, "delete service");
    }

    private string RequireProviderUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("The authenticated provider has no Identity user ID.");
    }

    private async Task<IActionResult> ExecuteDatabaseActionAsync(
        Func<Task<IActionResult>> action,
        string operation)
    {
        try
        {
            return await action();
        }
        catch (MySqlException exception)
        {
            _logger.LogError(exception, "Provider service database operation failed: {Operation}.", operation);
            TempData["ErrorMessage"] = "The service could not be processed. Please try again.";
            return RedirectToAction(nameof(Dashboard));
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError(exception, "Provider service database configuration is unavailable during {Operation}.", operation);
            TempData["ErrorMessage"] = "Service storage is not configured. Contact the administrator.";
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
