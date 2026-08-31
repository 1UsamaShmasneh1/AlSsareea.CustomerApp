using AlSsareea.CustomerApp.Core;

namespace AlSsareea.CustomerApp.UnitTests;

public sealed class AddressViewModelTests
{
    [Fact] public async Task Add_maps_explicit_customer_fields_and_geocoded_location() { var api = new CustomerStub(); var maps = new MapsStub(); var vm = Create(api, maps); await vm.LoadAsync(); bool saved = await vm.SaveAsync(null, Request(null)); Assert.True(saved); Assert.Equal("City", api.LastRequest!.City); Assert.Equal("Street", api.LastRequest.Street); Assert.Equal(31.9, api.LastRequest.Latitude); Assert.False(api.LastRequest.IsDefault); }
    [Fact] public async Task Edit_uses_existing_id_and_concurrency_stamp() { var api = new CustomerStub(); var vm = Create(api, new MapsStub()); await vm.LoadAsync(); AddressResponse existing = Assert.Single(vm.Items); await vm.SaveAsync(existing, Request(null) with { City = "New city", ConcurrencyStamp = null }); Assert.Equal(existing.Id, api.UpdatedId); Assert.Equal(existing.ConcurrencyStamp, api.LastRequest!.ConcurrencyStamp); Assert.Equal("New city", vm.Items[0].City); }
    [Fact] public async Task Invalid_explicit_fields_do_not_call_customer_api() { var api = new CustomerStub(); var vm = Create(api, new MapsStub()); bool saved = await vm.SaveAsync(null, Request(null) with { City = "" }); Assert.False(saved); Assert.Equal(0, api.Mutations); Assert.Equal("ErrorValidation", vm.ErrorMessage); }
    [Fact] public async Task Ineligible_changed_location_is_rejected() { var api = new CustomerStub(); var vm = Create(api, new MapsStub { Eligible = false }); bool saved = await vm.SaveAsync(null, Request(null)); Assert.False(saved); Assert.Equal(0, api.Mutations); Assert.Equal("AddressOutsideArea", vm.ErrorMessage); }
    [Fact] public async Task Address_without_coordinates_relies_on_explicit_fields_without_fabrication() { var api = new CustomerStub(); var maps = new MapsStub(); var vm = Create(api, maps); await vm.SaveAsync(null, Request(null) with { Latitude = null, Longitude = null, PlaceId = null }); Assert.Equal(0, maps.EligibilityCalls); Assert.Null(api.LastRequest!.Latitude); }
    [Fact] public async Task Delete_removes_owned_address() { var api = new CustomerStub(); var vm = Create(api, new MapsStub()); await vm.LoadAsync(); await vm.DeleteAsync(vm.Items[0]); Assert.Empty(vm.Items); Assert.Equal(RemoteStateKind.Empty, vm.State); }
    [Fact] public async Task Set_default_clears_previous_default_flags() { var api = new CustomerStub(twoAddresses: true); var vm = Create(api, new MapsStub()); await vm.LoadAsync(); await vm.SetDefaultAsync(vm.Items[1]); Assert.False(vm.Items[0].IsDefault); Assert.True(vm.Items[1].IsDefault); }
    private static AddressesViewModel Create(CustomerStub api, MapsStub maps) => new(api, maps, new OnlineConnectivity(), new TestText());
    private static AddressRequest Request(Guid? stamp) => new("Home", 1, "City", "Area", "Street", "4", "2", "8", null, "place", 31.9, 35.2, "Call", false, stamp);

