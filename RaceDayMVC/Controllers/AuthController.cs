using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;
using RaceDayMVC.Services;

namespace RaceDayMVC.Controllers;

public sealed class AuthController : Controller
{
    private readonly RaceDayApiClient _api;
    public AuthController(RaceDayApiClient api) => _api = api;

    [HttpGet]
    public IActionResult Login() => View(new LoginForm());

    [HttpPost]
    public async Task<IActionResult> Login(LoginForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var result = await _api.LoginAsync(form);
        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError("", result.Message);
            return View(form);
        }
        return RedirectToAction(result.Data.Role == "Organiser" ? "Dashboard" : "Index",
            result.Data.Role == "Organiser" ? "Organiser" : "Home");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterForm());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterForm form)
    {
        if (form.Role != "Organiser" && form.Role != "Participant")
            ModelState.AddModelError(nameof(form.Role), "Choose Organiser or Participant.");
        if (!ModelState.IsValid) return View(form);
        var result = await _api.SendAsync<AuthUser>(HttpMethod.Post, "api/auth/register", form);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(form);
        }
        TempData["Success"] = "Account created! Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _api.LogoutAsync();
        return RedirectToAction("Index", "Home");
    }
}
