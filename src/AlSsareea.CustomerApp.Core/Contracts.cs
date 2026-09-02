namespace AlSsareea.CustomerApp.Core;

// Numeric values are part of the Backend Identity wire contract. Do not reorder or stringify them.
public enum DevicePlatform : short { Android = 1, Ios = 2, Web = 3, Windows = 4, MacOs = 5, Linux = 6 }

public static class DevicePlatformDetector
{
    public static DevicePlatform Current() =>
        OperatingSystem.IsAndroid() ? DevicePlatform.Android :
        OperatingSystem.IsIOS() ? DevicePlatform.Ios :
        OperatingSystem.IsMacCatalyst() || OperatingSystem.IsMacOS() ? DevicePlatform.MacOs :
        OperatingSystem.IsWindows() ? DevicePlatform.Windows :
        OperatingSystem.IsLinux() ? DevicePlatform.Linux :
        throw new PlatformNotSupportedException("The current operating system has no Backend device-platform contract value.");
}
public enum OtpPurpose : short { Login = 1, PasswordReset, PhoneVerification, EmailVerification }

public sealed record LoginDeviceRequest(string DeviceIdentifier, string? DeviceName, DevicePlatform Platform, string? AppVersion, string? OperatingSystemVersion);
public sealed record LoginRequest(string Identifier, string Password, LoginDeviceRequest Device);
public sealed record RegisterCustomerRequest(string Email, string Password, LoginDeviceRequest Device);
public sealed record GoogleAuthenticationRequest(string IdToken, string? Nonce, LoginDeviceRequest Device);
public sealed record RefreshRequest(string RefreshToken, string DeviceIdentifier);
public sealed record OtpChallengeRequest(string Destination, OtpPurpose Purpose, string DeviceIdentifier);
public sealed record OtpVerifyRequest(string Code, string DeviceIdentifier);
public sealed record AuthenticatedUserResponse(Guid Id, string UserType);
public sealed record TokenResponse(string TokenType, string AccessToken, int ExpiresIn, string RefreshToken, DateTime RefreshTokenExpiresUtc, Guid SessionId, AuthenticatedUserResponse User);
public sealed record GoogleAuthenticationResponse(TokenResponse Tokens, bool IsNewUser, string Email, string? GivenName, string? FamilyName);
public sealed record OtpChallengeResponse(Guid ChallengeId, DateTime ExpiresUtc, DateTime NextResendUtc, string? DevelopmentCode);

public sealed record CustomerResponse(Guid Id, string FirstName, string LastName, string DisplayName, DateOnly? DateOfBirth, short Status, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, Guid ConcurrencyStamp);
public sealed record CreateCustomerRequest(string FirstName, string LastName, DateOnly? DateOfBirth);
public sealed record AddressRequest(string Label, short AddressType, string City, string? Area, string Street, string? BuildingNumber, string? Floor, string? Apartment, string? PostalCode, string? PlaceId, double? Latitude, double? Longitude, string? DeliveryInstructions, bool IsDefault, Guid? ConcurrencyStamp);
public sealed record AddressResponse(Guid Id, string Label, short AddressType, string City, string? Area, string Street, string? BuildingNumber, string? Floor, string? Apartment, string? PostalCode, string? PlaceId, double? Latitude, double? Longitude, string? DeliveryInstructions, bool IsDefault, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, Guid ConcurrencyStamp);
public sealed record UpdateCustomerRequest(string FirstName, string LastName, DateOnly? DateOfBirth, Guid ConcurrencyStamp);
public sealed record UpdateCustomerPreferencesRequest(string PreferredLanguage, string PreferredCurrency, bool AllowMarketingNotifications, bool AllowOrderStatusNotifications, bool AllowPromotionalNotifications, Guid ConcurrencyStamp);
public sealed record CustomerPreferencesResponse(string PreferredLanguage, string PreferredCurrency, bool AllowMarketingNotifications, bool AllowOrderStatusNotifications, bool AllowPromotionalNotifications, DateTime UpdatedAtUtc, Guid ConcurrencyStamp);

