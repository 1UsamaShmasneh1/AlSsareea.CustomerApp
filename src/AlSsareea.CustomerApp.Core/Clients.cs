namespace AlSsareea.CustomerApp.Core;

public sealed class AuthenticationApi(ApiClient api) : IAuthenticationApi
{
    public Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct) => api.SendAsync<TokenResponse>(HttpMethod.Post, "api/v1/auth/login", request, ct);
    public Task<TokenResponse> RegisterCustomerAsync(RegisterCustomerRequest request, string idempotencyKey, CancellationToken ct) => api.SendAsync<TokenResponse>(HttpMethod.Post, "api/v1/auth/register/customer", request, ct, idempotencyKey);
    public Task<GoogleAuthenticationResponse> AuthenticateWithGoogleAsync(GoogleAuthenticationRequest request, CancellationToken ct) => api.SendAsync<GoogleAuthenticationResponse>(HttpMethod.Post, "api/v1/auth/external/google", request, ct);
    public Task<TokenResponse> RefreshAsync(RefreshRequest request, CancellationToken ct) => api.SendAsync<TokenResponse>(HttpMethod.Post, "api/v1/auth/refresh", request, ct);
    public Task LogoutAsync(string idempotencyKey, CancellationToken ct) => api.SendAsync(HttpMethod.Post, "api/v1/auth/logout", null, ct, idempotencyKey);
    public Task<OtpChallengeResponse> RequestOtpAsync(OtpChallengeRequest request, string idempotencyKey, CancellationToken ct) => api.SendAsync<OtpChallengeResponse>(HttpMethod.Post, "api/v1/auth/otp/challenges", request, ct, idempotencyKey);
    public Task VerifyOtpAsync(Guid challengeId, OtpVerifyRequest request, CancellationToken ct) => api.SendAsync(HttpMethod.Post, $"api/v1/auth/otp/challenges/{challengeId}/verify", request, ct);
}

public interface IAccountSessionApi { Task LogoutAsync(string idempotencyKey, CancellationToken ct); }
public sealed class AccountSessionApi(ApiClient api) : IAccountSessionApi
{
    public Task LogoutAsync(string idempotencyKey, CancellationToken ct) => api.SendAsync(HttpMethod.Post, "api/v1/auth/logout", null, ct, idempotencyKey);
}

public interface ICustomerApi
{
    Task<CustomerResponse> GetAsync(CancellationToken ct);
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken ct) => throw new NotSupportedException();
    Task<CustomerResponse> UpdateAsync(UpdateCustomerRequest request, CancellationToken ct);
    Task<IReadOnlyList<AddressResponse>> AddressesAsync(CancellationToken ct);
    Task<AddressResponse> AddAddressAsync(AddressRequest request, CancellationToken ct);
    Task<AddressResponse> UpdateAddressAsync(Guid id, AddressRequest request, CancellationToken ct);
    Task DeleteAddressAsync(Guid id, Guid concurrencyStamp, CancellationToken ct);
    Task<AddressResponse> SetDefaultAddressAsync(Guid id, Guid concurrencyStamp, CancellationToken ct);
    Task<CustomerPreferencesResponse> PreferencesAsync(CancellationToken ct);
    Task<CustomerPreferencesResponse> UpdatePreferencesAsync(UpdateCustomerPreferencesRequest request, CancellationToken ct);
}

