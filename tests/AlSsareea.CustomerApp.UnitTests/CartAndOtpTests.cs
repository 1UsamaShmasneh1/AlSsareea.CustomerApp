using AlSsareea.CustomerApp.Core;

namespace AlSsareea.CustomerApp.UnitTests;

public sealed class CartViewModelTests
{
    [Fact] public async Task Load_fetches_active_cart_and_summary() { Fixture f = new(); await f.ViewModel.LoadAsync(); Assert.Single(f.ViewModel.Items); Assert.NotNull(f.ViewModel.Summary); Assert.Equal(RemoteStateKind.Content, f.ViewModel.State); }
    [Fact] public async Task Quantity_update_uses_latest_concurrency() { Fixture f = new(); await f.ViewModel.LoadAsync(); CartItemResponse item = f.ViewModel.Items[0]; await f.ViewModel.ChangeQuantityAsync(item, 3); Assert.Equal(3, f.Api.LastQuantity); Assert.Equal(f.Api.InitialStamp, f.Api.LastStamp); }
    [Fact] public async Task Remove_updates_empty_state() { Fixture f = new(); await f.ViewModel.LoadAsync(); await f.ViewModel.RemoveAsync(f.ViewModel.Items[0]); Assert.Empty(f.ViewModel.Items); Assert.Equal(RemoteStateKind.Empty, f.ViewModel.State); }
    [Fact] public async Task Coupon_apply_and_remove_use_mutations() { Fixture f = new(); await f.ViewModel.LoadAsync(); await f.ViewModel.ApplyCouponAsync(" SAVE "); await f.ViewModel.RemoveCouponAsync(); Assert.Equal(" SAVE ", f.Api.Coupon); Assert.True(f.Api.CouponRemoved); }
    [Fact] public async Task Mutation_failure_is_presented_and_does_not_replace_cart() { Fixture f = new(failMutation: true); await f.ViewModel.LoadAsync(); CartResponse before = f.ViewModel.Cart!; await f.ViewModel.ChangeQuantityAsync(f.ViewModel.Items[0], 2); Assert.Same(before, f.ViewModel.Cart); Assert.Equal(RemoteStateKind.Error, f.ViewModel.State); }
    [Fact] public async Task Duplicate_cart_mutation_taps_issue_one_write() { Fixture f = new(delay: true); await f.ViewModel.LoadAsync(); Task first = f.ViewModel.ChangeQuantityAsync(f.ViewModel.Items[0], 2); Task second = f.ViewModel.ChangeQuantityAsync(f.ViewModel.Items[0], 3); await Task.Delay(20); f.Api.Release(); await Task.WhenAll(first, second); Assert.Equal(1, f.Api.UpdateCalls); }
    private sealed class Fixture
    {
        public Fixture(bool failMutation = false, bool delay = false) { Api = new() { FailMutation = failMutation, Delay = delay }; var state = new CustomerAppState { MerchantId = Guid.NewGuid() }; ViewModel = new(Api, state, new OnlineConnectivity(), new TestText(), new TestNavigation()); }
        public CartStub Api { get; }
        public CartViewModel ViewModel { get; }
    }
    private sealed class CartStub : ICartApi
    {
        private readonly TaskCompletionSource gate = new(); private readonly Guid cartId = Guid.NewGuid(); private readonly Guid itemId = Guid.NewGuid(); public Guid InitialStamp { get; } = Guid.NewGuid(); public int? LastQuantity { get; private set; }
        public Guid? LastStamp { get; private set; }
        public string? Coupon { get; private set; }
        public bool CouponRemoved { get; private set; }
        public bool FailMutation { get; init; }
        public bool Delay { get; init; }
        public int UpdateCalls { get; private set; }
        public Task<CartResponse> ActiveAsync(Guid merchantId, Guid? branchId, CancellationToken ct) => Task.FromResult(Cart([Item(1)], InitialStamp));
        public async Task<CartResponse> UpdateQuantityAsync(Guid id, Guid item, UpdateCartItemQuantityRequest request, string key, CancellationToken ct) { UpdateCalls++; LastQuantity = request.Quantity; LastStamp = request.ConcurrencyStamp; if (Delay) await gate.Task; if (FailMutation) throw new ApiException(new(409, "Conflict", null, null, new Dictionary<string, string[]>())); return Cart([Item(request.Quantity)], Guid.NewGuid()); }
        public Task<CartResponse> RemoveAsync(Guid id, Guid item, Guid stamp, string key, CancellationToken ct) => Task.FromResult(Cart([], Guid.NewGuid())); public Task<CartResponse> ApplyCouponAsync(Guid id, ApplyCartCouponRequest request, string key, CancellationToken ct) { Coupon = request.CouponCode; return Task.FromResult(Cart([Item(1)], Guid.NewGuid()) with { CouponCode = request.CouponCode }); }
        public Task<CartResponse> RemoveCouponAsync(Guid id, Guid stamp, string key, CancellationToken ct) { CouponRemoved = true; return Task.FromResult(Cart([Item(1)], Guid.NewGuid())); }
        public Task<CartCheckoutSummaryResponse> SummaryAsync(Guid id, CancellationToken ct) => Task.FromResult(new CartCheckoutSummaryResponse(id, Guid.NewGuid(), Guid.NewGuid(), null, 1, "ILS", [], 100, 0, 0, 0, 0, 0, 100, [], true, null, null, DateTime.UtcNow, Guid.NewGuid(), DateTime.UtcNow.AddMinutes(5))); public Task<CartCheckoutSummaryResponse> RepriceAsync(Guid cartId, CancellationToken ct) => SummaryAsync(cartId, ct);
        private CartItemResponse Item(int quantity) => new(itemId, Guid.NewGuid(), null, quantity, null, 1, [], DateTime.UtcNow, DateTime.UtcNow); private CartResponse Cart(IReadOnlyList<CartItemResponse> items, Guid stamp) => new(cartId, Guid.NewGuid(), Guid.NewGuid(), null, 1, null, DateTime.UtcNow.AddMinutes(5), null, DateTime.UtcNow, DateTime.UtcNow, stamp, items);
        public void Release() => gate.TrySetResult(); public Task<CartResponse> CreateAsync(GetOrCreateActiveCartRequest request, string key, CancellationToken ct) => throw new NotSupportedException(); public Task<CartResponse> AddAsync(Guid cartId, AddCartItemRequest request, string key, CancellationToken ct) => throw new NotSupportedException(); public Task<CartResponse> ClearAsync(Guid cartId, Guid concurrencyStamp, string key, CancellationToken ct) => Task.FromResult(Cart([], Guid.NewGuid()));
    }
}

