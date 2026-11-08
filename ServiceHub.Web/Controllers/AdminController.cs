using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceHub.Web.Models;

namespace ServiceHub.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[Route("Admin")]
public sealed class AdminController : RoleDashboardControllerBase
{
    public AdminController(UserManager<ApplicationUser> userManager)
        : base(userManager)
    {
    }

    [HttpGet("Dashboard")]
    public Task<IActionResult> Dashboard()
    {
        return ShowDashboardAsync(AppRoles.Admin);
    }
}
