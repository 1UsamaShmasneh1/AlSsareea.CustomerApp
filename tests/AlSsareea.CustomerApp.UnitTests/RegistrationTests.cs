using AlSsareea.CustomerApp.Core;

namespace AlSsareea.CustomerApp.UnitTests;

public sealed class RegistrationTests
{
    [Fact]
    public async Task Email_registration_persists_session_bootstraps_profile_and_routes_home()
    {
        var auth = new AuthStub(); var customers = new CustomerStub(); var session = new SessionStub(); var navigation = new TestNavigation();
        var vm = new RegisterEmailViewModel(auth, session, new CustomerProfileBootstrapper(customers), new OnlineConnectivity(), new TestText(), navigation)
        { Email = "customer@example.test", Password = "Secure-Password-123", ConfirmPassword = "Secure-Password-123", FirstName = "First", LastName = "Last" };
        await vm.RegisterAsync();
        Assert.NotNull(auth.RegisterRequest); Assert.NotNull(customers.CreateRequest); Assert.Equal(1, session.SetCalls); Assert.Equal(AppRoutes.Home, Assert.Single(navigation.Visits).Route);
    }

    [Fact]
    public async Task Registration_collision_is_localized_and_does_not_create_profile()
    {
        var auth = new AuthStub { RegisterError = Problem(409, "auth.email_already_registered") }; var customers = new CustomerStub();
        var vm = new RegisterEmailViewModel(auth, new SessionStub(), new CustomerProfileBootstrapper(customers), new OnlineConnectivity(), new TestText(), new TestNavigation())
        { Email = "customer@example.test", Password = "Secure-Password-123", ConfirmPassword = "Secure-Password-123", FirstName = "First", LastName = "Last" };
        await vm.RegisterAsync();
        Assert.Equal("ErrorEmailAlreadyRegistered", vm.ErrorMessage); Assert.Null(customers.CreateRequest);
    }

    [Theory]
    [InlineData("not-an-email", "Secure-Password-123", "Secure-Password-123")]
    [InlineData("customer@example.test", "short", "short")]
    [InlineData("customer@example.test", "Secure-Password-123", "different-password")]
    public async Task Invalid_registration_form_is_rejected_before_calling_backend(string email, string password, string confirmation)
    {
        var auth = new AuthStub();
        var vm = new RegisterEmailViewModel(auth, new SessionStub(), new CustomerProfileBootstrapper(new CustomerStub()), new OnlineConnectivity(), new TestText(), new TestNavigation())
        { Email = email, Password = password, ConfirmPassword = confirmation, FirstName = "First", LastName = "Last" };
        await vm.RegisterAsync();
        Assert.Equal("ErrorValidation", vm.ErrorMessage); Assert.Equal(0, auth.RegisterCalls);
    }