public sealed class CustomerApi(ApiClient api) : ICustomerApi
{
    public Task<CustomerResponse> GetAsync(CancellationToken ct) => api.SendAsync<CustomerResponse>(HttpMethod.Get, "api/v1/customers/me/", null, ct);
    public Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken ct) => api.SendAsync<CustomerResponse>(HttpMethod.Post, "api/v1/customers/me/", request, ct);
    public Task<CustomerResponse> UpdateAsync(UpdateCustomerRequest request, CancellationToken ct) => api.SendAsync<CustomerResponse>(HttpMethod.Put, "api/v1/customers/me/", request, ct);
    public Task<IReadOnlyList<AddressResponse>> AddressesAsync(CancellationToken ct) => api.SendAsync<IReadOnlyList<AddressResponse>>(HttpMethod.Get, "api/v1/customers/me/addresses", null, ct);
    public Task<AddressResponse> AddAddressAsync(AddressRequest request, CancellationToken ct) => api.SendAsync<AddressResponse>(HttpMethod.Post, "api/v1/customers/me/addresses", request, ct);
    public Task<AddressResponse> UpdateAddressAsync(Guid id, AddressRequest request, CancellationToken ct) => api.SendAsync<AddressResponse>(HttpMethod.Put, $"api/v1/customers/me/addresses/{id}", request, ct);
    public Task DeleteAddressAsync(Guid id, Guid concurrencyStamp, CancellationToken ct) => api.SendAsync(HttpMethod.Delete, $"api/v1/customers/me/addresses/{id}?concurrencyStamp={concurrencyStamp}", null, ct);
    public Task<AddressResponse> SetDefaultAddressAsync(Guid id, Guid concurrencyStamp, CancellationToken ct) => api.SendAsync<AddressResponse>(HttpMethod.Put, $"api/v1/customers/me/addresses/{id}/default", new { concurrencyStamp }, ct);
    public Task<CustomerPreferencesResponse> PreferencesAsync(CancellationToken ct) => api.SendAsync<CustomerPreferencesResponse>(HttpMethod.Get, "api/v1/customers/me/preferences", null, ct);
    public Task<CustomerPreferencesResponse> UpdatePreferencesAsync(UpdateCustomerPreferencesRequest request, CancellationToken ct) => api.SendAsync<CustomerPreferencesResponse>(HttpMethod.Put, "api/v1/customers/me/preferences", request, ct);
}

public interface IMerchantApi
{
    Task<CustomerMerchantListResponse> DiscoverAsync(int page, int pageSize, string? query, bool? openNow, CancellationToken ct);
    Task<CustomerMerchantDetails> DetailsAsync(Guid merchantId, CancellationToken ct);
}

public sealed class MerchantApi(ApiClient api) : IMerchantApi
{
    public Task<CustomerMerchantListResponse> DiscoverAsync(int page, int pageSize, string? query, bool? openNow, CancellationToken ct)
    {
        string path = $"api/v1/customer/merchants/?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(query)) path += $"&query={Uri.EscapeDataString(query.Trim())}";
        if (openNow.HasValue) path += $"&openNow={openNow.Value.ToString().ToLowerInvariant()}";
        return api.SendAsync<CustomerMerchantListResponse>(HttpMethod.Get, path, null, ct);
    }
    public Task<CustomerMerchantDetails> DetailsAsync(Guid merchantId, CancellationToken ct) => api.SendAsync<CustomerMerchantDetails>(HttpMethod.Get, $"api/v1/customer/merchants/{merchantId}", null, ct);
}

public interface ICatalogApi
{
    Task<IReadOnlyList<CategoryResponse>> CategoriesAsync(Guid merchantId, string language, CancellationToken ct);
    Task<IReadOnlyList<MenuSectionResponse>> SectionsAsync(Guid merchantId, string language, CancellationToken ct);
    Task<ProductListResponse> ProductsAsync(Guid merchantId, int page, int pageSize, string? query, Guid? categoryId, string language, CancellationToken ct);
    Task<CustomerProductDetailsResponse> ProductAsync(Guid merchantId, Guid productId, string language, Guid? branchId, CancellationToken ct);
    Task<CatalogPriceResponse> PriceAsync(Guid merchantId, Guid productId, PriceRequest request, CancellationToken ct);
}

