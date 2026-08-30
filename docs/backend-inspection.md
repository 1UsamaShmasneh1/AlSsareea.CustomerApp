# Backend contract inspection (Phase 17 baseline)

Inspected repository: `AlSsareea.Backend`, branch `feature/17.notificationsModule`, commit `3be83a58d767c517f3106a9401b27f4e038986e8`. No backend file was modified.

- API uses mixed routes: most current modules use `/api/v1`; carts use `/api/carts`; media uses `/api/media`. JSON is camelCase; enums are numeric except Identity device platform accepts the defined enum representation; timestamps are UTC. `X-Correlation-ID` is supported.
- Auth: password login, rotating refresh, current user, logout/logout-all, sessions, OTP challenge and verification. OTP challenge and logout require 8–200 character `Idempotency-Key`. Access response is Bearer token plus seconds-to-expiry, rotating refresh token/UTC expiry, session ID and user. Refresh reuse triggers replay/family revocation per Identity implementation.
- Customer: authenticated `/api/v1/customers/me` profile, addresses and preferences; ownership comes from JWT. Address/profile mutations expose `concurrencyStamp`.
- Catalog: anonymous per-merchant catalog/categories/sections/products/search/product/price endpoints. Prices use `long` minor units plus currency. Products contain variants/options in the detailed application projection, while authoritative selected pricing comes from the price endpoint.
- Merchants: existing list/detail/branch/availability routes require merchant management permissions. No customer/public discovery endpoint exists.
- Cart: authenticated active cart, item mutations, coupons, reprice and checkout summary. Mutations use cart concurrency stamps and idempotency keys as implemented. Checkout summary contains backend-authoritative subtotal, fees, tax, promotion discount and grand total.
- Orders: authenticated create/list/detail/by-number/timeline/cancel. Create requires an idempotency key and snapshots the trusted cart/address/merchant/pricing data. Pagination is `{items,page,pageSize,totalCount}`.
- Delivery/tracking: customer current/detail delivery reads exist. Order tracking REST is `/api/v1/tracking/orders/{orderId}/latest`. SignalR hub `/hubs/tracking` exposes `SubscribeOrder`; reconnect requires REST reload and resubscribe.
- Notifications: inbox pagination/unread, mark read/read-all, device register/unregister, and preferences at `/api/v1/notifications`. Push registration uses numeric platform/provider and returns only a token mask.
- Maps: provider-neutral Maps module and PostGIS service areas exist, but the API composition exposes no Maps HTTP endpoints. Fake is the only configured provider.
- Errors use RFC Problem Details with a `code` extension; validation error dictionaries are supported. Relevant statuses include 400, 401, 403, 404, 409, 422, 429 and 5xx. Rate limits can include `Retry-After`.
- Languages are `ar`, `he`, and `en`. Money is minor-unit `long`. UTC `DateTime` serialization is enforced. List pagination is one-based and generally defaults to page 1/page size 20.

Blocking contracts: public/customer merchant discovery/details (including customer-visible operating/service status) and Maps HTTP operations are absent. No supported global customer search endpoint exists. These gaps prevent the requested full launch-to-browse/map journey without backend work.
