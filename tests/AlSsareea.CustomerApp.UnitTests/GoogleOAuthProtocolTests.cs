using System.Security.Cryptography;
using System.Text;
using AlSsareea.CustomerApp.Core;

namespace AlSsareea.CustomerApp.UnitTests;

public sealed class GoogleOAuthProtocolTests
{
    [Fact]
    public void Android_is_a_supported_Google_OAuth_platform()
    {
        Assert.True(GoogleOAuthPlatform.IsSupported(isAndroid: true, isIos: false, isMacCatalyst: false));
        Assert.False(GoogleOAuthPlatform.IsSupported(isAndroid: false, isIos: false, isMacCatalyst: false));
    }

    [Fact]
    public void Configuration_requires_client_id_and_uses_registered_default_callback()
    {
        GoogleClientConfiguration configuration = GoogleClientConfiguration.Resolve(null, null, null, null);

        Assert.False(configuration.IsConfigured);
        Assert.Equal("com.alssareea.customer:/oauth2redirect", configuration.RedirectUri?.AbsoluteUri);
    }

    [Fact]
    public void Runtime_configuration_takes_precedence_over_embedded_build_configuration()
    {
        GoogleClientConfiguration configuration = GoogleClientConfiguration.Resolve(
            "runtime-client.apps.googleusercontent.com",
            "com.alssareea.customer:/runtime-callback",
            "embedded-client.apps.googleusercontent.com",
            "com.alssareea.customer:/embedded-callback");

        Assert.True(configuration.IsConfigured);
        Assert.Equal("runtime-client.apps.googleusercontent.com", configuration.ClientId);
        Assert.Equal("com.alssareea.customer:/runtime-callback", configuration.RedirectUri?.AbsoluteUri);
    }

    [Fact]
    public void Embedded_build_configuration_is_used_when_mobile_process_has_no_environment_values()
    {
        GoogleClientConfiguration configuration = GoogleClientConfiguration.Resolve(
            null,
            null,
            "embedded-client.apps.googleusercontent.com",
            "com.alssareea.customer:/embedded-callback");

        Assert.True(configuration.IsConfigured);
        Assert.Equal("embedded-client.apps.googleusercontent.com", configuration.ClientId);
        Assert.Equal("com.alssareea.customer:/embedded-callback", configuration.RedirectUri?.AbsoluteUri);
    }

    [Fact]
    public void Authorization_request_uses_code_flow_pkce_state_nonce_and_exact_callback()
    {
        const string clientId = "android-client.apps.googleusercontent.com";
        var callback = new Uri("com.alssareea.customer:/oauth2redirect");

        GoogleOAuthAuthorization authorization = GoogleOAuthProtocol.CreateAuthorization(clientId, callback);
        IReadOnlyDictionary<string, string> query = ReadQuery(authorization.AuthorizationUri);

        Assert.Equal("https://accounts.google.com/o/oauth2/v2/auth", authorization.AuthorizationUri.GetLeftPart(UriPartial.Path));
        Assert.Equal(clientId, query["client_id"]);
        Assert.Equal(callback.AbsoluteUri, query["redirect_uri"]);
        Assert.Equal("code", query["response_type"]);
        Assert.Equal("openid email profile", query["scope"]);
        Assert.Equal(authorization.State, query["state"]);
        Assert.Equal(authorization.Nonce, query["nonce"]);
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.Equal(Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(authorization.CodeVerifier))), query["code_challenge"]);
        Assert.InRange(authorization.CodeVerifier.Length, 43, 128);
        Assert.Matches("^[A-Za-z0-9_-]+$", authorization.CodeVerifier);
    }

    [Fact]
    public void Authorization_generates_unique_state_nonce_and_verifier()
    {
        var callback = new Uri("com.alssareea.customer:/oauth2redirect");

        GoogleOAuthAuthorization first = GoogleOAuthProtocol.CreateAuthorization("client", callback);
        GoogleOAuthAuthorization second = GoogleOAuthProtocol.CreateAuthorization("client", callback);

        Assert.NotEqual(first.State, second.State);
        Assert.NotEqual(first.Nonce, second.Nonce);
        Assert.NotEqual(first.CodeVerifier, second.CodeVerifier);
    }

    [Fact]
    public void Callback_returns_code_only_for_matching_state()
    {
        var valid = new Dictionary<string, string> { ["state"] = "expected-state", ["code"] = "authorization-code" };
        var wrongState = new Dictionary<string, string> { ["state"] = "other-state", ["code"] = "authorization-code" };
        var missingState = new Dictionary<string, string> { ["code"] = "authorization-code" };
        var missingCode = new Dictionary<string, string> { ["state"] = "expected-state" };

        Assert.True(GoogleOAuthProtocol.TryReadCode(valid, "expected-state", out string? code));
        Assert.Equal("authorization-code", code);
        Assert.False(GoogleOAuthProtocol.TryReadCode(wrongState, "expected-state", out _));
        Assert.False(GoogleOAuthProtocol.TryReadCode(missingState, "expected-state", out _));
        Assert.False(GoogleOAuthProtocol.TryReadCode(missingCode, "expected-state", out _));
    }

    private static IReadOnlyDictionary<string, string> ReadQuery(Uri uri) => uri.Query.TrimStart('?')
        .Split('&', StringSplitOptions.RemoveEmptyEntries)
        .Select(part => part.Split('=', 2))
        .ToDictionary(part => Uri.UnescapeDataString(part[0]), part => Uri.UnescapeDataString(part[1]), StringComparer.Ordinal);

    private static string Base64Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
