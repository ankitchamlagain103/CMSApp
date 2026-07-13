# CMSApp — Auth Rate Limiting (UI)

**What changed (2026-07-12)**: every `/api/auth/*` endpoint (`login`, `google`, `refresh-token`, `logout`, `change-password`, `set-password`, `verify-email`, `resend-verification-email`, `forgot-password`, `reset-password`) is now rate-limited **per client IP**. This is the brute-force / email-spam protection for the anonymous auth surface.

## Limits

- **Default: 10 requests per 60-second fixed window per IP.** No queuing — request 11 inside the window is rejected immediately.
- Backend-configurable via the `RateLimiting` section in `appsettings.json` (`AuthPermitLimit`, `AuthWindowSeconds`); the values above are the shipped defaults.

## Rejection response

HTTP `429`, standard envelope:

```json
{ "responseCode": "TOO_MANY_REQUESTS", "responseMessage": "Too many requests. Please try again later.", "data": null }
```

`TOO_MANY_REQUESTS` is a new `responseCode` value — add it to the client's response-code union/enum.

## What the UI should do

- On `429` from login: show the message and disable the submit button briefly — do **not** auto-retry.
- On `429` from refresh-token: treat it like a transient failure — wait and retry once after a pause rather than looping (a refresh loop is the most likely accidental trigger).
- Never fire auth calls in a retry loop without backoff; ten failures in a minute means something is wrong client-side or the user is guessing passwords.

## Failure table

| HTTP | `responseCode` | When |
|---|---|---|
| 429 | `TOO_MANY_REQUESTS` | More than the permitted requests from one IP inside the window |

## Notes

- Non-auth endpoints are currently **not** rate-limited.
- Deployment caveat (backend): the partition key is the direct connection IP — behind a reverse proxy, configure `ForwardedHeadersMiddleware` or all clients share the proxy's single bucket.
