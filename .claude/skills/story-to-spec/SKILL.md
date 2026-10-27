---
name: story-to-spec
description: Turns a GitHub issue (a user story) into a proposed requirement in specs/*.spec.md, with an EARS statement, testable acceptance criteria and the open questions the story leaves. Use it when the task mentions a GitHub issue or user story (#12, "story 12", an issues URL) that has to become a spec. It does not implement anything.
---

# From a GitHub issue to a spec

A user story says what someone needs. A requirement is the contract the code is checked against. Your job is to
write that contract **without deciding anything the story does not decide**. A person approves it afterwards.

## 1. Read the story

```bash
gh issue view <number> --json number,title,body,labels,url,comments
```

Stories created with the "User story" template have the sections *As a*, *I want*, *So that*,
*Acceptance criteria* and *Notes*. Read the comments too: decisions are often made there.

## 2. Read what already exists

Use the `spectrace` MCP tools (or `dotnet spectrace report` / `dotnet spectrace context <ID>`):

- `list_requirements`: is this already specified, in full or in part? If so, propose a change to that
  requirement instead of a new one, and say so.
- `get_requirement` on the related requirements: the new one must be consistent with them
  (opening hours, capacity, double booking, who may cancel...).
- `get_code` with `type` for the domain types it touches (`Booking`, `Room`, `BookingService`...): note what the
  domain does not support yet (for example, rooms have no equipment), but do not design the solution.

## 3. Write the requirement

- **Where**: the spec of its domain (`rooms.spec.md` for rooms, `bookings.spec.md` for bookings). A new spec file
  only for a new domain.
- **ID**: let `propose_requirement` pick the next one of that spec (RM-004, BK-008...).
- **Statement**: one sentence in EARS (`When ..., the system shall ...`, `If ..., then the system shall ...`,
  `While ..., when ..., the system shall ...`). It describes behaviour, not endpoints or classes.
- **Acceptance criteria**: `Given / when / then`, one observable behaviour each, with concrete values
  (10:00, 4 attendees, "atlas"). Each one must be testable by a single test. Cover:
  - what the story asks for, including its own criteria rewritten in this form;
  - the edge cases the existing rules make obvious (outside opening hours, a taken room, another user...).
- **Do not invent rules.** If the story does not say what happens in a case that matters, it is an open question,
  not a criterion.

Create it with `propose_requirement` (status `proposed`). Then add the source and the open questions as a
blockquote right under the `Status:` line. Blockquotes are notes: they are not part of the requirement text and
do not trigger spec-change warnings.

```markdown
## BK-008: Move a booking
Status: proposed

> Source: https://github.com/amondejar/room-booking/issues/12
> Open questions:
> - Can a booking be moved to another room, or only to another time?
> - Is moving allowed after the booking has started?

When the organizer moves an upcoming booking to another time, the system shall ...

- AC-1: Given ..., when ..., then ...
```

If the story is too vague to write even one testable criterion, do not create a requirement. Write the questions
that would make it specifiable and go to step 5.

## 4. Check

```bash
dotnet build
dotnet spectrace report
```

The build must not report `ST0007` (duplicate ID) or `ST0008` (spec format). A `proposed` requirement is not
enforced, so there must be no new `ST0003`/`ST0005`.

## 5. Hand it over to a person

- Work on a branch `spec/gh-<number>-<short-name>` and commit only the spec change.
- Ask the person before writing anything on GitHub. With their go-ahead, open a pull request titled
  `Spec for #<number>: <title>` with `Refs #<number>` in the body (not `Closes`: the issue closes when the
  implementation is merged), and list the open questions in it.
- Never change the status to `approved` yourself, and never implement the requirement in the same task.

## In your summary

List: the issue, the requirement ID and spec file, the criteria you wrote, which of them came from the story and
which from existing rules, and the open questions.
