using System.Text.Json;
using AlSsareea.CustomerApp.Core;

namespace AlSsareea.CustomerApp;

public sealed class GoogleAuthenticationService(GoogleClientConfiguration configuration) : IGoogleAuthenticationService
{
    public async Task<GoogleSignInResult> SignInAsync(CancellationToken ct)
    {
        if (!configuration.IsConfigured) return new(GoogleSignInStatus.NotConfigured);
        if (!GoogleOAuthPlatform.IsSupported(OperatingSystem.IsAndroid(), OperatingSystem.IsIOS(), OperatingSystem.IsMacCatalyst())) return new(GoogleSignInStatus.Unsupported);
        string clientId = configuration.ClientId!;
        Uri redirectUri = configuration.RedirectUri!;
        GoogleOAuthAuthorization authorization = GoogleOAuthProtocol.CreateAuthorization(clientId, redirectUri);
        try
        {
            WebAuthenticatorResult result = await WebAuthenticator.Default.AuthenticateAsync(new WebAuthenticatorOptions { Url = authorization.AuthorizationUri, CallbackUrl = redirectUri, PrefersEphemeralWebBrowserSession = true });
            if (!GoogleOAuthProtocol.TryReadCode(result.Properties, authorization.State, out string? code)) return new(GoogleSignInStatus.Failed);
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["code"] = code!,
                ["code_verifier"] = authorization.CodeVerifier,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = redirectUri.AbsoluteUri,
            });
            using HttpResponseMessage response = await client.PostAsync(GoogleOAuthProtocol.TokenEndpoint, content, ct);
            if (!response.IsSuccessStatusCode) return new(GoogleSignInStatus.Failed);
            using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
            return json.RootElement.TryGetProperty("id_token", out JsonElement token) && !string.IsNullOrWhiteSpace(token.GetString())
                ? new(GoogleSignInStatus.Succeeded, token.GetString(), authorization.Nonce) : new(GoogleSignInStatus.Failed);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return new(GoogleSignInStatus.Cancelled); }
        catch (TaskCanceledException) { return new(GoogleSignInStatus.Cancelled); }
        catch (Exception exception) when (exception is HttpRequestException or FeatureNotSupportedException or InvalidOperationException) { return new(GoogleSignInStatus.Failed); }
    }

}
