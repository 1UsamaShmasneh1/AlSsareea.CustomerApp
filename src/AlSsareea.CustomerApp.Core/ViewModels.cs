namespace AlSsareea.CustomerApp.Core;

public sealed class SplashViewModel(IPreferencesStore preferences, ILocalizationService text, ISessionManager session, INavigationService navigation) : ObservableObject
{
    public async Task StartAsync(CancellationToken ct)
    {
        text.Apply(preferences.Language);
        if (!preferences.OnboardingCompleted) { await navigation.GoToAsync(AppRoutes.Onboarding); return; }
        bool restored = await session.RestoreAsync(ct);
        await navigation.GoToAsync(restored ? AppRoutes.Home : AppRoutes.Login);
    }
}

public sealed class OnboardingViewModel(IPreferencesStore preferences, ILocalizationService text, INavigationService navigation) : ObservableObject
{
    private string selectedLanguage = preferences.Language;
    public string SelectedLanguage { get => selectedLanguage; set { if (Set(ref selectedLanguage, value)) text.Apply(value); } }
    public async Task CompleteAsync()
    {
        preferences.Language = SelectedLanguage;
        preferences.OnboardingCompleted = true;
        await navigation.GoToAsync(AppRoutes.Login);
    }
}

public sealed class LoginViewModel(IAuthenticationApi auth, ISessionManager session, IConnectivityService connectivity, ILocalizationService text, INavigationService navigation) : RemoteViewModel(connectivity, text)
{
    private string identifier = string.Empty;
    private string password = string.Empty;
    private string deviceIdentifier = Guid.NewGuid().ToString("N");
    private Guid? challengeId;
    private string otpCode = string.Empty;
    public string Identifier { get => identifier; set => Set(ref identifier, value); }
    public string Password { get => password; set => Set(ref password, value); }
    public string DeviceIdentifier { get => deviceIdentifier; set => Set(ref deviceIdentifier, value); }
    public Guid? ChallengeId { get => challengeId; private set => Set(ref challengeId, value); }
    public string OtpCode { get => otpCode; set => Set(ref otpCode, value); }
    public async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Identifier) || string.IsNullOrWhiteSpace(Password)) { State = RemoteStateKind.Error; ErrorMessage = Text["ErrorValidation"]; return; }
        await RunAsync(async () =>
        {
            TokenResponse tokens = await auth.LoginAsync(new(Identifier.Trim(), Password, new(DeviceIdentifier, "Customer app", CurrentPlatform(), null, null)), default);
            await session.SetAsync(tokens, DeviceIdentifier, default);
            State = RemoteStateKind.Content;
            await navigation.GoToAsync(AppRoutes.Home);
        });
    }
    public async Task RequestOtpAsync()
    {
        if (string.IsNullOrWhiteSpace(Identifier)) { State = RemoteStateKind.Error; ErrorMessage = Text["ErrorValidation"]; return; }
        await RunAsync(async () => { OtpChallengeResponse response = await auth.RequestOtpAsync(new(Identifier.Trim(), OtpPurpose.Login, DeviceIdentifier), Guid.NewGuid().ToString("N"), default); ChallengeId = response.ChallengeId; State = RemoteStateKind.Content; });
    }
    public async Task VerifyOtpAsync()
    {
        if (!ChallengeId.HasValue || string.IsNullOrWhiteSpace(OtpCode)) { State = RemoteStateKind.Error; ErrorMessage = Text["ErrorValidation"]; return; }
        await RunAsync(async () => { await auth.VerifyOtpAsync(ChallengeId.Value, new(OtpCode.Trim(), DeviceIdentifier), default); State = RemoteStateKind.Content; });
    }
    private static DevicePlatform CurrentPlatform() => OperatingSystem.IsAndroid() ? DevicePlatform.Android : OperatingSystem.IsIOS() ? DevicePlatform.Ios : DevicePlatform.Web;
}

