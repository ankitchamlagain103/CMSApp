# CancellationToken Implementation Guide

How request cancellation works in CMSApp, why it's there, and the rules for writing new code.

## Why

When a client disconnects (closed tab, navigation away, mobile app killed, HTTP timeout), ASP.NET Core cancels `HttpContext.RequestAborted`. Without cooperative cancellation, the server keeps executing the full pipeline — database queries, SMTP sends — for a response nobody will ever read. With it, in-flight work stops at the next cancellation checkpoint and the connection's resources are freed. Under load (or a slow query being hammered by an impatient user hitting refresh), this is the difference between shedding abandoned work and stacking it up.

## How a token flows through this codebase

```
Client disconnects
      │
      ▼
HttpContext.RequestAborted  (ASP.NET Core cancels this token)
      │
      ├──► Controller action parameter: `CancellationToken cancellationToken`
      │    (model binding fills it from RequestAborted automatically — no attribute needed)
      │         │
      │         ▼
      │    I<Feature>Service method (last parameter, `= default`)
      │         │
      │         ▼
      │    EF Core / SMTP calls: ToListAsync(ct), FirstOrDefaultAsync(ct),
      │    SaveChangesAsync(ct), CountAsync(ct), AnyAsync(ct), SendMailAsync(msg, ct)
      │
      └──► AuthorizedAction filter uses context.HttpContext.RequestAborted directly
           for its permission queries (filters don't get the token via model binding)
```

When the token fires mid-query, the provider throws `OperationCanceledException`. It bubbles up to `ExceptionHandlingMiddleware`, which has a dedicated catch: if `RequestAborted` is the reason, it logs one information-level line and writes **no** response (the client is gone) — it does *not* produce the 500/`SERVER_ERROR` envelope a real fault produces.

## The convention (for new code)

1. **Every async method on a service or repository interface takes `CancellationToken cancellationToken = default` as its last parameter.** The `= default` keeps non-request callers (seeders, tests) uncluttered.
2. **Every controller action declares `CancellationToken cancellationToken` as its last parameter** and passes it to the service call. ASP.NET Core binds it to `HttpContext.RequestAborted` automatically.
3. **Inside an implementation, forward the token to every awaited call that accepts one**: all EF Core async operators, `DbContext.SaveChangesAsync`, `SmtpClient.SendMailAsync`, nested service/helper calls.
4. **Private async helpers take the token as a required parameter (no `= default`)** — inside an implementation there is always a token in hand; the default would just invite forgetting to pass it.
5. **Action filters and middleware** read `HttpContext.RequestAborted` directly.

## Where cancellation deliberately does NOT apply

These are not omissions — each is a decision:

- **ASP.NET Core Identity manager calls** (`UserManager.*`, `RoleManager.*`, `SignInManager.*`) expose no per-call token parameter; internally they use the store's token (`CancellationToken.None` in practice). So `CreateAsync`, `FindByIdAsync`, `ChangePasswordAsync`, etc. run to completion once started. Service methods still accept and forward tokens — the cancellable EF/SMTP portions around the manager calls honor them.
- **Security cleanup past the point of no return runs with `CancellationToken.None`:**
  - `AuthService.ChangePasswordAsync` / `ResetPasswordAsync`: once the password has changed, the follow-up revoke-all-sessions call must not be skippable by a disconnect — otherwise old refresh tokens would outlive a password change.
  - `AuthService.RefreshTokenAsync`: once the *new* token pair is issued, revoking the presented token must complete — cancelling between the two steps would leave both tokens redeemable.
  - `AuthService.LogoutAsync`'s final `SaveChangesAsync`: revocation is the entire point of the call; abandoning it halfway defeats it.
  The rule generalizes: **cancel freely before a mutation; never cancel between two mutations that must stay consistent.** Reads and validations are always safe to abandon; once the first irreversible write lands, finish the set.
- **Startup seeders** (`IdentitySeeder`, `MenuSeeder`) take no token — they run during application boot, not inside a request; there is no meaningful canceller.
- **`GoogleJsonWebSignature.ValidateAsync`** (Google's library) offers no token overload.

## Pitfalls to avoid

- **Don't catch `OperationCanceledException` and convert it into a normal failure response.** `AuthorizedAction`'s permission check swallows exceptions into "not authorized" — it explicitly re-throws `OperationCanceledException` first so a disconnect isn't misreported as a 403. Follow that pattern anywhere a broad `catch` exists.
- **Don't pass the request token into fire-and-forget background work.** If work is meant to outlive the request (none exists in this codebase yet), it needs its own lifetime token (e.g. `IHostApplicationLifetime.ApplicationStopping`), not `RequestAborted`.
- **Don't add manual `cancellationToken.ThrowIfCancellationRequested()` calls between awaits.** The awaited EF/SMTP calls already check the token at every I/O boundary; sprinkling manual checks adds noise for microseconds of benefit. (The exception would be a long CPU-bound loop, which this codebase doesn't have.)

## What the UI sees

Nothing — that's the point. A cancelled request gets no response because the client already went away. UI code should simply use `AbortController` (or its platform equivalent) when abandoning requests (typeahead searches, page navigations), knowing the server stops the corresponding work instead of finishing it silently.
