using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;
using RaceDayMVC.Services;

namespace RaceDayMVC.Controllers;

[RoleRequired("Participant")]
public sealed class ParticipantController : Controller
{
    private readonly RaceDayApiClient _api;
    public ParticipantController(RaceDayApiClient api) => _api = api;

    [HttpPost]
    public async Task<IActionResult> Enter(int eventId, int categoryId)
    {
        var result = await _api.SendAsync<object>(HttpMethod.Post, "api/enrolments",
            new { EventID = eventId, CategoryID = categoryId });
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "You're enrolled! Check My Events." : result.Message;
        return RedirectToAction("Details", "Home", new { id = eventId });
    }

    public async Task<IActionResult> MyEvents()
        => View((await _api.GetAsync<List<RaceEnrolment>>("api/enrolments/my")).Data ?? new());

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _api.SendAsync<object>(HttpMethod.Delete, $"api/enrolments/{id}");
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Enrolment cancelled." : result.Message;
        return RedirectToAction(nameof(MyEvents));
    }

    public async Task<IActionResult> MyResults()
    {
        var list = (await _api.GetAsync<List<RaceResult>>("api/results/my")).Data ?? new();
        foreach (var group in list.GroupBy(r => r.EventID))
        {
            var eventResults = await _api.GetAsync<List<RaceResult>>($"api/events/{group.Key}/results");
            int count = eventResults.Data?.Count ?? 0;
            foreach (var result in group) result.TotalFinishers = count;
        }
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var response = await _api.GetAsync<RaceProfile>("api/users/profile");
        if (!response.Success || response.Data == null)
        {
            TempData["Error"] = response.Message;
            return RedirectToAction("Index", "Home");
        }
        return View(response.Data);
    }

    [HttpPost]
    public async Task<IActionResult> Profile(RaceProfile model)
    {
        if (!ModelState.IsValid) return View(model);
        var response = await _api.SendAsync<RaceProfile>(HttpMethod.Put, "api/users/profile",
            new { model.FirstName, model.LastName, model.Email });
        if (!response.Success)
        {
            ModelState.AddModelError("", response.Message);
            return View(model);
        }
        HttpContext.Session.SetString("Name", model.FirstName);
        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    public async Task<IActionResult> UploadPicture(IFormFile image)
    {
        if (image == null)
        {
            TempData["Error"] = "Choose an image.";
            return RedirectToAction(nameof(Profile));
        }
        var result = await _api.UploadAsync("api/media/profile/picture", image);
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "Profile picture uploaded." : result.Message;
        return RedirectToAction(nameof(Profile));
    }
}