public sealed record CustomerMerchantBranchSummary(Guid Id, string Name, string City, string? Area, string Street, double Latitude, double Longitude, bool IsPrimary, bool IsOpen);
public sealed record CustomerMerchantSummary(Guid Id, string DisplayName, string? Description, bool IsOpen, CustomerMerchantBranchSummary? PrimaryBranch);
public sealed record CustomerMerchantListResponse(IReadOnlyList<CustomerMerchantSummary> Items, int Page, int PageSize, int TotalCount);
public sealed record CustomerMerchantDetails(Guid Id, string DisplayName, string? Description, bool IsOpen, IReadOnlyList<CustomerMerchantBranchSummary> Branches, string CatalogPath);

public sealed record LocalizedTextResponse(string LanguageCode, string Name, string? Description);
public sealed record CategoryResponse(Guid Id, Guid CatalogId, Guid MerchantId, Guid? ParentCategoryId, Guid? MediaAssetId, int SortOrder, bool IsVisible, LocalizedTextResponse Text, Guid ConcurrencyStamp);
public sealed record MenuSectionResponse(Guid Id, Guid CatalogId, Guid MerchantId, int SortOrder, bool IsVisible, DateTime? AvailableFromUtc, DateTime? AvailableUntilUtc, LocalizedTextResponse Text, IReadOnlyList<Guid> ProductIds, Guid ConcurrencyStamp);
public sealed record ProductResponse(Guid Id, Guid CatalogId, Guid MerchantId, Guid? CategoryId, string? Sku, long BasePriceMinor, string Currency, string? TaxCategoryReference, short Status, short InventoryStatus, int SortOrder, bool IsVisible, bool IsFeatured, int CurrentVersion, LocalizedTextResponse Text, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, Guid ConcurrencyStamp);
public sealed record CustomerProductMediaResponse(Guid Id, Guid? MediaId, string Url, string? AltText, int SortOrder, bool IsPrimary);
public sealed record CustomerProductVariantResponse(Guid Id, LocalizedTextResponse Text, long PriceAdjustmentMinor, short InventoryStatus, bool IsDefault, bool IsAvailable, int SortOrder);
public sealed record CustomerProductOptionResponse(Guid Id, LocalizedTextResponse Text, long PriceAdjustmentMinor, bool IsDefault, bool IsAvailable, int SortOrder);
public sealed record CustomerProductOptionGroupResponse(Guid Id, LocalizedTextResponse Text, short SelectionType, bool IsRequired, int MinSelections, int MaxSelections, int SortOrder, IReadOnlyList<CustomerProductOptionResponse> Options);
public sealed record CustomerProductDetailsResponse(Guid Id, Guid CatalogId, Guid MerchantId, Guid? CategoryId, string? Sku, long BasePriceMinor, string Currency, string? TaxCategoryReference, short Status, short InventoryStatus, int SortOrder, bool IsVisible, bool IsFeatured, int CurrentVersion, LocalizedTextResponse Text, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, Guid ConcurrencyStamp, bool IsAvailable, IReadOnlyList<CustomerProductMediaResponse> Media, IReadOnlyList<CustomerProductVariantResponse> Variants, IReadOnlyList<CustomerProductOptionGroupResponse> OptionGroups);
public sealed record ProductListResponse(IReadOnlyList<ProductResponse> Items, int Page, int PageSize, int TotalCount);
public sealed record PriceRequest(Guid? VariantId, IReadOnlyList<Guid> OptionIds, string? Language);
public sealed record SelectedPriceItem(Guid Id, string Name, long AdjustmentMinor);
public sealed record CatalogPriceResponse(Guid ProductId, int ProductVersion, string Currency, long BasePriceMinor, long VariantAdjustmentMinor, long OptionsAdjustmentMinor, long TotalPriceMinor, SelectedPriceItem? SelectedVariant, IReadOnlyList<SelectedPriceItem> SelectedOptions);

