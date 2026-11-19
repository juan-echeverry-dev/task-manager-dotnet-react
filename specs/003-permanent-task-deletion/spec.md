# Feature Specification: Permanent Task Deletion

**Feature Branch**: `003-permanent-task-deletion`

**Created**: 2026-11-19

**Status**: Draft

**Input**: User description: "Permanent task deletion. Replace the recoverable ("soft") deletion
defined in specs/002-task-management (User Story 3, FR-002, FR-010, the deleted-tasks list and
restore) with permanent deletion: when the owner deletes one of their tasks and confirms, the task
is removed for good and cannot be viewed, listed, edited, or restored afterwards. Remove the
deleted-tasks list, the read-only view of deleted tasks, and the restore action from the product.
Deleting still requires confirmation, cancelling changes nothing, and a person can only delete
their own tasks (others' tasks behave as not found). Opening, editing, or deleting a task that was
already deleted (for example from another tab) reports that the task no longer exists. Existing
tasks currently in the deleted state are removed permanently when this change is rolled out. The
demo seeding rule (seed only when the demo account has no tasks) keeps working. This spec amends
spec 002; record the decision in an ADR. Out of scope: the kanban board redesign (spec 004)."

## Relationship to Spec 002

This feature **amends** `specs/002-task-management/spec.md`. Where the two disagree, this spec
wins. Specifically:

| Spec 002 item | Change |
|---|---|
| Clarification "How does deletion work?" | Replaced: deleting removes the task permanently. |
| Clarifications on deleted tasks (read-only view, deleted-tasks list tools) | Withdrawn. |
| User Story 3 "Delete and restore a task" | Replaced by User Story 1 below. |
| FR-002 | "delete, or restore" becomes "or delete". |
| FR-010 | Deleted tasks can no longer be viewed. |
| FR-011 | "non-deleted" qualifier no longer needed. |
| FR-013 | Replaced by FR-001 to FR-004 below. |
| FR-014, FR-015 | Withdrawn. |
| FR-018 | Applies to the task list only. |
| FR-021 | "save, delete, or restore" becomes "save or delete". |
| SC-002 | "or restore" removed. |
| SC-003 | Withdrawn. |
| Key Entity "Task" | No longer records whether and when it was deleted. |
| Assumptions on recoverability and demo data | Replaced by the Assumptions below. |

All other parts of spec 002 (ownership, validation, overdue, finding tasks, user experience) stay
as they are.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Delete a task permanently (Priority: P1)

A person deletes a task they no longer need. After confirming, the task is gone for good: it is no
longer in their list, cannot be opened, and cannot be brought back.

**Why this priority**: It is the only behavior this feature delivers; it keeps the product simple
(one task list, no second "deleted" area) and makes "delete" mean what people expect.

**Independent Test**: Create a task, delete it and confirm, then verify it no longer appears in the
list, search results, or counts, and that opening its former address reports that the task does not
exist.

**Acceptance Scenarios**:

1. **Given** a task of mine, **When** I choose "Delete", **Then** I am asked to confirm, and the
   confirmation states that the task will be deleted permanently and cannot be recovered.
2. **Given** the delete confirmation, **When** I confirm, **Then** the task disappears from my task
   list, search results, and counts, and I see a message that the task was deleted.
3. **Given** the delete confirmation, **When** I cancel, **Then** nothing changes.
4. **Given** a task I deleted, **When** I open its former address, try to edit it, or try to delete
   it again (for example from a tab opened before the deletion), **Then** I get the same "task not
   found" outcome as for a task that never existed, and nothing changes.
5. **Given** a task that belongs to someone else, **When** I try to delete it by any means, **Then**
   I get the same "task not found" outcome and the task is not affected.

---

### User Story 2 - No trace of the former deleted-tasks area (Priority: P2)

The product no longer offers anything related to recovering tasks: there is no deleted-tasks list,
no "Restore" action, and no read-only view of a deleted task.

**Why this priority**: Leftover screens or links would promise a recovery that no longer exists.

**Independent Test**: Browse every task screen and verify there is no link, button, or address that
leads to deleted tasks or restoring them; visiting the former deleted-tasks address shows the
"not found" screen.

**Acceptance Scenarios**:

1. **Given** the task list, **When** I look for a way to see deleted tasks, **Then** there is none.
2. **Given** the former deleted-tasks address, **When** I open it, **Then** I see a "not found"
   screen with a way back to my tasks, and no deleted tasks are shown.
3. **Given** any task screen, **When** I look for a "Restore" action, **Then** there is none.
4. **Given** a client that still calls the former "list deleted tasks" or "restore task" operations,
   **When** it does, **Then** the operation is not available (the same outcome as any unknown
   operation).

---

### User Story 3 - Existing deleted tasks are cleaned up (Priority: P3)

Tasks that were already in the deleted state before this change are removed permanently when the
change is rolled out, so no unreachable data is left behind.

**Why this priority**: A one-time consequence of the change; it matters for data hygiene but
affects no daily flow.

**Independent Test**: Before rollout, have one active and one deleted task; after rollout, verify the
active task is unchanged and the deleted one no longer exists anywhere.

**Acceptance Scenarios**:

1. **Given** tasks in the deleted state before rollout, **When** the change is rolled out, **Then**
   those tasks no longer exist.
2. **Given** active tasks before rollout, **When** the change is rolled out, **Then** they keep all
   their values (title, description, status, due date, created and updated times).

---

### Edge Cases

- Two tabs show the same task; it is deleted in one. Any later open, edit, or delete from the other
  tab reports "task not found" and changes nothing.
- Two delete requests for the same task arrive almost at once: the first deletes it, the second
  reports "task not found"; neither produces an error page.
- Deleting the last task on a page of the list leaves that page empty; the existing "page beyond
  the last page" behavior of spec 002 applies.
- Deleting the person's last task shows the "you have no tasks yet" empty state (spec 002 FR-022).
- The service is unreachable while deleting: a non-technical message is shown and the task is kept,
  so the person can retry.
- A bookmark or shared address that pointed to the deleted-tasks list shows a "not found" screen.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: People MUST be able to delete one of their own tasks after confirming. The
  confirmation MUST state that the deletion is permanent.
- **FR-002**: A deleted task MUST be removed permanently: it MUST NOT appear in the task list,
  search results, or counts, and MUST NOT be viewable, editable, or recoverable afterwards by any
  means offered by the product.
- **FR-003**: Opening, editing, or deleting a task that no longer exists, or that belongs to someone
  else, MUST produce the same "task not found" outcome, and nothing changes.
- **FR-004**: Cancelling the confirmation MUST leave the task unchanged.
- **FR-005**: The product MUST NOT offer a deleted-tasks list, a "Restore" action, or a view of
  deleted tasks; the corresponding screens and operations are removed.
- **FR-006**: Tasks in the deleted state at the time of rollout MUST be removed permanently; all
  other tasks MUST keep their values.
- **FR-007**: While a delete is in progress, the delete action MUST be disabled so it cannot be
  submitted twice.
- **FR-008**: In local development, the demo account's sample tasks MUST still be created only when
  the demo account has no tasks, and MUST NOT be duplicated on restart.
- **FR-009**: The decision to replace recoverable deletion with permanent deletion MUST be recorded
  as an architecture decision record that references spec 002.

### Key Entities

- **Task**: As in spec 002, without the record of whether and when it was deleted: unique
  identifier, owner, title, optional description, status, optional due date, created-at,
  updated-at. A task either exists or does not.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In 100% of automated checks, a deleted task cannot be listed, found by search,
  counted, opened, edited, deleted again, or restored.
- **SC-002**: In 100% of automated checks, deleting another person's task, or a task that no longer
  exists, is indistinguishable from deleting a task that never existed, and changes nothing.
- **SC-003**: No screen, link, or available operation refers to deleted tasks or restoring them
  (verified by automated checks of the screens and of the available operations).
- **SC-004**: After rollout, the number of active tasks and their values are unchanged, and the
  number of tasks in the deleted state is zero.
- **SC-005**: A person can delete a task in two actions (choose "Delete", confirm) and see it gone
  from the list within 2 seconds on a local installation.

## Assumptions

- The repository owner accepts that deletion can no longer be undone; the confirmation is the only
  safeguard. An "undo" period or recycle bin is out of scope.
- Tasks currently in the deleted state have no value worth keeping; no export is made before they
  are removed.
- Demo data: because deleted tasks no longer count, a presenter who deletes every demo task will
  get the six sample tasks again after the next restart in local development. This is accepted.
- The "task not found" outcome for a task that no longer exists replaces spec 002's distinct
  "restore it first" outcome for edits of deleted tasks.
- The kanban board redesign (spec 004) is out of scope; this feature keeps the current task list
  screen, minus the deleted-tasks link.
