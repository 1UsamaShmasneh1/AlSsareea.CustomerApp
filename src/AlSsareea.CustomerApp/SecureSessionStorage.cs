using AlSsareea.CustomerApp.Core;
using System.Text.Json;
namespace AlSsareea.CustomerApp;

public sealed class SecureSessionStorage : ISessionStorage
{
    private const string Key = "customer.refresh-session.v1";
    public async Task<StoredSession?> GetAsync(CancellationToken ct) { string? value = await SecureStorage.Default.GetAsync(Key); ct.ThrowIfCancellationRequested(); return value is null ? null : JsonSerializer.Deserialize<StoredSession>(value, ApiJson.Options); }
    public async Task SetAsync(StoredSession session, CancellationToken ct) { ct.ThrowIfCancellationRequested(); await SecureStorage.Default.SetAsync(Key, JsonSerializer.Serialize(session, ApiJson.Options)); }
    public Task ClearAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); SecureStorage.Default.Remove(Key); return Task.CompletedTask; }
}
