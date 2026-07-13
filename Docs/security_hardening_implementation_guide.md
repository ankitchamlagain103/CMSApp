# CMSApp — Security Hardening Batch (UI)

**What changed (2026-07-12)**: three cross-cutting hardening changes. None alter any endpoint's request/response shape, but two have consequences the frontend team must know about.

## 1. CORS is now an explicit origin allowlist

The API previously accepted any origin. It now only serves cross-origin requests from the origins listed in the backend's `App:AllowedOrigins` config (`WebApi/appsettings.json`). Shipped defaults:

```json
"App": {
  "AllowedOrigins": [ "http://localhost:3000", "http://localhost:5173" ]
}
```

- **Your dev server's origin must be on this list** or every browser call fails CORS preflight (network error in the console, no envelope response). React/Next default (`:3000`) and Vite default (`:5173`) are pre-listed.
- Deployed frontend origins (scheme + host + port, no trailing slash) must be added per environment.
- An empty list disables cross-origin access entirely — the backend fails closed, it never falls back to "allow all".
- Allowed methods: `GET, POST, PUT, DELETE, PATCH`; all headers allowed; credentials allowed (for listed origins only).

## 2. Refresh-token reuse detection

Redeeming a refresh token that was **already rotated** (used once before) is treated as theft: the backend revokes **all** of that user's active sessions, on every device, then returns the usual `401 "Invalid or expired refresh token."`.

Client rules this makes mandatory (they were already best practice):

- Never run two refresh calls concurrently — serialize behind a single in-flight promise.
- On refresh success, atomically replace *both* stored tokens before releasing queued requests.
- On refresh `401`, drop straight to login; do **not** retry with any stored token.

An expired (rather than rotated) token replay does *not* trigger the global revocation — it's just rejected.

## 3. Security response headers

Every response now carries `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`; HSTS is enabled outside Development. Transparent to the frontend — listed for completeness (note the API can't be iframed).

## Failure table

| HTTP | Symptom | When |
|---|---|---|
| — | CORS/network error, no response body | Frontend origin missing from `App:AllowedOrigins` |
| 401 | `UNAUTHORIZED` `"Invalid or expired refresh token."` + all sessions revoked | A rotated refresh token was replayed |
