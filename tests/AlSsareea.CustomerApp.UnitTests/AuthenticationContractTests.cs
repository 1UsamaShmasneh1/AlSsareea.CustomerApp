using System.Net;
using System.Text.Json;
using AlSsareea.CustomerApp.Core;

namespace AlSsareea.CustomerApp.UnitTests;

public sealed class AuthenticationContractTests
{
    [Theory]
    [InlineData(DevicePlatform.Android, 1)]
    [InlineData(DevicePlatform.Ios, 2)]
    [InlineData(DevicePlatform.Windows, 4)]
    public void Login_payload_uses_backend_numeric_platform(DevicePlatform platform, int expected)
    {
        var request = new LoginRequest(
            "customer@example.test",
            "synthetic-password",
            new("synthetic-device", "Customer app", platform, "1.0", "Synthetic OS"));

        string json = JsonSerializer.Serialize(request, ApiJson.Options);
        using JsonDocument payload = JsonDocument.Parse(json);
        JsonElement root = payload.RootElement;
        Assert.Equal("customer@example.test", root.GetProperty("identifier").GetString());
        Assert.Equal("synthetic-password", root.GetProperty("password").GetString());
        JsonElement device = root.GetProperty("device");
        Assert.Equal("synthetic-device", device.GetProperty("deviceIdentifier").GetString());
        Assert.Equal(JsonValueKind.Number, device.GetProperty("platform").ValueKind);
        Assert.Equal(expected, device.GetProperty("platform").GetInt32());

        BackendLoginRequest? compatible = JsonSerializer.Deserialize<BackendLoginRequest>(json, ApiJson.Options);
        Assert.NotNull(compatible);
        Assert.Equal((BackendDevicePlatform)expected, compatible.Device.Platform);
    }

    [Fact]
    public void Current_runtime_maps_to_backend_platform()
    {
        DevicePlatform expected = OperatingSystem.IsWindows() ? DevicePlatform.Windows :
            OperatingSystem.IsLinux() ? DevicePlatform.Linux :
            OperatingSystem.IsMacOS() ? DevicePlatform.MacOs :
            throw new PlatformNotSupportedException();

        Assert.Equal(expected, DevicePlatformDetector.Current());
    }

    [Fact]
    public void Otp_and_refresh_payloads_remain_numeric_and_camel_case()
    {
        using JsonDocument otp = JsonDocument.Parse(JsonSerializer.Serialize(new OtpChallengeRequest("customer@example.test", OtpPurpose.Login, "synthetic-device"), ApiJson.Options));
        Assert.Equal(1, otp.RootElement.GetProperty("purpose").GetInt32());
        Assert.Equal("synthetic-device", otp.RootElement.GetProperty("deviceIdentifier").GetString());

        using JsonDocument verify = JsonDocument.Parse(JsonSerializer.Serialize(new OtpVerifyRequest("000000", "synthetic-device"), ApiJson.Options));
        Assert.Equal("000000", verify.RootElement.GetProperty("code").GetString());

        using JsonDocument refresh = JsonDocument.Parse(JsonSerializer.Serialize(new RefreshRequest("synthetic-refresh-token", "synthetic-device"), ApiJson.Options));
        Assert.Equal("synthetic-refresh-token", refresh.RootElement.GetProperty("refreshToken").GetString());
        Assert.Equal("synthetic-device", refresh.RootElement.GetProperty("deviceIdentifier").GetString());
    }

    [Fact]
    public async Task Authentication_client_sends_numeric_windows_payload_and_maps_invalid_credentials()
    {
        string? body = null;
        var handler = new StubHandler(request =>
        {
            body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return Responses.Json("{\"title\":\"Unauthorized\",\"status\":401,\"code\":\"auth.invalid_credentials\"}", HttpStatusCode.Unauthorized);
        });
        var api = new AuthenticationApi(new(new HttpClient(handler) { BaseAddress = new("http://localhost:5257/") }));

        ApiException exception = await Assert.ThrowsAsync<ApiException>(() => api.LoginAsync(
            new("customer@example.test", "synthetic-password", new("synthetic-device", "Customer app", DevicePlatform.Windows, "1.0", "Windows")),
            default));

        using JsonDocument payload = JsonDocument.Parse(body!);
        Assert.Equal(4, payload.RootElement.GetProperty("device").GetProperty("platform").GetInt32());
        Assert.Equal("auth.invalid_credentials", exception.Problem.Code);
        Assert.Equal("ErrorUnauthorized", UiErrorMapper.Map(exception, new TestText(), true));
    }

    [Fact]
    public void Registration_and_google_payloads_match_backend_contract()
    {
        var device = new LoginDeviceRequest("synthetic-device", "Customer app", DevicePlatform.Android, null, null);
        using JsonDocument registration = JsonDocument.Parse(JsonSerializer.Serialize(new RegisterCustomerRequest("customer@example.test", "synthetic-password", device), ApiJson.Options));
        Assert.Equal("customer@example.test", registration.RootElement.GetProperty("email").GetString()); Assert.Equal(1, registration.RootElement.GetProperty("device").GetProperty("platform").GetInt32());
        using JsonDocument google = JsonDocument.Parse(JsonSerializer.Serialize(new GoogleAuthenticationRequest("synthetic-id-token", "synthetic-nonce", device), ApiJson.Options));
        Assert.Equal("synthetic-id-token", google.RootElement.GetProperty("idToken").GetString()); Assert.Equal("synthetic-nonce", google.RootElement.GetProperty("nonce").GetString());
    }

    private sealed record BackendLoginRequest(string Identifier, string Password, BackendLoginDeviceRequest Device);
    private sealed record BackendLoginDeviceRequest(string DeviceIdentifier, string? DeviceName, BackendDevicePlatform Platform, string? AppVersion, string? OperatingSystemVersion);
    private enum BackendDevicePlatform : short { Android = 1, Ios = 2, Web = 3, Windows = 4, MacOs = 5, Linux = 6 }
}
