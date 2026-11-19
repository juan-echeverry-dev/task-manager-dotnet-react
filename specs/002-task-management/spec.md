# Feature Specification: Task Management

**Feature Branch**: `002-task-management`

**Created**: 2026-11-18

**Status**: Draft

**Input**: User description: "Create, read, update, and delete tasks (CRUD). Each task has a title,
description, status, and due_date. Tasks are associated with a user (assume basic User model
exists)." The user account and sign-in already exist (feature `001-user-authentication`).

## Clarifications

### Session 2026-11-18

- Q: Who can see and change a task? → A: Each person sees and manages only their own tasks; another
  person's task behaves exactly as if it did not exist.
- Q: Which statuses exist? → A: Three fixed statuses: To do (default), In progress, Done; any status
  can change to any other, including reopening a finished task.
- Q: What is the due date? → A: Optional calendar date without time; past dates are allowed; an
  unfinished task whose due date has passed is shown as overdue.
- Q: Which fields are required? → A: Title required, 1–200 characters after trimming; description
  optional, up to 2,000 characters.
- Q: How does deletion work? → A: Deleting moves a task to a recoverable "deleted" state; it
  disappears from the task list and can be restored.
- Q: What does the task list offer? → A: Text search, filter by status, choice of sort order, and
  pagination.
- Q: What happens when the same task is edited in two places? → A: The last saved change wins.
- Q: What happens when a person opens or edits one of their own deleted tasks? → A: For now, the
  owner can view a deleted task in read-only mode; no editing options are offered, and any edit
  attempt is refused until the task is restored.
- Q: Is the "no more than 90 days" limit for the due date field or for the list filter? → A: For
  the list filter: tasks can be filtered by a due-date range spanning at most 90 days, a limit
  that must be changeable later by configuration if the client decides so. The due date field
  itself only rejects obvious typos: it accepts dates from 2000-01-01 to 2099-12-31.
- Q: How is the deleted-tasks list ordered and which tools does it offer? → A: The same tools as
  the task list: text search, status filter, due-date range filter, sort, and pagination.
- Q: How are the demo account's sample tasks provisioned? → A: In local development only, six
  sample tasks are created when the demo account has no tasks at all (including deleted ones) and
  are never modified afterwards.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create and see my tasks (Priority: P1)

A signed-in person creates a task with a title and, optionally, a description, a status, and a due
date, and sees it in their own task list.

**Why this priority**: Creating and seeing tasks is the core value; every other story builds on it.

**Independent Test**: Sign in, create a task, and verify it appears in the list with the entered
values; sign in as someone else and verify it does not appear.

**Acceptance Scenarios**:

1. **Given** a signed-in person with no tasks, **When** they create a task titled "Prepare demo"
   without other values, **Then** the task is saved with status "To do", no description, and no
   due date, and it appears in their list.
2. **Given** a signed-in person, **When** they create a task with title, description, status
   "In progress", and due date 2026-12-01, **Then** all values are saved and shown as entered.
3. **Given** the create form, **When** the title is empty, only spaces, or longer than 200
   characters, or the description is longer than 2,000 characters, **Then** the task is not
   created and each invalid field shows a specific message, while the other values are kept.
4. **Given** two people with tasks, **When** either opens their list, **Then** they see only
   their own tasks.

---

### User Story 2 - View and edit a task (Priority: P1)

A person opens one of their tasks, sees all its details, and changes any of its values, including
moving it between statuses.

**Why this priority**: Tasks change over time; without editing, status tracking is impossible.

**Independent Test**: Create a task, change its title, description, due date, and status, and
verify the list and detail show the new values.

**Acceptance Scenarios**:

1. **Given** a task of mine, **When** I open it, **Then** I see its title, description, status,
   due date, and when it was created and last updated.
2. **Given** a task of mine in "To do", **When** I change it to "Done", **Then** the new status is
   saved and shown; **and When** I later change it back to "To do", **Then** it is reopened.
3. **Given** a task of mine with a due date, **When** I remove the due date, **Then** the task has
   no due date.
4. **Given** an edit with invalid values (same rules as creation), **When** I save, **Then**
   nothing changes and each invalid field shows its message.
5. **Given** a task that belongs to someone else, **When** I try to open, edit, or delete it by
   any means, **Then** I get the same "task not found" outcome as for a task that never existed.
6. **Given** an unfinished task whose due date is before today, **When** it is shown, **Then** it
   is marked as overdue; a task in "Done" is never marked overdue.

---

### User Story 3 - Delete and restore a task (Priority: P2)

A person deletes a task they no longer need; it leaves their task list but can be found among
deleted tasks and restored.

**Why this priority**: Keeps the list clean while protecting against accidental loss.

**Independent Test**: Delete a task, verify it is gone from the list and present among deleted
tasks, restore it, and verify it is back with its values unchanged.

**Acceptance Scenarios**:

