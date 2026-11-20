using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDayMVC.Services;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RoleRequiredAttribute : ActionFilterAttribute
{
    private readonly string _role;
    public RoleRequiredAttribute(string role) => _role = role;
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        string? role = session.GetString("Role");
        string? cookie = session.GetString("ApiCookie");
        if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(cookie))
            context.Result = new RedirectToActionResult("Login", "Auth", null);
        else if (!string.Equals(role, _role, StringComparison.Ordinal))
            context.Result = new RedirectToActionResult("Index", "Home", null);
    }
}
