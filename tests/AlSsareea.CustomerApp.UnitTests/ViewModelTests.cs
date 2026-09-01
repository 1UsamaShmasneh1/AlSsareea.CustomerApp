using AlSsareea.CustomerApp.Core;

namespace AlSsareea.CustomerApp.UnitTests;

public sealed class ViewModelTests
{
    [Fact]
    public async Task Splash_first_launch_routes_to_onboarding()
    {
        var navigation = new TestNavigation(); var vm = new SplashViewModel(new TestPreferences(), new TestText(), new SessionStub(false), navigation);
        await vm.StartAsync(default);
        Assert.Equal(AppRoutes.Onboarding, Assert.Single(navigation.Visits).Route);
    }

    [Theory]
    [InlineData(false, "login")]
    [InlineData(true, "//main/home")]
    public async Task Splash_routes_from_session_restore(bool restored, string route)
    {
        var navigation = new TestNavigation(); var vm = new SplashViewModel(new TestPreferences { OnboardingCompleted = true, Language = "he" }, new TestText(), new SessionStub(restored), navigation);
        await vm.StartAsync(default);
        Assert.Equal(route, Assert.Single(navigation.Visits).Route);
    }

    [Fact]
    public async Task Onboarding_persists_language_and_completion()
    {
        var preferences = new TestPreferences(); var text = new TestText(); var navigation = new TestNavigation(); var vm = new OnboardingViewModel(preferences, text, navigation) { SelectedLanguage = "ar" };
        await vm.CompleteAsync();
        Assert.True(preferences.OnboardingCompleted); Assert.Equal("ar", preferences.Language); Assert.True(text.IsRightToLeft); Assert.Equal(AppRoutes.Login, navigation.Visits[0].Route);
    }

    [Fact]
    public async Task Merchant_discovery_handles_results_pagination_and_search()
    {
        var api = new MerchantStub(); var vm = new MerchantDiscoveryViewModel(api, new OnlineConnectivity(), new TestText(), new TestNavigation());
        await vm.LoadAsync(); Assert.Equal(RemoteStateKind.Content, vm.State); Assert.Single(vm.Items);
        await vm.LoadMoreAsync(); Assert.Equal(2, vm.Items.Count);
        vm.Query = "coffee"; await vm.SearchDebouncedAsync(); Assert.Equal("coffee", api.LastQuery);
    }

    [Fact]
    public async Task Merchant_discovery_exposes_offline_state_without_calling_api()
    {
        var api = new MerchantStub(); var vm = new MerchantDiscoveryViewModel(api, new OnlineConnectivity(false), new TestText(), new TestNavigation());
        await vm.LoadAsync(); Assert.Equal(RemoteStateKind.Offline, vm.State); Assert.Equal(0, api.Calls);
    }

    [Fact]
    public async Task Merchant_details_maps_not_found_to_error_state()
    {
        var vm = new MerchantDetailsViewModel(new MerchantStub { NotFound = true }, new CustomerAppState(), new OnlineConnectivity(), new TestText(), new TestNavigation());
        await vm.LoadAsync(Guid.NewGuid()); Assert.Equal(RemoteStateKind.Error, vm.State); Assert.Equal("ErrorNotFound", vm.ErrorMessage);
    }

    [Fact]
    public async Task Login_validation_does_not_call_backend()
    {
        var auth = new AuthStub(); var vm = new LoginViewModel(auth, new SessionStub(false), new OnlineConnectivity(), new TestText(), new TestNavigation(), new ClientRuntimeEnvironment(true));
        await vm.LoginAsync(); Assert.Equal(0, auth.LoginCalls); Assert.Equal("ErrorValidation", vm.ErrorMessage);
    }

    private sealed class SessionStub(bool restore) : ISessionManager
    {
        public string? AccessToken => restore ? "token" : null; public DateTime? AccessTokenExpiresUtc => null; public Guid? UserId => null; public bool IsAuthenticated => restore;
        public Task ClearAsync(CancellationToken ct) => Task.CompletedTask; public Task<bool> RefreshAsync(string? failedAccessToken, CancellationToken ct) => Task.FromResult(restore); public Task<bool> RestoreAsync(CancellationToken ct) => Task.FromResult(restore); public Task SetAsync(TokenResponse tokens, string deviceIdentifier, CancellationToken ct) => Task.CompletedTask;
    }
    private sealed class AuthStub : IAuthenticationApi
    {
        public int LoginCalls { get; private set; }
        public Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct) { LoginCalls++; throw new NotSupportedException(); }
        public Task LogoutAsync(string idempotencyKey, CancellationToken ct) => Task.CompletedTask; public Task<OtpChallengeResponse> RequestOtpAsync(OtpChallengeRequest request, string idempotencyKey, CancellationToken ct) => throw new NotSupportedException(); public Task<TokenResponse> RefreshAsync(RefreshRequest request, CancellationToken ct) => throw new NotSupportedException(); public Task VerifyOtpAsync(Guid challengeId, OtpVerifyRequest request, CancellationToken ct) => Task.CompletedTask;
    }
    private sealed class MerchantStub : IMerchantApi
    {
        public int Calls { get; private set; }
        public string? LastQuery { get; private set; }
        public bool NotFound { get; init; }
        public Task<CustomerMerchantListResponse> DiscoverAsync(int page, int pageSize, string? query, bool? openNow, CancellationToken ct) { Calls++; LastQuery = query; CustomerMerchantSummary item = new(Guid.NewGuid(), $"Merchant {page}", null, true, null); return Task.FromResult(new CustomerMerchantListResponse([item], page, pageSize, 2)); }
        public Task<CustomerMerchantDetails> DetailsAsync(Guid merchantId, CancellationToken ct) => NotFound ? throw new ApiException(new(404, "Not found", null, "merchant_not_found", new Dictionary<string, string[]>())) : Task.FromResult(new CustomerMerchantDetails(merchantId, "Merchant", null, true, [], $"/api/v1/merchants/{merchantId}/catalog"));
    }
}
