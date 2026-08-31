using AlSsareea.CustomerApp.Core;

namespace AlSsareea.CustomerApp.UnitTests;

public sealed class CheckoutTests
{
    [Fact]
    public async Task Submit_uses_selected_address_and_backend_eligibility()
    {
        var orders = new OrderStub(); CheckoutViewModel vm = Create(orders);
        await vm.LoadAsync(CartId); await vm.SubmitAsync();
        Assert.Equal(AddressId, orders.Request!.DeliveryAddressId); Assert.Single(orders.Keys); Assert.Equal(AppRoutes.OrderDetails, orders.Navigation.Visits[0].Route);
    }

    [Fact]
    public async Task Duplicate_submit_taps_create_one_request()
    {
        var orders = new OrderStub { Delay = true }; CheckoutViewModel vm = Create(orders); await vm.LoadAsync(CartId);
        Task first = vm.SubmitAsync(); Task second = vm.SubmitAsync(); await Task.Delay(20); orders.Release(); await Task.WhenAll(first, second);
        Assert.Single(orders.Keys);
    }

    [Fact]
    public async Task Uncertain_retry_reuses_same_idempotency_key()
    {
        var orders = new OrderStub { FailFirst = true }; CheckoutViewModel vm = Create(orders); await vm.LoadAsync(CartId);
        await vm.SubmitAsync(); Assert.Equal(RemoteStateKind.Error, vm.State); string first = Assert.Single(orders.Keys);
        await vm.SubmitAsync(); Assert.Equal(first, orders.Keys[1]);
    }

    private static readonly Guid CartId = Guid.NewGuid();
    private static readonly Guid AddressId = Guid.NewGuid();
    private static CheckoutViewModel Create(OrderStub orders)
    {
        orders.Navigation = new TestNavigation();
        return new(new CartStub(), new CustomerStub(), new MapsStub(), orders, new CustomerAppState(), new OnlineConnectivity(), new TestText(), orders.Navigation);
    }

    private sealed class CartStub : ICartApi
    {
        public Task<CartCheckoutSummaryResponse> RepriceAsync(Guid cartId, CancellationToken ct) => Task.FromResult(Summary());
        public Task<CartCheckoutSummaryResponse> SummaryAsync(Guid cartId, CancellationToken ct) => Task.FromResult(Summary());
        private static CartCheckoutSummaryResponse Summary() => new(CartId, Guid.NewGuid(), Guid.NewGuid(), null, 1, "ILS", [], 100, 10, 0, 0, 0, 0, 110, [], true, null, null, DateTime.UtcNow, Guid.NewGuid(), DateTime.UtcNow.AddMinutes(10));
        public Task<CartResponse> ActiveAsync(Guid merchantId, Guid? branchId, CancellationToken ct) => throw new NotSupportedException(); public Task<CartResponse> AddAsync(Guid cartId, AddCartItemRequest request, string key, CancellationToken ct) => throw new NotSupportedException(); public Task<CartResponse> ApplyCouponAsync(Guid cartId, ApplyCartCouponRequest request, string key, CancellationToken ct) => throw new NotSupportedException(); public Task<CartResponse> ClearAsync(Guid cartId, Guid concurrencyStamp, string key, CancellationToken ct) => throw new NotSupportedException(); public Task<CartResponse> CreateAsync(GetOrCreateActiveCartRequest request, string key, CancellationToken ct) => throw new NotSupportedException(); public Task<CartResponse> RemoveAsync(Guid cartId, Guid itemId, Guid concurrencyStamp, string key, CancellationToken ct) => throw new NotSupportedException(); public Task<CartResponse> RemoveCouponAsync(Guid cartId, Guid concurrencyStamp, string key, CancellationToken ct) => throw new NotSupportedException(); public Task<CartResponse> UpdateQuantityAsync(Guid cartId, Guid itemId, UpdateCartItemQuantityRequest request, string key, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class CustomerStub : ICustomerApi
    {
        public Task<IReadOnlyList<AddressResponse>> AddressesAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<AddressResponse>>([new(AddressId, "Home", 1, "City", null, "Street", null, null, null, null, null, 31.9, 35.2, null, true, DateTime.UtcNow, DateTime.UtcNow, Guid.NewGuid())]);
        public Task<AddressResponse> AddAddressAsync(AddressRequest request, CancellationToken ct) => throw new NotSupportedException(); public Task DeleteAddressAsync(Guid id, Guid concurrencyStamp, CancellationToken ct) => throw new NotSupportedException(); public Task<CustomerResponse> GetAsync(CancellationToken ct) => throw new NotSupportedException(); public Task<CustomerPreferencesResponse> PreferencesAsync(CancellationToken ct) => throw new NotSupportedException(); public Task<AddressResponse> SetDefaultAddressAsync(Guid id, Guid concurrencyStamp, CancellationToken ct) => throw new NotSupportedException(); public Task<AddressResponse> UpdateAddressAsync(Guid id, AddressRequest request, CancellationToken ct) => throw new NotSupportedException(); public Task<CustomerResponse> UpdateAsync(UpdateCustomerRequest request, CancellationToken ct) => throw new NotSupportedException(); public Task<CustomerPreferencesResponse> UpdatePreferencesAsync(UpdateCustomerPreferencesRequest request, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class MapsStub : IMapsApi
    {
        public Task<DeliveryEligibilityResponse> EligibilityAsync(DeliveryEligibilityRequest request, CancellationToken ct) => Task.FromResult(new DeliveryEligibilityResponse(true, Guid.NewGuid(), null));
        public Task<IReadOnlyList<GeocodingResult>> GeocodeAsync(GeocodingRequest request, CancellationToken ct) => throw new NotSupportedException(); public Task<ReverseGeocodingResult> ReverseGeocodeAsync(ReverseGeocodingRequest request, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class OrderStub : IOrdersApi
    {
        private readonly TaskCompletionSource gate = new(); public List<string> Keys { get; } = []; public CreateOrderRequest? Request { get; private set; }
        public bool FailFirst { get; init; }
        public bool Delay { get; init; }
        public TestNavigation Navigation { get; set; } = null!;
        public async Task<CreateOrderResponse> CreateAsync(CreateOrderRequest request, string stableKey, CancellationToken ct) { Request = request; Keys.Add(stableKey); if (Delay) await gate.Task; if (FailFirst && Keys.Count == 1) throw new ApiNetworkException("uncertain", new HttpRequestException()); return new(Guid.NewGuid(), "O-1", 4, "ILS", 110, DateTime.UtcNow); }
        public void Release() => gate.TrySetResult();
        public Task<OrderDetailsResponse> CancelAsync(Guid id, CancelOrderRequest request, CancellationToken ct) => throw new NotSupportedException(); public Task<OrderDetailsResponse> GetAsync(Guid id, CancellationToken ct) => throw new NotSupportedException(); public Task<OrderListResponse> ListAsync(int page, CancellationToken ct) => throw new NotSupportedException();
    }
}
