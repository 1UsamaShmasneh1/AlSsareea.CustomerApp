using System.Security.Cryptography;
using System.Text;

namespace AlSsareea.CustomerApp.Core;

public sealed record GoogleClientConfiguration(string? ClientId, Uri? RedirectUri)
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && RedirectUri is not null;

    public static GoogleClientConfiguration Resolve(string? runtimeClientId, string? runtimeRedirectUri, string? embeddedClientId, string? embeddedRedirectUri)
    {
        string? clientId = FirstValue(runtimeClientId, embeddedClientId);
        string redirect = FirstValue(runtimeRedirectUri, embeddedRedirectUri) ?? GoogleOAuthProtocol.DefaultRedirectUri.AbsoluteUri;
        return new(clientId, Uri.TryCreate(redirect, UriKind.Absolute, out Uri? uri) ? uri : null);
    }

    private static string? FirstValue(string? preferred, string? fallback) => !string.IsNullOrWhiteSpace(preferred) ? preferred.Trim() : !string.IsNullOrWhiteSpace(fallback) ? fallback.Trim() : null;
}

public sealed record GoogleOAuthAuthorization(Uri AuthorizationUri, string State, string Nonce, string CodeVerifier);

public static class GoogleOAuthPlatform
{
    public static bool IsSupported(bool isAndroid, bool isIos, bool isMacCatalyst) => isAndroid || isIos || isMacCatalyst;
}

public static class GoogleOAuthProtocol
{
    public static readonly Uri AuthorizationEndpoint = new("https://accounts.google.com/o/oauth2/v2/auth");
    public static readonly Uri TokenEndpoint = new("https://oauth2.googleapis.com/token");
    public static readonly Uri DefaultRedirectUri = new("com.alssareea.customer:/oauth2redirect");

    public static GoogleOAuthAuthorization CreateAuthorization(string clientId, Uri redirectUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentNullException.ThrowIfNull(redirectUri);
        string state = RandomToken(32);
        string nonce = RandomToken(32);
        string verifier = RandomToken(64);
        string challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        Uri authorization = new(AuthorizationEndpoint + "?" + Query(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri.AbsoluteUri,
            ["response_type"] = "code",
            ["scope"] = "openid email profile",
            ["state"] = state,
            ["nonce"] = nonce,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
        }));
        return new(authorization, state, nonce, verifier);
    }

    public static bool TryReadCode(IReadOnlyDictionary<string, string> callback, string expectedState, out string? code)
    {
        code = null;
        if (!callback.TryGetValue("state", out string? returnedState) || string.IsNullOrEmpty(returnedState) ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expectedState), Encoding.UTF8.GetBytes(returnedState)) ||
            !callback.TryGetValue("code", out string? returnedCode) || string.IsNullOrWhiteSpace(returnedCode)) return false;
        code = returnedCode;
        return true;
    }

    private static string Query(IReadOnlyDictionary<string, string> values) => string.Join('&', values.Select(pair => Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value)));
    private static string RandomToken(int bytes) => Base64Url(RandomNumberGenerator.GetBytes(bytes));
    private static string Base64Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
