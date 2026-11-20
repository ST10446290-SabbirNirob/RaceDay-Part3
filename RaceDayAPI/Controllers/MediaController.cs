using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Security;

namespace RaceDayAPI.Controllers;

// MVC only uploads to this API. The browser never needs an Azure key.
[ApiController]
[Authorize]
[Route("api/media")]
public sealed class MediaController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IConfiguration _configuration;
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly Dictionary<string, string> Types = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    public MediaController(RaceDayDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    [HttpPost("events/{eventId:int}/banner")]
    [Authorize(Roles = "Organiser")]
    [RequestSizeLimit(MaxBytes + 65536)]
    public async Task<IActionResult> EventBanner(int eventId, IFormFile image)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev == null) return NotFound(new { Message = "Event not found." });
        if (ev.OrganiserID != User.GetRaceDayUserId()) return Forbid();
        var result = await Upload(image, "event-banners");
        if (!result.Ok) return BadRequest(new { Message = result.Error });
        ev.BannerUrl = result.Url;
        await _db.SaveChangesAsync();
        return Ok(new { Url = result.Url });
    }

    [HttpPost("profile/picture")]
    [Authorize(Roles = "Participant")]
    [RequestSizeLimit(MaxBytes + 65536)]
    public async Task<IActionResult> ProfilePicture(IFormFile image)
    {
        int userId = User.GetRaceDayUserId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId);
        if (user == null) return NotFound();
        var result = await Upload(image, "profile-pictures");
        if (!result.Ok) return BadRequest(new { Message = result.Error });
        user.ProfilePictureUrl = result.Url;
        await _db.SaveChangesAsync();
        return Ok(new { Url = result.Url });
    }

    private async Task<(bool Ok, string? Url, string? Error)> Upload(IFormFile? file, string containerName)
    {
        if (file == null || file.Length == 0 || file.Length > MaxBytes ||
            !Types.TryGetValue(file.ContentType, out var extension))
            return (false, null, "Upload a JPG, PNG or WebP image smaller than 5 MB.");

        string? connection = _configuration["AzureBlobStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connection))
            return (false, null, "Image storage has not been configured by the administrator.");

        try
        {
            var client = new BlobServiceClient(connection);
            var container = client.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.Blob);
            var blob = container.GetBlobClient(Guid.NewGuid().ToString("N") + extension);
            await using var stream = file.OpenReadStream();
            await blob.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
            });
            return (true, blob.Uri.ToString(), null);
        }
        catch (Exception ex) when (ex is Azure.RequestFailedException or ArgumentException)
        {
            return (false, null, "The image could not be uploaded. Check your Azure storage settings.");
        }
    }
}
