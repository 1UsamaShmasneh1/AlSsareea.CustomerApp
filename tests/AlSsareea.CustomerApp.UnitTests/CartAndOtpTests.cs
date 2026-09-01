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
    [Fact]
    public async Task Request_stores_challenge_and_resend_time()
    {
        var auth = new AuthStub();
        LoginViewModel vm = Create(auth);

        await RequestAsync(vm);

        Assert.Equal(auth.ChallengeId, vm.ChallengeId);
        Assert.Equal(auth.NextResendUtc, vm.NextOtpRequestUtc);
    }

    [Fact]
    public async Task Development_code_populates_input_in_development()
    {
        var auth = new AuthStub { DevelopmentCode = "123456" };
        LoginViewModel vm = Create(auth, development: true);

        await RequestAsync(vm);

        Assert.Equal("123456", vm.OtpCode);
        Assert.Equal("DevelopmentOtpGenerated", vm.OtpStatusMessage);
        Assert.True(vm.HasOtpStatus);
    }

    [Fact]
    public async Task Null_development_code_leaves_input_empty()
    {
        LoginViewModel vm = Create(new AuthStub(), development: true);
        vm.OtpCode = "old-code";

        await RequestAsync(vm);

        Assert.Empty(vm.OtpCode);
        Assert.Equal("OtpRequested", vm.OtpStatusMessage);
    }

    [Fact]
    public async Task Production_ignores_development_code()
    {
        LoginViewModel vm = Create(new AuthStub { DevelopmentCode = "123456" }, development: false);

        await RequestAsync(vm);

        Assert.Empty(vm.OtpCode);
        Assert.Equal("OtpRequested", vm.OtpStatusMessage);
    }

    [Fact]
    public async Task Request_does_not_persist_otp_or_session()
    {
        var session = new SessionStub();
        LoginViewModel vm = Create(new AuthStub { DevelopmentCode = "123456" }, session: session);

        await RequestAsync(vm);

        Assert.Equal(0, session.SetCalls);
    }

    [Fact]
    public async Task Request_error_maps_correctly()
    {
        LoginViewModel vm = Create(new AuthStub { RequestStatus = 503 });

        await RequestAsync(vm);

        Assert.Equal("ErrorUnavailable", vm.ErrorMessage);
        Assert.False(vm.HasOtpStatus);
    }

    [Fact]
    public async Task Resend_blocked_maps_specific_localized_error()
    {
        LoginViewModel vm = Create(new AuthStub { RequestStatus = 429, RequestCode = "auth.otp_resend_blocked" });

        await RequestAsync(vm);

        Assert.Equal("ErrorOtpResendBlocked", vm.ErrorMessage);
    }

    [Fact]
    public async Task Verify_uses_challenge_and_device_identifier()
    {
        var auth = new AuthStub { DevelopmentCode = "123456" };
        LoginViewModel vm = Create(auth);
        vm.DeviceIdentifier = "test-device";
        await RequestAsync(vm);

        await vm.VerifyOtpAsync();

        Assert.Equal(auth.ChallengeId, auth.VerifyChallengeId);
        Assert.Equal("123456", auth.VerifyRequest!.Code);
        Assert.Equal("test-device", auth.VerifyRequest.DeviceIdentifier);
    }

    [Fact]
    public async Task Concurrent_request_taps_issue_one_request()
    {
        var auth = new AuthStub { DelayRequest = true };
        LoginViewModel vm = Create(auth);
        vm.Identifier = "user@example.test";

        Task first = vm.RequestOtpAsync();
        Task second = vm.RequestOtpAsync();
        await Task.Delay(20);
        auth.ReleaseRequest();
        await Task.WhenAll(first, second);

        Assert.Equal(1, auth.RequestCalls);
    }

    [Fact] public async Task Invalid_code_maps_to_localized_validation() { LoginViewModel vm = Create(new AuthStub { VerifyStatus = 400 }); await RequestAsync(vm); vm.OtpCode = "bad"; await vm.VerifyOtpAsync(); Assert.Equal("ErrorValidation", vm.ErrorMessage); }
    [Fact] public async Task Expired_code_maps_to_expired_message() { LoginViewModel vm = Create(new AuthStub { VerifyStatus = 410 }); await RequestAsync(vm); vm.OtpCode = "123"; await vm.VerifyOtpAsync(); Assert.Equal("ErrorOtpExpired", vm.ErrorMessage); }
    [Fact] public async Task Throttled_request_maps_rate_limit() { LoginViewModel vm = Create(new AuthStub { RequestStatus = 429 }); await RequestAsync(vm); Assert.Equal("ErrorRateLimit", vm.ErrorMessage); }
    [Fact] public async Task Offline_request_does_not_call_backend() { var auth = new AuthStub(); LoginViewModel vm = Create(auth, online: false); await RequestAsync(vm); Assert.Equal(0, auth.RequestCalls); Assert.Equal(RemoteStateKind.Offline, vm.State); }

    private static async Task RequestAsync(LoginViewModel vm) { vm.Identifier = "user@example.test"; await vm.RequestOtpAsync(); }
    private static LoginViewModel Create(AuthStub auth, bool online = true, bool development = true, SessionStub? session = null) => new(auth, session ?? new(), new OnlineConnectivity(online), new TestText(), new TestNavigation(), new ClientRuntimeEnvironment(development));

    private sealed class AuthStub : IAuthenticationApi
    {
        private readonly TaskCompletionSource requestGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int? RequestStatus { get; init; }
        public string? RequestCode { get; init; }
        public int? VerifyStatus { get; init; }
        public string? DevelopmentCode { get; init; }
        public bool DelayRequest { get; init; }
        public int RequestCalls { get; private set; }
        public Guid ChallengeId { get; } = Guid.NewGuid();
        public DateTime NextResendUtc { get; } = DateTime.UtcNow.AddMinutes(1);
        public Guid? VerifyChallengeId { get; private set; }
        public OtpVerifyRequest? VerifyRequest { get; private set; }
        public async Task<OtpChallengeResponse> RequestOtpAsync(OtpChallengeRequest request, string idempotencyKey, CancellationToken ct) { RequestCalls++; if (DelayRequest) await requestGate.Task.WaitAsync(ct); if (RequestStatus.HasValue) throw Problem(RequestStatus.Value, RequestCode); return new(ChallengeId, DateTime.UtcNow.AddMinutes(5), NextResendUtc, DevelopmentCode); }
        public Task VerifyOtpAsync(Guid challengeId, OtpVerifyRequest request, CancellationToken ct) { VerifyChallengeId = challengeId; VerifyRequest = request; return VerifyStatus.HasValue ? Task.FromException(Problem(VerifyStatus.Value)) : Task.CompletedTask; }
        public void ReleaseRequest() => requestGate.TrySetResult();
        private static ApiException Problem(int status, string? code = null) => new(new(status, "Problem", null, code, new Dictionary<string, string[]>())); public Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct) => throw new NotSupportedException(); public Task<TokenResponse> RefreshAsync(RefreshRequest request, CancellationToken ct) => throw new NotSupportedException(); public Task LogoutAsync(string idempotencyKey, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class SessionStub : ISessionManager { public int SetCalls { get; private set; } public string? AccessToken => null; public DateTime? AccessTokenExpiresUtc => null; public Guid? UserId => null; public bool IsAuthenticated => false; public Task<bool> RestoreAsync(CancellationToken ct) => Task.FromResult(false); public Task SetAsync(TokenResponse tokens, string deviceIdentifier, CancellationToken ct) { SetCalls++; return Task.CompletedTask; } public Task<bool> RefreshAsync(string? failedAccessToken, CancellationToken ct) => Task.FromResult(false); public Task ClearAsync(CancellationToken ct) => Task.CompletedTask; }
}
