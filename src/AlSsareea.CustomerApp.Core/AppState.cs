namespace AlSsareea.CustomerApp.Core;

public sealed class CustomerAppState : ObservableObject, IUserStateResetter
{
    private Guid? merchantId;
    private Guid? branchId;
    private CartResponse? cart;
    public Guid? MerchantId { get => merchantId; set => Set(ref merchantId, value); }
    public Guid? BranchId { get => branchId; set => Set(ref branchId, value); }
    public CartResponse? Cart { get => cart; set => Set(ref cart, value); }
    public Task ResetAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        MerchantId = null;
        BranchId = null;
        Cart = null;
        return Task.CompletedTask;
    }
}

public interface IPushTokenSource
{
    short Platform { get; }
    short Provider { get; }
    bool IsConfigured { get; }
    Task<string?> GetTokenAsync(CancellationToken ct);
    event EventHandler<string>? TokenChanged;
}

public interface IPushRegistrationStore
{
    Task<Guid?> GetAsync(CancellationToken ct);
    Task SetAsync(Guid id, CancellationToken ct);
    Task ClearAsync(CancellationToken ct);
}

public sealed class PushRegistrationCoordinator(INotificationsApi notifications, IPushTokenSource source, IPushRegistrationStore store) : IUserStateResetter, IAsyncDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private Guid? registeredId;
    private bool subscribed;
    public async Task RegisterAsync(CancellationToken ct)
    {
        if (!source.IsConfigured) return;
        string? token = await source.GetTokenAsync(ct);
        if (string.IsNullOrWhiteSpace(token)) return;
        await ReplaceAsync(token, ct);
        if (!subscribed)
        {
            source.TokenChanged += TokenChanged;
            subscribed = true;
        }
    }
    private async void TokenChanged(object? sender, string token)
    {
        try { await ReplaceAsync(token, CancellationToken.None); }
        catch (Exception) { }
    }
    private async Task ReplaceAsync(string token, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            registeredId ??= await store.GetAsync(ct);
            DeviceTokenResponse replacement = await notifications.RegisterAsync(new(token, source.Platform, source.Provider), ct);
            Guid? old = registeredId;
            registeredId = replacement.Id;
            await store.SetAsync(replacement.Id, ct);
            if (old.HasValue && old != replacement.Id) await notifications.UnregisterAsync(old.Value, ct);
        }
        finally { gate.Release(); }
    }
    public async Task ResetAsync(CancellationToken ct)
    {
        source.TokenChanged -= TokenChanged;
        subscribed = false;
        registeredId ??= await store.GetAsync(ct);
        if (registeredId.HasValue) await notifications.UnregisterAsync(registeredId.Value, ct);
        registeredId = null;
        await store.ClearAsync(ct);
    }
    public ValueTask DisposeAsync() { source.TokenChanged -= TokenChanged; subscribed = false; gate.Dispose(); return ValueTask.CompletedTask; }
}
