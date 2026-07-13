# CMSApp — Sign in with Google (UI)

**What this is**: the backend exposes `POST /api/auth/google` which accepts a **Google ID token** and returns the exact same auth payload as a normal login (access token + refresh token). The UI's job is to obtain that ID token from Google in the browser and POST it — no redirects to the backend, no OAuth callback URL on the API, no client secret anywhere in the frontend.

**Change note (2026-07-10)**: a brand-new account created by Google sign-in now receives the default `User` role automatically (same as normal registration). Previously it got no roles, so `GET /api/roles/user-menus` returned an empty tree.

## 1. One-time setup (Google Cloud Console)

1. Create an **OAuth 2.0 Client ID** of type **Web application** (APIs & Services → Credentials).
2. Add every origin the UI runs on to **Authorized JavaScript origins** (e.g. `http://localhost:5173`, `https://app.example.com`). No redirect URI is needed for this flow.
3. The **same Client ID** goes in two places: the frontend (GIS init below) and the backend's `Authentication:Google:ClientId` in `appsettings.json`. If they differ, the backend rejects every token with `401 "Invalid Google sign-in token."` (audience mismatch).

## 2. Getting the ID token (web — Google Identity Services)

Load the GIS script and render the button (or use One Tap):

```html
<script src="https://accounts.google.com/gsi/client" async defer></script>
```

```js
window.google.accounts.id.initialize({
  client_id: "YOUR_CLIENT_ID.apps.googleusercontent.com",
  callback: handleGoogleCredential
});

// Rendered button:
window.google.accounts.id.renderButton(
  document.getElementById("googleSignInDiv"),
  { theme: "outline", size: "large" }
);

// Optional One Tap prompt:
window.google.accounts.id.prompt();

async function handleGoogleCredential(response) {
  // response.credential IS the ID token (a JWT string) — send it as-is.
  const apiResponse = await fetch("/api/auth/google", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ idToken: response.credential })
  });
  const result = await apiResponse.json();
  // result.data has the same shape as POST /api/auth/login
}
```

For React, wrappers like `@react-oauth/google` (`<GoogleLogin onSuccess={r => ...r.credential} />`) produce the same `credential` string. Mobile apps use the native Google Sign-In SDKs, which also yield an ID token — the API call is identical.

## 3. POST /api/auth/google — *anonymous*

**Request**:
```json
{ "idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6..." }
```

**Response** (`200`) — identical shape to `POST /api/auth/login`:
```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Login successful.",
  "data": {
    "userId": "7b9f3f4e-5c2a-4d1e-9f6b-2a8c4e0d1b3a",
    "userName": "jdoe",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAtUtc": "2026-07-10T11:30:00Z",
    "refreshToken": "u8Zk3vJxN2mQ...base64...==",
    "refreshTokenExpiresAtUtc": "2026-07-17T10:30:00Z",
    "roles": ["User"]
  }
}
```

Store and refresh the tokens exactly like a password login — from here on the session is indistinguishable from one.

## 4. What the backend does with the token (account matrix)

| Situation | Result |
|---|---|
| A user already linked to this Google account | Logs them in |
| No Google link, but an account exists with the same **email** | Links Google to that account (both login methods work from then on); marks the email confirmed if Google says it's verified |
| No account at all | Creates one: username derived from the email's local part (de-duplicated with a numeric suffix — `jdoe`, `jdoe1`, ...), `emailConfirmed` taken from Google, default `User` role, **no password** |

Matching is keyed on Google's stable account id (`sub`), not the email — a user who changes their Gmail address keeps their CMSApp account.

## 5. Passwordless accounts — pair with set-password

A Google-created account **has no password** until the user adds one via `POST /api/auth/set-password` (see `Docs/set_password_implementation_guide.md`). Recommended UI:

- After a first Google sign-in, offer "Add a password so you can also log in without Google."
- Show the user their auto-generated `userName` (from the login response) next to that form — they'll need it for password login.
- Google-signed-in users skip email verification and the password-expiry check entirely — no "verify your email" or "password expired" screens on this path.

## 6. Failures

| HTTP | `responseCode` | When / message |
|---|---|---|
| 401 | `UNAUTHORIZED` | `"Invalid Google sign-in token."` — expired/forged token, or frontend and backend use different Client IDs |
| 401 | `UNAUTHORIZED` | `"This account has been deactivated."` |
| 400 | `VALIDATION_ERROR` | `idToken` empty, or account auto-creation failed (e.g. that email is soft-deleted — its address is still reserved) |

ID tokens from Google expire in ~1 hour and are **single-purpose** — obtain a fresh one from GIS for each sign-in attempt; never store or reuse them.
