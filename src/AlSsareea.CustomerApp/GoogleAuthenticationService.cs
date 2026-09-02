using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AlSsareea.CustomerApp.Core;

namespace AlSsareea.CustomerApp;

public sealed record GoogleClientConfiguration(string? ClientId, Uri? RedirectUri)
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && RedirectUri is not null;
}

public sealed class GoogleAuthenticationService(GoogleClientConfiguration configuration) : IGoogleAuthenticationService
{
    private static readonly Uri AuthorizationEndpoint = new("https://accounts.google.com/o/oauth2/v2/auth");
    private static readonly Uri TokenEndpoint = new("https://oauth2.googleapis.com/token");

    public async Task<GoogleSignInResult> SignInAsync(CancellationToken ct)
    {
        if (!configuration.IsConfigured) return new(GoogleSignInStatus.NotConfigured);
        if (!(OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst())) return new(GoogleSignInStatus.Unsupported);
        string state = RandomToken(32); string nonce = RandomToken(32); string verifier = RandomToken(64);
        string challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        Uri authorization = new(AuthorizationEndpoint + "?" + Query(new Dictionary<string, string>
        {
            ["client_id"] = configuration.ClientId!,
            ["redirect_uri"] = configuration.RedirectUri!.AbsoluteUri,
            ["response_type"] = "code",
            ["scope"] = "openid email profile",
            ["state"] = state,
            ["nonce"] = nonce,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
        }));
        try
        {
            WebAuthenticatorResult result = await WebAuthenticator.Default.AuthenticateAsync(new WebAuthenticatorOptions { Url = authorization, CallbackUrl = configuration.RedirectUri, PrefersEphemeralWebBrowserSession = true });
            if (!result.Properties.TryGetValue("state", out string? returnedState) || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(state), Encoding.UTF8.GetBytes(returnedState))) return new(GoogleSignInStatus.Failed);
            if (!result.Properties.TryGetValue("code", out string? code) || string.IsNullOrWhiteSpace(code)) return new(GoogleSignInStatus.Failed);
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = configuration.ClientId!,
                ["code"] = code,
                ["code_verifier"] = verifier,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = configuration.RedirectUri.AbsoluteUri,
            });
            using HttpResponseMessage response = await client.PostAsync(TokenEndpoint, content, ct);
            if (!response.IsSuccessStatusCode) return new(GoogleSignInStatus.Failed);
            using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
            return json.RootElement.TryGetProperty("id_token", out JsonElement token) && !string.IsNullOrWhiteSpace(token.GetString())
                ? new(GoogleSignInStatus.Succeeded, token.GetString(), nonce) : new(GoogleSignInStatus.Failed);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return new(GoogleSignInStatus.Cancelled); }
        catch (TaskCanceledException) { return new(GoogleSignInStatus.Cancelled); }
        catch (Exception exception) when (exception is HttpRequestException or FeatureNotSupportedException or InvalidOperationException) { return new(GoogleSignInStatus.Failed); }
    }

    private static string Query(IReadOnlyDictionary<string, string> values) => string.Join('&', values.Select(pair => Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value)));
    private static string RandomToken(int bytes) => Base64Url(RandomNumberGenerator.GetBytes(bytes));
    private static string Base64Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
