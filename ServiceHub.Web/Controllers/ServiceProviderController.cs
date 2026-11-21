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

    [HttpGet("Services/Data")]
    public async Task<IActionResult> GetServicesData(CancellationToken cancellationToken)
    {
        try
        {
            var services = await _providerServices.GetForProviderAsync(
                RequireProviderUserId(),
                cancellationToken);

            return Ok(new
            {
                success = true,
                services = services.Select(s => new
                {
                    s.Id,
                    s.ServiceName,
                    s.Description,
                    s.Price,
                    PriceFormatted = s.Price.ToString("F2"),
                    s.EstimatedDurationMinutes,
                    s.IsActive,
                    s.CreatedAtUtc,
                    s.UpdatedAtUtc
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching services data via AJAX.");
            return StatusCode(500, new { success = false, message = "Could not load services. Please try again." });
        }
    }

    [HttpGet("Services/Data/{id:long}")]
    public async Task<IActionResult> GetServiceDataById(long id, CancellationToken cancellationToken)
    {
        try
        {
            var service = await _providerServices.GetByIdForProviderAsync(
                id,
                RequireProviderUserId(),
                cancellationToken);

            if (service is null)
            {
                return NotFound(new { success = false, message = "Service not found." });
            }

            return Ok(new
            {
                success = true,
                service = new
                {
                    service.Id,
                    service.ServiceName,
                    service.Description,
                    service.Price,
                    PriceFormatted = service.Price.ToString("F2"),
                    service.EstimatedDurationMinutes,
                    service.IsActive
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching service {ServiceId} data via AJAX.", id);
            return StatusCode(500, new { success = false, message = "Could not load service details." });
        }
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

    [HttpPost("Services/CreateAjax")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateServiceAjax(
        ProviderServiceFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = "Validation failed. Please correct the highlighted errors.",
                errors = GetModelStateErrors()
            });
        }

        try
        {
            var newId = await _providerServices.CreateAsync(
                RequireProviderUserId(),
                model,
                cancellationToken);

            return Ok(new
            {
                success = true,
                message = "Your service was added successfully.",
                id = newId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AJAX Create service failed.");
            return StatusCode(500, new { success = false, message = "The service could not be saved. Please try again." });
        }
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

    [HttpPost("Services/EditAjax/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditServiceAjax(
        long id,
        ProviderServiceFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = "Validation failed. Please correct the highlighted errors.",
                errors = GetModelStateErrors()
            });
        }

        try
        {
            var updated = await _providerServices.UpdateForProviderAsync(
                id,
                RequireProviderUserId(),
                model,
                cancellationToken);

            if (!updated)
            {
                return NotFound(new { success = false, message = "Service not found or you do not have permission to modify it." });
            }

            return Ok(new
            {
                success = true,
                message = "Your service was updated successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AJAX Edit service {ServiceId} failed.", id);
            return StatusCode(500, new { success = false, message = "The service could not be updated. Please try again." });
        }
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

    [HttpPost("Services/DeleteAjax/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteServiceAjax(long id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _providerServices.DeleteForProviderAsync(
                id,
                RequireProviderUserId(),
                cancellationToken);

            if (!deleted)
            {
                return NotFound(new { success = false, message = "Service not found or you do not have permission to delete it." });
            }

            return Ok(new
            {
                success = true,
                message = "Your service was deleted successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AJAX Delete service {ServiceId} failed.", id);
            return StatusCode(500, new { success = false, message = "The service could not be deleted. Please try again." });
        }
    }

    [HttpGet("Profile")]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var providerUserId = RequireProviderUserId();
        var services = await _providerServices.GetForProviderAsync(providerUserId, cancellationToken);

        var model = new ProviderProfileViewModel
        {
            Id = user.Id,
            FullName = user.FullName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Role = AppRoles.ServiceProvider,
            CreatedAtUtc = user.CreatedAtUtc,
            TotalServicesOffered = services.Count,
            ActiveServicesCount = services.Count(s => s.IsActive)
        };

        return View("Profile", model);
    }

    [HttpGet("Profile/Data")]
    public async Task<IActionResult> GetProfileData(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var providerUserId = RequireProviderUserId();
        var services = await _providerServices.GetForProviderAsync(providerUserId, cancellationToken);

        return Ok(new
        {
            success = true,
            data = new
            {
                user.Id,
                FullName = user.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Role = AppRoles.ServiceProvider,
                user.CreatedAtUtc,
                TotalServicesOffered = services.Count,
                ActiveServicesCount = services.Count(s => s.IsActive)
            }
        });
    }

    [HttpPost("Profile/Update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(
        ProviderProfileViewModel model,
        CancellationToken cancellationToken)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                     Request.Headers.Accept.ToString().Contains("application/json");

        if (!ModelState.IsValid)
        {
            if (isAjax)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Validation failed. Please correct the highlighted errors.",
                    errors = GetModelStateErrors()
                });
            }

            return View("Profile", model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return isAjax ? Unauthorized(new { success = false, message = "User not found." }) : Challenge();
        }

        user.FullName = model.FullName.Trim();
        user.PhoneNumber = model.PhoneNumber.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            if (isAjax)
            {
                return BadRequest(new { success = false, message = errors });
            }

            ModelState.AddModelError(string.Empty, errors);
            return View("Profile", model);
        }

        if (isAjax)
        {
            return Ok(new
            {
                success = true,
                message = "Your profile was updated successfully.",
                data = new
                {
                    user.FullName,
                    user.PhoneNumber
                }
            });
        }

        TempData["SuccessMessage"] = "Your profile was updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    private string RequireProviderUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("The authenticated provider has no Identity user ID.");
    }

    private IDictionary<string, string[]> GetModelStateErrors()
    {
        return ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            );
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
