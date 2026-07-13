# CMSApp — Set Password for Passwordless (Google) Accounts (UI)

**What changed**:

1. New endpoint `POST /api/auth/set-password` — lets a signed-in user whose account has **no password** (created via Google sign-in) add one, so they can afterwards also log in with username/password. Google sign-in keeps working as before; this just adds a second way in.
2. **Fix**: `POST /api/auth/logout` and `POST /api/auth/change-password` previously returned `403 FORBIDDEN` for every non-SuperAdmin user (a misconfigured server-side allowlist). They now work for any authenticated user, as originally documented. No UI change needed beyond removing any workaround.

## POST /api/auth/set-password — authenticated

Send the usual `Authorization: Bearer <token>` header. No user id in the body — the account is taken from the token.

**Request**:
```json
{ "newPassword": "New!Pass456" }
```

**Response** (`200`):
```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Password set successfully. You can now also log in with your username and password.",
  "data": true
}
```

The user **stays logged in** — unlike change-password/reset-password, no sessions are revoked (no existing credential was replaced, one was only added). Don't route to the login page after success.

## Password rules

Same policy as registration and change-password:

- 8–128 characters
- at least one uppercase letter, one lowercase letter, one number, one special character
- not a common password (`password123` etc.)

## Failures

| HTTP | `responseCode` | When / message |
|---|---|---|
| 400 | `CONFLICT` | `"This account already has a password. Use change-password instead."` |
| 400 | `VALIDATION_ERROR` | Any password-policy violation (all failed rules joined in `responseMessage`) |
| 401 | `UNAUTHORIZED` | Missing/expired token — refresh or re-login |
| 404 | `NOT_FOUND` | `"User was not found."` (token references a deleted account) |

## Typical UI uses

- **Account/security settings page**: if the account is passwordless, show "Set a password" (this endpoint); otherwise show "Change password" (`POST /api/auth/change-password`). There is no dedicated "has password?" lookup — either track it client-side from how the user signed up, or optimistically show one form and switch to the other on the `CONFLICT` response.
- **Post-Google-signup nudge**: after a first Google sign-in, offer "Add a password so you can also log in without Google."
- **Username display**: a Google-created account gets an auto-generated username (email local part, possibly with a numeric suffix). Show the user their `userName` (returned by the login response) next to the set-password form — they'll need it for password login.
