namespace AlSsareea.CustomerApp.Core;

public interface ITrackingHubClient : IAsyncDisposable
{
    ConnectionState State { get; }
    event EventHandler<TrackingRealtimePayload>? LocationUpdated;
    event EventHandler<ConnectionState>? StateChanged;
    event EventHandler? Reconnected;
    Task StartAsync(CancellationToken ct);
    Task SubscribeOrderAsync(Guid orderId, CancellationToken ct);
    Task StopAsync(CancellationToken ct);
}

public sealed class TrackingCoordinator(ITrackingApi api, ITrackingHubClient hub) : IAsyncDisposable, IUserStateResetter
{
    private Guid orderId;
    public event EventHandler<TrackingRealtimePayload>? LocationUpdated;
    public event EventHandler<ConnectionState>? StateChanged;
    public async Task<TrackingRealtimePayload?> StartAsync(Guid id, CancellationToken ct)
    {
        orderId = id;
        hub.LocationUpdated += OnLocation;
        hub.StateChanged += OnState;
        hub.Reconnected += OnReconnected;
        DriverLocationResponse? snapshot = null;
        try { snapshot = await api.LatestAsync(id, ct); }
        catch (ApiException exception) when (exception.Problem.Status == 404) { }
        await hub.StartAsync(ct);
        await hub.SubscribeOrderAsync(id, ct);
        return snapshot is null ? null : new(snapshot.Latitude, snapshot.Longitude, snapshot.RecordedAtUtc, snapshot.AccuracyMeters, snapshot.SpeedMetersPerSecond, snapshot.HeadingDegrees);
    }
    private void OnLocation(object? sender, TrackingRealtimePayload value) => LocationUpdated?.Invoke(this, value);
    private void OnState(object? sender, ConnectionState value) => StateChanged?.Invoke(this, value);
    private async void OnReconnected(object? sender, EventArgs args)
    {
        try
        {
            DriverLocationResponse latest = await api.LatestAsync(orderId, CancellationToken.None);
            LocationUpdated?.Invoke(this, new(latest.Latitude, latest.Longitude, latest.RecordedAtUtc, latest.AccuracyMeters, latest.SpeedMetersPerSecond, latest.HeadingDegrees));
            await hub.SubscribeOrderAsync(orderId, CancellationToken.None);
        }
        catch (Exception) { StateChanged?.Invoke(this, ConnectionState.Failed); }
    }
    public async Task StopAsync(CancellationToken ct)
    {
        hub.LocationUpdated -= OnLocation;
        hub.StateChanged -= OnState;
        hub.Reconnected -= OnReconnected;
        await hub.StopAsync(ct);
    }
    public Task ResetAsync(CancellationToken ct) => StopAsync(ct);
    public async ValueTask DisposeAsync() { await StopAsync(CancellationToken.None); await hub.DisposeAsync(); }
}
