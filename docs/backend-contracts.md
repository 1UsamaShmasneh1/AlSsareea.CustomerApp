# CustomerApp backend contract matrix

All paths were inspected against the Phase 18A backend source. JSON is camelCase, timestamps are UTC, money is minor-unit `long`, and errors use RFC Problem Details with a `code` extension.

| Area | Method and path | Client behavior |
|---|---|---|
| Auth | `POST /api/v1/auth/login` | Password login and rotating token response |
| Auth | `POST /api/v1/auth/refresh` | Secure refresh restoration; never generic-retried |
| Auth | `POST /api/v1/auth/logout` | Authenticated, idempotency-keyed logout |
| OTP | `POST /api/v1/auth/otp/challenges` | Purpose=Login, stable challenge request key |
| OTP | `POST /api/v1/auth/otp/challenges/{id}/verify` | Exact challenge verification contract |
| Customer | `GET/PUT /api/v1/customers/me/` | Profile read/update |
| Addresses | `GET/POST /api/v1/customers/me/addresses` | Owned address list/add |
| Addresses | `PUT/DELETE /api/v1/customers/me/addresses/{id}` | Concurrency-aware edit/delete |
| Addresses | `PUT /api/v1/customers/me/addresses/{id}/default` | Set default |
| Merchants | `GET /api/v1/customer/merchants` | Public pagination/query/openNow |
| Merchants | `GET /api/v1/customer/merchants/{id}` | Customer-safe details and catalog path |
| Catalog | `GET /api/v1/merchants/{merchantId}/catalog/categories` | Public scoped categories |
| Catalog | `GET /api/v1/merchants/{merchantId}/catalog/sections` | Public scoped sections |
| Catalog | `GET /api/v1/merchants/{merchantId}/catalog/products` | Scoped pagination/query/category |
| Catalog | `GET /api/v1/merchants/{merchantId}/catalog/products/{id}?language=&branchId=` | Customer product details with media, variants, options, selection limits, availability, and stable Cart IDs |
| Pricing | `POST /api/v1/merchants/{merchantId}/catalog/products/{id}/price` | Authoritative selection price |
| Cart | `/api/carts` family | Active/create/add/patch/remove/reprice/summary with concurrency and mutation keys |
| Maps | `POST /api/v1/maps/geocode` | Authenticated normalized suggestions |
| Maps | `POST /api/v1/maps/reverse-geocode` | Authenticated normalized address |
| Maps | `POST /api/v1/maps/delivery-eligibility` | Backend service-area authority |
| Orders | `/api/v1/orders` family | Create/list/details/cancel; creation preserves one logical idempotency key |
| Tracking | `GET /api/v1/tracking/orders/{orderId}/latest` | Owned latest snapshot |
| Tracking | `/hubs/tracking` | JWT access-token provider, `SubscribeOrder`, `LocationUpdated` |
| Notifications | `/api/v1/notifications` family | Inbox/read/read-all/preferences/device registration |

## Product configuration boundary

The customer details response supplies only public product media URLs and customer-safe configuration fields. CustomerApp maps `variant.id`, `optionGroup.id`, and `option.id` directly to the existing Cart request and sends the selected IDs to the existing price endpoint. It never derives IDs from labels, reads Media persistence, or treats the locally displayed adjustment sum as authoritative.
