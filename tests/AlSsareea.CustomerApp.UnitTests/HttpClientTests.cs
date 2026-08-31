using System.Net;
using AlSsareea.CustomerApp.Core;

namespace AlSsareea.CustomerApp.UnitTests;

public sealed class HttpClientTests
{
    [Fact]
    public async Task Merchant_discovery_composes_query_and_open_filter()
    {
        var handler = new StubHandler(_ => Responses.Json("{\"items\":[],\"page\":2,\"pageSize\":20,\"totalCount\":0}"));
        var api = new MerchantApi(Client(handler));
        await api.DiscoverAsync(2, 20, "Coffee shop", true, default);
        Assert.Equal("/api/v1/customer/merchants/?page=2&pageSize=20&query=Coffee%20shop&openNow=true", Assert.Single(handler.Requests).RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task Catalog_search_is_merchant_scoped_and_escaped()
    {
        Guid merchant = Guid.NewGuid(); var handler = new StubHandler(_ => Responses.Json("{\"items\":[],\"page\":1,\"pageSize\":20,\"totalCount\":0}"));
        await new CatalogApi(Client(handler)).ProductsAsync(merchant, 1, 20, "tea & cake", null, "ar", default);
        Assert.Contains($"/api/v1/merchants/{merchant}/catalog/products", handler.Requests[0].RequestUri!.PathAndQuery);
        Assert.Contains("query=tea%20%26%20cake", handler.Requests[0].RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task Product_details_composes_language_and_branch_query()
    {
        Guid merchant = Guid.NewGuid(), product = Guid.NewGuid(), branch = Guid.NewGuid(); var handler = new StubHandler(_ => Responses.Json(ProductJson(product, merchant)));
        CustomerProductDetailsResponse result = await new CatalogApi(Client(handler)).ProductAsync(merchant, product, "he", branch, default);
        Assert.Equal(product, result.Id); Assert.Equal($"/api/v1/merchants/{merchant}/catalog/products/{product}?language=he&branchId={branch}", handler.Requests[0].RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task Product_details_deserializes_media_variants_and_options()
    {
        Guid merchant = Guid.NewGuid(), product = Guid.NewGuid(); var handler = new StubHandler(_ => Responses.Json(ProductJson(product, merchant)));
        CustomerProductDetailsResponse result = await new CatalogApi(Client(handler)).ProductAsync(merchant, product, "en", null, default);
        Assert.True(result.IsAvailable); Assert.Single(result.Media); Assert.Single(result.Variants); Assert.Single(Assert.Single(result.OptionGroups).Options);
    }

    [Fact]
    public async Task Pricing_request_serializes_signed_adjustments_selection_ids()
    {
        Guid merchant = Guid.NewGuid(), product = Guid.NewGuid(), variant = Guid.NewGuid(), option = Guid.NewGuid(); string? body = null; var handler = new StubHandler(request => { body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult(); return Responses.Json($"{{\"productId\":\"{product}\",\"productVersion\":1,\"currency\":\"ILS\",\"basePriceMinor\":10,\"variantAdjustmentMinor\":-3,\"optionsAdjustmentMinor\":2,\"totalPriceMinor\":9,\"selectedVariant\":null,\"selectedOptions\":[]}}"); });
        CatalogPriceResponse response = await new CatalogApi(Client(handler)).PriceAsync(merchant, product, new(variant, [option], "ar"), default);
        Assert.Contains(variant.ToString(), body); Assert.Contains(option.ToString(), body); Assert.Equal(-3, response.VariantAdjustmentMinor);
    }

    [Fact]
    public async Task Cart_add_serializes_catalog_option_group_and_item_ids()
    {
        Guid cart = Guid.NewGuid(), product = Guid.NewGuid(), group = Guid.NewGuid(), option = Guid.NewGuid(); string? body = null; var handler = new StubHandler(request => { body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult(); return Responses.Json(CartJson(cart)); });
        await new CartApi(Client(handler)).AddAsync(cart, new(product, null, 1, null, [new(group, option)], Guid.NewGuid()), "key", default);
        Assert.Contains(group.ToString(), body); Assert.Contains(option.ToString(), body); Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
    }

    [Fact]
    public async Task Address_update_uses_owned_customer_route_and_concurrency_body()
    {
        Guid id = Guid.NewGuid(), stamp = Guid.NewGuid(); string? body = null; var handler = new StubHandler(request => { body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult(); return Responses.Json(AddressJson(id, stamp)); });
        await new CustomerApi(Client(handler)).UpdateAddressAsync(id, new("Home", 1, "City", null, "Street", null, null, null, null, null, null, null, null, true, stamp), default);
        Assert.Equal($"/api/v1/customers/me/addresses/{id}", handler.Requests[0].RequestUri!.AbsolutePath); Assert.Contains(stamp.ToString(), body);
    }

    [Fact]
    public async Task Notifications_clients_use_exact_inbox_and_preferences_routes()
    {
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath.EndsWith("preferences", StringComparison.Ordinal) ? Responses.Json("{\"items\":[]}") : Responses.Json("{\"items\":[],\"page\":1,\"pageSize\":20,\"totalCount\":0,\"unreadCount\":0}")); var api = new NotificationsApi(Client(handler));
        await api.ListAsync(1, default); await api.PreferencesAsync(default); Assert.Equal("/api/v1/notifications/?page=1&pageSize=20", handler.Requests[0].RequestUri!.PathAndQuery); Assert.Equal("/api/v1/notifications/preferences", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Order_create_sends_stable_idempotency_key()
    {
        var handler = new StubHandler(_ => Responses.Json($"{{\"orderId\":\"{Guid.NewGuid()}\",\"orderNumber\":\"O1\",\"status\":2,\"currency\":\"ILS\",\"totalMinor\":100,\"createdAtUtc\":\"2026-01-01T00:00:00Z\"}}", HttpStatusCode.Created));
        var api = new OrdersApi(Client(handler)); string key = Guid.NewGuid().ToString("N");
        await api.CreateAsync(new(Guid.NewGuid(), Guid.NewGuid(), 1, null, null, Guid.NewGuid()), key, default);
        Assert.Equal(key, Assert.Single(handler.Requests).Headers.GetValues("Idempotency-Key").Single());
    }

    [Fact]
    public async Task Problem_details_are_parsed()
    {
        var handler = new StubHandler(_ => Responses.Json("{\"title\":\"Invalid\",\"code\":\"maps.invalid_request\",\"errors\":{\"query\":[\"required\"]}}", HttpStatusCode.BadRequest));
        ApiException exception = await Assert.ThrowsAsync<ApiException>(() => Client(handler).SendAsync<object>(HttpMethod.Get, "failure", null, default));
        Assert.Equal("maps.invalid_request", exception.Problem.Code); Assert.Equal("required", exception.Problem.Errors["query"][0]);
    }

    [Fact]
    public async Task Empty_success_body_is_valid_for_commands()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        await Client(handler).SendAsync(HttpMethod.Delete, "resource", null, default);
    }

    [Fact]
    public async Task Cancellation_token_is_honored()
    {
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        var handler = new DelayedHandler();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Client(handler).SendAsync<object>(HttpMethod.Get, "slow", null, cancelled.Token));
    }

    [Fact]
    public async Task Safe_read_retries_one_transient_response()
    {
        var terminal = new SequenceHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK); var retry = new SafeReadRetryHandler { InnerHandler = terminal };
        using HttpResponseMessage response = await new HttpClient(retry).GetAsync("https://localhost/read");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal(2, terminal.Calls);
    }

    [Fact]
    public async Task Writes_are_never_generically_retried()
    {
        var terminal = new SequenceHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK); var retry = new SafeReadRetryHandler { InnerHandler = terminal };
        using HttpResponseMessage response = await new HttpClient(retry).PostAsync("https://localhost/write", new StringContent("{}"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); Assert.Equal(1, terminal.Calls);
    }

    private static ApiClient Client(HttpMessageHandler handler) => new(new HttpClient(handler) { BaseAddress = new("https://localhost/") });
    private static string ProductJson(Guid product, Guid merchant) => $"{{\"id\":\"{product}\",\"catalogId\":\"{Guid.NewGuid()}\",\"merchantId\":\"{merchant}\",\"categoryId\":null,\"sku\":null,\"basePriceMinor\":100,\"currency\":\"ILS\",\"taxCategoryReference\":null,\"status\":1,\"inventoryStatus\":1,\"sortOrder\":1,\"isVisible\":true,\"isFeatured\":false,\"currentVersion\":1,\"text\":{{\"languageCode\":\"en\",\"name\":\"Tea\",\"description\":null}},\"createdAtUtc\":\"2026-01-01T00:00:00Z\",\"updatedAtUtc\":\"2026-01-01T00:00:00Z\",\"concurrencyStamp\":\"{Guid.NewGuid()}\",\"isAvailable\":true,\"media\":[{{\"id\":\"{Guid.NewGuid()}\",\"mediaId\":null,\"url\":\"https://example.test/a.jpg\",\"altText\":\"Tea\",\"sortOrder\":1,\"isPrimary\":true}}],\"variants\":[{{\"id\":\"{Guid.NewGuid()}\",\"text\":{{\"languageCode\":\"en\",\"name\":\"Large\",\"description\":null}},\"priceAdjustmentMinor\":-10,\"inventoryStatus\":1,\"isDefault\":true,\"isAvailable\":true,\"sortOrder\":1}}],\"optionGroups\":[{{\"id\":\"{Guid.NewGuid()}\",\"text\":{{\"languageCode\":\"en\",\"name\":\"Milk\",\"description\":null}},\"selectionType\":1,\"isRequired\":true,\"minSelections\":1,\"maxSelections\":1,\"sortOrder\":1,\"options\":[{{\"id\":\"{Guid.NewGuid()}\",\"text\":{{\"languageCode\":\"en\",\"name\":\"Oat\",\"description\":null}},\"priceAdjustmentMinor\":5,\"isDefault\":true,\"isAvailable\":true,\"sortOrder\":1}}]}}]}}";
    private static string CartJson(Guid id) => $"{{\"id\":\"{id}\",\"customerId\":\"{Guid.NewGuid()}\",\"merchantId\":\"{Guid.NewGuid()}\",\"branchId\":null,\"status\":1,\"couponCode\":null,\"expiresAtUtc\":\"2026-01-01T01:00:00Z\",\"lastPricedAtUtc\":null,\"createdAtUtc\":\"2026-01-01T00:00:00Z\",\"updatedAtUtc\":\"2026-01-01T00:00:00Z\",\"concurrencyStamp\":\"{Guid.NewGuid()}\",\"items\":[]}}";
    private static string AddressJson(Guid id, Guid stamp) => $"{{\"id\":\"{id}\",\"label\":\"Home\",\"addressType\":1,\"city\":\"City\",\"area\":null,\"street\":\"Street\",\"buildingNumber\":null,\"floor\":null,\"apartment\":null,\"postalCode\":null,\"placeId\":null,\"latitude\":null,\"longitude\":null,\"deliveryInstructions\":null,\"isDefault\":true,\"createdAtUtc\":\"2026-01-01T00:00:00Z\",\"updatedAtUtc\":\"2026-01-01T00:00:00Z\",\"concurrencyStamp\":\"{stamp}\"}}";
    private sealed class DelayedHandler : HttpMessageHandler { protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) { await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken); return Responses.Json("{}"); } }
    private sealed class SequenceHandler(params HttpStatusCode[] statuses) : HttpMessageHandler { public int Calls { get; private set; } protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(statuses[Calls++])); }
}
