# Contract: SPA routes and session behavior

## Routes

| Path | Access | Behavior |
|---|---|---|
| `/` | Public | Home with the API status badge and links to sign in / register (or the profile when signed in) |
| `/register` | Anonymous only | Registration form. A signed-in person is redirected to `/profile` (US3-4) |
| `/sign-in` | Anonymous only | Sign-in form. Accepts `?returnTo=`. A signed-in person is redirected to `/profile` |
| `/profile` | Signed in | Display name, email, member-since date (local time); "Sign out" action |
| `*` | Public | Not found page |

## Navigation outcomes

| Trigger | Result |
|---|---|
| Registration succeeds | Navigate to `/sign-in`. The email field is pre-filled and the message "Account created. You can now sign in." is shown |
| Registration returns 409 | Message "This email is already registered." on the email field |
| Registration returns 400 | Each `errors` entry is shown on its field. Values are kept and the password is cleared |
| Sign-in succeeds | Save the session, then navigate to the safe `returnTo` (see below) or `/profile` |
| Sign-in returns 401 | Form-level message "Invalid email or password." The password is cleared |
| Anonymous visit to a protected route | Navigate to `/sign-in?returnTo=<original path + query>` |
| Any 401 from an authenticated request | Clear the session and navigate to `/sign-in?returnTo=…`. If `expiresAt` has passed, show "Your session has expired. Please sign in again." |
| Sign out | Clear the session and the cached server data, then navigate to `/sign-in` |
| Network error / 5xx | Non-technical message "We couldn't reach the service. Please try again." The form keeps its values except the password |

## Safe `returnTo`

A `returnTo` value is accepted only if **all** of these hold:

1. It starts with exactly one `/` (it is not `//…` or `/\…`).
2. Resolving it against the app origin keeps the same origin.
3. It matches a defined route other than the catch-all `*`.

Otherwise the destination is `/profile`. Examples: `/profile` ✅; `https://evil.example` ❌;
`//evil.example` ❌; `/does-not-exist` ❌.

## Session storage

- Key `tasklane.session` in `sessionStorage`, value `{ "accessToken": string, "expiresAt": ISO string }`.
- When read, a value that is invalid JSON or malformed is discarded. An expired session is
  cleared by the next protected navigation or request, which shows the expiry message (US3-3).

## Form behavior (FR-017, FR-018)

- Every input has a visible `<label>`. Errors are linked to their field with
  `aria-describedby` and `aria-invalid`. Form-level messages use `role="alert"`.
- While a request is pending, the submit button is disabled with `aria-busy` and the text
  "Creating account…" / "Signing in…".
- Layout works from 360 px wide, and focus is visible on every interactive element.