    private sealed class CustomerStub(bool twoAddresses = false) : ICustomerApi
    {
        private readonly List<AddressResponse> values = twoAddresses ? [Address(true), Address(false)] : [Address(true)]; public AddressRequest? LastRequest { get; private set; }
        public Guid? UpdatedId { get; private set; }
        public int Mutations { get; private set; }
        public Task<IReadOnlyList<AddressResponse>> AddressesAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<AddressResponse>>(values.ToArray());
        public Task<AddressResponse> AddAddressAsync(AddressRequest request, CancellationToken ct) { LastRequest = request; Mutations++; AddressResponse value = From(Guid.NewGuid(), request); values.Add(value); return Task.FromResult(value); }
        public Task<AddressResponse> UpdateAddressAsync(Guid id, AddressRequest request, CancellationToken ct) { LastRequest = request; UpdatedId = id; Mutations++; AddressResponse value = From(id, request); int index = values.FindIndex(x => x.Id == id); values[index] = value; return Task.FromResult(value); }
        public Task DeleteAddressAsync(Guid id, Guid concurrencyStamp, CancellationToken ct) { values.RemoveAll(x => x.Id == id); return Task.CompletedTask; }
        public Task<AddressResponse> SetDefaultAddressAsync(Guid id, Guid concurrencyStamp, CancellationToken ct) { for (int i = 0; i < values.Count; i++) values[i] = values[i] with { IsDefault = values[i].Id == id }; return Task.FromResult(values.Single(x => x.Id == id)); }
        private static AddressResponse Address(bool isDefault) => new(Guid.NewGuid(), isDefault ? "Home" : "Work", 1, "City", null, "Street", null, null, null, null, null, 31.9, 35.2, null, isDefault, DateTime.UtcNow, DateTime.UtcNow, Guid.NewGuid());
        private static AddressResponse From(Guid id, AddressRequest x) => new(id, x.Label, x.AddressType, x.City, x.Area, x.Street, x.BuildingNumber, x.Floor, x.Apartment, x.PostalCode, x.PlaceId, x.Latitude, x.Longitude, x.DeliveryInstructions, x.IsDefault, DateTime.UtcNow, DateTime.UtcNow, Guid.NewGuid());
        public Task<CustomerResponse> GetAsync(CancellationToken ct) => throw new NotSupportedException(); public Task<CustomerResponse> UpdateAsync(UpdateCustomerRequest request, CancellationToken ct) => throw new NotSupportedException(); public Task<CustomerPreferencesResponse> PreferencesAsync(CancellationToken ct) => throw new NotSupportedException(); public Task<CustomerPreferencesResponse> UpdatePreferencesAsync(UpdateCustomerPreferencesRequest request, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class MapsStub : IMapsApi { public bool Eligible { get; init; } = true; public int EligibilityCalls { get; private set; } public Task<DeliveryEligibilityResponse> EligibilityAsync(DeliveryEligibilityRequest request, CancellationToken ct) { EligibilityCalls++; return Task.FromResult(new DeliveryEligibilityResponse(Eligible, Eligible ? Guid.NewGuid() : null, Eligible ? null : "outside")); } public Task<IReadOnlyList<GeocodingResult>> GeocodeAsync(GeocodingRequest request, CancellationToken ct) => Task.FromResult<IReadOnlyList<GeocodingResult>>([]); public Task<ReverseGeocodingResult> ReverseGeocodeAsync(ReverseGeocodingRequest request, CancellationToken ct) => Task.FromResult(new ReverseGeocodingResult("Normalized", request.Latitude, request.Longitude, "place")); }
}

public sealed class OrderViewModelTests
{
    [Fact] public async Task List_success_exposes_orders() { var api = new OrdersStub(); var vm = new OrdersViewModel(api, new OnlineConnectivity(), new TestText(), new TestNavigation()); await vm.LoadAsync(); Assert.Single(vm.Items); Assert.Equal(RemoteStateKind.Content, vm.State); }
    [Fact] public async Task List_empty_exposes_empty_state() { var api = new OrdersStub { Empty = true }; var vm = new OrdersViewModel(api, new OnlineConnectivity(), new TestText(), new TestNavigation()); await vm.LoadAsync(); Assert.Equal(RemoteStateKind.Empty, vm.State); }
    [Fact] public async Task List_error_maps_problem() { var api = new OrdersStub { FailList = true }; var vm = new OrdersViewModel(api, new OnlineConnectivity(), new TestText(), new TestNavigation()); await vm.LoadAsync(); Assert.Equal(RemoteStateKind.Error, vm.State); Assert.Equal("ErrorUnavailable", vm.ErrorMessage); }
    [Fact] public async Task Details_exposes_items_timeline_and_cancel_capability() { var vm = Details(new OrdersStub(status: 4), out _); await vm.LoadAsync(Guid.NewGuid()); Assert.Single(vm.Items); Assert.Equal(2, vm.Timeline.Count); Assert.True(vm.CanCancel); Assert.False(vm.CanTrack); }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    public void Cancellation_capability_matches_backend_state_machine(short status) => Assert.True(OrderCapabilities.CanCancel(status));

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(19)]
    public void Cancellation_capability_rejects_non_cancellable_states(short status) => Assert.False(OrderCapabilities.CanCancel(status));

