using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceHub.Web.Models;

namespace ServiceHub.Web.Controllers;

[Authorize(Roles = AppRoles.Customer)]
[Route("Customer")]
public sealed class CustomerController : RoleDashboardControllerBase
{
    public CustomerController(UserManager<ApplicationUser> userManager)
        : base(userManager)
    {
    }

    [HttpGet("Dashboard")]
    public Task<IActionResult> Dashboard()
    {
        return ShowDashboardAsync(AppRoles.Customer);
    }
}