1. **Given** a task of mine, **When** I choose "Delete", **Then** I am asked to confirm; **and
   When** I confirm, **Then** the task disappears from my task list and appears in my deleted
   tasks.
2. **Given** a deleted task, **When** I choose "Restore", **Then** it returns to my task list with
   the same title, description, status, and due date it had before deletion.
3. **Given** a deleted task of mine, **When** I open it from my deleted tasks, **Then** I see all
   its details in read-only mode, clearly marked as deleted, with "Restore" as the only action and
   no editing options.
4. **Given** a deleted task of mine, **When** an edit is attempted anyway (for example from a tab
   opened before the deletion), **Then** the edit is refused with a message to restore the task
   first, and nothing changes.
5. **Given** the delete confirmation, **When** I cancel, **Then** nothing changes.
6. **Given** my deleted tasks, **When** I open the deleted-tasks list, **Then** the most recently
   deleted tasks come first, each showing when it was deleted, and I can use the same search,
   status filter, due-date range filter, sort, and paging as in the task list.

---

### User Story 4 - Find tasks (Priority: P2)

A person with many tasks narrows and orders their list: searches by text, filters by status,
chooses the sort order, and moves between pages.

**Why this priority**: Makes the list usable as it grows, but the core value exists without it.

**Independent Test**: Create tasks with different titles, statuses, and due dates; verify search,
filter, sort, and paging each return the expected subset and order.

**Acceptance Scenarios**:

1. **Given** my tasks, **When** I search for "demo", **Then** I see only tasks whose title or
   description contains "demo", ignoring letter case.
2. **Given** my tasks, **When** I filter by "In progress", **Then** I see only tasks in that
   status; with no filter I see all statuses.
3. **Given** my tasks, **When** no sort order is chosen, **Then** they are ordered by due date,
   earliest first, with tasks without a due date last, and ties broken by most recently created.
4. **Given** my tasks, **When** I sort by due date, creation date, or title, ascending or
   descending, **Then** the list follows that order.
5. **Given** 25 matching tasks and a page size of 10, **When** I open the list, **Then** I see the
   first 10, the total count of 25, and can move to pages 2 and 3; search, filter, and sort apply
   across all pages.
6. **Given** search and filter values, **When** I reload the page or share its address with
   myself in another tab, **Then** the same search, filter, sort, and page are applied.
7. **Given** my tasks, **When** I filter by due dates from 2026-12-01 to 2026-12-31, **Then** I see
   only tasks due within that range, both dates included; tasks without a due date are excluded.
8. **Given** the due-date range filter, **When** the range spans more than 90 days, or the start
   date is after the end date, **Then** the filter is not applied and a message explains the
   allowed range (for example "Choose a range of at most 90 days.").
9. **Given** the due-date range filter, **When** it is combined with search, status filter, sort,
   and paging, **Then** all of them apply together.

---

### Edge Cases

- A title with surrounding spaces is stored trimmed; a title of only spaces is treated as empty.
- Inputs longer than the documented maximums are rejected with a field message, never truncated.
- A due date in the past is accepted; it is shown as overdue unless the task is "Done".
- "Overdue" is evaluated against the person's local calendar date, so a task due today is not
  overdue.
- A due-date range of exactly 90 days (for example 2026-01-01 to 2026-04-01) is accepted; one day
  more is rejected. Only one end of the range given: the range filter requires both dates.
- A due date outside 2000-01-01 to 2099-12-31 is rejected on create and edit with a field message.
- Requesting a page beyond the last page returns an empty page with the correct total, not an
  error.
- A search that matches nothing shows an empty state with a way to clear the search and filters.
- An unknown status, sort field, or invalid page value in the request is rejected with a clear
  message (or, in the screen, reset to the default).
- A task deleted in one tab and then edited in another: the edit is refused with a message to
  restore it first (US3, scenario 4).
- Two edits of the same task from different tabs: the last saved values are kept (accepted).
- A session that expires while editing: the person is sent to sign-in (feature 001) and unsaved
  changes are lost; this is accepted.
- The service is unreachable while saving: a non-technical message is shown and the form keeps
  the entered values so the person can retry.

## Requirements *(mandatory)*

### Functional Requirements

**Ownership and access**

- **FR-001**: Only signed-in people can use tasks; every task belongs to exactly one person, set
  when it is created and never changed.
- **FR-002**: A person MUST only be able to see, edit, delete, or restore their own tasks. Any
  attempt on another person's task MUST produce the same "task not found" outcome as a task that
  does not exist.

**Task data and rules**

- **FR-003**: A task MUST have a title of 1–200 characters after trimming surrounding spaces.
- **FR-004**: A task MAY have a description of up to 2,000 characters.
- **FR-005**: A task MUST have exactly one status among "To do", "In progress", and "Done"; new
  tasks default to "To do". Any status MAY change to any other.
- **FR-006**: A task MAY have a due date, which is a calendar date without time between 2000-01-01
  and 2099-12-31 inclusive; past dates within that range are allowed and the due date can be
  removed.