    [Theory]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public void Tracking_capability_matches_backend_customer_visibility(short status) => Assert.True(OrderCapabilities.CanTrack(status));

    [Theory]
    [InlineData(9)]
    [InlineData(11)]
    [InlineData(15)]
    [InlineData(16)]
    public void Tracking_capability_rejects_hidden_delivery_states(short status) => Assert.False(OrderCapabilities.CanTrack(status));
    [Fact] public async Task Track_navigation_only_occurs_for_trackable_status() { var vm = Details(new OrdersStub(status: 12), out TestNavigation navigation); await vm.LoadAsync(Guid.NewGuid()); await vm.TrackAsync(); Assert.Equal(AppRoutes.Tracking, Assert.Single(navigation.Visits).Route); }
    [Fact] public async Task Cancel_allowed_refreshes_status() { var api = new OrdersStub(status: 4); var vm = Details(api, out _); await vm.LoadAsync(Guid.NewGuid()); await vm.CancelAsync("reason"); Assert.Equal(16, vm.Order!.Status); Assert.False(vm.CanCancel); }
    [Fact] public async Task Cancel_failure_is_presented_without_weakening_state() { var api = new OrdersStub(status: 4) { FailCancel = true }; var vm = Details(api, out _); await vm.LoadAsync(Guid.NewGuid()); await vm.CancelAsync("reason"); Assert.Equal(RemoteStateKind.Error, vm.State); Assert.Equal("ErrorConflict", vm.ErrorMessage); }
    private static OrderDetailsViewModel Details(OrdersStub api, out TestNavigation navigation) { navigation = new(); return new(api, new OnlineConnectivity(), new TestText(), navigation); }
    private sealed class OrdersStub(short status = 4) : IOrdersApi
    {
        public bool Empty { get; init; }
        public bool FailList { get; init; }
        public bool FailCancel { get; init; }
        public Task<OrderListResponse> ListAsync(int page, CancellationToken ct) => FailList ? throw Problem(503) : Task.FromResult(new OrderListResponse(Empty ? [] : [new(Guid.NewGuid(), "O1", 1, status, "ILS", 100, "Merchant", DateTime.UtcNow, null)], page, 20, Empty ? 0 : 1));
        public Task<OrderDetailsResponse> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(DetailsResponse(id, status)); public Task<OrderDetailsResponse> CancelAsync(Guid id, CancelOrderRequest request, CancellationToken ct) => FailCancel ? throw Problem(409) : Task.FromResult(DetailsResponse(id, 16)); public Task<CreateOrderResponse> CreateAsync(CreateOrderRequest request, string stableKey, CancellationToken ct) => throw new NotSupportedException();
        private static ApiException Problem(int statusCode) => new(new(statusCode, "Problem", null, null, new Dictionary<string, string[]>()));
        private static OrderDetailsResponse DetailsResponse(Guid id, short value) { DateTime now = DateTime.UtcNow; var item = new OrderItemResponse(Guid.NewGuid(), Guid.NewGuid(), 1, null, "Tea", null, null, 2, 50, 0, 0, 50, 100, 0, 100, null, []); var timeline = new[] { new OrderTimelineEntryResponse(Guid.NewGuid(), null, 1, now.AddMinutes(-2), 1, null, null, null), new OrderTimelineEntryResponse(Guid.NewGuid(), 1, value, now, 1, null, "Updated", null) }; return new(id, "O1", Guid.NewGuid(), 1, value, "ILS", 100, null, null, null, null, null, now, now, Guid.NewGuid(), new(Guid.NewGuid(), "Customer", "en"), new(Guid.NewGuid(), "Home", "City", null, "Street", null, null, null, null, 31, 35, null, null), new(Guid.NewGuid(), null, "Merchant", null, null, null), new(100, 0, 0, 0, 0, 0, 0, 0, 0, 0, 100, "ILS", null, now), [item], timeline); }
    }
}

public sealed class NotificationViewModelTests
{
    [Fact] public async Task Inbox_and_preferences_load_together() { var api = new NotificationsStub(); var vm = Create(api); await vm.LoadAsync(); Assert.Single(vm.Items); Assert.Single(vm.Preferences); Assert.Equal(1, vm.UnreadCount); }
    [Fact] public async Task Pagination_appends_items() { var api = new NotificationsStub { Total = 2 }; var vm = Create(api); await vm.LoadAsync(); await vm.LoadMoreAsync(); Assert.Equal(2, vm.Items.Count); }
    [Fact] public async Task Mark_read_updates_unread_count() { var api = new NotificationsStub(); var vm = Create(api); await vm.LoadAsync(); await vm.MarkReadAsync(vm.Items[0]); Assert.Equal(0, vm.UnreadCount); Assert.NotNull(vm.Items[0].ReadAtUtc); }
    [Fact] public async Task Mark_all_read_updates_all_items() { var api = new NotificationsStub(); var vm = Create(api); await vm.LoadAsync(); await vm.MarkAllReadAsync(); Assert.All(vm.Items, item => Assert.NotNull(item.ReadAtUtc)); }
    [Fact] public async Task Preference_save_uses_complete_updated_set() { var api = new NotificationsStub(); var vm = Create(api); await vm.LoadAsync(); await vm.SetPreferenceAsync(vm.Preferences[0], false); Assert.False(Assert.Single(api.SavedPreferences!).Enabled); }
    [Fact] public async Task Inbox_error_is_presented() { var api = new NotificationsStub { Fail = true }; var vm = Create(api); await vm.LoadAsync(); Assert.Equal(RemoteStateKind.Error, vm.State); }
    private static NotificationsViewModel Create(NotificationsStub api) => new(api, new OnlineConnectivity(), new TestText(), new TestNavigation());
    private sealed class NotificationsStub : INotificationsApi
    {
        public int Total { get; init; } = 1; public bool Fail { get; init; }
        public IReadOnlyList<NotificationPreferenceItem>? SavedPreferences { get; private set; }
        public Task<NotificationListResponse> ListAsync(int page, CancellationToken ct) { if (Fail) throw new ApiNetworkException("network", new HttpRequestException()); NotificationListItem item = new(Guid.NewGuid(), "orders", $"order:{Guid.NewGuid()}", 1, "en", "Update", "Body", 1, DateTime.UtcNow, null); return Task.FromResult(new NotificationListResponse([item], page, 20, Total, 1)); }
        public Task<NotificationPreferencesResponse> PreferencesAsync(CancellationToken ct) => Task.FromResult(new NotificationPreferencesResponse([new("orders", 1, true)])); public Task MarkReadAsync(Guid id, CancellationToken ct) => Task.CompletedTask; public Task MarkAllReadAsync(CancellationToken ct) => Task.CompletedTask; public Task<NotificationPreferencesResponse> UpdatePreferencesAsync(UpdateNotificationPreferencesRequest request, CancellationToken ct) { SavedPreferences = request.Items; return Task.FromResult(new NotificationPreferencesResponse(request.Items)); }
        public Task<DeviceTokenResponse> RegisterAsync(RegisterDeviceTokenRequest request, CancellationToken ct) => throw new NotSupportedException(); public Task UnregisterAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
    }
}