public sealed class MerchantDiscoveryViewModel(IMerchantApi merchants, IConnectivityService connectivity, ILocalizationService text, INavigationService navigation) : RemoteViewModel(connectivity, text)
{
    private IReadOnlyList<CustomerMerchantSummary> items = [];
    private string query = string.Empty;
    private bool openNow;
    private int page = 1;
    private int total;
    private CancellationTokenSource? searchCancellation;
    public IReadOnlyList<CustomerMerchantSummary> Items { get => items; private set => Set(ref items, value); }
    public string Query { get => query; set => Set(ref query, value); }
    public bool OpenNow { get => openNow; set => Set(ref openNow, value); }
    public int Page => page;
    public bool HasMore => Items.Count < total;
    public Task LoadAsync(bool refresh = false) => LoadPageAsync(refresh ? 1 : page, refresh);
    public async Task SearchDebouncedAsync()
    {
        searchCancellation?.Cancel(); searchCancellation?.Dispose(); searchCancellation = new();
        try { await Task.Delay(350, searchCancellation.Token); page = 1; await LoadPageAsync(1, false, searchCancellation.Token); }
        catch (OperationCanceledException) { }
    }
    public async Task LoadMoreAsync() { if (HasMore && !IsBusy) await LoadPageAsync(page + 1, false); }
    private Task LoadPageAsync(int requestedPage, bool refreshing, CancellationToken ct = default) => RunAsync(async () =>
    {
        CustomerMerchantListResponse response = await merchants.DiscoverAsync(requestedPage, 20, Query, OpenNow ? true : null, ct);
        Items = requestedPage == 1 ? response.Items : Items.Concat(response.Items).ToArray();
        page = response.Page; total = response.TotalCount; Raise(nameof(Page)); Raise(nameof(HasMore));
        State = Items.Count == 0 ? RemoteStateKind.Empty : RemoteStateKind.Content;
    }, refreshing);
    public Task OpenAsync(CustomerMerchantSummary merchant) => navigation.GoToAsync(AppRoutes.MerchantDetails, new Dictionary<string, object> { ["merchantId"] = merchant.Id });
}

public sealed class MerchantDetailsViewModel(IMerchantApi merchants, CustomerAppState appState, IConnectivityService connectivity, ILocalizationService text, INavigationService navigation) : RemoteViewModel(connectivity, text)
{
    private CustomerMerchantDetails? merchant;
    public CustomerMerchantDetails? Merchant { get => merchant; private set => Set(ref merchant, value); }
    public Task LoadAsync(Guid id) => RunAsync(async () => { Merchant = await merchants.DetailsAsync(id, default); appState.MerchantId = id; appState.BranchId = Merchant.Branches.FirstOrDefault(x => x.IsPrimary)?.Id; State = RemoteStateKind.Content; });
    public Task OpenCatalogAsync() => Merchant is null ? Task.CompletedTask : navigation.GoToAsync(AppRoutes.Catalog, new Dictionary<string, object> { ["merchantId"] = Merchant.Id });
}

