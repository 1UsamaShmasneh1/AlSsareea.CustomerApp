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
        var notifications = new NotificationStub(); var source = new PushSource(); var coordinator = new PushRegistrationCoordinator(notifications, source, new RegistrationStoreStub());
        await coordinator.RegisterAsync(default); source.Rotate("token-2"); await Task.Delay(30);
        Assert.Equal(["token-1", "token-2"], notifications.Tokens); Assert.Single(notifications.Removed);
    }

    [Fact]
    public async Task Push_initial_registration_is_persisted()
    {
        var store = new RegistrationStoreStub(); var coordinator = new PushRegistrationCoordinator(new NotificationStub(), new PushSource(), store);
        await coordinator.RegisterAsync(default); Assert.NotNull(store.Value);
    }

    [Fact]
    public async Task Failed_replacement_keeps_previous_registration()
    {
        var notifications = new NotificationStub(); var source = new PushSource(); var store = new RegistrationStoreStub(); var coordinator = new PushRegistrationCoordinator(notifications, source, store); await coordinator.RegisterAsync(default); Guid previous = store.Value!.Value; notifications.FailNext = true; source.Rotate("token-2"); await Task.Delay(30); Assert.Equal(previous, store.Value); Assert.Empty(notifications.Removed);
    }

    [Theory]
    [InlineData("orders", "orderId", AppRoutes.OrderDetails)]
    [InlineData("tracking", "orderId", AppRoutes.Tracking)]
    [InlineData("notifications", "notificationId", AppRoutes.Notifications)]
    public void Push_payload_maps_only_supported_destinations(string destination, string idKey, string route)
    {
        Guid id = Guid.NewGuid(); DeepLinkDestination parsed = Assert.IsType<DeepLinkDestination>(PushPayloadParser.Parse(new Dictionary<string, string> { ["destination"] = destination, [idKey] = id.ToString() })); Assert.Equal(route, parsed.Route); Assert.Equal(id, parsed.Id);
    }

    [Fact] public void Malformed_push_payload_is_rejected() => Assert.Null(PushPayloadParser.Parse(new Dictionary<string, string> { ["destination"] = "admin", ["orderId"] = "bad" }));

    [Fact]
    public async Task Authenticated_push_dispatches_through_central_navigation()
    {
        var navigation = new TestNavigation(); var dispatcher = new PushMessageDispatcher(new SessionStub(), navigation); bool dispatched = await dispatcher.DispatchAsync(new Dictionary<string, string> { ["deepLink"] = $"alssareea://tracking/{Guid.NewGuid()}" }); Assert.True(dispatched); Assert.Equal(AppRoutes.Tracking, Assert.Single(navigation.Visits).Route);
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
    private sealed class RegistrationStoreStub : IPushRegistrationStore
    {
        private Guid? value;
        public Guid? Value => value;
        public Task<Guid?> GetAsync(CancellationToken ct) => Task.FromResult(value);
        public Task SetAsync(Guid id, CancellationToken ct) { value = id; return Task.CompletedTask; }
        public Task ClearAsync(CancellationToken ct) { value = null; return Task.CompletedTask; }
    }
    private sealed class NotificationStub : INotificationsApi
    {
        public List<string> Tokens { get; } = []; public List<Guid> Removed { get; } = []; public bool FailNext { get; set; }
        public Task<DeviceTokenResponse> RegisterAsync(RegisterDeviceTokenRequest request, CancellationToken ct) { if (FailNext) { FailNext = false; throw new ApiNetworkException("network", new HttpRequestException()); } Tokens.Add(request.Token); return Task.FromResult(new DeviceTokenResponse(Guid.NewGuid(), request.Platform, request.Provider, "***", true, DateTime.UtcNow)); }
        public Task UnregisterAsync(Guid id, CancellationToken ct) { Removed.Add(id); return Task.CompletedTask; }
        public Task<NotificationListResponse> ListAsync(int page, CancellationToken ct) => throw new NotSupportedException(); public Task MarkAllReadAsync(CancellationToken ct) => throw new NotSupportedException(); public Task MarkReadAsync(Guid id, CancellationToken ct) => throw new NotSupportedException(); public Task<NotificationPreferencesResponse> PreferencesAsync(CancellationToken ct) => throw new NotSupportedException(); public Task<NotificationPreferencesResponse> UpdatePreferencesAsync(UpdateNotificationPreferencesRequest request, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class SessionStub : ISessionManager
    {
        public string? AccessToken => "token"; public DateTime? AccessTokenExpiresUtc => DateTime.UtcNow.AddMinutes(5); public Guid? UserId => Guid.NewGuid(); public bool IsAuthenticated => true; public Task<bool> RestoreAsync(CancellationToken ct) => Task.FromResult(true); public Task SetAsync(TokenResponse tokens, string deviceIdentifier, CancellationToken ct) => Task.CompletedTask; public Task<bool> RefreshAsync(string? failedAccessToken, CancellationToken ct) => Task.FromResult(true); public Task ClearAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
