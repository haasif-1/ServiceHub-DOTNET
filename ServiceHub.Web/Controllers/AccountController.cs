using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceHub.Web.Models;
using ServiceHub.Web.Models.ViewModels;
using ServiceHub.Web.Services;

namespace ServiceHub.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAppEmailSender _emailSender;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAppEmailSender emailSender,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var signedInUser = await _userManager.GetUserAsync(User);
            if (signedInUser is not null)
            {
                return await RedirectToDashboardAsync(signedInUser, returnUrl);
            }
        }

        await SetExternalLoginsAsync();
        return View(new LoginViewModel { ReturnUrl = GetSafeReturnUrl(returnUrl) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);
        await SetExternalLoginsAsync();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password. If you have tried too many times, wait 15 minutes before trying again.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} signed in.", user.Id);
            return await RedirectToDashboardAsync(user, model.ReturnUrl);
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password. If you have tried too many times, wait 15 minutes before trying again.");
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var signedInUser = await _userManager.GetUserAsync(User);
            if (signedInUser is not null)
            {
                return await RedirectToDashboardAsync(signedInUser, returnUrl);
            }
        }

        ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        returnUrl = GetSafeReturnUrl(returnUrl);
        ViewData["ReturnUrl"] = returnUrl;
        model.FullName = model.FullName?.Trim() ?? string.Empty;
        model.Email = model.Email?.Trim() ?? string.Empty;
        model.PhoneNumber = model.PhoneNumber?.Trim() ?? string.Empty;

        if (model.FullName.Length < 2)
        {
            ModelState.AddModelError(nameof(model.FullName), "Enter a full name of at least 2 characters.");
        }

        if (!AppRoles.SelfServiceRoles.Contains(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Choose Customer or Service Provider.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            FullName = model.FullName.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        var addRoleResult = await _userManager.AddToRoleAsync(user, model.Role);
        if (!addRoleResult.Succeeded)
        {
            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                _logger.LogError(
                    "Could not remove a newly registered account after role assignment failed. UserId: {UserId}; Identity errors: {ErrorCodes}",
                    user.Id,
                    string.Join(", ", deleteResult.Errors.Select(error => error.Code)));
            }

            ModelState.AddModelError(string.Empty, "Your account could not be completed. Please try again.");
            return View(model);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        _logger.LogInformation("Created a new account for {UserId} with role {Role}.", user.Id, model.Role);

        return await RedirectToDashboardAsync(user, returnUrl);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is not null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action(
                nameof(ResetPassword),
                "Account",
                new { token, email = user.Email },
                Request.Scheme) ?? string.Empty;

            await _emailSender.SendAsync(
                user.Email!,
                "Reset your ServiceHub password",
                $"Reset your password using this link: {callbackUrl}",
                cancellationToken);
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ResetPassword(string? token = null, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("A password reset token and email are required.");
        }

        return View(new ResetPasswordViewModel { Token = token, Email = email });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (result.Succeeded)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl = GetSafeReturnUrl(returnUrl);

        if (!string.IsNullOrEmpty(remoteError))
        {
            TempData["ErrorMessage"] = $"Google sign-in failed: {remoteError}";
            return RedirectToAction(nameof(Login));
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            TempData["ErrorMessage"] = "Google sign-in did not return any account details.";
            return RedirectToAction(nameof(Login));
        }

        var signInResult = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: true);

        if (signInResult.Succeeded)
        {
            var signedInUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            return signedInUser is null
                ? RedirectToAction(nameof(Login))
                : await RedirectToDashboardAsync(signedInUser, returnUrl);
        }

        if (signInResult.IsLockedOut)
        {
            TempData["ErrorMessage"] = "This account is locked after too many failed attempts. Try again later.";
            return RedirectToAction(nameof(Login));
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var fullName = info.Principal.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["ErrorMessage"] = "Google did not provide an email address.";
            return RedirectToAction(nameof(Login));
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join(" ", createResult.Errors.Select(error => error.Description));
                return RedirectToAction(nameof(Login));
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, AppRoles.Customer);
            if (!addRoleResult.Succeeded)
            {
                var deleteResult = await _userManager.DeleteAsync(user);
                if (!deleteResult.Succeeded)
                {
                    _logger.LogError(
                        "Could not remove a new external account after role assignment failed. UserId: {UserId}; Identity errors: {ErrorCodes}",
                        user.Id,
                        string.Join(", ", deleteResult.Errors.Select(error => error.Code)));
                }

                TempData["ErrorMessage"] = "Your account could not be completed. Please try again.";
                return RedirectToAction(nameof(Login));
            }
        }

        var addLoginResult = await _userManager.AddLoginAsync(user, info);
        if (!addLoginResult.Succeeded && addLoginResult.Errors.All(error => error.Code != "LoginAlreadyAssociated"))
        {
            TempData["ErrorMessage"] = string.Join(" ", addLoginResult.Errors.Select(error => error.Description));
            return RedirectToAction(nameof(Login));
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return await RedirectToDashboardAsync(user, returnUrl);
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task SetExternalLoginsAsync()
    {
        ViewData["ExternalLogins"] = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
    }

    private string? GetSafeReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : null;
    }

    private async Task<IActionResult> RedirectToDashboardAsync(ApplicationUser user, string? returnUrl)
    {
        var dashboardPath = await GetDashboardPathAsync(user);
        if (dashboardPath is null)
        {
            return RedirectToAction(nameof(AccessDenied));
        }

        if (returnUrl is not null
            && Url.IsLocalUrl(returnUrl)
            && string.Equals(returnUrl.TrimEnd('/'), dashboardPath, StringComparison.OrdinalIgnoreCase))
        {
            return LocalRedirect(returnUrl);
        }

        return LocalRedirect(dashboardPath);
    }

    private async Task<string?> GetDashboardPathAsync(ApplicationUser user)
    {
        if (await _userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            return "/Admin/Dashboard";
        }

        if (await _userManager.IsInRoleAsync(user, AppRoles.Customer))
        {
            return "/Customer/Dashboard";
        }

        if (await _userManager.IsInRoleAsync(user, AppRoles.ServiceProvider))
        {
            return "/ServiceProvider/Dashboard";
        }

        return null;
    }
}
