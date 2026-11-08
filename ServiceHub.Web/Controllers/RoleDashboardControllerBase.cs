using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceHub.Web.Models;
using ServiceHub.Web.Models.ViewModels;

namespace ServiceHub.Web.Controllers;

public abstract class RoleDashboardControllerBase : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    protected RoleDashboardControllerBase(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    protected async Task<IActionResult> ShowDashboardAsync(string role)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        return View("Dashboard", new DashboardViewModel
        {
            FullName = user.FullName ?? user.Email ?? "there",
            Role = role
        });
    }
}
