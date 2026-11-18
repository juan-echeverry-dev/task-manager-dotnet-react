# Feature Specification: User Registration and Authentication

**Feature Branch**: `001-user-authentication`

**Created**: 2026-11-18

**Status**: Draft

**Input**: User description: "001-user-authentication" — people can create an account, sign in
with it, and only signed-in people can reach protected capabilities; the account is stored by the
system, and a demo account exists for presentations.

## Clarifications

### Session 2026-11-18

- Q: When someone signs out, must the session credential also become unusable on the server, or is
  discarding it in the browser enough? → A: Discarding it in the browser is enough; a credential
  copied before sign-out stays valid until it expires (at most 60 minutes). Accepted, documented
  risk.
- Q: Which authentication events must be logged, and what must never appear in logs? → A: Log
  account registration and successful sign-in (with the account identifier) and failed sign-in
  (event only, without the entered email); never log passwords, session credentials, or emails.
- Q: What happens when a valid session refers to an account that no longer exists? → A: It is
  treated exactly like an invalid session: "authentication required", the session is discarded,
  and the person is sent to sign-in.
- Q: After signing in, which destinations may the "return to the original screen" redirect lead
  to? → A: Only existing internal screens of the application; any other value (external,
  malformed, or unknown) leads to the profile.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create an account (Priority: P1)

A visitor who does not have an account provides a display name, an email address, and a password
to create one. The system stores the account and confirms success, then takes the visitor to the
sign-in screen with the email already filled in.

**Why this priority**: Without accounts nobody can sign in; every other story depends on it.

**Independent Test**: Register a new account and verify that it exists (a second registration with
the same email is rejected) and that the stored password is not readable.

**Acceptance Scenarios**:

1. **Given** no account exists for `ana@example.com`, **When** a visitor registers with display
   name "Ana", that email, and a valid password, **Then** the account is created, a success message
   is shown, and the visitor lands on the sign-in screen with `ana@example.com` pre-filled.
2. **Given** an account exists for `ana@example.com`, **When** a visitor registers with
   `ANA@Example.com `, **Then** registration is rejected with a message stating that the email is
   already registered, and no second account is created.
3. **Given** the registration form, **When** the visitor submits an invalid email, an empty display
   name, or a password that does not meet the password rules, **Then** registration is rejected and
   each invalid field shows a specific message next to it, while valid input is preserved
   (except the password).

---

### User Story 2 - Sign in and reach protected content (Priority: P1)

A person with an account signs in with email and password and can then see their own profile
(display name, email, member-since date), which is only available to signed-in people.

**Why this priority**: Proves the core value of authentication: the system recognizes the person
and unlocks content that anonymous visitors cannot reach.

**Independent Test**: With an existing account, sign in and open the profile; then try the same
profile access without signing in and verify it is refused.

**Acceptance Scenarios**:

1. **Given** an account for `ana@example.com`, **When** she signs in with the correct password,
   **Then** she is taken to her profile, which shows "Ana", `ana@example.com`, and her registration
   date.
2. **Given** an account for `ana@example.com`, **When** someone signs in with a wrong password,
   **Then** sign-in is refused with the message "Invalid email or password." and no session starts.
3. **Given** no account for `nobody@example.com`, **When** someone tries to sign in with it,
   **Then** the response is exactly the same as for a wrong password (same message, same outcome).
4. **Given** a visitor who is not signed in, **When** they try to open the profile, **Then** they
   are sent to the sign-in screen and, after signing in, they are returned to the profile.

---

### User Story 3 - Session lifecycle (Priority: P2)

A signed-in person stays signed in while they keep using the same browser tab (including page
reloads), can sign out at any time, and is signed out automatically when the session expires.

**Why this priority**: Makes authentication usable and safe, but the system already delivers value
with stories 1 and 2.

**Independent Test**: Sign in, reload the page and verify the session persists; sign out and
verify protected content is no longer reachable; simulate an expired session and verify the person
is asked to sign in again.

**Acceptance Scenarios**:

1. **Given** a signed-in person, **When** they reload the page in the same tab, **Then** they remain
   signed in.
2. **Given** a signed-in person, **When** they choose "Sign out", **Then** the session ends, they
   land on the sign-in screen, and navigating back to the profile requires signing in again.
3. **Given** a session older than 60 minutes, **When** the person performs any action that needs
   authentication, **Then** they are sent to the sign-in screen with the message "Your session has
   expired. Please sign in again."
4. **Given** a signed-in person, **When** they open the sign-in or registration screen, **Then** they
   are redirected to their profile instead.

---

### User Story 4 - Demo account available out of the box (Priority: P3)

Someone presenting or evaluating the application can sign in immediately with documented demo
credentials on a fresh local installation, without registering first.

**Why this priority**: Required for demos and reviews, but not needed for the feature to work.

**Independent Test**: On a fresh installation, sign in with the documented demo credentials and
reach the profile.

**Acceptance Scenarios**:

1. **Given** a fresh local installation, **When** someone signs in with the demo credentials
   documented in the README, **Then** sign-in succeeds and the demo profile is shown.
2. **Given** the demo account already exists, **When** the application restarts, **Then** the demo
   account is not duplicated and its password is unchanged.

---

### Edge Cases

- Email differing only in letter case or surrounding spaces from an existing account is treated as
  the same email (rejected on registration, accepted on sign-in).
- Two registrations for the same email submitted at the same time: exactly one succeeds; the other
  receives the "already registered" outcome.
- Display name made only of spaces is treated as empty; surrounding spaces are removed.
- Password made only of spaces, or longer than the maximum length, is rejected.
- Inputs longer than the documented maximums are rejected with a field message, never truncated.
- A session that expires while the person is on a protected screen: the next action redirects to
  sign-in with the expiry message; no partial data is shown.
- A tampered, malformed, or foreign session credential is treated exactly like no session.
- A session whose account no longer exists (for example, after the local database is recreated)
  is treated exactly like no session: the session is discarded and the person is sent to sign-in.
- A sign-in link whose return destination points outside the application (for example
  `https://evil.example`) or to an unknown screen: after signing in, the person lands on their
  profile, never on the external address.
- Closing the browser tab ends the session; opening the application in a new tab requires signing
  in again.
- The service is temporarily unreachable during sign-in or registration: the person sees a
  non-technical error message and can retry; the form keeps the entered values (except password).

## Requirements *(mandatory)*

### Functional Requirements

**Registration**

- **FR-001**: Visitors MUST be able to create an account by providing a display name, an email
  address, and a password.
- **FR-002**: The system MUST normalize emails by trimming surrounding spaces and comparing them
  case-insensitively; each normalized email MUST belong to at most one account, including under
  concurrent registrations.
- **FR-003**: The system MUST validate registration input: email in a valid format and at most 254
  characters; display name 1–100 characters after trimming; password 8–128 characters and not made
  only of whitespace. Each failing field MUST be reported individually.
- **FR-004**: Registering an email that already belongs to an account MUST be rejected with an
  explicit "email already registered" outcome, distinguishable from validation errors.
- **FR-005**: Registration MUST NOT start a session; after success the person is directed to sign
  in with the email pre-filled.

**Authentication**

- **FR-006**: People MUST be able to sign in with email and password; a successful sign-in starts a
  session valid for 60 minutes, without automatic renewal.
- **FR-007**: Failed sign-ins (unknown email or wrong password) MUST produce an identical outcome
  and message, without revealing which part was wrong.
- **FR-008**: People MUST be able to sign out, which immediately discards the session in that
  browser tab. The system does not keep a record of revoked sessions: a session credential copied
  before sign-out remains valid until its 60-minute expiry.
- **FR-009**: The session MUST survive page reloads within the same browser tab and MUST end when
  the tab is closed.

**Access control**

- **FR-010**: Capabilities MUST require a valid session by default; only explicitly public
  capabilities may be used anonymously. Public capabilities in this feature: registration, sign-in,
  and the service status check.
