using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;
using RaceDayMVC.Services;

namespace RaceDayMVC.Controllers;

[RoleRequired("Organiser")]
public sealed class OrganiserController : Controller
{
    private readonly RaceDayApiClient _api;
    public OrganiserController(RaceDayApiClient api) => _api = api;

    private async Task<List<RaceEvent>> MyEvents()
        => (await _api.GetAsync<List<RaceEvent>>("api/events/mine")).Data ?? new();

    private async Task<RaceEvent?> OwnedEvent(int eventId)
        => (await MyEvents()).FirstOrDefault(e => e.EventID == eventId);

    public async Task<IActionResult> Dashboard()
    {
        var events = await MyEvents();
        foreach (var ev in events)
            ev.EnrolmentCount = (await _api.GetAsync<List<RaceEnrolment>>(
                $"api/events/{ev.EventID}/enrolments")).Data?.Count ?? 0;
        return View(new OrganiserDashboard { Events = events });
    }

    public async Task<IActionResult> Events() => View(await MyEvents());

    [HttpGet]
    public async Task<IActionResult> EventForm(int? id)
    {
        if (id == null) return View(new EventForm());
        var ev = await OwnedEvent(id.Value);
        if (ev == null) return NotFound();
        return View(new EventForm
        {
            EventID = ev.EventID, EventName = ev.EventName, Description = ev.Description,
            EventDate = ev.EventDate, RegistrationDeadline = ev.RegistrationDeadline,
            Location = ev.Location, EventType = ev.EventType, DistanceKm = ev.DistanceKm,
            BannerUrl = ev.BannerUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> SaveEvent(EventForm form)
    {
        if (form.RegistrationDeadline >= form.EventDate)
            ModelState.AddModelError(nameof(form.RegistrationDeadline), "Registration must close before the event.");
        if (!ModelState.IsValid) return View("EventForm", form);
        var method = form.EventID == 0 ? HttpMethod.Post : HttpMethod.Put;
        string path = form.EventID == 0 ? "api/events" : $"api/events/{form.EventID}";
        var result = await _api.SendAsync<RaceEvent>(method, path, form);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View("EventForm", form);
        }
        TempData["Success"] = "Event saved.";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var result = await _api.SendAsync<object>(HttpMethod.Delete, $"api/events/{id}");
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Event deleted." : result.Message;
        return RedirectToAction(nameof(Events));
    }

    public async Task<IActionResult> Categories(int eventId)
    {
        var ev = await OwnedEvent(eventId);
        if (ev == null) return NotFound();
        ViewBag.Event = ev;
        return View((await _api.GetAsync<List<RaceCategory>>(
            $"api/events/{eventId}/categories")).Data ?? new());
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(CategoryForm form)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Enter valid category information.";
            return RedirectToAction(nameof(Categories), new { eventId = form.EventID });
        }
        var result = await _api.SendAsync<RaceCategory>(
            HttpMethod.Post, $"api/events/{form.EventID}/categories", form);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Category added." : result.Message;
        return RedirectToAction(nameof(Categories), new { eventId = form.EventID });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int id, int eventId)
    {
        var result = await _api.SendAsync<object>(HttpMethod.Delete, $"api/categories/{id}");
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Category deleted." : result.Message;
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    public async Task<IActionResult> Enrolments(int eventId)
    {
        var ev = await OwnedEvent(eventId);
        if (ev == null) return NotFound();
        ViewBag.Event = ev;
        return View((await _api.GetAsync<List<RaceEnrolment>>(
            $"api/events/{eventId}/enrolments")).Data ?? new());
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, int eventId, string status)
    {
        var result = await _api.SendAsync<object>(HttpMethod.Put,
            $"api/enrolments/{id}", new { Status = status });
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Status updated." : result.Message;
        return RedirectToAction(nameof(Enrolments), new { eventId });
    }

    public async Task<IActionResult> Results(int eventId)
    {
        var ev = await OwnedEvent(eventId);
        if (ev == null) return NotFound();
        ViewBag.Event = ev;
        ViewBag.Entries = (await _api.GetAsync<List<RaceEnrolment>>(
            $"api/events/{eventId}/enrolments")).Data ?? new();
        return View((await _api.GetAsync<List<RaceResult>>(
            $"api/events/{eventId}/results")).Data ?? new());
    }

    [HttpPost]
    public async Task<IActionResult> RecordResult(int eventId, ResultForm form)
    {
        if (!ModelState.IsValid || form.FinishTime <= TimeSpan.Zero)
        {
            TempData["Error"] = "Enter a valid finish time and enrolment.";
            return RedirectToAction(nameof(Results), new { eventId });
        }
        var result = await _api.SendAsync<object>(HttpMethod.Post, "api/results", form);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Result published." : result.Message;
        return RedirectToAction(nameof(Results), new { eventId });
    }

    [HttpPost]
    public async Task<IActionResult> UploadBanner(int eventId, IFormFile image)
    {
        if (image == null)
        {
            TempData["Error"] = "Choose an image.";
            return RedirectToAction(nameof(EventForm), new { id = eventId });
        }
        var result = await _api.UploadAsync($"api/media/events/{eventId}/banner", image);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Banner uploaded." : result.Message;
        return RedirectToAction(nameof(EventForm), new { id = eventId });
    }
}