public sealed class CatalogApi(ApiClient api) : ICatalogApi
{
    public Task<IReadOnlyList<CategoryResponse>> CategoriesAsync(Guid merchantId, string language, CancellationToken ct) => api.SendAsync<IReadOnlyList<CategoryResponse>>(HttpMethod.Get, $"api/v1/merchants/{merchantId}/catalog/categories?language={Uri.EscapeDataString(language)}", null, ct);
    public Task<IReadOnlyList<MenuSectionResponse>> SectionsAsync(Guid merchantId, string language, CancellationToken ct) => api.SendAsync<IReadOnlyList<MenuSectionResponse>>(HttpMethod.Get, $"api/v1/merchants/{merchantId}/catalog/sections?language={Uri.EscapeDataString(language)}", null, ct);
    public Task<ProductListResponse> ProductsAsync(Guid merchantId, int page, int pageSize, string? query, Guid? categoryId, string language, CancellationToken ct)
    {
        string path = $"api/v1/merchants/{merchantId}/catalog/products?page={page}&pageSize={pageSize}&language={Uri.EscapeDataString(language)}";
        if (!string.IsNullOrWhiteSpace(query)) path += $"&query={Uri.EscapeDataString(query.Trim())}";
        if (categoryId.HasValue) path += $"&categoryId={categoryId.Value}";
        return api.SendAsync<ProductListResponse>(HttpMethod.Get, path, null, ct);
    }
    public Task<CustomerProductDetailsResponse> ProductAsync(Guid merchantId, Guid productId, string language, Guid? branchId, CancellationToken ct)
    {
        string path = $"api/v1/merchants/{merchantId}/catalog/products/{productId}?language={Uri.EscapeDataString(language)}";
        if (branchId.HasValue) path += $"&branchId={branchId.Value}";
        return api.SendAsync<CustomerProductDetailsResponse>(HttpMethod.Get, path, null, ct);
    }
    public Task<CatalogPriceResponse> PriceAsync(Guid merchantId, Guid productId, PriceRequest request, CancellationToken ct) => api.SendAsync<CatalogPriceResponse>(HttpMethod.Post, $"api/v1/merchants/{merchantId}/catalog/products/{productId}/price", request, ct);
}

public interface ICartApi
{
    Task<CartResponse> ActiveAsync(Guid merchantId, Guid? branchId, CancellationToken ct);
    Task<CartResponse> CreateAsync(GetOrCreateActiveCartRequest request, string key, CancellationToken ct);
    Task<CartResponse> AddAsync(Guid cartId, AddCartItemRequest request, string key, CancellationToken ct);
    Task<CartResponse> UpdateQuantityAsync(Guid cartId, Guid itemId, UpdateCartItemQuantityRequest request, string key, CancellationToken ct);
    Task<CartResponse> RemoveAsync(Guid cartId, Guid itemId, Guid concurrencyStamp, string key, CancellationToken ct);
    Task<CartResponse> ApplyCouponAsync(Guid cartId, ApplyCartCouponRequest request, string key, CancellationToken ct);
    Task<CartResponse> RemoveCouponAsync(Guid cartId, Guid concurrencyStamp, string key, CancellationToken ct);
    Task<CartResponse> ClearAsync(Guid cartId, Guid concurrencyStamp, string key, CancellationToken ct);
    Task<CartCheckoutSummaryResponse> RepriceAsync(Guid cartId, CancellationToken ct);
    Task<CartCheckoutSummaryResponse> SummaryAsync(Guid cartId, CancellationToken ct);
}

