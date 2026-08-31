using AlSsareea.CustomerApp.Core;

namespace AlSsareea.CustomerApp.UnitTests;

public sealed class ApplicationTests
{
    [Theory]
    [InlineData("alssareea://orders/1067b70f-0fdb-4ec2-a772-f1b7c232457d", AppRoutes.OrderDetails)]
    [InlineData("alssareea://tracking/1067b70f-0fdb-4ec2-a772-f1b7c232457d", AppRoutes.Tracking)]
    [InlineData("alssareea://notifications/1067b70f-0fdb-4ec2-a772-f1b7c232457d", AppRoutes.Notifications)]
    public void Supported_deep_links_parse(string value, string route) => Assert.Equal(route, DeepLinkParser.Parse(new(value))!.Route);

    [Theory]
    [InlineData("https://orders/1067b70f-0fdb-4ec2-a772-f1b7c232457d")]
    [InlineData("alssareea://admin/1067b70f-0fdb-4ec2-a772-f1b7c232457d")]
    [InlineData("alssareea://orders/not-a-guid")]
    public void Malformed_or_unsupported_deep_links_are_rejected(string value) => Assert.Null(DeepLinkParser.Parse(new(value)));

    [Fact]
    public void Logical_submission_reuses_key_until_completed()
    {
        var submission = new IdempotentSubmission(); string first = submission.CurrentKey;
        Assert.Equal(first, submission.CurrentKey); submission.Complete(); Assert.NotEqual(first, submission.CurrentKey);
    }

    [Fact]
    public async Task Async_command_prevents_duplicate_taps()
    {
        int calls = 0; var gate = new TaskCompletionSource(); var command = new AsyncCommand(async () => { Interlocked.Increment(ref calls); await gate.Task; });
        Task first = command.ExecuteAsync(); Task second = command.ExecuteAsync();
        Assert.Equal(1, calls); gate.SetResult(); await Task.WhenAll(first, second); Assert.Equal(1, calls);
    }

    [Fact] public void Push_values_match_backend_contract() { Assert.Equal(1, PushValues.Android); Assert.Equal(2, PushValues.Ios); Assert.Equal(1, PushValues.Fcm); Assert.Equal(2, PushValues.Apns); }
    [Theory][InlineData(1, "OrderDraft")][InlineData(4, "OrderSubmitted")][InlineData(15, "OrderDelivered")][InlineData(99, "OrderStatusUnknown")] public void Order_status_mapping_is_centralized(short value, string key) => Assert.Equal(key, OrderStatusPresentation.Key(value));
}