public sealed record GetOrCreateActiveCartRequest(Guid MerchantId, Guid? BranchId);
public sealed record CartItemOptionRequest(Guid OptionGroupId, Guid OptionItemId, int Quantity = 1);
public sealed record AddCartItemRequest(Guid ProductId, Guid? ProductVariantId, int Quantity, string? CustomerNote, IReadOnlyList<CartItemOptionRequest> SelectedOptions, Guid ConcurrencyStamp);
public sealed record UpdateCartItemQuantityRequest(int Quantity, Guid ConcurrencyStamp);
public sealed record ApplyCartCouponRequest(string CouponCode, Guid ConcurrencyStamp);
public sealed record CartItemOptionResponse(Guid OptionGroupId, Guid OptionItemId, int Quantity, int CatalogVersion);
public sealed record CartItemResponse(Guid Id, Guid ProductId, Guid? ProductVariantId, int Quantity, string? CustomerNote, int CatalogVersion, IReadOnlyList<CartItemOptionResponse> SelectedOptions, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record CartResponse(Guid Id, Guid CustomerId, Guid MerchantId, Guid? BranchId, short Status, string? CouponCode, DateTime ExpiresAtUtc, DateTime? LastPricedAtUtc, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, Guid ConcurrencyStamp, IReadOnlyList<CartItemResponse> Items);
public sealed record CartBlockingReason(string Code, string Message);
public sealed record CartCheckoutOptionResponse(Guid OptionGroupId, Guid OptionId, string OptionGroupName, string OptionName, int Quantity, long UnitPriceAdjustmentMinor, long TotalPriceAdjustmentMinor);
public sealed record CartCheckoutItemResponse(Guid CartItemId, Guid ProductId, int ProductVersion, string? ProductName, string? Sku, Guid? VariantId, string? VariantName, int Quantity, long UnitBasePriceMinor, long UnitOptionsPriceMinor, long UnitPriceMinor, long LineSubtotalMinor, long LineDiscountMinor, long LineTotalMinor, string? CustomerNote, bool IsAvailable, bool HasChanged, IReadOnlyList<CartCheckoutOptionResponse> Options, IReadOnlyList<CartBlockingReason> BlockingReasons);
public sealed record CartCheckoutSummaryResponse(Guid CartId, Guid CustomerId, Guid MerchantId, Guid? BranchId, short CartStatus, string? Currency, IReadOnlyList<CartCheckoutItemResponse> Items, long SubtotalMinor, long DeliveryFeeMinor, long ServiceFeeMinor, long TaxMinor, long OtherFeesMinor, long PromotionDiscountMinor, long GrandTotalMinor, IReadOnlyList<CartBlockingReason> BlockingReasons, bool IsCheckoutReady, string? PricingReference, string? PromotionEvaluationReference, DateTime CalculatedAtUtc, Guid ConcurrencyStamp, DateTime ExpiresAtUtc);

public sealed record CreateOrderRequest(Guid CartId, Guid DeliveryAddressId, short OrderType, DateTime? ScheduledForUtc, string? CustomerNotes, Guid? ExpectedCartVersion);
public sealed record CreateOrderResponse(Guid OrderId, string OrderNumber, short Status, string Currency, long TotalMinor, DateTime CreatedAtUtc);
public sealed record OrderListItemResponse(Guid Id, string OrderNumber, short Type, short Status, string Currency, long TotalMinor, string MerchantDisplayName, DateTime CreatedAtUtc, DateTime? ScheduledForUtc);
public sealed record OrderListResponse(IReadOnlyList<OrderListItemResponse> Items, int Page, int PageSize, int TotalCount);
public sealed record CancelOrderRequest(short Actor, string ReasonCode, string? Reason, Guid ConcurrencyStamp);
public sealed record OrderOptionResponse(Guid OptionGroupId, Guid OptionId, string OptionGroupName, string OptionName, int Quantity, long UnitPriceAdjustmentMinor, long TotalPriceAdjustmentMinor);
public sealed record OrderItemResponse(Guid Id, Guid ProductId, int ProductVersion, Guid? VariantId, string ProductName, string? VariantName, string? Sku, int Quantity, long UnitBasePriceMinor, long UnitOptionsPriceMinor, long UnitDiscountMinor, long UnitFinalPriceMinor, long LineSubtotalMinor, long LineDiscountMinor, long LineTotalMinor, string? CustomerNote, IReadOnlyList<OrderOptionResponse> Options);
public sealed record OrderCustomerResponse(Guid CustomerId, string DisplayName, string PreferredLanguage);
public sealed record OrderAddressResponse(Guid AddressId, string Label, string City, string? Area, string Street, string? BuildingNumber, string? Floor, string? Apartment, string? DeliveryInstructions, double? Latitude, double? Longitude, string? PlaceId, string? FormattedAddress);
public sealed record OrderMerchantResponse(Guid MerchantId, Guid? BranchId, string MerchantDisplayName, string? BranchDisplayName, string? BranchAddress, string? BranchPhoneNumber);
public sealed record OrderPricingResponse(long SubtotalMinor, long OptionsTotalMinor, long ProductDiscountMinor, long CouponDiscountMinor, long DeliveryDiscountMinor, long DeliveryFeeMinor, long ServiceFeeMinor, long PlatformFeeMinor, long SmallOrderFeeMinor, long TaxMinor, long TotalMinor, string Currency, string? PricingReference, DateTime CalculatedAtUtc);
public sealed record OrderTimelineEntryResponse(Guid Id, short? PreviousStatus, short NewStatus, DateTime ChangedAtUtc, short Source, string? ReasonCode, string? ReasonText, string? CorrelationId);
public sealed record OrderDetailsResponse(Guid Id, string OrderNumber, Guid SourceCartId, short Type, short Status, string Currency, long TotalMinor, DateTime? ScheduledForUtc, string? CustomerNotes, string? CancellationCode, string? CancellationReason, short? CancelledBy, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, Guid ConcurrencyStamp, OrderCustomerResponse Customer, OrderAddressResponse DeliveryAddress, OrderMerchantResponse Merchant, OrderPricingResponse Pricing, IReadOnlyList<OrderItemResponse> Items, IReadOnlyList<OrderTimelineEntryResponse> Timeline);
public sealed record DeliveryResponse(Guid Id, Guid OrderId, Guid CustomerId, Guid MerchantId, Guid? BranchId, Guid? DriverId, short Status, short ProofRequirements, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, DateTime? AssignedAtUtc, DateTime? ArrivedAtPickupAtUtc, DateTime? PickedUpAtUtc, DateTime? StartedAtUtc, DateTime? ArrivedAtDropOffAtUtc, DateTime? DeliveredAtUtc, DateTime? FailedAtUtc, short? FailureReason, string? FailureNotes, Guid ConcurrencyStamp, object Pickup, object DropOff, IReadOnlyList<object> Timeline, IReadOnlyList<object> Proofs);
public sealed record DriverLocationResponse(Guid LocationId, Guid DriverId, double Latitude, double Longitude, DateTime RecordedAtUtc, DateTime ReceivedAtUtc, double AccuracyMeters, double? SpeedMetersPerSecond, double? HeadingDegrees, long SequenceNumber);
public sealed record NotificationListItem(Guid Id, string Category, string TemplateKey, short Channel, string Language, string? Subject, string Body, short Status, DateTime CreatedAtUtc, DateTime? ReadAtUtc);
public sealed record NotificationListResponse(IReadOnlyList<NotificationListItem> Items, int Page, int PageSize, int TotalCount, int UnreadCount);
public sealed record RegisterDeviceTokenRequest(string Token, short Platform, short Provider);
public sealed record DeviceTokenResponse(Guid Id, short Platform, short Provider, string TokenMask, bool Active, DateTime UpdatedAtUtc);
public sealed record NotificationPreferenceItem(string Category, short Channel, bool Enabled);
public sealed record NotificationPreferencesResponse(IReadOnlyList<NotificationPreferenceItem> Items);
public sealed record UpdateNotificationPreferencesRequest(IReadOnlyList<NotificationPreferenceItem> Items);

public sealed record GeocodingRequest(string Query, string? CountryCode = null);
public sealed record GeocodingResult(string FormattedAddress, double Latitude, double Longitude, string? PlaceId = null);
public sealed record ReverseGeocodingRequest(double Latitude, double Longitude);
public sealed record ReverseGeocodingResult(string FormattedAddress, double Latitude, double Longitude, string? PlaceId = null);
public sealed record DeliveryEligibilityRequest(double Latitude, double Longitude);
public sealed record DeliveryEligibilityResponse(bool Eligible, Guid? ServiceAreaId, string? ReasonCode);

public sealed record TrackingRealtimePayload(double Latitude, double Longitude, DateTime RecordedAtUtc, double AccuracyMeters, double? SpeedMetersPerSecond, double? HeadingDegrees);

public static class PushValues
{
    public const short Android = 1;
    public const short Ios = 2;
    public const short Fcm = 1;
    public const short Apns = 2;
}

public sealed record ApiProblem(int? Status, string? Title, string? Detail, string? Code, IReadOnlyDictionary<string, string[]> Errors);
public sealed class ApiException(ApiProblem problem) : Exception(problem.Title) { public ApiProblem Problem { get; } = problem; }
public sealed class ApiNetworkException(string message, Exception inner) : Exception(message, inner);
public sealed class ApiTimeoutException(string message, Exception inner) : Exception(message, inner);
