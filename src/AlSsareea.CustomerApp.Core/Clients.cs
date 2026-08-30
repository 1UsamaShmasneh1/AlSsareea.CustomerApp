namespace AlSsareea.CustomerApp.Core;

public sealed class AuthenticationApi(ApiClient api) : IAuthenticationApi
{
    public Task<TokenResponse> LoginAsync(LoginRequest r, CancellationToken ct) => api.SendAsync<TokenResponse>(HttpMethod.Post, "api/v1/auth/login", r, ct);
    public Task<TokenResponse> RefreshAsync(RefreshRequest r, CancellationToken ct) => api.SendAsync<TokenResponse>(HttpMethod.Post, "api/v1/auth/refresh", r, ct);
    public Task LogoutAsync(string key, CancellationToken ct) => api.SendAsync(HttpMethod.Post, "api/v1/auth/logout", null, ct, key);
    public Task<OtpChallengeResponse> RequestOtpAsync(OtpChallengeRequest r, string key, CancellationToken ct) => api.SendAsync<OtpChallengeResponse>(HttpMethod.Post, "api/v1/auth/otp/challenges", r, ct, key);
    public Task VerifyOtpAsync(Guid id, OtpVerifyRequest r, CancellationToken ct) => api.SendAsync(HttpMethod.Post, $"api/v1/auth/otp/challenges/{id}/verify", r, ct);
}
public sealed class CustomerApi(ApiClient api)
{
    public Task<CustomerResponse> GetAsync(CancellationToken ct) => api.SendAsync<CustomerResponse>(HttpMethod.Get, "api/v1/customers/me/", null, ct);
    public Task<IReadOnlyList<AddressResponse>> AddressesAsync(CancellationToken ct) => api.SendAsync<IReadOnlyList<AddressResponse>>(HttpMethod.Get, "api/v1/customers/me/addresses", null, ct);
}
public sealed class CatalogApi(ApiClient api)
{
    public Task<IReadOnlyList<CategoryResponse>> CategoriesAsync(Guid merchantId, CancellationToken ct) => api.SendAsync<IReadOnlyList<CategoryResponse>>(HttpMethod.Get, $"api/v1/merchants/{merchantId}/catalog/categories", null, ct);
    public Task<ProductListResponse> ProductsAsync(Guid merchantId, int page, int pageSize, string? search, CancellationToken ct) => api.SendAsync<ProductListResponse>(HttpMethod.Get, $"api/v1/merchants/{merchantId}/catalog/products?page={page}&pageSize={pageSize}&search={Uri.EscapeDataString(search ?? string.Empty)}", null, ct);
    public Task<ProductResponse> ProductAsync(Guid merchantId, Guid productId, CancellationToken ct) => api.SendAsync<ProductResponse>(HttpMethod.Get, $"api/v1/merchants/{merchantId}/catalog/products/{productId}", null, ct);
}
public sealed class CartApi(ApiClient api)
{
    public Task<CartResponse> ActiveAsync(CancellationToken ct) => api.SendAsync<CartResponse>(HttpMethod.Get, "api/carts/active", null, ct);
    public Task<CartResponse> CreateAsync(GetOrCreateActiveCartRequest r, string key, CancellationToken ct) => api.SendAsync<CartResponse>(HttpMethod.Post, "api/carts/", r, ct, key);
    public Task<CartCheckoutSummaryResponse> SummaryAsync(Guid id, CancellationToken ct) => api.SendAsync<CartCheckoutSummaryResponse>(HttpMethod.Get, $"api/carts/{id}/checkout-summary", null, ct);
}
public sealed class OrdersApi(ApiClient api)
{
    public Task<OrderListResponse> ListAsync(int page, CancellationToken ct) => api.SendAsync<OrderListResponse>(HttpMethod.Get, $"api/v1/orders/?page={page}&pageSize=20", null, ct);
    public Task<OrderDetailsResponse> GetAsync(Guid id, CancellationToken ct) => api.SendAsync<OrderDetailsResponse>(HttpMethod.Get, $"api/v1/orders/{id}", null, ct);
    public Task<CreateOrderResponse> CreateAsync(CreateOrderRequest r, string stableKey, CancellationToken ct) => api.SendAsync<CreateOrderResponse>(HttpMethod.Post, "api/v1/orders/", r, ct, stableKey);
}
public sealed class TrackingApi(ApiClient api) { public Task<DriverLocationResponse> LatestAsync(Guid orderId, CancellationToken ct) => api.SendAsync<DriverLocationResponse>(HttpMethod.Get, $"api/v1/tracking/orders/{orderId}/latest", null, ct); }
public sealed class NotificationsApi(ApiClient api)
{
    public Task<NotificationListResponse> ListAsync(int page, CancellationToken ct) => api.SendAsync<NotificationListResponse>(HttpMethod.Get, $"api/v1/notifications/?page={page}&pageSize=20", null, ct);
    public Task<DeviceTokenResponse> RegisterAsync(RegisterDeviceTokenRequest r, CancellationToken ct) => api.SendAsync<DeviceTokenResponse>(HttpMethod.Post, "api/v1/notifications/devices", r, ct);
}