public sealed class CatalogViewModel(ICatalogApi catalog, IPreferencesStore preferences, IConnectivityService connectivity, ILocalizationService text, INavigationService navigation) : RemoteViewModel(connectivity, text)
{
    private Guid merchantId;
    private IReadOnlyList<CategoryResponse> categories = [];
    private IReadOnlyList<MenuSectionResponse> sections = [];
    private IReadOnlyList<ProductResponse> products = [];
    private string query = string.Empty;
    private int page = 1;
    private int total;
    public IReadOnlyList<CategoryResponse> Categories { get => categories; private set => Set(ref categories, value); }
    public IReadOnlyList<MenuSectionResponse> Sections { get => sections; private set => Set(ref sections, value); }
    public IReadOnlyList<ProductResponse> Products { get => products; private set => Set(ref products, value); }
    public string Query { get => query; set => Set(ref query, value); }
    public Task LoadAsync(Guid id, bool refresh = false) => RunAsync(async () =>
    {
        merchantId = id; page = 1;
        Task<IReadOnlyList<CategoryResponse>> categoryTask = catalog.CategoriesAsync(id, preferences.Language, default);
        Task<IReadOnlyList<MenuSectionResponse>> sectionTask = catalog.SectionsAsync(id, preferences.Language, default);
        Task<ProductListResponse> productTask = catalog.ProductsAsync(id, 1, 20, Query, null, preferences.Language, default);
        await Task.WhenAll(categoryTask, sectionTask, productTask);
        Categories = await categoryTask; Sections = await sectionTask; ProductListResponse result = await productTask; Products = result.Items; total = result.TotalCount;
        State = Products.Count == 0 ? RemoteStateKind.Empty : RemoteStateKind.Content;
    }, refresh);
    public async Task LoadMoreAsync()
    {
        if (Products.Count >= total || IsBusy) return;
        await RunAsync(async () => { ProductListResponse result = await catalog.ProductsAsync(merchantId, ++page, 20, Query, null, preferences.Language, default); Products = Products.Concat(result.Items).ToArray(); State = RemoteStateKind.Content; });
    }
    public Task OpenProductAsync(ProductResponse product) => navigation.GoToAsync(AppRoutes.ProductDetails, new Dictionary<string, object> { ["merchantId"] = merchantId, ["productId"] = product.Id });
}

public sealed class ProductViewModel(ICatalogApi catalog, ICartApi carts, CustomerAppState appState, IPreferencesStore preferences, IConnectivityService connectivity, ILocalizationService text, INavigationService navigation) : RemoteViewModel(connectivity, text)
{
    private int adding;
    private Guid merchantId;
    private ProductResponse? product;
    private CatalogPriceResponse? price;
    private int quantity = 1;
    public ProductResponse? Product { get => product; private set => Set(ref product, value); }
    public CatalogPriceResponse? Price { get => price; private set => Set(ref price, value); }
    public int Quantity { get => quantity; set => Set(ref quantity, Math.Clamp(value, 1, 99)); }
    public string OptionsAvailabilityMessage => Text["ProductOptionsBackendUnavailable"];
    public Task LoadAsync(Guid merchant, Guid productId) => RunAsync(async () =>
    {
        merchantId = merchant;
        Product = await catalog.ProductAsync(merchant, productId, preferences.Language, default);
        Price = await catalog.PriceAsync(merchant, productId, new(null, [], preferences.Language), default);
        State = RemoteStateKind.Content;
    });
    public async Task AddToCartAsync()
    {
        if (Interlocked.Exchange(ref adding, 1) != 0) return;
        try { await RunAsync(async () => { if (Product is null) return; CartResponse cart = appState.Cart ?? await carts.CreateAsync(new(merchantId, appState.BranchId), Guid.NewGuid().ToString("N"), default); cart = await carts.AddAsync(cart.Id, new(Product.Id, null, Quantity, null, [], cart.ConcurrencyStamp), Guid.NewGuid().ToString("N"), default); appState.Cart = cart; appState.MerchantId = merchantId; State = RemoteStateKind.Content; await navigation.GoToAsync(AppRoutes.Cart); }); }
        finally { Volatile.Write(ref adding, 0); }
    }
}

