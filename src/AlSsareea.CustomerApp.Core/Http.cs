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
        try
        {
            using HttpRequestMessage request = CreateRequest(method, path, body, idempotencyKey);
            using HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode) throw await CreateExceptionAsync(response, ct);
            return (await response.Content.ReadFromJsonAsync<T>(ApiJson.Options, ct)) ?? throw new InvalidDataException("The API returned an empty response.");
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested) { throw new ApiTimeoutException("The backend request timed out.", ex); }
        catch (HttpRequestException ex) { throw new ApiNetworkException("The backend is unavailable.", ex); }
    }

    public async Task SendAsync(HttpMethod method, string path, object? body, CancellationToken ct, string? idempotencyKey = null)
    {
        try
        {
            using HttpRequestMessage request = CreateRequest(method, path, body, idempotencyKey);
            using HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode) throw await CreateExceptionAsync(response, ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested) { throw new ApiTimeoutException("The backend request timed out.", ex); }
        catch (HttpRequestException ex) { throw new ApiNetworkException("The backend is unavailable.", ex); }
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path, object? body, string? idempotencyKey)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString("N"));
        if (idempotencyKey is not null) request.Headers.Add("Idempotency-Key", idempotencyKey);
        if (body is not null) request.Content = JsonContent.Create(body, options: ApiJson.Options);
        return request;
    }

    private static async Task<ApiException> CreateExceptionAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.Content.Headers.ContentLength == 0) return new(new((int)response.StatusCode, response.ReasonPhrase, null, null, new Dictionary<string, string[]>()));
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

public sealed class SafeReadRetryHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (request.Method != HttpMethod.Get) return await base.SendAsync(request, ct);
        HttpResponseMessage response;
        try { response = await base.SendAsync(request, ct); }
        catch (HttpRequestException) when (!ct.IsCancellationRequested)
        {
            using HttpRequestMessage retryAfterTransport = await CloneAsync(request, ct);
            return await base.SendAsync(retryAfterTransport, ct);
        }
        if ((int)response.StatusCode is not (502 or 503 or 504)) return response;
        response.Dispose();
        using HttpRequestMessage retry = await CloneAsync(request, ct);
        return await base.SendAsync(retry, ct);
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage source, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(source.Method, source.RequestUri);
        foreach (KeyValuePair<string, IEnumerable<string>> header in source.Headers) clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        if (source.Content is not null)
        {
            byte[] bytes = await source.Content.ReadAsByteArrayAsync(ct); clone.Content = new ByteArrayContent(bytes);
            foreach (KeyValuePair<string, IEnumerable<string>> header in source.Content.Headers) clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        return clone;
    }
}