public sealed class CartApi(ApiClient api) : ICartApi
{
    public Task<CartResponse> ActiveAsync(Guid merchantId, Guid? branchId, CancellationToken ct) => api.SendAsync<CartResponse>(HttpMethod.Get, $"api/carts/active?merchantId={merchantId}{(branchId.HasValue ? $"&branchId={branchId}" : string.Empty)}", null, ct);
    public Task<CartResponse> CreateAsync(GetOrCreateActiveCartRequest request, string key, CancellationToken ct) => api.SendAsync<CartResponse>(HttpMethod.Post, "api/carts/", request, ct, key);
    public Task<CartResponse> AddAsync(Guid cartId, AddCartItemRequest request, string key, CancellationToken ct) => api.SendAsync<CartResponse>(HttpMethod.Post, $"api/carts/{cartId}/items", request, ct, key);
    public Task<CartResponse> UpdateQuantityAsync(Guid cartId, Guid itemId, UpdateCartItemQuantityRequest request, string key, CancellationToken ct) => api.SendAsync<CartResponse>(HttpMethod.Patch, $"api/carts/{cartId}/items/{itemId}", request, ct, key);
    public Task<CartResponse> RemoveAsync(Guid cartId, Guid itemId, Guid concurrencyStamp, string key, CancellationToken ct) => api.SendAsync<CartResponse>(HttpMethod.Delete, $"api/carts/{cartId}/items/{itemId}?concurrencyStamp={concurrencyStamp}", null, ct, key);
    public Task<CartResponse> ApplyCouponAsync(Guid cartId, ApplyCartCouponRequest request, string key, CancellationToken ct) => api.SendAsync<CartResponse>(HttpMethod.Put, $"api/carts/{cartId}/coupon", request, ct, key);
    public Task<CartResponse> RemoveCouponAsync(Guid cartId, Guid concurrencyStamp, string key, CancellationToken ct) => api.SendAsync<CartResponse>(HttpMethod.Delete, $"api/carts/{cartId}/coupon?concurrencyStamp={concurrencyStamp}", null, ct, key);
    public Task<CartResponse> ClearAsync(Guid cartId, Guid concurrencyStamp, string key, CancellationToken ct) => api.SendAsync<CartResponse>(HttpMethod.Delete, $"api/carts/{cartId}/items?concurrencyStamp={concurrencyStamp}", null, ct, key);
    public Task<CartCheckoutSummaryResponse> RepriceAsync(Guid cartId, CancellationToken ct) => api.SendAsync<CartCheckoutSummaryResponse>(HttpMethod.Post, $"api/carts/{cartId}/reprice", null, ct);
    public Task<CartCheckoutSummaryResponse> SummaryAsync(Guid cartId, CancellationToken ct) => api.SendAsync<CartCheckoutSummaryResponse>(HttpMethod.Get, $"api/carts/{cartId}/checkout-summary", null, ct);
}

public interface IMapsApi
{
    Task<IReadOnlyList<GeocodingResult>> GeocodeAsync(GeocodingRequest request, CancellationToken ct);
    Task<ReverseGeocodingResult> ReverseGeocodeAsync(ReverseGeocodingRequest request, CancellationToken ct);
    Task<DeliveryEligibilityResponse> EligibilityAsync(DeliveryEligibilityRequest request, CancellationToken ct);
}
public sealed class MapsApi(ApiClient api) : IMapsApi
{
    public Task<IReadOnlyList<GeocodingResult>> GeocodeAsync(GeocodingRequest request, CancellationToken ct) => api.SendAsync<IReadOnlyList<GeocodingResult>>(HttpMethod.Post, "api/v1/maps/geocode", request, ct);
    public Task<ReverseGeocodingResult> ReverseGeocodeAsync(ReverseGeocodingRequest request, CancellationToken ct) => api.SendAsync<ReverseGeocodingResult>(HttpMethod.Post, "api/v1/maps/reverse-geocode", request, ct);
    public Task<DeliveryEligibilityResponse> EligibilityAsync(DeliveryEligibilityRequest request, CancellationToken ct) => api.SendAsync<DeliveryEligibilityResponse>(HttpMethod.Post, "api/v1/maps/delivery-eligibility", request, ct);
}

