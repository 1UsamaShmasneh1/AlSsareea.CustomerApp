using AlSsareea.CustomerApp.Core;

namespace AlSsareea.CustomerApp.UnitTests;

public sealed class TrackingAndPushTests
{
    [Fact]
    public async Task Tracking_loads_snapshot_connects_and_subscribes()
    {
        Guid order = Guid.NewGuid(); var hub = new HubStub(); var coordinator = new TrackingCoordinator(new TrackingApiStub(), hub);
        TrackingRealtimePayload? snapshot = await coordinator.StartAsync(order, default);
        Assert.NotNull(snapshot); Assert.True(hub.Started); Assert.Equal(order, hub.Subscribed);
    }

    [Fact]
    public async Task Tracking_forwards_events_and_resubscribes_after_reconnect()
    {
        Guid order = Guid.NewGuid(); var hub = new HubStub(); var api = new TrackingApiStub(); var coordinator = new TrackingCoordinator(api, hub); TrackingRealtimePayload? latest = null; coordinator.LocationUpdated += (_, value) => latest = value;
        await coordinator.StartAsync(order, default); hub.Publish(new(1, 2, DateTime.UtcNow, 3, null, null)); Assert.Equal(1, latest!.Latitude);
        hub.Reconnect(); await Task.Delay(30); Assert.Equal(2, hub.SubscribeCount); Assert.True(api.Calls >= 2);
    }

    [Fact]
    public async Task Tracking_stop_unsubscribes_and_stops_hub()
    {
        var hub = new HubStub(); var coordinator = new TrackingCoordinator(new TrackingApiStub(), hub); await coordinator.StartAsync(Guid.NewGuid(), default); await coordinator.StopAsync(default); Assert.True(hub.Stopped);
    }

    [Fact]
    public async Task Push_rotation_registers_replacement_then_unregisters_old_id()
    {
        var notifications = new NotificationStub(); var source = new PushSource(); var coordinator = new PushRegistrationCoordinator(notifications, source);
        await coordinator.RegisterAsync(default); source.Rotate("token-2"); await Task.Delay(30);
        Assert.Equal(["token-1", "token-2"], notifications.Tokens); Assert.Single(notifications.Removed);
    }

    private sealed class TrackingApiStub : ITrackingApi
    {
        public int Calls { get; private set; }
        public Task<DriverLocationResponse> LatestAsync(Guid orderId, CancellationToken ct) { Calls++; return Task.FromResult(new DriverLocationResponse(Guid.NewGuid(), Guid.NewGuid(), 31.9, 35.2, DateTime.UtcNow, DateTime.UtcNow, 5, null, null, 1)); }
    }
    private sealed class HubStub : ITrackingHubClient
    {
        public ConnectionState State { get; private set; }
        public bool Started { get; private set; }
        public bool Stopped { get; private set; }
        public Guid Subscribed { get; private set; }
        public int SubscribeCount { get; private set; }
        public event EventHandler<TrackingRealtimePayload>? LocationUpdated; public event EventHandler<ConnectionState>? StateChanged; public event EventHandler? Reconnected;
        public Task StartAsync(CancellationToken ct) { Started = true; State = ConnectionState.Connected; StateChanged?.Invoke(this, State); return Task.CompletedTask; }
        public Task StopAsync(CancellationToken ct) { Stopped = true; return Task.CompletedTask; }
        public Task SubscribeOrderAsync(Guid orderId, CancellationToken ct) { Subscribed = orderId; SubscribeCount++; return Task.CompletedTask; }
        public void Publish(TrackingRealtimePayload value) => LocationUpdated?.Invoke(this, value); public void Reconnect() => Reconnected?.Invoke(this, EventArgs.Empty); public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
    private sealed class PushSource : IPushTokenSource
    {
        public short Platform => PushValues.Android; public short Provider => PushValues.Fcm; public bool IsConfigured => true; public event EventHandler<string>? TokenChanged;
        public Task<string?> GetTokenAsync(CancellationToken ct) => Task.FromResult<string?>("token-1"); public void Rotate(string token) => TokenChanged?.Invoke(this, token);
    }
    private sealed class NotificationStub : INotificationsApi
    {
        public List<string> Tokens { get; } = []; public List<Guid> Removed { get; } = [];
        public Task<DeviceTokenResponse> RegisterAsync(RegisterDeviceTokenRequest request, CancellationToken ct) { Tokens.Add(request.Token); return Task.FromResult(new DeviceTokenResponse(Guid.NewGuid(), request.Platform, request.Provider, "***", true, DateTime.UtcNow)); }
        public Task UnregisterAsync(Guid id, CancellationToken ct) { Removed.Add(id); return Task.CompletedTask; }
        public Task<NotificationListResponse> ListAsync(int page, CancellationToken ct) => throw new NotSupportedException(); public Task MarkAllReadAsync(CancellationToken ct) => throw new NotSupportedException(); public Task MarkReadAsync(Guid id, CancellationToken ct) => throw new NotSupportedException(); public Task<NotificationPreferencesResponse> PreferencesAsync(CancellationToken ct) => throw new NotSupportedException(); public Task<NotificationPreferencesResponse> UpdatePreferencesAsync(UpdateNotificationPreferencesRequest request, CancellationToken ct) => throw new NotSupportedException();
    }
}
