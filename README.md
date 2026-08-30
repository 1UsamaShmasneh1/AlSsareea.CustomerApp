# AlSsareea Customer App

.NET 10 MAUI client for Android and iOS, with a testable `Core` assembly. The app communicates only through backend HTTP/realtime contracts and has no backend project references.

## Setup

Install .NET SDK 10.0.400 and the `android`, `ios`, `maccatalyst`, and `maui-windows` workloads. Development defaults are centralized in `ApiConfiguration` in `MauiProgram.cs`: Android emulator uses `https://10.0.2.2:7080/`; other targets use `https://localhost:7080/`. Change this in a local-only configuration before connecting to another environment. Never commit credentials or production provider keys.

Run `dotnet restore`, `dotnet build`, and `dotnet test`. Android can be built with `dotnet build src/AlSsareea.CustomerApp/AlSsareea.CustomerApp.csproj -f net10.0-android`.

## Architecture and security

Pages route through Shell; client logic lives in `AlSsareea.CustomerApp.Core`. Typed clients centralize camel-case JSON, correlation IDs, Problem Details, cancellation, and endpoint paths. Access tokens remain in memory. Refresh tokens and only their expiry/device identifier are serialized through MAUI `SecureStorage`. Concurrent 401s enter `SessionManager`'s single-flight gate; after another request rotates the token, waiters reuse it. Permanent refresh rejection clears both tokens.

No automatic write retry exists. Order creation accepts a stable logical idempotency key which callers must retain across uncertain retries. Backend totals, catalog validation, availability, promotions, order state, service eligibility, and authorization remain authoritative.

## Current limitations

The inspected backend has no public/customer merchant discovery API and no Maps HTTP API. Consequently a complete browse-from-home and backend-backed map selection experience cannot be implemented without a backend contract. Catalog works only with a known merchant ID. Static page shells expose the intended navigation but several screens are not yet fully bound to ViewModels. SignalR and native FCM/APNs acquisition are documented contracts but provider/device validation is pending.

Electronic payments depend on Phase 22. Support tickets depend on Phase 24. Shared UI extraction is intentionally deferred.