public interface IOrdersApi
{
    Task<OrderListResponse> ListAsync(int page, CancellationToken ct);
    Task<OrderDetailsResponse> GetAsync(Guid id, CancellationToken ct);
    Task<CreateOrderResponse> CreateAsync(CreateOrderRequest request, string stableKey, CancellationToken ct);
    Task<OrderDetailsResponse> CancelAsync(Guid id, CancelOrderRequest request, CancellationToken ct);
}
public sealed class OrdersApi(ApiClient api) : IOrdersApi
{
    public Task<OrderListResponse> ListAsync(int page, CancellationToken ct) => api.SendAsync<OrderListResponse>(HttpMethod.Get, $"api/v1/orders/?page={page}&pageSize=20", null, ct);
    public Task<OrderDetailsResponse> GetAsync(Guid id, CancellationToken ct) => api.SendAsync<OrderDetailsResponse>(HttpMethod.Get, $"api/v1/orders/{id}", null, ct);
    public Task<CreateOrderResponse> CreateAsync(CreateOrderRequest request, string stableKey, CancellationToken ct) => api.SendAsync<CreateOrderResponse>(HttpMethod.Post, "api/v1/orders/", request, ct, stableKey);
    public Task<OrderDetailsResponse> CancelAsync(Guid id, CancelOrderRequest request, CancellationToken ct) => api.SendAsync<OrderDetailsResponse>(HttpMethod.Post, $"api/v1/orders/{id}/cancel", request, ct);
}

public interface ITrackingApi { Task<DriverLocationResponse> LatestAsync(Guid orderId, CancellationToken ct); }
public sealed class TrackingApi(ApiClient api) : ITrackingApi { public Task<DriverLocationResponse> LatestAsync(Guid orderId, CancellationToken ct) => api.SendAsync<DriverLocationResponse>(HttpMethod.Get, $"api/v1/tracking/orders/{orderId}/latest", null, ct); }

public interface INotificationsApi
{
    Task<NotificationListResponse> ListAsync(int page, CancellationToken ct);
    Task MarkReadAsync(Guid id, CancellationToken ct);
    Task MarkAllReadAsync(CancellationToken ct);
    Task<DeviceTokenResponse> RegisterAsync(RegisterDeviceTokenRequest request, CancellationToken ct);
    Task UnregisterAsync(Guid id, CancellationToken ct);
    Task<NotificationPreferencesResponse> PreferencesAsync(CancellationToken ct);
    Task<NotificationPreferencesResponse> UpdatePreferencesAsync(UpdateNotificationPreferencesRequest request, CancellationToken ct);
}
public sealed class NotificationsApi(ApiClient api) : INotificationsApi
{
    public Task<NotificationListResponse> ListAsync(int page, CancellationToken ct) => api.SendAsync<NotificationListResponse>(HttpMethod.Get, $"api/v1/notifications/?page={page}&pageSize=20", null, ct);
    public Task MarkReadAsync(Guid id, CancellationToken ct) => api.SendAsync(HttpMethod.Post, $"api/v1/notifications/{id}/read", null, ct);
    public Task MarkAllReadAsync(CancellationToken ct) => api.SendAsync(HttpMethod.Post, "api/v1/notifications/read-all", null, ct);
    public Task<DeviceTokenResponse> RegisterAsync(RegisterDeviceTokenRequest request, CancellationToken ct) => api.SendAsync<DeviceTokenResponse>(HttpMethod.Post, "api/v1/notifications/devices", request, ct);
    public Task UnregisterAsync(Guid id, CancellationToken ct) => api.SendAsync(HttpMethod.Delete, $"api/v1/notifications/devices/{id}", null, ct);
    public Task<NotificationPreferencesResponse> PreferencesAsync(CancellationToken ct) => api.SendAsync<NotificationPreferencesResponse>(HttpMethod.Get, "api/v1/notifications/preferences", null, ct);
    public Task<NotificationPreferencesResponse> UpdatePreferencesAsync(UpdateNotificationPreferencesRequest request, CancellationToken ct) => api.SendAsync<NotificationPreferencesResponse>(HttpMethod.Put, "api/v1/notifications/preferences", request, ct);
}
