using System.Text.Json.Serialization;

namespace AlSsareea.CustomerApp.Core;

[JsonConverter(typeof(JsonStringEnumConverter<DevicePlatform>))]
public enum DevicePlatform { Unknown, Web, Android, Ios }
public enum OtpPurpose : short { Login = 1, PasswordReset, PhoneVerification, EmailVerification }

public sealed record LoginDeviceRequest(string DeviceIdentifier, string? DeviceName, DevicePlatform Platform, string? AppVersion, string? OperatingSystemVersion);
public sealed record LoginRequest(string Identifier, string Password, LoginDeviceRequest Device);
public sealed record RefreshRequest(string RefreshToken, string DeviceIdentifier);
public sealed record OtpChallengeRequest(string Destination, OtpPurpose Purpose, string DeviceIdentifier);
public sealed record OtpVerifyRequest(string Code, string DeviceIdentifier);
public sealed record AuthenticatedUserResponse(Guid Id, string UserType);
public sealed record TokenResponse(string TokenType, string AccessToken, int ExpiresIn, string RefreshToken, DateTime RefreshTokenExpiresUtc, Guid SessionId, AuthenticatedUserResponse User);
public sealed record OtpChallengeResponse(Guid ChallengeId, DateTime ExpiresUtc, DateTime NextResendUtc, string? DevelopmentCode);

public sealed record CustomerResponse(Guid Id, string FirstName, string LastName, string DisplayName, DateOnly? DateOfBirth, short Status, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, Guid ConcurrencyStamp);
public sealed record AddressRequest(string Label, short AddressType, string City, string? Area, string Street, string? BuildingNumber, string? Floor, string? Apartment, string? PostalCode, string? PlaceId, double? Latitude, double? Longitude, string? DeliveryInstructions, bool IsDefault, Guid? ConcurrencyStamp);
public sealed record AddressResponse(Guid Id, string Label, short AddressType, string City, string? Area, string Street, string? BuildingNumber, string? Floor, string? Apartment, string? PostalCode, string? PlaceId, double? Latitude, double? Longitude, string? DeliveryInstructions, bool IsDefault, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, Guid ConcurrencyStamp);

public sealed record LocalizedTextResponse(string LanguageCode, string Name, string? Description);
public sealed record CategoryResponse(Guid Id, Guid CatalogId, Guid MerchantId, Guid? ParentCategoryId, Guid? MediaAssetId, int SortOrder, bool IsVisible, LocalizedTextResponse Text, Guid ConcurrencyStamp);
public sealed record ProductResponse(Guid Id, Guid CatalogId, Guid MerchantId, Guid? CategoryId, string? Sku, long BasePriceMinor, string Currency, string? TaxCategoryReference, short Status, short InventoryStatus, int SortOrder, bool IsVisible, bool IsFeatured, int CurrentVersion, LocalizedTextResponse Text, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, Guid ConcurrencyStamp);
public sealed record ProductListResponse(IReadOnlyList<ProductResponse> Items, int Page, int PageSize, int TotalCount);
public sealed record PriceRequest(Guid? VariantId, IReadOnlyList<Guid> OptionIds, string? Language);
public sealed record CatalogPriceResponse(Guid ProductId, int ProductVersion, string Currency, long BasePriceMinor, long VariantAdjustmentMinor, long OptionsAdjustmentMinor, long TotalPriceMinor, object? SelectedVariant, IReadOnlyList<object> SelectedOptions);