public sealed class CartViewModel(ICartApi carts, CustomerAppState appState, IConnectivityService connectivity, ILocalizationService text, INavigationService navigation) : RemoteViewModel(connectivity, text)
{
    private int mutating;
    private CartCheckoutSummaryResponse? summary;
    public CartResponse? Cart => appState.Cart;
    public IReadOnlyList<CartItemResponse> Items => Cart?.Items ?? [];
    public CartCheckoutSummaryResponse? Summary { get => summary; private set => Set(ref summary, value); }
    public async Task LoadAsync(bool refresh = false)
    {
        if (!appState.MerchantId.HasValue) { State = RemoteStateKind.Empty; return; }
        await RunAsync(async () => { appState.Cart = await carts.ActiveAsync(appState.MerchantId.Value, appState.BranchId, default); Summary = await carts.SummaryAsync(appState.Cart.Id, default); Raise(nameof(Cart)); Raise(nameof(Items)); State = Items.Count == 0 ? RemoteStateKind.Empty : RemoteStateKind.Content; }, refresh);
    }
    public Task ChangeQuantityAsync(CartItemResponse item, int quantity) => MutateAsync(() => carts.UpdateQuantityAsync(Cart!.Id, item.Id, new(quantity, Cart!.ConcurrencyStamp), Guid.NewGuid().ToString("N"), default));
    public Task RemoveAsync(CartItemResponse item) => MutateAsync(() => carts.RemoveAsync(Cart!.Id, item.Id, Cart!.ConcurrencyStamp, Guid.NewGuid().ToString("N"), default));
    public Task ApplyCouponAsync(string code) => Cart is null ? Task.CompletedTask : MutateAsync(() => carts.ApplyCouponAsync(Cart.Id, new(code, Cart.ConcurrencyStamp), Guid.NewGuid().ToString("N"), default));
    public Task RemoveCouponAsync() => Cart is null ? Task.CompletedTask : MutateAsync(() => carts.RemoveCouponAsync(Cart.Id, Cart.ConcurrencyStamp, Guid.NewGuid().ToString("N"), default));
    public Task ClearAsync() => Cart is null ? Task.CompletedTask : MutateAsync(() => carts.ClearAsync(Cart.Id, Cart.ConcurrencyStamp, Guid.NewGuid().ToString("N"), default));
    private async Task MutateAsync(Func<Task<CartResponse>> mutation)
    {
        if (Interlocked.Exchange(ref mutating, 1) != 0) return;
        try { await RunAsync(async () => { appState.Cart = await mutation(); Summary = await carts.SummaryAsync(appState.Cart.Id, default); Raise(nameof(Cart)); Raise(nameof(Items)); State = Items.Count == 0 ? RemoteStateKind.Empty : RemoteStateKind.Content; }); }
        finally { Volatile.Write(ref mutating, 0); }
    }
    public Task CheckoutAsync() => Cart is null ? Task.CompletedTask : navigation.GoToAsync(AppRoutes.Checkout, new Dictionary<string, object> { ["cartId"] = Cart.Id });
}

public sealed class AddressesViewModel(ICustomerApi customer, IMapsApi maps, IConnectivityService connectivity, ILocalizationService text) : RemoteViewModel(connectivity, text)
{
    private IReadOnlyList<AddressResponse> items = [];
    private IReadOnlyList<GeocodingResult> suggestions = [];
    public IReadOnlyList<AddressResponse> Items { get => items; private set => Set(ref items, value); }
    public IReadOnlyList<GeocodingResult> Suggestions { get => suggestions; private set => Set(ref suggestions, value); }
    public Task LoadAsync(bool refresh = false) => RunAsync(async () => { Items = await customer.AddressesAsync(default); State = Items.Count == 0 ? RemoteStateKind.Empty : RemoteStateKind.Content; }, refresh);
    public Task SearchAddressAsync(string query) => RunAsync(async () => { Suggestions = await maps.GeocodeAsync(new(query), default); State = Suggestions.Count == 0 ? RemoteStateKind.Empty : RemoteStateKind.Content; });
    public async Task<bool> ReverseAndAddAsync(string label, string city, string street, double latitude, double longitude)
    {
        bool added = false;
        await RunAsync(async () =>
        {
            ReverseGeocodingResult normalized = await maps.ReverseGeocodeAsync(new(latitude, longitude), default);
            added = await AddAsync(label, city, street, new(normalized.FormattedAddress, normalized.Latitude, normalized.Longitude, normalized.PlaceId));
        });
        return added;
    }
    public async Task<bool> AddAsync(string label, string city, string street, GeocodingResult location)
    {
        bool eligible = false;
        await RunAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(street))
            {
                State = RemoteStateKind.Error;
                ErrorMessage = Text["ErrorValidation"];
                return;
            }
            DeliveryEligibilityResponse result = await maps.EligibilityAsync(new(location.Latitude, location.Longitude), default);
            if (!result.Eligible) { State = RemoteStateKind.Error; ErrorMessage = Text["AddressOutsideArea"]; return; }
            AddressResponse address = await customer.AddAddressAsync(new(label.Trim(), 1, city.Trim(), null, street.Trim(), null, null, null, null, location.PlaceId, location.Latitude, location.Longitude, null, Items.Count == 0, null), default);
            Items = Items.Append(address).ToArray(); State = RemoteStateKind.Content; eligible = true;
        });
        return eligible;
    }
    public Task SetDefaultAsync(AddressResponse address) => RunAsync(async () => { AddressResponse changed = await customer.SetDefaultAddressAsync(address.Id, address.ConcurrencyStamp, default); Items = Items.Select(x => x.Id == changed.Id ? changed : x with { IsDefault = false }).ToArray(); State = RemoteStateKind.Content; });
    public Task DeleteAsync(AddressResponse address) => RunAsync(async () => { await customer.DeleteAddressAsync(address.Id, address.ConcurrencyStamp, default); Items = Items.Where(x => x.Id != address.Id).ToArray(); State = Items.Count == 0 ? RemoteStateKind.Empty : RemoteStateKind.Content; });
}