    [Fact]
    public async Task Repeated_registration_tap_submits_only_once_while_request_is_running()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var auth = new AuthStub { RegisterGate = gate.Task };
        var vm = new RegisterEmailViewModel(auth, new SessionStub(), new CustomerProfileBootstrapper(new CustomerStub()), new OnlineConnectivity(), new TestText(), new TestNavigation())
        { Email = "customer@example.test", Password = "Secure-Password-123", ConfirmPassword = "Secure-Password-123", FirstName = "First", LastName = "Last" };
        Task first = vm.RegisterAsync();
        await Task.Yield();
        await vm.RegisterAsync();
        Assert.Equal(1, auth.RegisterCalls);
        gate.SetResult();
        await first;
    }

    [Fact]
    public async Task Profile_bootstrap_recovers_create_race_by_reading_existing_profile()
    {
        var customers = new CustomerStub { CreateConflict = true };
        CustomerProfileBootstrapResult result = await new CustomerProfileBootstrapper(customers).EnsureAsync(new("First", "Last"), default);
        Assert.True(result.IsComplete); Assert.Equal(2, customers.GetCalls); Assert.NotNull(result.Profile);
    }

    [Fact]
    public async Task Google_cancellation_is_not_an_error_and_does_not_call_backend()
    {
        var auth = new AuthStub(); var navigation = new TestNavigation();
        var vm = new RegisterChoiceViewModel(auth, new SessionStub(), new GoogleStub(new(GoogleSignInStatus.Cancelled)), new CustomerProfileBootstrapper(new CustomerStub()), new OnlineConnectivity(), new TestText(), navigation);
        await vm.ContinueWithGoogleAsync();
        Assert.Null(auth.GoogleRequest); Assert.Empty(navigation.Visits); Assert.Equal(RemoteStateKind.Content, vm.State);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Google_sign_in_for_new_or_existing_identity_persists_session_and_routes_home(bool isNewUser)
    {
        var auth = new AuthStub { GoogleIsNewUser = isNewUser }; var session = new SessionStub(); var navigation = new TestNavigation();
        var vm = new RegisterChoiceViewModel(auth, session, new GoogleStub(new(GoogleSignInStatus.Succeeded, "id-token", "nonce")), new CustomerProfileBootstrapper(new CustomerStub()), new OnlineConnectivity(), new TestText(), navigation);
        await vm.ContinueWithGoogleAsync();
        Assert.NotNull(auth.GoogleRequest); Assert.Equal("nonce", auth.GoogleRequest.Nonce); Assert.Equal(1, session.SetCalls); Assert.Equal(AppRoutes.Home, Assert.Single(navigation.Visits).Route);
    }

    [Theory]
    [InlineData(GoogleSignInStatus.Failed, "ErrorGoogleInvalid")]
    [InlineData(GoogleSignInStatus.NotConfigured, "ErrorGoogleNotConfigured")]
    [InlineData(GoogleSignInStatus.Unsupported, "ErrorGoogleUnsupported")]
    public async Task Google_client_failure_is_localized(GoogleSignInStatus status, string expected)
    {
        var auth = new AuthStub();
        var vm = new RegisterChoiceViewModel(auth, new SessionStub(), new GoogleStub(new(status)), new CustomerProfileBootstrapper(new CustomerStub()), new OnlineConnectivity(), new TestText(), new TestNavigation());
        await vm.ContinueWithGoogleAsync();
        Assert.Equal(expected, vm.ErrorMessage); Assert.Null(auth.GoogleRequest);
    }

    [Fact]
    public async Task Google_email_collision_from_backend_is_localized()
    {
        var auth = new AuthStub { GoogleError = Problem(409, "auth.external_link_required") };
        var vm = new RegisterChoiceViewModel(auth, new SessionStub(), new GoogleStub(new(GoogleSignInStatus.Succeeded, "id-token", "nonce")), new CustomerProfileBootstrapper(new CustomerStub()), new OnlineConnectivity(), new TestText(), new TestNavigation());
        await vm.ContinueWithGoogleAsync();
        Assert.Equal("ErrorExternalLinkRequired", vm.ErrorMessage);
    }

    [Fact]
    public async Task Offline_registration_does_not_call_backend()
    {
        var auth = new AuthStub();
        var vm = new RegisterEmailViewModel(auth, new SessionStub(), new CustomerProfileBootstrapper(new CustomerStub()), new OnlineConnectivity(false), new TestText(), new TestNavigation())
        { Email = "customer@example.test", Password = "Secure-Password-123", ConfirmPassword = "Secure-Password-123", FirstName = "First", LastName = "Last" };
        await vm.RegisterAsync();
        Assert.Equal(RemoteStateKind.Offline, vm.State); Assert.Equal(0, auth.RegisterCalls);
    }

    [Fact]
    public async Task Complete_profile_creates_missing_profile_and_routes_home()
    {
        var navigation = new TestNavigation(); var customers = new CustomerStub();
        var vm = new CompleteProfileViewModel(new CustomerProfileBootstrapper(customers), new OnlineConnectivity(), new TestText(), navigation) { FirstName = "First", LastName = "Last" };
        await vm.SaveAsync();
        Assert.NotNull(customers.CreateRequest); Assert.Equal(AppRoutes.Home, Assert.Single(navigation.Visits).Route);
    }

    [Fact]
    public async Task Restored_session_without_customer_profile_routes_completion()
    {
        var navigation = new TestNavigation(); var customers = new CustomerStub();
        await new SplashViewModel(new TestPreferences { OnboardingCompleted = true }, new TestText(), new SessionStub(restore: true), navigation, new CustomerProfileBootstrapper(customers)).StartAsync(default);
        Assert.Equal(AppRoutes.CompleteProfile, Assert.Single(navigation.Visits).Route);
    }

    private static ApiException Problem(int status, string code) => new(new(status, "Problem", null, code, new Dictionary<string, string[]>()));

    private sealed class AuthStub : IAuthenticationApi
    {
        public RegisterCustomerRequest? RegisterRequest { get; private set; }
        public GoogleAuthenticationRequest? GoogleRequest { get; private set; }
        public ApiException? RegisterError { get; init; }
        public ApiException? GoogleError { get; init; }
        public int RegisterCalls { get; private set; }
        public Task? RegisterGate { get; init; }
        public bool GoogleIsNewUser { get; init; } = true;
        public async Task<TokenResponse> RegisterCustomerAsync(RegisterCustomerRequest request, string idempotencyKey, CancellationToken ct) { RegisterCalls++; RegisterRequest = request; if (RegisterGate is not null) await RegisterGate; return RegisterError is null ? Token() : throw RegisterError; }
        public Task<GoogleAuthenticationResponse> AuthenticateWithGoogleAsync(GoogleAuthenticationRequest request, CancellationToken ct) { GoogleRequest = request; return GoogleError is null ? Task.FromResult(new GoogleAuthenticationResponse(Token(), GoogleIsNewUser, "customer@example.test", "First", "Last")) : Task.FromException<GoogleAuthenticationResponse>(GoogleError); }
        public Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct) => throw new NotSupportedException(); public Task<TokenResponse> RefreshAsync(RefreshRequest request, CancellationToken ct) => throw new NotSupportedException(); public Task LogoutAsync(string idempotencyKey, CancellationToken ct) => throw new NotSupportedException(); public Task<OtpChallengeResponse> RequestOtpAsync(OtpChallengeRequest request, string idempotencyKey, CancellationToken ct) => throw new NotSupportedException(); public Task VerifyOtpAsync(Guid challengeId, OtpVerifyRequest request, CancellationToken ct) => throw new NotSupportedException();
        private static TokenResponse Token() => new("Bearer", "access", 900, "refresh", DateTime.UtcNow.AddDays(1), Guid.NewGuid(), new(Guid.NewGuid(), "Customer"));
    }

    private sealed class CustomerStub : ICustomerApi
    {
        public int GetCalls { get; private set; }
        public CreateCustomerRequest? CreateRequest { get; private set; }
        public bool CreateConflict { get; init; }
        public Task<CustomerResponse> GetAsync(CancellationToken ct) { GetCalls++; return GetCalls == 1 ? Task.FromException<CustomerResponse>(Problem(404, "customers.not_found")) : Task.FromResult(Profile()); }
        public Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken ct) { CreateRequest = request; return CreateConflict ? Task.FromException<CustomerResponse>(Problem(409, "customers.already_exists")) : Task.FromResult(Profile()); }
        public Task<CustomerResponse> UpdateAsync(UpdateCustomerRequest request, CancellationToken ct) => throw new NotSupportedException(); public Task<IReadOnlyList<AddressResponse>> AddressesAsync(CancellationToken ct) => throw new NotSupportedException(); public Task<AddressResponse> AddAddressAsync(AddressRequest request, CancellationToken ct) => throw new NotSupportedException(); public Task<AddressResponse> UpdateAddressAsync(Guid id, AddressRequest request, CancellationToken ct) => throw new NotSupportedException(); public Task DeleteAddressAsync(Guid id, Guid concurrencyStamp, CancellationToken ct) => throw new NotSupportedException(); public Task<AddressResponse> SetDefaultAddressAsync(Guid id, Guid concurrencyStamp, CancellationToken ct) => throw new NotSupportedException(); public Task<CustomerPreferencesResponse> PreferencesAsync(CancellationToken ct) => throw new NotSupportedException(); public Task<CustomerPreferencesResponse> UpdatePreferencesAsync(UpdateCustomerPreferencesRequest request, CancellationToken ct) => throw new NotSupportedException();
        private static CustomerResponse Profile() => new(Guid.NewGuid(), "First", "Last", "First Last", null, 1, DateTime.UtcNow, DateTime.UtcNow, Guid.NewGuid());
    }

    private sealed class SessionStub(bool restore = false) : ISessionManager
    {
        public int SetCalls { get; private set; }
        public string? AccessToken => null; public DateTime? AccessTokenExpiresUtc => null; public Guid? UserId => null; public bool IsAuthenticated => restore;
        public Task SetAsync(TokenResponse tokens, string deviceIdentifier, CancellationToken ct) { SetCalls++; return Task.CompletedTask; }
        public Task<bool> RestoreAsync(CancellationToken ct) => Task.FromResult(restore); public Task<bool> RefreshAsync(string? failedAccessToken, CancellationToken ct) => Task.FromResult(false); public Task ClearAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class GoogleStub(GoogleSignInResult result) : IGoogleAuthenticationService { public Task<GoogleSignInResult> SignInAsync(CancellationToken ct) => Task.FromResult(result); }
}
