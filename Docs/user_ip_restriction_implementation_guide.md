# CMSApp — Per-User IP Restriction Fields (UI)

**What changed**: `ApplicationUser` gained two fields, exposed on the user endpoints:

- `isIpRestricted` (bool) — whether this user's access should be limited to specific IPs.
- `userIpAllowed` (string, nullable) — comma-separated list of the allowed IP addresses.

> ✅ **Enforced as of 2026-07-12.** The restriction is now checked in two places:
>
> 1. **At token issuance** — password login, Google login, and refresh-token all refuse a restricted user calling from a non-allowlisted address. Login/Google return `401 UNAUTHORIZED` with `"Login is not allowed from this IP address."` (only *after* the credentials are verified, so it reveals nothing to guessers); refresh-token returns the same generic `"Invalid or expired refresh token."` as any bad token.
> 2. **On every authenticated request** — the authorization filter re-checks the allowlist against the live database row before anything else (even the SuperAdmin bypass), so tightening a user's list takes effect on their very next request, not at next login. A blocked request gets the standard `403 FORBIDDEN` envelope.
>
> Matching is by **exact address** (comma-separated list, IPv4 or IPv6; IPv4-mapped IPv6 is normalized, and any loopback entry matches any loopback caller for local dev). It **fails closed**: `isIpRestricted: true` with an empty list blocks everywhere — the update validation already prevents saving that state.
>
> ⚠️ **Deployment caveat**: the checked address is the direct TCP peer. Behind a reverse proxy/load balancer that's the proxy's IP, so `ForwardedHeadersMiddleware` (with `KnownProxies`) must be configured before this feature is meaningful in such a topology — otherwise every caller appears to come from the proxy.

## Where the fields appear

| Endpoint | Behavior |
|---|---|
| `GET /api/users/{id}`, `GET /api/users` | Both fields returned on every `UserDto` |
| `PUT /api/users/{id}` | Both fields accepted in the body (admin-gated like the rest of update) |
| `POST /api/users` (create) | **Not accepted** — new accounts start `isIpRestricted: false`, `userIpAllowed: null`; set the policy afterwards via update |

## PUT /api/users/{id} — request fields

```json
{
  "firstName": "John",
  "lastName": "Doe",
  "gender": 0,
  "userType": 2,
  "isActive": true,
  "isIpRestricted": true,
  "userIpAllowed": "203.0.113.7, 198.51.100.22,2001:db8::1"
}
```

Rules:

- `userIpAllowed` accepts IPv4 and IPv6, comma-separated; spaces around commas are fine (the backend trims and stores the canonical `"ip,ip,ip"` form — the response echoes the normalized value).
- IPv4 entries must be the **full dotted quad** (`203.0.113.7` — shorthand like `203.0.113` is rejected).
- Exact addresses only — **no CIDR ranges or wildcards** (`203.0.113.0/24` fails validation).
- When `isIpRestricted` is `true`, `userIpAllowed` must contain at least one address.
- When `isIpRestricted` is `false`, `userIpAllowed` may still be sent (or kept) — it's stored but inert, so an admin can toggle the restriction on/off without retyping the list.

## Failures

| HTTP | `responseCode` | Message |
|---|---|---|
| 400 | `VALIDATION_ERROR` | `"At least one allowed IP is required when IP restriction is enabled."` |
| 400 | `VALIDATION_ERROR` | `"UserIpAllowed must be a comma-separated list of valid IPv4/IPv6 addresses."` |

## Typical UI uses

- **User edit form (admin)**: an "IP restriction" toggle; when on, show a tag-style multi-entry input for addresses and join with commas on submit. Split the stored value on `","` to re-populate.
- Validate each entry client-side as a plain IP (not CIDR) before submit to avoid a round-trip.
- Warn the admin when they enable the restriction without including their user's current network — **enforcement is live**, so a wrong list locks that user out immediately (their active session starts 403ing on the next request).
- If a restricted user reports 403s everywhere or `"Login is not allowed from this IP address."`, the fix is an admin correcting their allowlist via `PUT /api/users/{id}`.