public sealed class CheckoutViewModel(ICartApi carts, ICustomerApi customer, IMapsApi maps, IOrdersApi orders, CustomerAppState appState, IConnectivityService connectivity, ILocalizationService text, INavigationService navigation) : RemoteViewModel(connectivity, text)
{
    private readonly IdempotentSubmission submission = new();
    private int submitting;
    private CartCheckoutSummaryResponse? summary;
    private IReadOnlyList<AddressResponse> addresses = [];
    private AddressResponse? selectedAddress;
    public CartCheckoutSummaryResponse? Summary { get => summary; private set => Set(ref summary, value); }
    public IReadOnlyList<AddressResponse> Addresses { get => addresses; private set => Set(ref addresses, value); }
    public AddressResponse? SelectedAddress { get => selectedAddress; set => Set(ref selectedAddress, value); }
    public string CurrentIdempotencyKey => submission.CurrentKey;
    public Task LoadAsync(Guid cartId) => RunAsync(async () => { Summary = await carts.RepriceAsync(cartId, default); Addresses = await customer.AddressesAsync(default); SelectedAddress = Addresses.FirstOrDefault(x => x.IsDefault) ?? Addresses.FirstOrDefault(); State = !Summary.IsCheckoutReady || SelectedAddress is null ? RemoteStateKind.Empty : RemoteStateKind.Content; });
    public async Task SubmitAsync()
    {
        if (Interlocked.Exchange(ref submitting, 1) != 0) return;
        try { await RunAsync(async () => { if (Summary is null || SelectedAddress is null || SelectedAddress.Latitude is null || SelectedAddress.Longitude is null) { State = RemoteStateKind.Error; ErrorMessage = Text["ErrorValidation"]; return; } DeliveryEligibilityResponse eligibility = await maps.EligibilityAsync(new(SelectedAddress.Latitude.Value, SelectedAddress.Longitude.Value), default); if (!eligibility.Eligible) { State = RemoteStateKind.Error; ErrorMessage = Text["AddressOutsideArea"]; return; } Summary = await carts.RepriceAsync(Summary.CartId, default); CreateOrderResponse created = await orders.CreateAsync(new(Summary.CartId, SelectedAddress.Id, 1, null, null, Summary.ConcurrencyStamp), submission.CurrentKey, default); submission.Complete(); appState.Cart = null; State = RemoteStateKind.Content; await navigation.GoToAsync(AppRoutes.OrderDetails, new Dictionary<string, object> { ["orderId"] = created.OrderId }); }); }
        finally { Volatile.Write(ref submitting, 0); }
    }
}

