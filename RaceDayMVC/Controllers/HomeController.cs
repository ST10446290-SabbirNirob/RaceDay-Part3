using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;
using RaceDayMVC.Services;

namespace RaceDayMVC.Controllers;

public sealed class HomeController : Controller
{
    private readonly RaceDayApiClient _api;
    public HomeController(RaceDayApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? type, string? search)
    {
        var response = await _api.GetAsync<List<RaceEvent>>("api/events");
        var events = (response.Data ?? new()).Where(e => e.EventDate >= DateTime.Today);
        if (!string.IsNullOrWhiteSpace(type))
            events = events.Where(e => e.EventType.Equals(type, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(search))
            events = events.Where(e => e.EventName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || e.Location.Contains(search, StringComparison.OrdinalIgnoreCase));
        ViewBag.Type = type;
        ViewBag.Search = search;
        if (!response.Success) ViewBag.Error = response.Message;
        return View(events.OrderBy(e => e.EventDate).ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var response = await _api.GetAsync<EventDetail>($"api/events/{id}");
        if (!response.Success || response.Data == null)
        {
            TempData["Error"] = response.Message;
            return RedirectToAction(nameof(Index));
        }
        return View(response.Data);
    }

    public IActionResult Error() => View();
}