- **FR-011**: Signed-in people MUST be able to view their own profile: display name, email, and
  registration date. The profile MUST NOT expose any credential data.
- **FR-012**: Requests without a valid session (missing, expired, malformed, tampered, or
  referring to an account that no longer exists) to
  protected capabilities MUST be refused with an "authentication required" outcome; the
  application MUST then send the person to sign-in and return them to the original screen after
  signing in. The return destination MUST be an existing internal screen of the application; any
  other value (external address, malformed, or unknown screen) MUST lead to the profile instead.

**Security and data**

- **FR-013**: Passwords MUST never be stored, logged, displayed, or returned in readable form; only
  a one-way, salted representation is stored.
- **FR-014**: The system MUST store for each account: a unique identifier, email (as entered and
  normalized), display name, password representation, and creation timestamp.
- **FR-015**: Error responses MUST NOT expose internal details (stack traces, queries, internal
  identifiers beyond the person's own).
- **FR-019**: The system MUST log these authentication events: account registered (with the
  account identifier), sign-in succeeded (with the account identifier), and sign-in failed (event
  only, without the entered email). Logs MUST NEVER contain passwords, session credentials, or
  email addresses.

**Demo data**

- **FR-016**: On a local development installation, the system MUST provide a demo account with
  credentials documented in the README; provisioning MUST be idempotent (no duplicates, no
  password reset on restart).

**User experience**

- **FR-017**: Registration, sign-in, and profile screens MUST be usable on screens from 360 px
  wide to desktop, operable by keyboard alone, with visible focus, labelled fields, and error
  messages associated with their fields.
- **FR-018**: While a request is in progress, the submit action MUST be disabled and show progress
  so the same form cannot be submitted twice.

### Key Entities

- **User account**: A person who can sign in. Attributes: unique identifier, email (unique after
  normalization), display name, password representation (never readable), created-at timestamp.
- **Session**: Proof that a person signed in, valid for 60 minutes from sign-in, tied to one
  user account, held only in the browser tab where sign-in happened. Not stored by the system.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new visitor can create an account and sign in within 90 seconds on first attempt.
- **SC-002**: 100% of protected capabilities refuse access without a valid session (verified by
  automated tests covering missing, expired, malformed, and tampered sessions, and sessions whose
  account no longer exists).
- **SC-003**: 0 stored passwords are readable: inspecting the stored account data never reveals the
  original password, and two accounts with the same password have different stored values.
- **SC-004**: Failed sign-in outcomes for "unknown email" and "wrong password" are indistinguishable
  in message and outcome in 100% of automated checks.
- **SC-005**: On a fresh local installation, the documented demo credentials work on the first
  attempt, every time.
- **SC-006**: All screens of this feature work without horizontal scrolling at 360 px width, are
  completable with keyboard only, and produce no errors or warnings in the browser console during
  the flows described in the user stories.
- **SC-007**: During automated runs of registration and sign-in (successful and failed), 100% of
  the expected authentication events are logged, and 0 log entries contain the password, the
  session credential, or the email used.

## Assumptions

- A single kind of user; no roles, administration, or permissions beyond "signed in / not signed
  in" (out of scope).
- Out of scope: password reset, email verification, changing email/password/display name,
  account deletion, social or single sign-on, multi-factor authentication, "remember me",
  session renewal, and rate limiting or lockout of repeated failed sign-ins.
- Returning "email already registered" on registration allows someone to learn whether an email
  has an account; this is accepted for usability because there is no email verification flow.
- Sessions are per browser tab: opening the application in another tab requires signing in again.
- Sign-out is not enforced server-side: a session credential obtained before sign-out (for example,
  by a malicious script) keeps working until it expires. This is an accepted risk, bounded by the
  60-minute lifetime without renewal.
- Password rules follow current guidance favoring length over composition rules (no mandatory
  symbols or digits).
- Timestamps are recorded in UTC and displayed in the person's local time.
- The demo account is provisioned only for local development installations, never in production.
- Future business features will rely on this feature to know who the signed-in person is; they
  depend only on the person's unique identifier.
