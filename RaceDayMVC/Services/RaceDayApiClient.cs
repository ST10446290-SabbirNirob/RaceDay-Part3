using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDayMVC.Models;

namespace RaceDayMVC.Services;

public sealed record ApiResult<T>(bool Success, T? Data, string Message);

public sealed class RaceDayApiClient
{
    private readonly HttpClient _client;
    private readonly IHttpContextAccessor _accessor;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public RaceDayApiClient(HttpClient client, IHttpContextAccessor accessor)
    {
        _client = client;
        _accessor = accessor;
    }

    private HttpContext Context => _accessor.HttpContext
        ?? throw new InvalidOperationException("HTTP context is unavailable.");

    private HttpRequestMessage Request(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path.TrimStart('/'));
        string? cookie = Context.Session.GetString("ApiCookie");
        if (!string.IsNullOrWhiteSpace(cookie))
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        if (method != HttpMethod.Get && method != HttpMethod.Head)
            request.Headers.Add("X-RaceDay-Request", "1");
        return request;
    }

    public async Task<ApiResult<T>> GetAsync<T>(string path)
        => await SendAsync<T>(HttpMethod.Get, path);

    public async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? payload = null)
    {
        try
        {
            using var request = Request(method, path);
            if (payload != null)
                request.Content = JsonContent.Create(payload, options: Json);
            using var response = await _client.SendAsync(request);
            return await Parse<T>(response);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new(false, default, "Cannot reach RaceDay API. Check that the API is running.");
        }
    }

    public async Task<ApiResult<AuthUser>> LoginAsync(LoginForm model)
    {
        try
        {
            using var request = Request(HttpMethod.Post, "api/auth/login");
            request.Content = JsonContent.Create(model, options: Json);
            using var response = await _client.SendAsync(request);
            var result = await Parse<AuthUser>(response);
            if (!result.Success || result.Data == null) return result;
            var setCookies = response.Headers.TryGetValues("Set-Cookie", out var values)
                ? values : Array.Empty<string>();
            string? apiCookie = setCookies
                .Select(v => v.Split(';', 2)[0])
                .FirstOrDefault(v => v.StartsWith(".RaceDay.Session=", StringComparison.Ordinal));
            if (apiCookie == null)
                return new(false, null, "The API did not start a login session.");
            Context.Session.Clear();
            Context.Session.SetString("ApiCookie", apiCookie);
            Context.Session.SetInt32("UserID", result.Data.UserID);
            Context.Session.SetString("Role", result.Data.Role);
            Context.Session.SetString("Name", result.Data.FirstName);
            await Context.Session.CommitAsync();
            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new(false, null, "Unable to log in. Check that the API is running.");
        }
    }

    public async Task LogoutAsync()
    {
        await SendAsync<object>(HttpMethod.Post, "api/auth/logout");
        Context.Session.Clear();
    }

    public async Task<ApiResult<string>> UploadAsync(string path, IFormFile file)
    {
        if (file.Length == 0 || file.Length > 5 * 1024 * 1024)
            return new(false, null, "Choose an image smaller than 5 MB.");
        try
        {
            using var request = Request(HttpMethod.Post, path);
            using var form = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            using var content = new StreamContent(stream);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            form.Add(content, "image", file.FileName);
            request.Content = form;
            using var response = await _client.SendAsync(request);
            var result = await Parse<JsonElement>(response);
            string? url = null;
            if (result.Success && result.Data is JsonElement el &&
                el.ValueKind == JsonValueKind.Object && el.TryGetProperty("url", out var value))
                url = value.GetString();
            return new(result.Success, url, result.Message);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new(false, null, "Could not upload the image.");
        }
    }

    private static async Task<ApiResult<T>> Parse<T>(HttpResponseMessage response)
    {
        string raw = response.Content == null ? "" : await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            string message = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Your session has expired. Please log in again.",
                HttpStatusCode.Forbidden => "You do not have permission for this action.",
                HttpStatusCode.NotFound => "The requested item was not found.",
                _ => "Please check your information and try again."
            };
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("message", out var m) ||
                    doc.RootElement.TryGetProperty("Message", out m))
                    message = m.GetString() ?? message;
            }
            catch (JsonException) { }
            return new(false, default, message);
        }
        if (string.IsNullOrWhiteSpace(raw))
            return new(true, default, "Saved successfully.");
        var data = JsonSerializer.Deserialize<T>(raw, Json);
        return new(true, data, "Saved successfully.");
    }
}
