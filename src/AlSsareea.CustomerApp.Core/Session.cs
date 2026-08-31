namespace AlSsareea.CustomerApp.Core;

public sealed record StoredSession(string RefreshToken, DateTime RefreshTokenExpiresUtc, string DeviceIdentifier);
public interface ISessionStorage { Task<StoredSession?> GetAsync(CancellationToken ct); Task SetAsync(StoredSession session, CancellationToken ct); Task ClearAsync(CancellationToken ct); }
public interface IAuthenticationApi { Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct); Task<TokenResponse> RefreshAsync(RefreshRequest request, CancellationToken ct); Task LogoutAsync(string idempotencyKey, CancellationToken ct); Task<OtpChallengeResponse> RequestOtpAsync(OtpChallengeRequest request, string idempotencyKey, CancellationToken ct); Task VerifyOtpAsync(Guid challengeId, OtpVerifyRequest request, CancellationToken ct); }

public interface ISessionManager { string? AccessToken { get; } DateTime? AccessTokenExpiresUtc { get; } Guid? UserId { get; } bool IsAuthenticated { get; } Task SetAsync(TokenResponse tokens, string deviceIdentifier, CancellationToken ct); Task<bool> RestoreAsync(CancellationToken ct); Task<bool> RefreshAsync(string? failedAccessToken, CancellationToken ct); Task ClearAsync(CancellationToken ct); }

public sealed class SessionManager(ISessionStorage storage, IAuthenticationApi auth) : ISessionManager
{
    private readonly SemaphoreSlim refreshGate = new(1, 1);
    private string? accessToken;
    private DateTime? accessTokenExpiresUtc;
    private Guid? userId;
    public string? AccessToken => Volatile.Read(ref accessToken);
    public DateTime? AccessTokenExpiresUtc => accessTokenExpiresUtc;
    public Guid? UserId => userId;
    public bool IsAuthenticated => AccessToken is not null;

    public async Task SetAsync(TokenResponse tokens, string deviceIdentifier, CancellationToken ct)
    {
        await storage.SetAsync(new(tokens.RefreshToken, tokens.RefreshTokenExpiresUtc, deviceIdentifier), ct);
        Volatile.Write(ref accessToken, tokens.AccessToken);
        accessTokenExpiresUtc = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn);
        userId = tokens.User.Id;
    }

    public async Task<bool> RestoreAsync(CancellationToken ct)
    {
        StoredSession? stored = await storage.GetAsync(ct);
        if (stored is null || stored.RefreshTokenExpiresUtc <= DateTime.UtcNow) { await ClearAsync(ct); return false; }
        return await RefreshCoreAsync(stored, ct);
    }

    public async Task<bool> RefreshAsync(string? failedAccessToken, CancellationToken ct)
    {
        await refreshGate.WaitAsync(ct);
        try
        {
            if (AccessToken is not null && failedAccessToken is not null && !StringComparer.Ordinal.Equals(AccessToken, failedAccessToken)) return true;
            StoredSession? stored = await storage.GetAsync(ct);
            return stored is not null && await RefreshCoreAsync(stored, ct);
        }
        finally { refreshGate.Release(); }
    }

    private async Task<bool> RefreshCoreAsync(StoredSession stored, CancellationToken ct)
    {
        try { TokenResponse tokens = await auth.RefreshAsync(new(stored.RefreshToken, stored.DeviceIdentifier), ct); await SetAsync(tokens, stored.DeviceIdentifier, ct); return true; }
        catch (ApiException ex) when (ex.Problem.Status is 400 or 401 or 403 or 409) { await ClearAsync(CancellationToken.None); return false; }
    }

    public async Task ClearAsync(CancellationToken ct) { Volatile.Write(ref accessToken, null); accessTokenExpiresUtc = null; userId = null; await storage.ClearAsync(ct); }
}