public sealed record GetOrCreateActiveCartRequest(Guid MerchantId, Guid? BranchId);
public sealed record CartItemOptionRequest(Guid OptionGroupId, Guid OptionItemId, int Quantity = 1);
public sealed record AddCartItemRequest(Guid ProductId, Guid? ProductVariantId, int Quantity, string? CustomerNote, IReadOnlyList<CartItemOptionRequest> SelectedOptions, Guid ConcurrencyStamp);
public sealed record CartItemResponse(Guid Id, Guid ProductId, Guid? ProductVariantId, int Quantity, string? CustomerNote, int CatalogVersion, IReadOnlyList<object> SelectedOptions, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record CartResponse(Guid Id, Guid CustomerId, Guid MerchantId, Guid? BranchId, short Status, string? CouponCode, DateTime ExpiresAtUtc, DateTime? LastPricedAtUtc, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, Guid ConcurrencyStamp, IReadOnlyList<CartItemResponse> Items);
public sealed record CartCheckoutSummaryResponse(Guid CartId, Guid CustomerId, Guid MerchantId, Guid? BranchId, short CartStatus, string? Currency, IReadOnlyList<object> Items, long SubtotalMinor, long DeliveryFeeMinor, long ServiceFeeMinor, long TaxMinor, long OtherFeesMinor, long PromotionDiscountMinor, long GrandTotalMinor, IReadOnlyList<object> BlockingReasons, bool IsCheckoutReady, string? PricingReference, string? PromotionEvaluationReference, DateTime CalculatedAtUtc, Guid ConcurrencyStamp, DateTime ExpiresAtUtc);

public sealed record CreateOrderRequest(Guid CartId, Guid DeliveryAddressId, short OrderType, DateTime? ScheduledForUtc, string? CustomerNotes, Guid? ExpectedCartVersion);
public sealed record CreateOrderResponse(Guid OrderId, string OrderNumber, short Status, string Currency, long TotalMinor, DateTime CreatedAtUtc);
public sealed record OrderListItemResponse(Guid Id, string OrderNumber, short Type, short Status, string Currency, long TotalMinor, string MerchantDisplayName, DateTime CreatedAtUtc, DateTime? ScheduledForUtc);
public sealed record OrderListResponse(IReadOnlyList<OrderListItemResponse> Items, int Page, int PageSize, int TotalCount);
public sealed record OrderDetailsResponse(Guid Id, string OrderNumber, Guid SourceCartId, short Type, short Status, string Currency, long TotalMinor, DateTime? ScheduledForUtc, string? CustomerNotes, string? CancellationCode, string? CancellationReason, short? CancelledBy, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, Guid ConcurrencyStamp, object Customer, object DeliveryAddress, object Merchant, object Pricing, IReadOnlyList<object> Items, IReadOnlyList<object> Timeline);
public sealed record DeliveryResponse(Guid Id, Guid OrderId, Guid CustomerId, Guid MerchantId, Guid? BranchId, Guid? DriverId, short Status, short ProofRequirements, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, DateTime? AssignedAtUtc, DateTime? ArrivedAtPickupAtUtc, DateTime? PickedUpAtUtc, DateTime? StartedAtUtc, DateTime? ArrivedAtDropOffAtUtc, DateTime? DeliveredAtUtc, DateTime? FailedAtUtc, short? FailureReason, string? FailureNotes, Guid ConcurrencyStamp, object Pickup, object DropOff, IReadOnlyList<object> Timeline, IReadOnlyList<object> Proofs);
public sealed record DriverLocationResponse(Guid LocationId, Guid DriverId, double Latitude, double Longitude, DateTime RecordedAtUtc, DateTime ReceivedAtUtc, double AccuracyMeters, double? SpeedMetersPerSecond, double? HeadingDegrees, long SequenceNumber);
public sealed record NotificationListItem(Guid Id, string Category, string TemplateKey, short Channel, string Language, string? Subject, string Body, short Status, DateTime CreatedAtUtc, DateTime? ReadAtUtc);
public sealed record NotificationListResponse(IReadOnlyList<NotificationListItem> Items, int Page, int PageSize, int TotalCount, int UnreadCount);
public sealed record RegisterDeviceTokenRequest(string Token, short Platform, short Provider);
public sealed record DeviceTokenResponse(Guid Id, short Platform, short Provider, string TokenMask, bool Active, DateTime UpdatedAtUtc);

public sealed record ApiProblem(int? Status, string? Title, string? Detail, string? Code, IReadOnlyDictionary<string, string[]> Errors);
public sealed class ApiException(ApiProblem problem) : Exception(problem.Title) { public ApiProblem Problem { get; } = problem; }
public sealed class ApiNetworkException(string message, Exception inner) : Exception(message, inner);