public sealed class OtpViewModelTests
{
    [Fact] public async Task Invalid_code_maps_to_localized_validation() { LoginViewModel vm = Create(new AuthStub { VerifyStatus = 400 }); vm.Identifier = "user"; await vm.RequestOtpAsync(); vm.OtpCode = "bad"; await vm.VerifyOtpAsync(); Assert.Equal("ErrorValidation", vm.ErrorMessage); }
    [Fact] public async Task Expired_code_maps_to_expired_message() { LoginViewModel vm = Create(new AuthStub { VerifyStatus = 410 }); vm.Identifier = "user"; await vm.RequestOtpAsync(); vm.OtpCode = "123"; await vm.VerifyOtpAsync(); Assert.Equal("ErrorOtpExpired", vm.ErrorMessage); }
    [Fact] public async Task Throttled_request_maps_rate_limit() { LoginViewModel vm = Create(new AuthStub { RequestStatus = 429 }); vm.Identifier = "user"; await vm.RequestOtpAsync(); Assert.Equal("ErrorRateLimit", vm.ErrorMessage); }
    [Fact] public async Task Offline_request_does_not_call_backend() { var auth = new AuthStub(); LoginViewModel vm = Create(auth, false); vm.Identifier = "user"; await vm.RequestOtpAsync(); Assert.Equal(0, auth.RequestCalls); Assert.Equal(RemoteStateKind.Offline, vm.State); }
    private static LoginViewModel Create(AuthStub auth, bool online = true) => new(auth, new SessionStub(), new OnlineConnectivity(online), new TestText(), new TestNavigation());
    private sealed class AuthStub : IAuthenticationApi
    {
        public int? RequestStatus { get; init; }
        public int? VerifyStatus { get; init; }
        public int RequestCalls { get; private set; }
        public Task<OtpChallengeResponse> RequestOtpAsync(OtpChallengeRequest request, string idempotencyKey, CancellationToken ct) { RequestCalls++; if (RequestStatus.HasValue) throw Problem(RequestStatus.Value); return Task.FromResult(new OtpChallengeResponse(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(5), DateTime.UtcNow, null)); }
        public Task VerifyOtpAsync(Guid challengeId, OtpVerifyRequest request, CancellationToken ct) => VerifyStatus.HasValue ? Task.FromException(Problem(VerifyStatus.Value)) : Task.CompletedTask;
        private static ApiException Problem(int status) => new(new(status, "Problem", null, null, new Dictionary<string, string[]>())); public Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct) => throw new NotSupportedException(); public Task<TokenResponse> RefreshAsync(RefreshRequest request, CancellationToken ct) => throw new NotSupportedException(); public Task LogoutAsync(string idempotencyKey, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class SessionStub : ISessionManager { public string? AccessToken => null; public DateTime? AccessTokenExpiresUtc => null; public Guid? UserId => null; public bool IsAuthenticated => false; public Task<bool> RestoreAsync(CancellationToken ct) => Task.FromResult(false); public Task SetAsync(TokenResponse tokens, string deviceIdentifier, CancellationToken ct) => Task.CompletedTask; public Task<bool> RefreshAsync(string? failedAccessToken, CancellationToken ct) => Task.FromResult(false); public Task ClearAsync(CancellationToken ct) => Task.CompletedTask; }
}