- **FR-007**: The system MUST record when each task was created and last updated.
- **FR-008**: Each invalid field MUST be reported individually, and nothing is saved when any
  field is invalid.

**Create, read, update**

- **FR-009**: People MUST be able to create a task with title (required) and optional description,
  status, and due date.
- **FR-010**: People MUST be able to view the details of one of their tasks, including deleted
  ones, which are shown read-only and marked as deleted.
- **FR-011**: People MUST be able to change the title, description, status, and due date of one of
  their non-deleted tasks. When the same task is saved from two places, the last save wins.
- **FR-012**: An unfinished task whose due date is earlier than the person's current local date
  MUST be shown as overdue.

**Delete and restore**

- **FR-013**: People MUST be able to delete one of their tasks after confirming. A deleted task
  MUST no longer appear in the task list, search results, or counts.
- **FR-014**: People MUST be able to list their deleted tasks and restore any of them; a restored
  task keeps all the values it had before deletion. The deleted-tasks list MUST offer the same
  search, filters, sort options, and pagination as the task list (FR-016 to FR-019, FR-023), plus
  sorting by deletion date, which is its default order (most recently deleted first).
- **FR-015**: Deleted tasks MUST NOT be editable until restored: screens offer no editing options
  for them, and any edit attempt is refused with an outcome distinct from "task not found" that
  tells the owner to restore the task first.

**Finding tasks**

- **FR-016**: The task list MUST support text search over title and description (case-insensitive,
  "contains"), filtering by one status, and sorting by due date, creation date, or title in
  ascending or descending order.
- **FR-017**: Default order: due date ascending with tasks without a due date last, then most
  recently created first.
- **FR-018**: The task list and the deleted-tasks list MUST be paginated, with a default page size
  of 20 and a maximum of 100, and MUST report the total number of matching tasks.
- **FR-019**: The search, filters, sort, and page in use MUST be reflected in the screen address so
  that reloading keeps them.
- **FR-023**: The task list MUST support filtering by a due-date range (start and end dates, both
  required and inclusive). The range MUST NOT span more than the configured maximum, 90 days by
  default; a longer or inverted range MUST be rejected with a clear message. The maximum MUST be
  changeable by configuration, without code changes, and screens MUST use the configured value.

**User experience**

- **FR-020**: Task screens MUST be usable from 360 px wide to desktop, operable by keyboard alone,
  with labelled fields and error messages associated with their fields.
- **FR-021**: While a save, delete, or restore is in progress, the triggering action MUST be
  disabled so it cannot be submitted twice.
- **FR-022**: Empty states MUST distinguish "you have no tasks yet" (with a way to create one) from
  "no tasks match" (with a way to clear the search and filters).

### Key Entities

- **Task**: Something a person wants to get done. Attributes: unique identifier, owner (the
  person), title, optional description, status (To do / In progress / Done), optional due date,
  created-at, updated-at, and whether and when it was deleted.
- **Person (user account)**: Existing from feature 001; owns zero or more tasks. Tasks refer to the
  person only through their unique identifier.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A signed-in person can create a task and see it in their list within 30 seconds.
- **SC-002**: In 100% of automated checks, a person cannot read, change, delete, or restore
  another person's task, and the outcome is indistinguishable from a task that does not exist.
- **SC-003**: 100% of deleted tasks can be restored with all values unchanged.
- **SC-004**: With 500 tasks for one person, search, filter, sort, and page changes show results
  without a noticeable wait (under 1 second on a local installation).
- **SC-005**: Every validation rule (title, description, status, due date, list parameters) is
  covered by automated checks for both valid and invalid values.
- **SC-006**: All task screens work without horizontal scrolling at 360 px width, are completable
  with keyboard only, and produce no errors or warnings in the browser console during the flows
  described in the user stories.

## Assumptions

- Builds on feature 001: sign-in, sessions, and "authentication required by default" are reused.
  Tasks are linked to the existing account through its identifier only.
- Out of scope: sharing or assigning tasks, comments, attachments, tags or categories, priorities,
  subtasks, reminders or notifications, recurring tasks, bulk operations, permanent deletion
  (purging) of deleted tasks, and an activity history.
- Deleted tasks stay recoverable indefinitely; an automatic purge is out of scope.
- If an account is ever removed, its tasks are removed with it (no account deletion exists today).
- "Last save wins" for concurrent edits is an accepted trade-off; there is no conflict detection.
- Dates are stored without time zone; timestamps are recorded in UTC and shown in local time.
- Text search is a simple "contains" match; no ranking, stemming, or accent folding.
- The maximum span of the due-date range filter is a deployment setting changed by whoever runs
  the application, not by end users in the screens; changing it needs no new release.
- Demo data: in local development only, the demo account from feature 001 receives six
  sample tasks covering the three statuses, at least one overdue task, and at least one task
  without a due date. They are created only when the demo account has no tasks at all (including
  deleted ones) and are never changed afterwards, so restarts do not duplicate them or undo what
  a presenter did.
