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
    private sealed class DelayedHandler : HttpMessageHandler { protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) { await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken); return Responses.Json("{}"); } }
    private sealed class SequenceHandler(params HttpStatusCode[] statuses) : HttpMessageHandler { public int Calls { get; private set; } protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(statuses[Calls++])); }
}
