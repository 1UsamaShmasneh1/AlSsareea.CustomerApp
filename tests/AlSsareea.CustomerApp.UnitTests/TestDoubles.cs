using AlSsareea.CustomerApp.Core;

namespace AlSsareea.CustomerApp.UnitTests;

internal sealed class OnlineConnectivity(bool online = true) : IConnectivityService
{
    public bool IsOnline { get; set; } = online;
    public event EventHandler<bool>? ConnectivityChanged;
    public void Set(bool value) { IsOnline = value; ConnectivityChanged?.Invoke(this, value); }
}

internal sealed class TestText(string language = "en") : ILocalizationService
{
    public string Language { get; private set; } = language;
    public bool IsRightToLeft => Language is "ar" or "he";
    public string this[string key] => key;
    public void Apply(string value) => Language = value;
}

internal sealed class TestNavigation : INavigationService
{
    public List<(string Route, IReadOnlyDictionary<string, object>? Parameters)> Visits { get; } = [];
    public Task GoToAsync(string route, IReadOnlyDictionary<string, object>? parameters = null) { Visits.Add((route, parameters)); return Task.CompletedTask; }
}

internal sealed class TestPreferences : IPreferencesStore
{
    public bool OnboardingCompleted { get; set; }
    public string Language { get; set; } = "en";
}

internal sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(Clone(request));
        return Task.FromResult(respond(request));
    }
    private static HttpRequestMessage Clone(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers) clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        return clone;
    }
}

internal static class Responses
{
    public static HttpResponseMessage Json(string json, System.Net.HttpStatusCode code = System.Net.HttpStatusCode.OK) => new(code) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
}
