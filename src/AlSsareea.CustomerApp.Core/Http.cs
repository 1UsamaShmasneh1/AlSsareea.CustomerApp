using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AlSsareea.CustomerApp.Core;

public static class ApiJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
}

public sealed class ApiClient(HttpClient http)
{
    public async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct, string? idempotencyKey = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString("N"));
        if (idempotencyKey is not null) request.Headers.Add("Idempotency-Key", idempotencyKey);
        if (body is not null) request.Content = JsonContent.Create(body, options: ApiJson.Options);
        try
        {
            using HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode) throw await CreateExceptionAsync(response, ct);
            return (await response.Content.ReadFromJsonAsync<T>(ApiJson.Options, ct)) ?? throw new InvalidDataException("The API returned an empty response.");
        }
        catch (HttpRequestException ex) { throw new ApiNetworkException("The backend is unavailable.", ex); }
    }

    public async Task SendAsync(HttpMethod method, string path, object? body, CancellationToken ct, string? idempotencyKey = null)
    { _ = await SendAsync<object>(method, path, body, ct, idempotencyKey); }

    private static async Task<ApiException> CreateExceptionAsync(HttpResponseMessage response, CancellationToken ct)
    {
        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        JsonElement root = document.RootElement;
        string? Get(string name) => root.TryGetProperty(name, out JsonElement item) ? item.GetString() : null;
        var errors = new Dictionary<string, string[]>();
        if (root.TryGetProperty("errors", out JsonElement e) && e.ValueKind == JsonValueKind.Object)
            foreach (JsonProperty p in e.EnumerateObject()) errors[p.Name] = p.Value.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToArray();
        return new(new((int)response.StatusCode, Get("title"), Get("detail"), Get("code"), errors));
    }
}

public sealed class AuthenticatedHandler(ISessionManager session) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        string? token = session.AccessToken;
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        HttpResponseMessage response = await base.SendAsync(request, ct);
        if (response.StatusCode != HttpStatusCode.Unauthorized || token is null || request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath.EndsWith("/auth/refresh", StringComparison.Ordinal) == true) return response;
        response.Dispose();
        if (!await session.RefreshAsync(token, ct)) return new(HttpStatusCode.Unauthorized) { RequestMessage = request };
        using HttpRequestMessage retry = await CloneAsync(request, ct);
        retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return await base.SendAsync(retry, ct);
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage source, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(source.Method, source.RequestUri);
        foreach (KeyValuePair<string, IEnumerable<string>> h in source.Headers) clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
        if (source.Content is not null) { var bytes = await source.Content.ReadAsByteArrayAsync(ct); clone.Content = new ByteArrayContent(bytes); foreach (var h in source.Content.Headers) clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value); }
        return clone;
    }
}