public sealed class OrdersViewModel(IOrdersApi orders, IConnectivityService connectivity, ILocalizationService text, INavigationService navigation) : RemoteViewModel(connectivity, text)
{
    private IReadOnlyList<OrderListItemResponse> items = [];
    private int page = 1; private int total;
    public IReadOnlyList<OrderListItemResponse> Items { get => items; private set => Set(ref items, value); }
    public Task LoadAsync(bool refresh = false) => LoadPageAsync(1, refresh);
    private Task LoadPageAsync(int requested, bool refresh) => RunAsync(async () => { OrderListResponse response = await orders.ListAsync(requested, default); Items = requested == 1 ? response.Items : Items.Concat(response.Items).ToArray(); page = requested; total = response.TotalCount; State = Items.Count == 0 ? RemoteStateKind.Empty : RemoteStateKind.Content; }, refresh);
    public Task LoadMoreAsync() => Items.Count < total ? LoadPageAsync(page + 1, false) : Task.CompletedTask;
    public Task OpenAsync(OrderListItemResponse order) => navigation.GoToAsync(AppRoutes.OrderDetails, new Dictionary<string, object> { ["orderId"] = order.Id });
}

public sealed class OrderDetailsViewModel(IOrdersApi orders, IConnectivityService connectivity, ILocalizationService text, INavigationService navigation) : RemoteViewModel(connectivity, text)
{
    private OrderDetailsResponse? order;
    public OrderDetailsResponse? Order { get => order; private set => Set(ref order, value); }
    public string StatusLabel => Order is null ? string.Empty : Text[OrderStatusPresentation.Key(Order.Status)];
    public Task LoadAsync(Guid id) => RunAsync(async () => { Order = await orders.GetAsync(id, default); Raise(nameof(StatusLabel)); State = RemoteStateKind.Content; });
    public Task TrackAsync() => Order is null ? Task.CompletedTask : navigation.GoToAsync(AppRoutes.Tracking, new Dictionary<string, object> { ["orderId"] = Order.Id });
    public Task CancelAsync(string reason) => Order is null ? Task.CompletedTask : RunAsync(async () => { Order = await orders.CancelAsync(Order.Id, new(1, "customer_requested", reason, Order.ConcurrencyStamp), default); Raise(nameof(StatusLabel)); State = RemoteStateKind.Content; });
}

