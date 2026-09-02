namespace AlSsareea.CustomerApp.Core;

public sealed record CustomerProfileHints(string? FirstName, string? LastName, DateOnly? DateOfBirth = null);
public sealed record CustomerProfileBootstrapResult(bool IsComplete, CustomerResponse? Profile);

public interface ICustomerProfileBootstrapper
{
    Task<CustomerProfileBootstrapResult> EnsureAsync(CustomerProfileHints? hints, CancellationToken ct);
}

public sealed class CustomerProfileBootstrapper(ICustomerApi customers) : ICustomerProfileBootstrapper
{
    public async Task<CustomerProfileBootstrapResult> EnsureAsync(CustomerProfileHints? hints, CancellationToken ct)
    {
        try { return new(true, await customers.GetAsync(ct)); }
        catch (ApiException ex) when (ex.Problem.Status == 404)
        {
            if (string.IsNullOrWhiteSpace(hints?.FirstName) || string.IsNullOrWhiteSpace(hints.LastName)) return new(false, null);
            try
            {
                CustomerResponse profile = await customers.CreateAsync(new(hints.FirstName.Trim(), hints.LastName.Trim(), hints.DateOfBirth), ct);
                return new(true, profile);
            }
            catch (ApiException createError) when (createError.Problem.Status == 409)
            {
                return new(true, await customers.GetAsync(ct));
            }
        }
    }
}

internal static class AuthenticationFlow
{
    internal static LoginDeviceRequest Device(string deviceIdentifier) => new(deviceIdentifier, "Customer app", DevicePlatformDetector.Current(), null, null);

    internal static async Task RouteAfterAuthenticationAsync(TokenResponse tokens, string deviceIdentifier, CustomerProfileHints? hints, ISessionManager session, ICustomerProfileBootstrapper profiles, INavigationService navigation, CancellationToken ct)
    {
        await session.SetAsync(tokens, deviceIdentifier, ct);
        try
        {
            CustomerProfileBootstrapResult result = await profiles.EnsureAsync(hints, ct);
            if (result.IsComplete) { await navigation.GoToAsync(AppRoutes.Home); return; }
        }
        catch (ApiException) { }
        catch (ApiNetworkException) { }
        catch (ApiTimeoutException) { }
        var parameters = new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(hints?.FirstName)) parameters["firstName"] = hints.FirstName;
        if (!string.IsNullOrWhiteSpace(hints?.LastName)) parameters["lastName"] = hints.LastName;
        await navigation.GoToAsync(AppRoutes.CompleteProfile, parameters);
    }
}
