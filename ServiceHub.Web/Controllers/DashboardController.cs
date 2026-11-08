using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub.Web.Models;

namespace ServiceHub.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var path = User.IsInRole(AppRoles.Admin)
            ? "/Admin/Dashboard"
            : User.IsInRole(AppRoles.Customer)
                ? "/Customer/Dashboard"
                : User.IsInRole(AppRoles.ServiceProvider)
                    ? "/ServiceProvider/Dashboard"
                    : null;

        return path is null
            ? RedirectToAction("AccessDenied", "Account")
            : LocalRedirect(path);
    }
}