public sealed class NotificationsViewModel(INotificationsApi notifications, IConnectivityService connectivity, ILocalizationService text, INavigationService navigation) : RemoteViewModel(connectivity, text)
{
    private IReadOnlyList<NotificationListItem> items = [];
    private IReadOnlyList<NotificationPreferenceItem> preferences = [];
    private int unread;
    private int page = 1;
    private int total;
    public IReadOnlyList<NotificationListItem> Items { get => items; private set => Set(ref items, value); }
    public IReadOnlyList<NotificationPreferenceItem> Preferences { get => preferences; private set => Set(ref preferences, value); }
    public int UnreadCount { get => unread; private set => Set(ref unread, value); }
    public Task LoadAsync(bool refresh = false) => RunAsync(async () => { page = 1; Task<NotificationListResponse> listTask = notifications.ListAsync(1, default); Task<NotificationPreferencesResponse> preferencesTask = notifications.PreferencesAsync(default); await Task.WhenAll(listTask, preferencesTask); NotificationListResponse response = await listTask; Items = response.Items; total = response.TotalCount; UnreadCount = response.UnreadCount; Preferences = (await preferencesTask).Items; State = Items.Count == 0 ? RemoteStateKind.Empty : RemoteStateKind.Content; }, refresh);
    public Task LoadMoreAsync() => Items.Count >= total ? Task.CompletedTask : RunAsync(async () => { NotificationListResponse response = await notifications.ListAsync(++page, default); Items = Items.Concat(response.Items).ToArray(); total = response.TotalCount; UnreadCount = response.UnreadCount; State = RemoteStateKind.Content; });
    public Task MarkReadAsync(NotificationListItem item) => RunAsync(async () => { await notifications.MarkReadAsync(item.Id, default); Items = Items.Select(x => x.Id == item.Id ? x with { ReadAtUtc = DateTime.UtcNow } : x).ToArray(); UnreadCount = Math.Max(0, UnreadCount - (item.ReadAtUtc is null ? 1 : 0)); State = RemoteStateKind.Content; });
    public Task MarkAllReadAsync() => RunAsync(async () => { await notifications.MarkAllReadAsync(default); Items = Items.Select(x => x with { ReadAtUtc = x.ReadAtUtc ?? DateTime.UtcNow }).ToArray(); UnreadCount = 0; State = RemoteStateKind.Content; });
    public Task SetPreferenceAsync(NotificationPreferenceItem item, bool enabled) => RunAsync(async () => { NotificationPreferenceItem changed = item with { Enabled = enabled }; NotificationPreferencesResponse response = await notifications.UpdatePreferencesAsync(new(Preferences.Select(x => x.Category == item.Category && x.Channel == item.Channel ? changed : x).ToArray()), default); Preferences = response.Items; State = Items.Count == 0 ? RemoteStateKind.Empty : RemoteStateKind.Content; });
    public async Task OpenAsync(NotificationListItem item)
    {
        await MarkReadAsync(item);
        if (Guid.TryParse(item.TemplateKey.Split(':').LastOrDefault(), out Guid id)) await navigation.GoToAsync(AppRoutes.OrderDetails, new Dictionary<string, object> { ["orderId"] = id });
    }
}

public sealed class ProfileViewModel(ICustomerApi customer, IAccountSessionApi account, ISessionManager session, UserStateResetter resetter, IPreferencesStore preferences, ILocalizationService text, IConnectivityService connectivity, INavigationService navigation) : RemoteViewModel(connectivity, text)
{
    private CustomerResponse? customerValue;
    public CustomerResponse? Customer { get => customerValue; private set => Set(ref customerValue, value); }
    public Task LoadAsync() => RunAsync(async () => { Customer = await customer.GetAsync(default); State = RemoteStateKind.Content; });
    public void ChangeLanguage(string language) { preferences.Language = language; Text.Apply(language); }
    public async Task LogoutAsync()
    {
        try { await resetter.ResetAsync(default); }
        catch (Exception) { }
        try { if (Connectivity.IsOnline) await account.LogoutAsync(Guid.NewGuid().ToString("N"), default); }
        catch (Exception) { }
        finally { await session.ClearAsync(default); await navigation.GoToAsync(AppRoutes.Login); }
    }
}

public sealed class TrackingViewModel(TrackingCoordinator tracking, IConnectivityService connectivity, ILocalizationService text) : RemoteViewModel(connectivity, text)
{
    private TrackingRealtimePayload? location;
    private ConnectionState connectionState;
    public TrackingRealtimePayload? Location { get => location; private set => Set(ref location, value); }
    public ConnectionState Connection { get => connectionState; private set => Set(ref connectionState, value); }
    public async Task StartAsync(Guid orderId)
    {
        tracking.LocationUpdated += OnLocation; tracking.StateChanged += OnState;
        await RunAsync(async () => { Location = await tracking.StartAsync(orderId, default); State = Location is null ? RemoteStateKind.Empty : RemoteStateKind.Content; });
    }
    private void OnLocation(object? sender, TrackingRealtimePayload value) { Location = value; State = RemoteStateKind.Content; }
    private void OnState(object? sender, ConnectionState value) => Connection = value;
    public async Task StopAsync() { tracking.LocationUpdated -= OnLocation; tracking.StateChanged -= OnState; await tracking.StopAsync(default); }
}
