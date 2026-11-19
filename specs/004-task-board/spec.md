# Feature Specification: Task Board

**Feature Branch**: `004-task-board`

**Created**: 2026-11-19

**Status**: Draft

**Input**: User description: "Task board (kanban). Redesign the task list screen as a kanban board
with one column per task status: "To do", "In progress" and "Done", in that order. Each column's
color follows the colors already used for that status in the product (To do: neutral slate; In
progress: amber; Done: emerald); the overdue marker stays red. Each card shows basic information:
title, a short excerpt of the description (at most two lines), the due date, and the overdue marker
when it applies. Each column header shows its name and how many tasks it holds. Each column loads its
first tasks and offers "Load more" to bring the next ones (no page numbers). Opening a card shows the
task's full details in a side panel over the board, the board remaining visible beside it; on narrow
screens the panel takes the whole screen. The panel has its own address (the task's address), so
reloading or sharing it opens the board with the panel open; closing the panel (close button, Escape
key, or browser Back) returns to the board. Creating a task happens in the same side panel: from a
"New task" button (status To do by default) or from a "+" in a column header (status preset to that
column). Editing a task also happens in the side panel. Deleting a task stays possible from the board
(a per-card action) and from the panel, always with the permanent-deletion confirmation of spec 003.
The filters sit above the board in a single horizontal bar with a professional look: search,
due-date range, sort field and order, and "Clear"; the bar wraps on narrow screens. The status filter
is removed because columns are the statuses. Search, due-date range and sort apply to every column
and are kept in the address. Empty states: a column with no tasks shows a short empty message; with
no tasks at all, the board invites creating the first one; with filters matching nothing, it offers
to clear them. Out of scope: drag and drop between columns, changing status other than through
editing, new task fields, and any change to the API. This spec amends spec 002 (task list
presentation, FR-018 pagination and FR-019 address state) and builds on spec 003 (permanent
deletion)."

## Relationship to Specs 002 and 003

This feature changes how tasks are **presented and reached**; task data, validation, ownership,
overdue rules, search semantics, sort orders and deletion are unchanged. Where this spec and
spec 002 disagree, this spec wins:

| Spec 002 item | Change |
|---|---|
| Task list screen (US1, US4 presentation) | Replaced by the task board (User Story 1). |
| FR-016 status filter | Replaced by the board's columns; the screen no longer offers a status filter. |
| FR-018 pagination (pages, page numbers) | On the board, each column loads tasks in batches with "Load more" (FR-006). The default batch size stays 20. |
| FR-019 address state | Search, due-date range and sort stay in the address; status and page no longer do. The open task, the create form and the edit form also have their own addresses (FR-011). |
| FR-022 empty states | Refined by FR-015 (per column, whole board, no match). |
| Separate detail, create and edit screens | Shown in a side panel over the board (User Stories 2 and 3). |

Spec 003 (permanent deletion, its confirmation and the "task no longer exists" outcome) applies as is.

## Clarifications

### Session 2026-11-19

- Q: How is a task opened from the board? → A: In a side panel over the board, with its own address;
  a full-page detail and a centered pop-up were considered and rejected.
- Q: Where are tasks created and edited? → A: In the same side panel.
- Q: Can cards be dragged between columns? → A: Not in this feature; status changes through editing.
- Q: How do columns page? → A: Each column loads its own batches with "Load more".

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See my tasks as a board (Priority: P1) 🎯 MVP

A signed-in person opens their tasks and sees a board with three columns, "To do", "In progress"
and "Done", each holding the tasks in that status as cards, with the column's count, and can load
more tasks in a column.

**Why this priority**: The board is the new way to see work at a glance; every other story acts on it.

**Independent Test**: With tasks in every status, open the tasks screen and verify each task appears
as a card in the column of its status, with the right counts, colors, card contents, and "Load more"
when a column has more tasks than shown.

**Acceptance Scenarios**:

1. **Given** tasks in every status, **When** I open my tasks, **Then** I see three columns in the order
   "To do", "In progress", "Done", each showing only the tasks in that status.
2. **Given** a column, **When** it is shown, **Then** its header shows the status name, the number of
   matching tasks in that status, and the status color (To do: neutral slate, In progress: amber,
   Done: emerald), matching the status colors used elsewhere in the product.
3. **Given** a task, **When** its card is shown, **Then** it shows the title, at most two lines of the
   description (when there is one), the due date or "No due date", and the red "Overdue" marker when
   the task is overdue (spec 002 FR-012).
4. **Given** a column with 25 matching tasks, **When** the board opens, **Then** the column shows the
   first 20 and a "Load more" action; **When** I choose it, **Then** the remaining 5 are added below
   the first 20 and "Load more" disappears.
5. **Given** a column with no tasks while other columns have some, **When** the board is shown,
   **Then** that column shows a short empty message ("No tasks").
6. **Given** I have no tasks at all, **When** I open my tasks, **Then** the board invites me to create
   my first task.
7. **Given** the cards in a column, **When** no sort is chosen, **Then** they follow the default order of
   spec 002 (due date ascending, tasks without due date last, then newest created first).

---

### User Story 2 - Open a task in a side panel (Priority: P1)

A person opens a card and sees the task's full details in a side panel, with the board still visible
beside it; they can edit the task in the panel and close it to return to the board.

**Why this priority**: Viewing and editing tasks without losing the board is the core benefit of the
redesign.

**Independent Test**: Open a card, verify the panel shows every detail and the board stays visible;
edit the task in the panel and verify the card updates; close the panel with the close button, Escape
and Back; reload the panel's address and verify the panel opens again.

**Acceptance Scenarios**:

1. **Given** the board, **When** I open a card, **Then** a side panel shows the task's title,
   description, status, due date, overdue marker, created and last-updated times, with "Edit" and
   "Delete" actions, and the board stays visible beside it on wide screens.
2. **Given** a narrow screen (for example 360 px wide), **When** I open a card, **Then** the panel takes
   the whole screen and still offers a way to close it.
3. **Given** an open panel, **When** I close it with the close button, the Escape key, or the browser's
   Back button, **Then** the panel closes, the board keeps its search, filters and sort, and keyboard
   focus returns to the card I opened.
4. **Given** the address of an open panel, **When** I reload it or open it in another tab, **Then** the
   board is shown with that task's panel open.
5. **Given** an open panel, **When** I choose "Edit", change values and save, **Then** the panel shows
   the updated task with a confirmation, and the card shows the new values in the column of its
   (possibly new) status, with the column counts updated.
6. **Given** the edit form in the panel, **When** values are invalid, **Then** each invalid field shows
   its message (spec 002 rules) and nothing is saved; **When** I cancel, **Then** the panel returns to
   the task's details unchanged.
7. **Given** the address of a task that does not exist or belongs to someone else, **When** I open it,
   **Then** the panel shows "Task not found" with a way back to the board.

---

### User Story 3 - Create a task from the board (Priority: P2)

A person creates a task in the side panel, either from a "New task" button or from the "+" in a
column header, which presets that column's status.

**Why this priority**: Creation is essential but already exists; this story moves it into the board.

**Independent Test**: Create a task from "New task" and from each column's "+"; verify the preset
status, that the new card appears in the right column, and that the panel then shows the new task.

**Acceptance Scenarios**:

1. **Given** the board, **When** I choose "New task", **Then** the side panel shows the create form with
   status "To do" selected.
2. **Given** the board, **When** I choose "+" in the "In progress" column header, **Then** the create
   form opens with status "In progress" selected; I can still change it.
3. **Given** the create form, **When** I save valid values, **Then** the task is created, the panel shows
   its details with a confirmation, and its card appears in the column of its status with the count
   updated.
4. **Given** the create form, **When** values are invalid, **Then** each invalid field shows its message
   and nothing is saved; **When** I cancel or close the panel, **Then** nothing is created.
5. **Given** the address of the create form (including the preset status), **When** I reload it,
   **Then** the board opens with the create form in the panel and the same preset status.

---

### User Story 4 - Delete a task from the board (Priority: P2)

A person deletes a task from its card on the board or from the open panel, after the permanent
deletion confirmation.

**Why this priority**: Deletion must stay available on the redesigned screen (user requirement), but
it already exists.

**Independent Test**: Delete a task from a card's action and another from the panel; verify the
confirmation, that the card disappears and the count decreases, and that cancelling changes nothing.

**Acceptance Scenarios**:

1. **Given** a card, **When** I choose its "Delete" action, **Then** the permanent-deletion confirmation
   of spec 003 is shown without opening the panel.
2. **Given** the confirmation, **When** I confirm, **Then** the card disappears, its column count
   decreases by one, and I see "Task deleted."; **When** I cancel, **Then** nothing changes.
3. **Given** an open panel, **When** I delete the task and confirm, **Then** the panel closes and the
   board shows "Task deleted." without the card.
4. **Given** a task already deleted elsewhere, **When** I try to delete it, **Then** I see "This task no
   longer exists." and the board refreshes (spec 003).

---

### User Story 5 - Find tasks with the filter bar (Priority: P3)

A person narrows and orders the board with a single horizontal bar above it: search, due-date range,
sort field and order, and "Clear search and filters".

**Why this priority**: Useful as the board grows; the board delivers value without it.

**Independent Test**: Apply search, a due-date range and a sort; verify every column and its count
follow them, that the address keeps them across reloads, and that "Clear search and filters" resets them.

**Acceptance Scenarios**:

1. **Given** the board on a wide screen, **When** it is shown, **Then** the search, the due-date range
   (from, to), the sort field, the order and "Clear search and filters" sit in one horizontal bar above the board; on
   narrow screens the bar wraps onto several lines without horizontal scrolling.
2. **Given** a search for "demo", **When** it is applied, **Then** every column shows only its matching
   tasks and its count of matches (spec 002 search rules).
3. **Given** a due-date range, **When** it is applied, **Then** every column shows only tasks due within
   the range; invalid or too-long ranges show the spec 002 messages and are not applied.
4. **Given** a sort field and order, **When** chosen, **Then** every column follows that order.
5. **Given** search, range and sort values, **When** I reload or share the address, **Then** the same
   values are applied; each column starts again from its first batch.
6. **Given** filters that match nothing, **When** the board is shown, **Then** it says no tasks match and
   offers to clear the search and filters.
7. **Given** the filter bar, **When** I look for a status filter, **Then** there is none (columns are the
   statuses).

---

### Edge Cases

- A task changes status through editing: its card leaves the old column and appears in the new one,
  and both counts update.
- A task opened from an address while a filter hides it: the panel still opens; its card is simply not
  on the board.
- "Load more" in one column does not reload or reset the other columns.
- Changing the search, range or sort resets every column to its first batch.
- A new or edited task that does not match the current filters is saved; its panel shows it, but no
  card appears until the filters are cleared.
- Closing the panel with unsaved changes in the create or edit form discards them (as leaving a page
  did before); this is accepted.
- The service is unreachable while loading a column: that column shows the non-technical service
  message; the other columns are unaffected.
- The service is unreachable while saving in the panel: the form keeps its values and shows the
  service message (spec 002).
- Opening the former list addresses with `status` or `page` values: those values are ignored and the
  board shows all columns from their first batch.
- Very long titles and descriptions wrap or are truncated on the card without breaking the layout; the
  panel shows them in full.

## Requirements *(mandatory)*

### Functional Requirements

**Board**

- **FR-001**: The tasks screen MUST show a board with exactly three columns in this order: "To do",
  "In progress", "Done". Each column holds only the person's tasks in that status.
- **FR-002**: Each column header MUST show the status name, the number of the person's tasks in that
  status that match the current search and range, and the status color used elsewhere in the product
  (To do: neutral slate, In progress: amber, Done: emerald). Colors MUST NOT be the only way to tell
  columns apart (the name is always shown).
- **FR-003**: Each card MUST show the title, up to two lines of the description when present, the due
  date or "No due date", and the red "Overdue" marker when spec 002 FR-012 applies.
- **FR-004**: Each card MUST offer a way to open the task and a "Delete" action.
- **FR-005**: Cards in every column MUST follow the chosen sort, defaulting to spec 002 FR-017.
- **FR-006**: Each column MUST first show up to 20 tasks and offer "Load more" while more matching
  tasks exist; "Load more" appends the next up-to-20 tasks of that column only.

**Side panel**

- **FR-007**: Opening a card MUST show the task's details in a side panel over the board, with "Edit"
  and "Delete" actions. On wide screens the board stays visible beside the panel; on narrow screens the
  panel takes the whole screen.
- **FR-008**: Creating a task MUST happen in the side panel, opened by a "New task" button (status "To
  do") or by the "+" in a column header (status preset to that column, still changeable).
- **FR-009**: Editing a task MUST happen in the side panel, with the validation and messages of spec 002;
  cancelling returns to the details unchanged.
- **FR-010**: The panel MUST close with a visible close button, the Escape key, and the browser's Back
  button, returning to the board with its search, range and sort unchanged and focus on the element
  that opened the panel when it still exists. Closing a panel opened from the board does not leave a
  history entry that reopens it on Back; closing a panel opened from its address replaces that address.
- **FR-011**: The open task, the create form (with its preset status) and the edit form MUST each have
  their own address; reloading or sharing it MUST open the board with the same panel content.
- **FR-012**: After a create or edit is saved, the board MUST reflect the change (card in the column of
  its status, counts updated) and the panel MUST show the saved task with a confirmation.

**Deletion**

- **FR-013**: Deleting from a card or from the panel MUST use the permanent-deletion confirmation and
  outcomes of spec 003. After a confirmed delete, the card disappears, its column count decreases, and
  the board shows "Task deleted."; a delete from the panel also closes it.

**Filter bar**

- **FR-014**: Above the board, a single horizontal bar MUST offer search, due-date range (from, to),
  sort field (due date, created, title), order (ascending, descending) and "Clear search and filters", with labelled
  controls; it wraps on narrow screens. There is no status filter. Search, range and sort follow the
  rules of spec 002 (FR-016, FR-017, FR-023) and apply to every column.
- **FR-015**: Empty states MUST distinguish: a column with no tasks ("No tasks"); no tasks at all (with
  a way to create the first one); and no tasks matching the search and range (with a way to clear them).
- **FR-016**: The search, range and sort in use MUST be kept in the address; how many tasks each column
  has loaded is not.

**User experience**

- **FR-017**: The board and panel MUST be usable from 360 px wide to desktop without horizontal page
  scrolling, operable by keyboard alone: columns are announced as named lists, the panel is announced
  as a dialog with the task title (or "New task"/"Edit task"), focus moves into the panel when it opens
  and stays inside it until it closes.
- **FR-018**: While a column loads, it shows a loading indicator; while a save or delete is in progress,
  the triggering action is disabled (spec 002 FR-021).

### Key Entities

- **Task**: Unchanged (spec 002 as amended by spec 003). Its status decides its column.
- **Board view**: Not stored. The person's current search, due-date range and sort (in the address),
  plus, per column, how many batches are loaded (not in the address).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A person can see how many tasks they have in each status within 2 seconds of opening the
  tasks screen on a local installation, without any extra action.
- **SC-002**: A person can open a task, read its details and return to the board with the same filters
  in 3 actions or fewer (open, read, close).
- **SC-003**: A person can create a task in a given status from the board in under 30 seconds, without
  leaving the board.
- **SC-004**: With 500 tasks for one person, changing the search, range or sort updates all three
  columns without a noticeable wait (under 1 second on a local installation).
- **SC-005**: All board and panel flows work at 360 px width without horizontal page scrolling, are
  completable with keyboard only, and produce no errors or warnings in the browser console.
- **SC-006**: In 100% of automated checks, each task appears in exactly one column, the one of its
  status, and each column count equals the number of matching tasks in that status.

## Assumptions

- No API change is needed: each column is the existing task list filtered by one status, with the
  existing search, range, sort and page size; "Load more" requests the next page of that column.
- On narrow screens the columns stack vertically (To do, then In progress, then Done), each at full
  width; on wide screens they sit side by side.
- The panel width on wide screens leaves the board visible (roughly the right third to half of the
  screen); exact sizes are a design detail.
- Cards show the due date in the person's local format, as elsewhere in the product.
- Unsaved changes in the panel are not protected by a "discard changes?" prompt.
- The "Load more" state is not kept across reloads; a reload starts each column from its first batch.
- Former list addresses with `status` or `page` values keep working: those values are ignored.
- Out of scope: drag and drop, changing status from the card, bulk actions, new task fields, column
  customization (hiding, renaming, reordering), and any change to the API.
