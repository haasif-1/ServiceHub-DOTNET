using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceHub.Web.Models;

namespace ServiceHub.Web.Controllers;

[Authorize(Roles = AppRoles.ServiceProvider)]
[Route("ServiceProvider")]
public sealed class ServiceProviderController : RoleDashboardControllerBase
{
    public ServiceProviderController(UserManager<ApplicationUser> userManager)
        : base(userManager)
    {
    }

    [HttpGet("Dashboard")]
    public Task<IActionResult> Dashboard()
    {
        return ShowDashboardAsync(AppRoles.ServiceProvider);
    }
}
